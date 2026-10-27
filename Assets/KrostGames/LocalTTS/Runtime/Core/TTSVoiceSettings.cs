using UnityEngine;

namespace KrostGames.TTS
{
    /// <summary>
    /// A reusable voice ("Narrator", "Robot", "Guide"...). Assign it to several speakers so they all sound the same,
    /// and change it in one place.
    /// </summary>
    [CreateAssetMenu(fileName = "TTS Voice Profile", menuName = "Local TTS/Voice Profile", order = 100)]
    public class TTSVoiceSettings : ScriptableObject
    {
        [Tooltip("Language used to pick a voice when Voice Name is empty or that voice is not installed on this PC.")]
        public TTSLanguage language = TTSLanguage.English;

        [Tooltip("Installed voice, full or partial name (\"Zira\", \"Microsoft Raul\"). Leave empty to pick the first voice of the language.")]
        public string voiceName = "";

        [Range(-10, 10), Tooltip("Speaking rate. -10 = slowest, 0 = normal, 10 = fastest.")]
        public int rate;

        [Range(-10, 10), Tooltip("Pitch. -10 = deepest, 0 = natural, 10 = highest.")]
        public int pitch;

        [Range(0, 100), Tooltip("Volume of the generated audio. 0 = silent, 100 = full.")]
        public int volume = 100;

        /// <summary>These settings as a <see cref="TTSVoiceOptions"/> (a new copy each call).</summary>
        public TTSVoiceOptions ToOptions() => new TTSVoiceOptions(voiceName, language, rate, pitch, volume);

        [System.Obsolete("Use TTSLanguage.CulturePrefix() instead.")]
        public static string GetCulturePrefix(TTSLanguage lang) => lang.CulturePrefix();
    }
}
