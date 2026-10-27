using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>Project-wide settings, saved in ProjectSettings/ so the whole team shares them.</summary>
    [FilePath("ProjectSettings/KrostGames.LocalTTS.asset", FilePathAttribute.Location.ProjectFolder)]
    public class TTSProjectSettings : ScriptableSingleton<TTSProjectSettings>
    {
        [SerializeField] string _outputFolder = "";
        [SerializeField] bool _organizeByLanguage = true;
        [SerializeField] bool _useNamingTemplate;
        [SerializeField] string _namingTemplate = "{lang}_{id}_{name}";
        [SerializeField] int _nextId = 1;
        [SerializeField] bool _recordPlayModeLines = true;
        [SerializeField] bool _trimSilence = true;

        /// <summary>Where generated .wav files go. Defaults to the package's GeneratedAudio folder.</summary>
        public string OutputFolder
        {
            get
            {
                string folder = (_outputFolder ?? "").Trim().TrimEnd('/');
                return folder.StartsWith("Assets", System.StringComparison.Ordinal) ? folder : TTSEditorUtility.DefaultOutputFolder;
            }
            set { _outputFolder = value ?? ""; SaveNow(); }
        }

        /// <summary>Put clips in a sub-folder per language (EN, ES, PT...).</summary>
        public bool OrganizeByLanguage
        {
            get => _organizeByLanguage;
            set { _organizeByLanguage = value; SaveNow(); }
        }

        public bool UseNamingTemplate
        {
            get => _useNamingTemplate;
            set { _useNamingTemplate = value; SaveNow(); }
        }

        /// <summary>Tokens: {lang} {id} {name} {voice}.</summary>
        public string NamingTemplate
        {
            get => string.IsNullOrWhiteSpace(_namingTemplate) ? "{name}" : _namingTemplate;
            set { _namingTemplate = value; SaveNow(); }
        }

        /// <summary>Record lines spoken from code in Play Mode into the Line Library.</summary>
        public bool RecordPlayModeLines
        {
            get => _recordPlayModeLines;
            set { _recordPlayModeLines = value; SaveNow(); }
        }

        /// <summary>Cut the silence Windows voices add before and after speech (real-time and baked).</summary>
        public bool TrimSilence
        {
            get => _trimSilence;
            set { _trimSilence = value; LocalTTS.TrimSilence = value; SaveNow(); }
        }

        /// <summary>Next value of the {id} token, then increments it.</summary>
        public int TakeNextId()
        {
            int id = Mathf.Max(1, _nextId);
            _nextId = id + 1;
            SaveNow();
            return id;
        }

        public int PeekNextId() => Mathf.Max(1, _nextId);

        public void SaveNow() => Save(true);
    }

    /// <summary>Per-user state of the Generator window, saved in UserSettings/ (not shared).</summary>
    [FilePath("UserSettings/KrostGames.LocalTTS.asset", FilePathAttribute.Location.ProjectFolder)]
    public class TTSUserSettings : ScriptableSingleton<TTSUserSettings>
    {
        [System.Serializable]
        public class HistoryItem
        {
            public string guid;
            public string text;
        }

        public const int MaxHistory = 20;

        public string text = "";
        public TTSVoiceOptions voice = new TTSVoiceOptions();
        public string languageFilter = "";
        public string fileName = "";
        public bool autoAssign = true;
        public string lastClipGuid = "";
        public List<HistoryItem> history = new List<HistoryItem>();

        public void AddToHistory(AudioClip clip, string spokenText)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip));
            if (string.IsNullOrEmpty(guid)) return;
            history.RemoveAll(h => h.guid == guid);
            history.Add(new HistoryItem { guid = guid, text = spokenText });
            while (history.Count > MaxHistory) history.RemoveAt(0);
            lastClipGuid = guid;
            SaveNow();
        }

        public void SaveNow() => Save(true);
    }

    [InitializeOnLoad]
    static class TTSSettingsBootstrap
    {
        // The runtime default follows the project setting, in edit mode and in Play Mode.
        static TTSSettingsBootstrap() => EditorApplication.delayCall += () => LocalTTS.TrimSilence = TTSProjectSettings.instance.TrimSilence;
    }
}
