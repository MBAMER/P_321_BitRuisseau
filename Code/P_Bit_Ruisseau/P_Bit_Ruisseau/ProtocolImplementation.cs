using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using P_Bit_Ruisseau;

namespace P_Bit_Ruisseau
{
    // Musique sur dans fichier sur disque
    public class SongModel : ISong
    {
        public SongModel(FileInfo fileInfo)
        {
            Path = fileInfo.FullName;
            Size = fileInfo.Length > int.MaxValue ? int.MaxValue : (int)fileInfo.Length;
            Extension = fileInfo.Extension.TrimStart('.').ToLowerInvariant();
            Hash = Helper.HashFile(fileInfo.FullName);

            try 
            {
                using var tfile = TagLib.File.Create(Path);
                Title = string.IsNullOrWhiteSpace(tfile.Tag.Title) ? System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name) : tfile.Tag.Title;
                Artist = string.IsNullOrWhiteSpace(tfile.Tag.FirstPerformer) ? "Inconnu" : tfile.Tag.FirstPerformer;
                Year = (int)tfile.Tag.Year;
                Duration = tfile.Properties.Duration;
                Featuring = tfile.Tag.Performers.Skip(1).ToArray();

                if (Featuring.Length == 0)
                {
                    // Fallback: parse from Title or Artist
                    var featMarkers = new[] { " ft.", " feat.", " featuring" };
                    foreach (var marker in featMarkers)
                    {
                        if (Artist.Contains(marker, StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = System.Text.RegularExpressions.Regex.Split(Artist, marker, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (parts.Length > 1)
                            {
                                Artist = parts[0].Trim();
                                Featuring = parts[1].Split(new[] { ',', '&' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToArray();
                                break;
                            }
                        }
                        else if (Title.Contains(marker, StringComparison.OrdinalIgnoreCase))
                        {
                             var parts = System.Text.RegularExpressions.Regex.Split(Title, marker, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (parts.Length > 1)
                            {
                                Title = parts[0].Trim();
                                Featuring = parts[1].Split(new[] { ',', '&' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToArray();
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                Title = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name);
                Artist = "Inconnu";
                Year = fileInfo.CreationTime.Year;
                Duration = TimeSpan.Zero;
                Featuring = Array.Empty<string>();
            }
        }

        public string Path { get; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public int Year { get; set; }
        public TimeSpan Duration { get; set; }
        public int Size { get; set; }
        public string[] Featuring { get; set; }
        public string Hash { get; set; }
        public string Extension { get; set; }
    }



    // Donnée mémoire pour testing
    public class MockProtocol : IProtocol
    {
        private readonly Func<IEnumerable<ISong>> _catalogProvider;
        private readonly Action<string>? _logger;
        private readonly List<string> _peers = new() { "maison", "bureau", "studio" };

        public MockProtocol(Func<IEnumerable<ISong>> catalogProvider, Action<string>? logger = null)
        {
            _catalogProvider = catalogProvider;
            _logger = logger;
        }

        public string[] GetOnlineMediatheque()
        {
            _logger?.Invoke("Découverte des médiathèques en cours...");
            return _peers.ToArray();
        }

        public void SayOnline()
        {
            _logger?.Invoke("Annonce de présence envoyée.");
        }

        public List<ISong> AskCatalog(string name)
        {
            _logger?.Invoke($"Catalogue demandé auprès de {name}.");
            return _catalogProvider()
                .Select(song => new BasicSong
                {
                    Title = song.Title,
                    Artist = song.Artist,
                    Year = song.Year,
                    Duration = song.Duration,
                    Size = song.Size,
                    Featuring = song.Featuring,
                    Hash = song.Hash,
                    Extension = song.Extension
                })
                .Cast<ISong>()
                .ToList();
        }

        public void SendCatalog(string name)
        {
            _logger?.Invoke($"Catalogue envoyé à {name}.");
        }

        public void AskMedia(ISong song, string name, int startByte, int endByte)
        {
            _logger?.Invoke($"Téléchargement demandé à {name} pour {song.Title} ({startByte}-{endByte}).");
        }

        public void SendMedia(ISong song, string name, int startByte, int endByte)
        {
            _logger?.Invoke($"Envoi de {song.Title} à {name} ({startByte}-{endByte}).");
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        public event EventHandler<string>? LogMessage;
        public event EventHandler<(string FileName, byte[] Data)>? MediaReceived;
    }
}
