using System;

namespace KrostGames.TTS
{
    /// <summary>Windows speech engine that owns a voice.</summary>
    public enum TTSEngineKind
    {
        /// <summary>Classic SAPI 5 "Desktop" voices (System.Speech).</summary>
        Sapi,
        /// <summary>Windows 10/11 OneCore voices (Windows.Media.SpeechSynthesis), the ones added in Settings > Speech.</summary>
        OneCore
    }

    /// <summary>A voice installed on this PC.</summary>
    public sealed class TTSVoice
    {
        internal TTSVoice(TTSEngineKind engine, string id, string name, string culture, string gender)
        {
            Engine = engine;
            Id = id ?? "";
            Name = name ?? "";
            Culture = culture ?? "";
            Gender = gender ?? "";
        }

        public TTSEngineKind Engine { get; }

        /// <summary>Voice name as Windows reports it, e.g. "Microsoft Zira Desktop".</summary>
        public string Name { get; }

        /// <summary>Culture such as "en-US" or "es-MX".</summary>
        public string Culture { get; }

        /// <summary>"Female", "Male" or "Neutral".</summary>
        public string Gender { get; }

        /// <summary>Engine-specific id (SAPI name or OneCore token).</summary>
        internal string Id { get; }

        /// <summary>Upper-case language code taken from the culture ("EN", "ES"...).</summary>
        public string LanguageCode => Culture.Length >= 2 ? Culture.Substring(0, 2).ToUpperInvariant() : "XX";

        /// <summary>Name shown in menus. OneCore voices get a suffix so they are not confused with the SAPI ones.</summary>
        public string DisplayName => Engine == TTSEngineKind.OneCore ? Name + " (OneCore)" : Name;

        public bool Matches(TTSLanguage language) => Culture.StartsWith(language.CulturePrefix(), StringComparison.OrdinalIgnoreCase);

        public override string ToString() => $"{DisplayName} ({Culture})";
    }
}
