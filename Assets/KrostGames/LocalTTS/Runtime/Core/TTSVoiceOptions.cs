using System;
using System.Globalization;
using UnityEngine;

namespace KrostGames.TTS
{
    /// <summary>
    /// How a line is spoken: voice, speed, pitch and volume. Serializable, so any component can expose it inline.
    /// </summary>
    [Serializable]
    public class TTSVoiceOptions
    {
        [Tooltip("Installed voice to use, full or partial name (\"Zira\", \"Microsoft Raul\"). Leave empty to use the first voice of the language below.")]
        public string voiceName = "";

        [Tooltip("Language used to pick a voice when Voice Name is empty or that voice is not installed on this PC.")]
        public TTSLanguage language = TTSLanguage.English;

        [Range(-10, 10), Tooltip("Speaking rate. -10 = slowest, 0 = normal, 10 = fastest.")]
        public int rate;

        [Range(-10, 10), Tooltip("Pitch. -10 = deepest, 0 = natural, 10 = highest.")]
        public int pitch;

        [Range(0, 100), Tooltip("Volume of the generated audio. 0 = silent, 100 = full.")]
        public int volume = 100;

        public TTSVoiceOptions() { }

        public TTSVoiceOptions(string voiceName, TTSLanguage language = TTSLanguage.English, int rate = 0, int pitch = 0, int volume = 100)
        {
            this.voiceName = voiceName ?? "";
            this.language = language;
            this.rate = rate;
            this.pitch = pitch;
            this.volume = volume;
        }

        public TTSVoiceOptions(TTSLanguage language) : this("", language) { }

        public TTSVoiceOptions Clone() => (TTSVoiceOptions)MemberwiseClone();

        public void CopyFrom(TTSVoiceOptions other)
        {
            if (other == null) return;
            voiceName = other.voiceName;
            language = other.language;
            rate = other.rate;
            pitch = other.pitch;
            volume = other.volume;
        }

        /// <summary>
        /// Short id of "this text spoken with these settings". Baked clips keep it, so a clip is known to be
        /// outdated as soon as the text or the voice settings change.
        /// </summary>
        public string Signature(string text)
        {
            string key = string.Concat(
                text ?? "", "\u001f",
                (voiceName ?? "").Trim().ToLowerInvariant(), "\u001f",
                ((int)language).ToString(CultureInfo.InvariantCulture), "\u001f",
                Mathf.Clamp(rate, -10, 10).ToString(CultureInfo.InvariantCulture), "\u001f",
                Mathf.Clamp(pitch, -10, 10).ToString(CultureInfo.InvariantCulture), "\u001f",
                Mathf.Clamp(volume, 0, 100).ToString(CultureInfo.InvariantCulture));
            return Hash(key);
        }

        /// <summary>FNV-1a 64-bit, as 16 hex characters. Stable across platforms and runs.</summary>
        internal static string Hash(string s)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in s)
            {
                h ^= c;
                h *= 1099511628211UL;
            }
            return h.ToString("x16", CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            string voice = string.IsNullOrWhiteSpace(voiceName) ? "auto " + language : voiceName;
            return $"{voice} (rate {rate}, pitch {pitch}, volume {volume})";
        }
    }
}
