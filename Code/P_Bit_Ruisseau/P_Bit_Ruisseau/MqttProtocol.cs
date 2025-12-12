using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using P_Bit_Ruisseau;
using MQTTnet;
using MQTTnet.Client; 
using System.Text.Json;
using System.Net;
using System.Configuration;
using BrMessage = P_Bit_Ruisseau.Message;

namespace P_Bit_Ruisseau
{
    public class MqttProtocol : IProtocol, IDisposable
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttOptions;
        private readonly string _topic;
        private readonly string _hostname;
        private readonly Action<string>? _logger;
        private readonly Func<IEnumerable<ISong>> _localCatalogProvider;

        // Store discovered peers and their last seen time
        private readonly ConcurrentDictionary<string, DateTime> _peers = new();
        
        // Pending catalog requests: Key = PeerName, Value = TCS
        private readonly ConcurrentDictionary<string, TaskCompletionSource<List<ISong>>> _pendingCatalogs = new();

        public MqttProtocol(Func<IEnumerable<ISong>> localCatalogProvider, Action<string>? logger = null)
        {
            _localCatalogProvider = localCatalogProvider;
            _logger = logger;
            // Append short unique ID to allow multiple instances on same machine
            _hostname = $"{Dns.GetHostName()}-{Guid.NewGuid().ToString().Substring(0, 5)}";

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _topic = ConfigurationManager.AppSettings["MqttTopic"] ?? "BitRuisseau";
            var host = ConfigurationManager.AppSettings["MqttHost"] ?? "localhost";
            var port = int.Parse(ConfigurationManager.AppSettings["MqttPort"] ?? "1883");
            var user = ConfigurationManager.AppSettings["MqttUsername"];
            var pass = ConfigurationManager.AppSettings["MqttPassword"];

            var builder = new MqttClientOptionsBuilder()
                 .WithTcpServer(host, port)
                 .WithClientId($"BitRuisseau-{Guid.NewGuid()}")
                 .WithCleanSession();
                 
            if (!string.IsNullOrEmpty(user))
            {
                builder.WithCredentials(user, pass);
            }
            
            _mqttOptions = builder.Build();

            _mqttClient.ApplicationMessageReceivedAsync += HandleMessageReceived;

            _ = ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            try
            {
                await _mqttClient.ConnectAsync(_mqttOptions);
                await _mqttClient.SubscribeAsync(_topic);
                _logger?.Invoke("Connecté au broker MQTT.");
                
                // Announce presence immediately
                SayOnline();
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"Erreur de connexion MQTT: {ex.Message}");
            }
        }

        private async Task HandleMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                var message = JsonSerializer.Deserialize<BrMessage>(payload);

                if (message == null) 
                {
                    _logger?.Invoke("Reçu message null ou mal formé");
                    return;
                }
                
                if (message.Sender == _hostname) 
                {
                    // _logger?.Invoke("Ignoré message de soi-même");
                    return; 
                }
                
                if (message.Recipient != "0.0.0.0" && message.Recipient != _hostname) 
                {
                    _logger?.Invoke($"Ignoré message pour {message.Recipient} (Je suis {_hostname})");
                    return; 
                }
                
                _logger?.Invoke($"Traitement {message.Action} de {message.Sender}");

                switch (message.Action)
                {
                    case "askOnline":
                        SayOnline();
                        break;
                    case "online":
                        _peers[message.Sender] = DateTime.Now;
                        break;
                    case "askCatalog":
                        HandleAskCatalog(message.Sender);
                        break;
                    // Typo in protocole.md says "askMedia" for SendCatalog, but we support both "sendCatalog" and the potential typo
                    case "sendCatalog":
                    case "askMedia" when message.SongList != null && message.SongList.Count > 0: 
                        if (_pendingCatalogs.TryRemove(message.Sender, out var tcs))
                        {
                            tcs.TrySetResult(message.SongList.Cast<ISong>().ToList());
                        }
                        break;
                    case "askMedia" when message.SongList == null:
                        // This matches "AskMedia" request (Action=askMedia, SongData=null)
                        // Verify we have the song requested (by Hash?)
                        // Protocol doesn't send Song object, only Hash in example? 
                        // Protocol example: "Hash":"SHA256"
                        if(!string.IsNullOrEmpty(message.Hash))
                        {
                            var requestedSong = _localCatalogProvider().FirstOrDefault(s => s.Hash == message.Hash);
                            if (requestedSong != null)
                            {
                                int start = message.StartByte ?? 0;
                                int end = message.EndByte ?? requestedSong.Size;
                                SendMedia(requestedSong, message.Sender, start, end);
                            }
                            else
                            {
                                 _logger?.Invoke($"Média demandé non trouvé: {message.Hash}");
                            }
                        }
                        break;
                        
                    case "sendMedia":
                        // Received media data
                        if (!string.IsNullOrEmpty(message.SongData))
                        {
                             var data = Convert.FromBase64String(message.SongData);
                             // Save to Downloads
                             var fileName = $"received_{message.Hash}_{DateTime.Now.Ticks}.mp3"; // We don't have extension here unfortunately unless we store pending requests
                             MediaReceived?.Invoke(this, (fileName, data));
                             _logger?.Invoke($"Reçu {data.Length} bytes de {message.Sender}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
               _logger?.Invoke($"Erreur traitement message: {ex.Message}");
            }
        }

        private async void HandleAskCatalog(string requester)
        {
            var catalog = _localCatalogProvider().Select(s => new BasicSong
            {
                Title = s.Title,
                Artist = s.Artist,
                Year = s.Year,
                Duration = s.Duration,
                Size = s.Size,
                Featuring = s.Featuring,
                Hash = s.Hash,
                Extension = s.Extension
            }).Cast<ISong>().ToList();

            var response = new BrMessage
            {
                Recipient = requester,
                Sender = _hostname,
                Action = "sendCatalog", 
                SongList = catalog
            };

            await SendBrMessageAsync(response);
        }

        private async Task SendBrMessageAsync(BrMessage message)
        {
            if (!_mqttClient.IsConnected) return;

            var json = JsonSerializer.Serialize(message);
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(_topic)
                .WithPayload(json)
                .Build();

            await _mqttClient.PublishAsync(mqttMessage);
        }

        public string[] GetOnlineMediatheque()
        {
            var msg = new BrMessage { Recipient = "0.0.0.0", Sender = _hostname, Action = "askOnline" };
            _ = SendBrMessageAsync(msg);
            
            var now = DateTime.Now;
            foreach (var peer in _peers)
            {
                if ((now - peer.Value).TotalMinutes > 1)
                {
                    _peers.TryRemove(peer.Key, out _);
                }
            }
            
            return _peers.Keys.ToArray();
        }

        public void SayOnline()
        {
            var msg = new BrMessage { Recipient = "0.0.0.0", Sender = _hostname, Action = "online" };
            _ = SendBrMessageAsync(msg);
        }

        public List<ISong> AskCatalog(string name)
        {
            var tcs = new TaskCompletionSource<List<ISong>>();
            _pendingCatalogs[name] = tcs;

            var msg = new BrMessage { Recipient = name, Sender = _hostname, Action = "askCatalog" };
            _ = SendBrMessageAsync(msg);

            if (Task.WhenAny(tcs.Task, Task.Delay(5000)).Result == tcs.Task)
            {
                return tcs.Task.Result;
            }
            else
            {
                _pendingCatalogs.TryRemove(name, out _);
                _logger?.Invoke($"Timeout demande catalogue {name}");
                return new List<ISong>();
            }
        }

        public void SendCatalog(string name)
        {
           HandleAskCatalog(name);
        }

        public void AskMedia(ISong song, string name, int startByte, int endByte)
        {
            var msg = new BrMessage 
            { 
                Recipient = name, 
                Sender = _hostname, 
                Action = "askMedia",
                Hash = song.Hash,
                StartByte = startByte,
                EndByte = endByte
            };
            _ = SendBrMessageAsync(msg);
        }

        public event EventHandler<string>? LogMessage;
        public event EventHandler<(string FileName, byte[] Data)>? MediaReceived;

        public void SendMedia(ISong song, string name, int startByte, int endByte)
        {
             // Find file in catalog
             // Find file in catalog
             var track = _localCatalogProvider().FirstOrDefault(s => s.Hash == song.Hash);
             
             if (!(track is SongModel songModel) || !System.IO.File.Exists(songModel.Path))
             {
                 _logger?.Invoke($"Fichier physique manquant ou type incorrect");
                 return;
             }
             var filePath = songModel.Path;

             try 
             {
                 using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                 // Warning: This reads the whole chunk into memory. 
                 // For very large requests, ensure logic matches start/end byte. 
                 // Here we assume basic implementation.
                 
                 // If start/end are 0/Size, read all. 
                 // If specific range, read range.
                 
                 fs.Seek(startByte, SeekOrigin.Begin);
                 var length = endByte - startByte;
                 if (length <= 0) length = (int)fs.Length; // Fallback
                 
                 var buffer = new byte[length];
                 var read = fs.Read(buffer, 0, length);
                 
                 // Resize if read less (EOF)
                 if (read < length)
                 {
                     Array.Resize(ref buffer, read);
                 }
                 
                 var base64 = Convert.ToBase64String(buffer);
                 
                 var msg = new BrMessage 
                 { 
                     Recipient = name, 
                     Sender = _hostname, 
                     Action = "sendMedia", 
                     StartByte = startByte,
                     EndByte = endByte,
                     SongData = base64,
                     Hash = song.Hash,
                     SongList = null
                 };
                 _ = SendBrMessageAsync(msg);
                 _logger?.Invoke($"Envoi de {song.Title} à {name} ({read} bytes)");
             }
             catch (Exception ex)
             {
                 _logger?.Invoke($"Erreur envoi média: {ex.Message}");
             }
        }

        public void Dispose()
        {
            _mqttClient?.Dispose();
        }
    }
}
