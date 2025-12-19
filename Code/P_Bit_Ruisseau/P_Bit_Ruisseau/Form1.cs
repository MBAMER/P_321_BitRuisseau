using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using P_Bit_Ruisseau;

namespace P_Bit_Ruisseau
{
    public partial class Form1 : Form
    {
        // Catalogue Local (votre propre musique) — exposé via l'interface pour que Title/Artist soient visibles
        private BindingList<ISong> _localMediatheque;
        // Interface pour la communication réseau
        private IProtocol _protocol;
        
        // Liste des noms de m�diath�ques en ligne d�couvertes (pour listBoxMediatheques)
        private BindingList<string> _onlineMediatheques;

        // Liste pour afficher le catalogue du pair s�lectionn� (pour dataGridViewRemote)
        private BindingList<ISong> _remoteCatalog;

        private readonly System.Windows.Forms.Timer _discoveryTimer;

        public Form1()
        {
            // 0. Initialisation des composants UI (essentiel)
            InitializeComponent();

            // 1. Initialisation des Modèles de Données
            _localMediatheque = new BindingList<ISong>();
            _onlineMediatheques = new BindingList<string>();
            _remoteCatalog = new BindingList<ISong>();

            // 2. Initialisation du Protocole (Utilisation de MqttProtocol)
            _protocol = new MqttProtocol(
                // fournir directement la collection d'ISong pour que DataGridView voie les propriétés de l'interface
                localCatalogProvider: () => _localMediatheque,
                // Logger qui utilise Invoke pour garantir la sécurité thread-safe de l'UI
                logger: message => Invoke((System.Windows.Forms.MethodInvoker)(() => Console.WriteLine($"[MQTT LOG] {message}")))
            );

            // S'abonner à l'événement de réception de média si le protocole le permet
            if (_protocol is MqttProtocol mqttProtocol)
            {
                mqttProtocol.MediaReceived += OnMediaReceived;
            }

            // 3. Liaison des sources de données aux contrôles UI
            // On crée nous-même les colonnes à partir des propriétés publiques de Song (fiable si Song n'expose pas de champs publics)
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = _localMediatheque;
            EnsureSongColumns();
            
            // Tente de lier la liste des pairs au ListBox (nomm� 'listBoxMediatheques')
            if (this.Controls.Find("listBoxMediatheques", true).FirstOrDefault() is ListBox peerList)
            {
                peerList.DataSource = _onlineMediatheques;
                // Ajouter le gestionnaire pour le double-clic
                peerList.DoubleClick += ListBoxMediatheques_DoubleClick;
            }

            // Tente de lier le catalogue distant au DataGridView (nomm� 'dataGridViewRemote')
            if (this.Controls.Find("dataGridViewRemote", true).FirstOrDefault() is DataGridView remoteGrid)
            {
                remoteGrid.DataSource = _remoteCatalog;
            }

            // 4. Initialisation du Timer de d�couverte de pairs
            _discoveryTimer = new System.Windows.Forms.Timer();
            _discoveryTimer.Interval = 5000; // 5 secondes
            _discoveryTimer.Tick += DiscoveryTimer_Tick;
            _discoveryTimer.Start();
        }

        /// <summary>
        /// G�re le clic sur le bouton "Choisir un dossier" et met � jour le catalogue local.
        /// </summary>
        private void ChoixDossier(object sender, EventArgs e)
        {
            Console.WriteLine("ChoixDossier invoked");
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choisissez un dossier contenant des fichiers musicaux";
                dialog.UseDescriptionForTitle = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolder = dialog.SelectedPath;
                    string[] audioExtensions = { ".mp3" };

                    // Utilisation de LINQ pour trouver, filtrer et cr�er les objets Song
                    var newSongs = Directory.EnumerateFiles(selectedFolder, "*.*", SearchOption.AllDirectories)
                        .Where(file => audioExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                        .Select(file =>
                        {
                            try { return new Song(file); }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erreur de traitement du fichier {file}: {ex.Message}");
                                return null;
                            }
                        })
                        .Where(song => song != null)
                        .ToList();

                    // Mise à jour du catalogue local
                    // BindingList se met à jour automatiquement : vider puis ajouter un par un
                    _localMediatheque.Clear();
                    foreach (var s in newSongs) _localMediatheque.Add((ISong)s);
                    EnsureSongColumns();
                    dataGridView1.Refresh();

                    MessageBox.Show($"{newSongs.Count} chansons chargées depuis : {selectedFolder}", "Catalogue local mis à jour");

                    // Annoncer la présence et le catalogue mis à jour sur le r�seau
                    _protocol.SayOnline();
                }
            }
        }

        /// <summary>
        /// G�re l'�v�nement du Timer pour d�couvrir les m�diath�ques en ligne.
        /// </summary>
        private async void DiscoveryTimer_Tick(object sender, EventArgs e)
        {
            _discoveryTimer.Stop();

            try
            {
                // Ex�cuter l'appel r�seau bloquant dans un thread s�par�
                string[] peers = await Task.Run(() => _protocol.GetOnlineMediatheque());

                // Mettre � jour la liste des pairs sur le thread UI
                _onlineMediatheques.Clear();
                foreach (var peer in peers.Where(p => p != Environment.MachineName)) // Exclure soi-m�me
                {
                    _onlineMediatheques.Add(peer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur de d�couverte des pairs: {ex.Message}");
            }
            finally
            {
                _discoveryTimer.Start();
            }
        }

        /// <summary>
        /// G�re le double-clic sur un pair dans la ListBox pour demander son catalogue.
        /// </summary>
        private async void ListBoxMediatheques_DoubleClick(object sender, EventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is string peerName)
            {
                try
                {
                    _remoteCatalog.Clear();

                    // Indication � l'utilisateur que l'op�ration est en cours
                    MessageBox.Show($"Demande de catalogue � {peerName}...", "Requ�te en cours");

                    // Ex�cuter l'appel r�seau bloquant (AskCatalog) dans un thread s�par�
                    List<ISong> catalog = await Task.Run(() => _protocol.AskCatalog(peerName));

                    if (catalog.Any())
                    {
                        foreach (var song in catalog)
                        {
                            _remoteCatalog.Add(song);
                        }
                        MessageBox.Show($"{catalog.Count} chansons re�ues de {peerName}.", "Catalogue Re�u");
                    }
                    else
                    {
                        MessageBox.Show($"Aucune chanson re�ue de {peerName} ou timeout.", "Catalogue Vide");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la demande de catalogue: {ex.Message}", "Erreur R�seau");
                }
            }
        }

        /// <summary>
        /// G�re la r�ception asynchrone d'un fragment de m�dia (thread-safe).
        /// </summary>
        private void OnMediaReceived(object sender, (string FileName, byte[] Data) e)
        {
            // S'assurer que le code est exécuté sur le thread de l'interface utilisateur (UI)
            if (InvokeRequired)
            {
                Invoke(new EventHandler<(string, byte[])>(OnMediaReceived), sender, e);
                return;
            }

            // Pour le débogage : afficher la réception
            Console.WriteLine($"Nouveau fragment de {e.Data.Length} octets reçu pour {e.FileName}.");
            // Sauvegarder le fichier re�u dans le dossier Musique/BitRuisseauDownloads
            try
            {
                var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "BitRuisseauDownloads");
                Directory.CreateDirectory(downloads);
                var filePath = Path.Combine(downloads, e.FileName);
                File.WriteAllBytes(filePath, e.Data);

                UpdateLog($"Fichier reçu et sauvegardé: {filePath}");

                // Tenter d'ajouter le fichier reçu au catalogue local (crée un Song à partir du fichier)
                try
                {
                    var song = new Song(filePath);
                    _localMediatheque.Add((ISong)song); // ajouter comme ISong pour que les colonnes Title/Artist soient lues

                    // Rafraichir la grille locale
                    dataGridView1.Refresh();

                    MessageBox.Show($"Fichier reçu: {song.Title}", "Téléchargement terminé");
                }
                catch (Exception ex)
                {
                    UpdateLog($"Erreur création Song depuis le fichier reçu: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                UpdateLog($"Erreur sauvegarde média: {ex.Message}");
            }
        }

        private void BtnSayOnline_Click(object? sender, EventArgs e)
        {
            try
            {
                _protocol.SayOnline();
                UpdateLog("Annonce 'online' envoyée.");
                UpdateStatus("Annonce envoyée");
            }
            catch (Exception ex)
            {
                UpdateLog($"Erreur SayOnline: {ex.Message}");
                UpdateStatus("Erreur SayOnline");
            }
        }

        private async void BtnSendCatalog_Click(object? sender, EventArgs e)
        {
            try
            {
                if (this.Controls.Find("listBoxMediatheques", true).FirstOrDefault() is ListBox lb && lb.SelectedItem is string peer)
                {
                    // Send catalog to selected peer
                    _protocol.SendCatalog(peer);
                    UpdateLog($"Catalogue envoyé à {peer}.");
                    UpdateStatus($"Catalogue envoyé à {peer}");
                }
                else
                {
                    MessageBox.Show("Sélectionnez une médiathèque distante dans la liste.", "Aucune sélection");
                }
            }
            catch (Exception ex)
            {
                UpdateLog($"Erreur envoi catalogue: {ex.Message}");
                UpdateStatus("Erreur envoi catalogue");
            }
        }

        private void BtnImport_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!(this.Controls.Find("listBoxMediatheques", true).FirstOrDefault() is ListBox lb) || !(lb.SelectedItem is string peer))
                {
                    MessageBox.Show("Sélectionnez d'abord la médiathèque source dans la liste.", "Aucune sélection");
                    return;
                }

                if (this.Controls.Find("dataGridViewRemote", true).FirstOrDefault() is DataGridView remoteGrid)
                {
                    if (remoteGrid.SelectedRows.Count == 0)
                    {
                        MessageBox.Show("Sélectionnez une ou plusieurs chansons dans le catalogue distant.", "Aucune sélection");
                        return;
                    }

                    foreach (DataGridViewRow row in remoteGrid.SelectedRows)
                    {
                        if (row.DataBoundItem is ISong song)
                        {
                            // Demander le média complet au pair
                            UpdateLog($"Demande média {song.Title} à {peer}...");
                            _protocol.AskMedia(song, peer, 0, song.Size);
                        }
                    }

                    UpdateStatus("Demandes d'import envoyées");
                }
            }
            catch (Exception ex)
            {
                UpdateLog($"Erreur import: {ex.Message}");
            }
        }

        private async void BtnDiscover_Click(object? sender, EventArgs e)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new EventHandler(BtnDiscover_Click), sender, e);
                    return;
                }

                UpdateStatus("Recherche des médiathèques...");
                if (this.Controls.Find("buttonDiscover", true).FirstOrDefault() is Button btn)
                {
                    btn.Enabled = false;
                }

                // Run discovery off UI thread
                string[] peers = await Task.Run(() => _protocol.GetOnlineMediatheque());

                // Update the binding list on UI thread
                _onlineMediatheques.Clear();
                foreach (var p in peers.Where(p => p != Environment.MachineName))
                {
                    _onlineMediatheques.Add(p);
                }

                UpdateLog($"Découverte terminée: {_onlineMediatheques.Count} médiathèques trouvées.");
                UpdateStatus("Découverte terminée");
                if (this.Controls.Find("buttonDiscover", true).FirstOrDefault() is Button btn2)
                {
                    btn2.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                UpdateLog($"Erreur découverte: {ex.Message}");
                UpdateStatus("Erreur découverte");
                if (this.Controls.Find("buttonDiscover", true).FirstOrDefault() is Button btn3)
                {
                    btn3.Enabled = true;
                }
            }
        }

        private void UpdateLog(string message)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string>(UpdateLog), message);
                    return;
                }

                Console.WriteLine(message);
                if (this.Controls.Find("textBoxLog", true).FirstOrDefault() is TextBox tb)
                {
                    tb.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                }
            }
            catch { }
        }

        private void UpdateStatus(string status)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string>(UpdateStatus), status);
                    return;
                }

                if (this.Controls.Find("statusLabel", true).FirstOrDefault() is Label lbl)
                {
                    lbl.Text = status;
                }
            }
            catch { }
        }

        /// <summary>
        /// Crée des colonnes dans dataGridView1 à partir des propriétés publiques et fields de Song.
        /// Vide d'abord toute colonne existante pour éviter colonnes "designer" sans DataPropertyName.
        /// </summary>
        private void EnsureSongColumns()
        {
            // Vider les colonnes existantes (designer parfois ajoute des colonnes vides)
            dataGridView1.Columns.Clear();
 
            // Lier aux propriétés de l'interface ISong pour s'assurer que Title/Artist sont visibles
            var type = typeof(ISong);
 
            // Priorité à des noms courants si présents (ordre utile pour visibilité)
            string[] preferred = { "Title", "Artist", "Album", "Duration", "Size", "FilePath", "Path" };
 
            var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                            .Where(p => p.GetMethod != null).ToDictionary(p => p.Name, p => (MemberInfo)p);
 
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                             .ToDictionary(f => f.Name, f => (MemberInfo)f);
 
            // Rassemble en respectant l'ordre privilégié
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 
            void AddMember(string name, MemberInfo member)
            {
                if (member == null || added.Contains(name)) return;
                var col = new DataGridViewTextBoxColumn
                {
                    DataPropertyName = name,
                    HeaderText = name,
                    Name = "col_" + name,
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                };
                dataGridView1.Columns.Add(col);
                added.Add(name);
            }
 
            // Ajoute en priorité les propriétés/fields préférés
            foreach (var n in preferred)
            {
                if (props.TryGetValue(n, out var pm)) AddMember(n, pm);
                else if (fields.TryGetValue(n, out var fm)) AddMember(n, fm);
            }
 
            // Ajoute ensuite toutes les autres propriétés publiques
            foreach (var kv in props)
            {
                AddMember(kv.Key, kv.Value);
            }
 
            // Puis les fields publics restants
            foreach (var kv in fields)
            {
                AddMember(kv.Key, kv.Value);
            }
 
            // Si aucune colonne créée (classe Song sans membres publics), ajouter une colonne FilePath de secours
            if (dataGridView1.Columns.Count == 0)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "FilePath",
                    HeaderText = "FilePath",
                    Name = "col_FilePath",
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                };
                dataGridView1.Columns.Add(col);
            }
        }

        // M�thode g�n�r�e par le designer mais non utilis�e pour l'instant
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    }
}