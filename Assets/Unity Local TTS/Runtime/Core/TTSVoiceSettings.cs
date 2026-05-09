using UnityEngine;

namespace KrostGames.LocalTTS.Runtime.Core
{
    /// <summary>
    /// Supported TTS languages. Maps to Windows SAPI culture codes.
    /// </summary>
    public enum TTSLanguage
    {
        English = 0,
        Spanish = 1,
        Portuguese = 2
    }

    /// <summary>
    /// ScriptableObject holding TTS voice configuration.
    /// Used by the editor tooling to persist voice settings across sessions.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TTSVoiceSettings",
        menuName = "Unity Local TTS/Voice Settings",
        order = 100)]
    public class TTSVoiceSettings : ScriptableObject
    {
        [Header("Language")]
        [Tooltip("Language for TTS synthesis. Voices will be filtered to match.")]
        public TTSLanguage language = TTSLanguage.English;

        [Header("Voice")]
        [Tooltip("SAPI voice name. Leave empty for auto-select based on language.")]
        public string voiceName = "";

        [Header("Speech")]
        [Range(-10, 10)]
        [Tooltip("Speech rate. -10 = slowest, 10 = fastest.")]
        public int rate = 0;

        [Range(0, 100)]
        [Tooltip("Volume. 0 = mute, 100 = max.")]
        public int volume = 100;

        /// <summary>
        /// Returns the SAPI culture prefix for the selected language.
        /// </summary>
        public static string GetCulturePrefix(TTSLanguage lang)
        {
            return lang switch
            {
                TTSLanguage.English => "en",
                TTSLanguage.Spanish => "es",
                TTSLanguage.Portuguese => "pt",
                _ => "en"
            };
        }
    }
}
