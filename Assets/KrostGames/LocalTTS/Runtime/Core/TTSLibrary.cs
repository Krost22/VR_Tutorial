using System;
using System.Collections.Generic;
using UnityEngine;

namespace KrostGames.TTS
{
    /// <summary>
    /// Lines spoken from code (<see cref="LocalTTS.Speak"/>, <see cref="TTSSpeaker.Speak(string)"/>) and the clips baked for them.
    /// In the Editor, every such line spoken in Play Mode is recorded here. Bake them once
    /// (Tools > Local TTS > Line Library) and the same code also speaks on Android, iOS, WebGL or consoles.
    /// The asset lives in a Resources folder, so builds find it without any setup.
    /// </summary>
    public class TTSLibrary : ScriptableObject
    {
        public const string ResourceName = "LocalTTSLibrary";

        [Serializable]
        public class Entry
        {
            [TextArea(1, 4)] public string text = "";
            public TTSVoiceOptions voice = new TTSVoiceOptions();
            public AudioClip clip;
            [SerializeField, HideInInspector] string _bakedSignature = "";

            /// <summary>True when the clip matches the current text and voice.</summary>
            public bool IsBaked => clip != null && voice != null && _bakedSignature == voice.Signature(text);

            public void SetBaked(AudioClip bakedClip)
            {
                clip = bakedClip;
                _bakedSignature = bakedClip != null && voice != null ? voice.Signature(text) : "";
            }
        }

        [SerializeField] List<Entry> _entries = new List<Entry>();

        static TTSLibrary _instance;
        static bool _searched;

        public List<Entry> Entries => _entries;

        /// <summary>The project's library (Resources/LocalTTSLibrary), or null if none was created yet.</summary>
        public static TTSLibrary Instance
        {
            get
            {
                if (!_searched)
                {
                    _instance = Resources.Load<TTSLibrary>(ResourceName);
                    _searched = true;
                }
                return _instance;
            }
        }

        internal static void ResetInstance()
        {
            _instance = null;
            _searched = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => ResetInstance();

        /// <summary>The entry for this text spoken with exactly these voice settings, or null.</summary>
        public Entry Find(string text, TTSVoiceOptions voice)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string signature = (voice ?? LocalTTS.DefaultVoice).Signature(text);
            foreach (Entry entry in _entries)
                if (entry != null && entry.text == text && entry.voice != null && entry.voice.Signature(entry.text) == signature)
                    return entry;
            return null;
        }

        /// <summary>
        /// A baked clip for this text: first one made with these exact voice settings,
        /// then (unless <paramref name="exactVoiceOnly"/>) any clip baked for the same text.
        /// </summary>
        public bool TryGetClip(string text, TTSVoiceOptions voice, bool exactVoiceOnly, out AudioClip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(text)) return false;

            Entry exact = Find(text, voice);
            if (exact != null && exact.IsBaked)
            {
                clip = exact.clip;
                return true;
            }
            if (exactVoiceOnly) return false;

            if (exact != null && exact.clip != null)
            {
                clip = exact.clip;
                return true;
            }
            foreach (Entry entry in _entries)
            {
                if (entry != null && entry.clip != null && entry.text == text)
                {
                    clip = entry.clip;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Looks the text up in the project's library (if there is one).</summary>
        public static bool TryGet(string text, TTSVoiceOptions voice, bool exactVoiceOnly, out AudioClip clip)
        {
            clip = null;
            TTSLibrary library = Instance;
            return library != null && library.TryGetClip(text, voice, exactVoiceOnly, out clip);
        }
    }
}
