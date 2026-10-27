using System;
using UnityEditor;
using UnityEngine;

namespace KrostGames.TTS.EditorTools
{
    public enum UILanguage { English, Spanish, Portuguese }

    /// <summary>Editor UI texts in English, Spanish and Portuguese. The choice is remembered per user.</summary>
    public static class TTSLocalization
    {
        const string PrefKey = "KrostGames.LocalTTS.UILanguage";
        static UILanguage? _current;

        /// <summary>Raised after the UI language changes.</summary>
        public static event Action Changed;

        public static UILanguage Current
        {
            get
            {
                if (_current == null) _current = (UILanguage)EditorPrefs.GetInt(PrefKey, (int)FromSystem());
                return _current.Value;
            }
            set
            {
                if (Current == value) return;
                _current = value;
                EditorPrefs.SetInt(PrefKey, (int)value);
                Changed?.Invoke();
            }
        }

        /// <summary>Picks the text for the current UI language.</summary>
        public static string T(string en, string es, string pt)
        {
            switch (Current)
            {
                case UILanguage.Spanish: return es;
                case UILanguage.Portuguese: return pt;
                default: return en;
            }
        }

        static UILanguage FromSystem()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Spanish: return UILanguage.Spanish;
                case SystemLanguage.Portuguese: return UILanguage.Portuguese;
                default: return UILanguage.English;
            }
        }
    }
}
