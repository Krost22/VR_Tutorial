using System;

namespace KrostGames.TTS
{
    /// <summary>Speech could not be produced: no voices, engine not available on this platform, engine error...</summary>
    public class TTSException : Exception
    {
        public TTSException(string message) : base(message) { }
        public TTSException(string message, Exception inner) : base(message, inner) { }
    }
}
