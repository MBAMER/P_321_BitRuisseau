using System;

namespace P_Bit_Ruisseau
{
    // Lightweight DTO for sending song metadata over the wire
    public class BasicSong : ISong
    {
        public BasicSong() { }

        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int Year { get; set; }
        public TimeSpan Duration { get; set; }
        public int Size { get; set; }
        public string[] Featuring { get; set; } = Array.Empty<string>();
        public string Hash { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
    }
}
