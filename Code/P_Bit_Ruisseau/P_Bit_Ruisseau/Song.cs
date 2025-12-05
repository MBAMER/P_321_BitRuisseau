using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using TagLib;

namespace P_Bit_Ruisseau
{
    // Implémente l'interface ISong sans la modifier.
    public class Song : ISong
    {
        private readonly string _filePath;
        private readonly string _hash;

        /// <summary>
        /// Constructeur pour charger une chanson depuis un fichier local.
        /// </summary>
        public Song(string filePath)
        {
            _filePath = filePath;

            // Lecture des métadonnées avec TagLibSharp
            using (var file = TagLib.File.Create(filePath))
            {
                Title = file.Tag.Title ?? Path.GetFileNameWithoutExtension(filePath);
                Artist = file.Tag.FirstPerformer ?? "Artiste Inconnu";
                Year = (int)file.Tag.Year;
                Duration = file.Properties.Duration;

                // Utilisation de LINQ pour filtrer les artistes 'Featuring'
                Featuring = file.Tag.Performers
                                .Where(p => p != Artist)
                                .ToArray();
            }

            var fileInfo = new FileInfo(filePath);
            Size = (int)fileInfo.Length;
            Extension = fileInfo.Extension;

            // Calcul du Hash (via Helper.cs)
            _hash = Helper.HashFile(filePath);
        }

        /// <summary>
        /// Constructeur vide pour la désérialisation JSON des chansons distantes.
        /// </summary>
        [JsonConstructor]
        public Song()
        {
            _filePath = string.Empty;
            // Les autres propriétés seront définies par le désérialiseur JSON.
        }

        // Implémentation des propriétés de l'interface ISong
        public string Title { get; set; }
        public string Artist { get; set; }
        public int Year { get; set; }
        public TimeSpan Duration { get; set; }
        public int Size { get; set; }
        public string[] Featuring { get; set; }

        public string Hash => _hash ?? "N/A";
        public string Extension { get; private set; }

        [JsonIgnore]
        public string FilePath => _filePath;
    }
}