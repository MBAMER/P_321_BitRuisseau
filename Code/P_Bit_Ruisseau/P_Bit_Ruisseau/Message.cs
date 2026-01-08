using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace P_Bit_Ruisseau
{
    public class Message
    {
        /// <summary>
        /// The message recipient
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// The message sender
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// What this message is for
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// The starting byte
        /// </summary>
        public int ?StartByte { get; set; }

        /// <summary>
        /// The ending Byte
        /// </summary>
        public int ?EndByte { get; set; }

        /// <summary>
        /// The list of song metadata (serialized as BasicSong objects)
        /// </summary>
        [JsonConverter(typeof(SongListJsonConverter))]
        public List<ISong> ?SongList { get; set; }

        /// <summary>
        /// The base64 encoded byte array of the audio file
        /// </summary>
        public string ?SongData { get; set; }

        /// <summary>
        /// The hash of the file asked/sent
        /// </summary>
        public string ?Hash { get; set; }
    }

    /// <summary>
    /// Custom JSON converter for handling List<ISong> serialization/deserialization
    /// </summary>
    public class SongListJsonConverter : JsonConverter<List<ISong>>
    {
        public override List<ISong> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new List<ISong>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return result;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var song = JsonSerializer.Deserialize<BasicSong>(ref reader, options);
                if (song != null)
                    result.Add(song);
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, List<ISong> value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            foreach (var song in value)
            {
                var basicSong = new BasicSong
                {
                    Title = song.Title,
                    Artist = song.Artist,
                    Year = song.Year,
                    Duration = song.Duration,
                    Size = song.Size,
                    Featuring = song.Featuring,
                    Hash = song.Hash,
                    Extension = song.Extension
                };
                JsonSerializer.Serialize(writer, basicSong, options);
            }
            writer.WriteEndArray();
        }
    }
}
