using P_Bit_Ruisseau;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace P_Bit_Ruisseau
{
    // Implémente l'interface IProtocol sans la modifier.
    public class P2PProtocol : IProtocol
    {
        // Le catalogue local est passé par référence dans le constructeur.
        private readonly List<Song> _localCatalog;
        private readonly string _localMediathequeName = Environment.MachineName;

        public P2PProtocol(List<Song> localCatalog)
        {
            _localCatalog = localCatalog;
            // L'initialisation réseau réelle (UDP/TCP listeners) irait ici.
        }

        private string SerializeMessage(Message message)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(message, options);
        }

        // --- Fonctions IProtocol (Stubs de simulation) ---

        public string[] GetOnlineMediatheque()
        {
            // Simulation : retourne une liste de médiathèques "trouvées" sur le réseau.
            SayOnline(); // Annonce que nous sommes en ligne
            return new[] { "Mediatheque", "Mediatheque_Mathieu", "192.168.1.10" };
        }

        public void SayOnline()
        {
            var message = new Message
            {
                Action = "OnlineAnnounce",
                Sender = _localMediathequeName,
            };
            Console.WriteLine($"Simulating: Sent 'OnlineAnnounce': {SerializeMessage(message)}");
        }

        public List<ISong> AskCatalog(string name)
        {
            Console.WriteLine($"Simulating: Asking catalog from {name}");
            // Simule le catalogue reçu d'un pair distant
            return new List<ISong>
            {
                new Song() { Title = "Distant Track 1", Artist = name, Size = 5000000},
                
            };
        }

        public void SendCatalog(string name)
        {
            Console.WriteLine($"Simulating: Sending catalog to {name}");
            var message = new Message
            {
                Action = "CatalogPublish",
                Sender = _localMediathequeName,
                Recipient = name,
                SongList = _localCatalog.Cast<ISong>().ToList()
            };
            Console.WriteLine($"Simulating: Sent 'CatalogPublish' to {name}.");
        }

        public void AskMedia(ISong song, string name, int startByte, int endByte)
        {
            Console.WriteLine($"Simulating: Asking for media fragment {startByte}-{endByte} for {song.Hash} from {name}");
            // Logique TCP pour envoyer MediaFragmentRequest
        }

        public void SendMedia(ISong song, string name, int startByte, int endByte)
        {
            Console.WriteLine($"Simulating: Sending media fragment {startByte}-{endByte} to {name}");
            // Logique TCP pour envoyer MediaFragmentPublish
        }
    }
}