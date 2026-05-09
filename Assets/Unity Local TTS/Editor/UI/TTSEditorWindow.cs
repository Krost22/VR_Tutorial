#if UNITY_EDITOR_WIN
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using KrostGames.LocalTTS.Editor.Core;
using KrostGames.LocalTTS.Runtime.Core;
using L = KrostGames.LocalTTS.Editor.UI.TTSLocalization;

namespace KrostGames.LocalTTS.Editor.UI
{
    public class TTSEditorWindow : EditorWindow
    {
        private string _text = "Hello, this is a test of Unity Local TTS.";
        private string _fileName = "";
        private int _rate;
        private int _volume = 100;
        private TTSLanguage _selectedLanguage = TTSLanguage.English;
        private TTSLanguage _lastLoadedLanguage = TTSLanguage.English;
        private string[] _filteredVoices = System.Array.Empty<string>();
        private int _selectedVoiceIndex;
        private bool _voicesLoaded;
        private AudioClip _lastGeneratedClip;
        private bool _isGenerating;

        private struct HistoryEntry { public AudioClip clip; public string text; public TTSLanguage language; }
        private readonly List<HistoryEntry> _history = new List<HistoryEntry>();
        private const int MaxHistoryEntries = 10;
        private bool _historyFoldout;
        private Vector2 _historyScroll;

        private TextAsset _batchTextAsset;
        private bool _batchFoldout;
        private bool _useNamingTemplate;
        private string _namingTemplate = "{lang}_{id}_{name}";
        private bool _autoAssign = true;
        private Vector2 _scrollPosition;

        private GUIStyle _headerStyle, _sectionStyle, _langLabelStyle, _signatureStyle, _histItemStyle;
        private bool _stylesInit;

        private static readonly string[] SampleTexts = {
            "Hello, this is a test of Unity Local TTS.",
            "Hola, esta es una prueba de Unity Local TTS.",
            "Olá, este é um teste do Unity Local TTS."
        };
        private static readonly string[] LanguageLabels = { "🇺🇸 English", "🇪🇸 Español", "🇧🇷 Português" };

        [MenuItem("Tools/Unity Local TTS/Generator", false, 100)]
        public static void ShowWindow()
        {
            var w = GetWindow<TTSEditorWindow>();
            w.titleContent = new GUIContent("TTS Generator", EditorGUIUtility.IconContent("d_AudioSource Icon").image);
            w.minSize = new Vector2(420, 600);
            w.Show();
        }

        private void OnEnable() => _stylesInit = false;

        private void InitStyles()
        {
            if (_stylesInit) return;
            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0,0,10,10) };
            _sectionStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10,10,8,8), margin = new RectOffset(4,4,4,4) };
            _langLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.6f,0.8f,1f) } };
            _signatureStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.5f,0.5f,0.5f,0.6f) }, fontStyle = FontStyle.Italic, fontSize = 10, margin = new RectOffset(0,0,10,10) };
            _histItemStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(6,6,4,4), margin = new RectOffset(0,0,1,1) };
            _stylesInit = true;
        }

        private void OnGUI()
        {
            InitStyles();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Header + UI Language selector
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("🔊 Unity Local TTS", _headerStyle, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // UI Language toggle (top right area)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(L.Get(L.UILang) + ":", EditorStyles.miniLabel, GUILayout.Width(75));
            L.Current = (UILanguage)GUILayout.Toolbar((int)L.Current, L.UILanguageLabels, GUILayout.Width(160), GUILayout.Height(20));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            DrawLanguageSection();
            DrawTextInputSection();
            DrawVoiceSettingsSection();
            DrawNamingTemplateSection();
            DrawGenerateSection();
            DrawResultSection();
            DrawBatchSection();
            DrawHistorySection();
            DrawUtilitySection();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Developed by KrostGames · 2026", _signatureStyle);
            EditorGUILayout.Space(4);

            EditorGUILayout.EndScrollView();

            // Repaint while live preview is active so the stop button stays responsive
            if (WindowsTTSEngine.IsLivePreviewPlaying)
                Repaint();
        }

        #region Sections

        private void DrawLanguageSection()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField(L.Get(L.Language), EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            _selectedLanguage = (TTSLanguage)GUILayout.Toolbar((int)_selectedLanguage, LanguageLabels, GUILayout.Height(28));
            if (EditorGUI.EndChangeCheck()) OnLanguageChanged();
            string cp = TTSVoiceSettings.GetCulturePrefix(_selectedLanguage);
            string lf = WindowsTTSEngine.GetLanguageSubfolder(_selectedLanguage);
            EditorGUILayout.LabelField($"Culture: {cp}-*  ·  Folder: GeneratedAudio/{lf}/", _langLabelStyle);
            EditorGUILayout.EndVertical();
        }

        private void DrawTextInputSection()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField(L.Get(L.TextInput), EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(L.Get(L.TextToSpeak));
            var s = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            _text = EditorGUILayout.TextArea(_text, s, GUILayout.Height(120));
            EditorGUILayout.Space(4);
            _fileName = EditorGUILayout.TextField(L.Get(L.FileName), _fileName);
            EditorGUILayout.HelpBox(L.Get(L.FileNameHint), MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawVoiceSettingsSection()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField(L.Get(L.VoiceSettings), EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            if (!_voicesLoaded)
            {
                if (GUILayout.Button("🔍 " + L.Get(L.ScanVoices))) LoadVoicesForCurrentLanguage();
            }
            else
            {
                if (_filteredVoices.Length > 0)
                {
                    _selectedVoiceIndex = Mathf.Clamp(_selectedVoiceIndex, 0, _filteredVoices.Length - 1);
                    _selectedVoiceIndex = EditorGUILayout.Popup("Voice", _selectedVoiceIndex, _filteredVoices);
                    EditorGUILayout.LabelField($"Culture: {WindowsTTSEngine.GetCultureForVoice(_filteredVoices[_selectedVoiceIndex])}", _langLabelStyle);
                }
                else
                {
                    EditorGUILayout.HelpBox($"{L.Get(L.NoVoices)} {_selectedLanguage}.\n{L.Get(L.InstallVoice)} Windows Settings → Time & Language → Speech → Add voices.", MessageType.Warning);
                }
                if (GUILayout.Button("🔄 " + L.Get(L.RefreshVoices)))
                {
                    WindowsTTSEngine.RefreshVoiceCache();
                    LoadVoicesForCurrentLanguage();
                }
            }

            EditorGUILayout.Space(4);
            _rate = EditorGUILayout.IntSlider("Rate", _rate, -10, 10);
            _volume = EditorGUILayout.IntSlider("Volume", _volume, 0, 100);

            // Live Preview + Stop
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            bool isPlaying = WindowsTTSEngine.IsLivePreviewPlaying;
            GUI.enabled = !string.IsNullOrWhiteSpace(_text);
            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = isPlaying ? new Color(0.3f, 0.5f, 0.7f) : new Color(0.4f, 0.6f, 0.9f);

            if (GUILayout.Button(isPlaying ? "🔊 " + L.Get(L.LivePreview) : "🔈 " + L.Get(L.LivePreview)))
            {
                string vn = _voicesLoaded && _filteredVoices.Length > 0 ? _filteredVoices[_selectedVoiceIndex] : null;
                WindowsTTSEngine.LivePreview(_text, _rate, _volume, vn);
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            GUI.enabled = isPlaying;
            if (GUILayout.Button("⏹ " + L.Get(L.Stop), GUILayout.Width(80)))
                WindowsTTSEngine.StopLivePreview();

            GUI.backgroundColor = bg;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawNamingTemplateSection()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            _useNamingTemplate = EditorGUILayout.ToggleLeft("📝 " + L.Get(L.UseNamingTemplate), _useNamingTemplate, EditorStyles.boldLabel);
            if (_useNamingTemplate)
            {
                EditorGUILayout.Space(2);
                _namingTemplate = EditorGUILayout.TextField("Template", _namingTemplate);
                EditorGUILayout.HelpBox("Tokens:  {lang} = EN/ES/PT  ·  {id} = 001, 002...  ·  {name} = file name or auto text", MessageType.Info);
                string preview = WindowsTTSEngine.ApplyNamingTemplate(_namingTemplate, _selectedLanguage, 1, string.IsNullOrWhiteSpace(_fileName) ? null : _fileName, _text);
                EditorGUILayout.LabelField($"Preview: {preview}.wav", _langLabelStyle);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawGenerateSection()
        {
            EditorGUILayout.Space(4);
            bool hasVoice = _voicesLoaded && _filteredVoices.Length > 0;
            bool canGen = !_isGenerating && !string.IsNullOrWhiteSpace(_text);

            if (_voicesLoaded && !hasVoice)
                EditorGUILayout.HelpBox($"{L.Get(L.NoVoices)} {_selectedLanguage}.", MessageType.Error);

            _autoAssign = EditorGUILayout.ToggleLeft("🎯 " + L.Get(L.AutoAssign), _autoAssign);
            EditorGUILayout.Space(2);

            GUI.enabled = canGen;
            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = hasVoice ? new Color(0.3f, 0.8f, 0.4f) : new Color(0.8f, 0.6f, 0.2f);

            if (GUILayout.Button(_isGenerating ? "⏳ " + L.Get(L.Generating) : "▶ " + L.Get(L.GenerateAudio), GUILayout.Height(36)))
            {
                _isGenerating = true;
                string vn = hasVoice ? _filteredVoices[_selectedVoiceIndex] : null;
                string outName = null;
                if (_useNamingTemplate)
                    outName = WindowsTTSEngine.ApplyNamingTemplate(_namingTemplate, _selectedLanguage, _history.Count + 1, string.IsNullOrWhiteSpace(_fileName) ? null : _fileName.Trim(), _text);
                else if (!string.IsNullOrWhiteSpace(_fileName))
                    outName = _fileName.Trim();

                _lastGeneratedClip = WindowsTTSEngine.Generate(_text, _selectedLanguage, outName, _rate, _volume, vn);
                if (_lastGeneratedClip != null)
                {
                    AddToHistory(_lastGeneratedClip, _text, _selectedLanguage);
                    if (_autoAssign) TTSEditorUtility.TryAutoAssignClip(_lastGeneratedClip);
                }
                _isGenerating = false;
            }
            GUI.backgroundColor = bg;
            GUI.enabled = true;
        }

        private void DrawResultSection()
        {
            if (_lastGeneratedClip == null) return;
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField(L.Get(L.LastClip), EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            EditorGUILayout.ObjectField("Clip", _lastGeneratedClip, typeof(AudioClip), false);
            EditorGUILayout.LabelField($"{L.Get(L.Duration)} {_lastGeneratedClip.length:F2}s");
            EditorGUILayout.LabelField($"{L.Get(L.Channels)} {_lastGeneratedClip.channels}");
            EditorGUILayout.LabelField($"{L.Get(L.SampleRate)} {_lastGeneratedClip.frequency} Hz");
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📂 " + L.Get(L.PingProject))) EditorGUIUtility.PingObject(_lastGeneratedClip);
            if (GUILayout.Button("▶ " + L.Get(L.PreviewAudio))) PlayClipInEditor(_lastGeneratedClip);
            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("⏹ " + L.Get(L.Stop), GUILayout.Width(70))) StopAllEditorAudio();
            GUI.backgroundColor = bg;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawBatchSection()
        {
            EditorGUILayout.Space(4);
            _batchFoldout = EditorGUILayout.Foldout(_batchFoldout, "📦 " + L.Get(L.BatchGen), true, EditorStyles.foldoutHeader);
            if (!_batchFoldout) return;
            EditorGUILayout.BeginVertical(_sectionStyle);
            _batchTextAsset = (TextAsset)EditorGUILayout.ObjectField("Text File (.txt/.csv)", _batchTextAsset, typeof(TextAsset), false);
            EditorGUILayout.HelpBox("Each line = one audio clip. CSV: text,filename. Lines starting with # or // are skipped.", MessageType.Info);
            GUI.enabled = _batchTextAsset != null && _voicesLoaded && _filteredVoices.Length > 0;
            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.7f, 0.2f);
            if (GUILayout.Button("⚡ " + L.Get(L.GenAllLines), GUILayout.Height(30)))
            {
                var entries = ParseBatchFile(_batchTextAsset.text);
                if (entries.Count > 0)
                {
                    var clips = WindowsTTSEngine.BatchGenerate(entries, _selectedLanguage, _rate, _volume, _filteredVoices[_selectedVoiceIndex], _useNamingTemplate ? _namingTemplate : null);
                    for (int i = 0; i < clips.Count; i++)
                        if (clips[i] != null) AddToHistory(clips[i], entries[i].text, _selectedLanguage);
                }
            }
            GUI.backgroundColor = bg;
            GUI.enabled = true;
            EditorGUILayout.EndVertical();
        }

        private void DrawHistorySection()
        {
            if (_history.Count == 0) return;
            EditorGUILayout.Space(4);
            _historyFoldout = EditorGUILayout.Foldout(_historyFoldout, $"📜 {L.Get(L.History)} ({_history.Count})", true, EditorStyles.foldoutHeader);
            if (!_historyFoldout) return;

            EditorGUILayout.BeginVertical(_sectionStyle);

            // Global stop button for history
            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("⏹ " + L.Get(L.StopAllAudio))) StopAllEditorAudio();
            GUI.backgroundColor = bg;
            EditorGUILayout.Space(2);

            _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll, GUILayout.MaxHeight(200));
            for (int i = _history.Count - 1; i >= 0; i--)
            {
                var e = _history[i];
                if (e.clip == null) continue;
                EditorGUILayout.BeginVertical(_histItemStyle);
                EditorGUILayout.BeginHorizontal();
                string badge = e.language switch { TTSLanguage.English => "EN", TTSLanguage.Spanish => "ES", TTSLanguage.Portuguese => "PT", _ => "??" };
                GUILayout.Label($"[{badge}]", GUILayout.Width(30));
                string cn = e.clip.name;
                GUILayout.Label(cn.Length > 30 ? cn.Substring(0, 27) + "..." : cn, GUILayout.ExpandWidth(true));
                GUILayout.Label($"{e.clip.length:F1}s", _langLabelStyle, GUILayout.Width(40));
                if (GUILayout.Button("▶", GUILayout.Width(25))) PlayClipInEditor(e.clip);
                if (GUILayout.Button("📂", GUILayout.Width(25))) EditorGUIUtility.PingObject(e.clip);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("🗑 " + L.Get(L.ClearHistory))) { _history.Clear(); _historyFoldout = false; }
            EditorGUILayout.EndVertical();
        }

        private void DrawUtilitySection()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField(L.Get(L.Utilities), EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            int cc = TTSEditorUtility.GetGeneratedClipCount();
            EditorGUILayout.LabelField($"{L.Get(L.GenClips)} {cc}");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📂 " + L.Get(L.OpenFolder))) TTSEditorUtility.PingGeneratedFolder();
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("🗑 " + L.Get(L.ClearAll)) && cc > 0)
                if (EditorUtility.DisplayDialog(L.Get(L.ClearAll), $"Delete {cc} .wav file(s)?", "Delete", "Cancel"))
                { TTSEditorUtility.ClearGeneratedAudio(); _lastGeneratedClip = null; _history.Clear(); }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Logic

        private void OnLanguageChanged()
        {
            int prev = (int)_lastLoadedLanguage;
            if (prev < SampleTexts.Length && _text == SampleTexts[prev]) _text = SampleTexts[(int)_selectedLanguage];
            _lastLoadedLanguage = _selectedLanguage;
            if (_voicesLoaded) LoadVoicesForCurrentLanguage();
        }

        private void LoadVoicesForCurrentLanguage()
        {
            _filteredVoices = WindowsTTSEngine.GetVoicesForLanguage(_selectedLanguage);
            _selectedVoiceIndex = 0;
            _voicesLoaded = true;
            if (_filteredVoices.Length == 0)
                Debug.LogWarning($"[TTS] No SAPI voices found for {_selectedLanguage}.");
            else
                Debug.Log($"[TTS] Found {_filteredVoices.Length} voice(s) for {_selectedLanguage}.");
        }

        private void AddToHistory(AudioClip clip, string text, TTSLanguage lang)
        {
            _history.Add(new HistoryEntry { clip = clip, text = text, language = lang });
            while (_history.Count > MaxHistoryEntries) _history.RemoveAt(0);
            _historyFoldout = true;
        }

        private static List<(string text, string fileName)> ParseBatchFile(string content)
        {
            var entries = new List<(string text, string fileName)>();
            foreach (string raw in content.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//")) continue;
                int ci = line.LastIndexOf(',');
                if (ci > 0 && ci < line.Length - 1)
                {
                    string t = line.Substring(0, ci).Trim().Trim('"');
                    string f = line.Substring(ci + 1).Trim().Trim('"');
                    if (f.Length < 60 && !f.Contains(" ")) { entries.Add((t, f)); continue; }
                }
                entries.Add((line, null));
            }
            return entries;
        }

        #endregion

        #region Audio

        private static void PlayClipInEditor(AudioClip clip)
        {
            var t = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (t == null) return;
            t.GetMethod("StopAllPreviewClips", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)?.Invoke(null, null);
            var m = t.GetMethod("PlayPreviewClip", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            m?.Invoke(null, new object[] { clip, 0, false });
        }

        private static void StopAllEditorAudio()
        {
            // Stop editor preview clips
            var t = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            t?.GetMethod("StopAllPreviewClips", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)?.Invoke(null, null);
            // Stop live preview process
            WindowsTTSEngine.StopLivePreview();
        }

        #endregion
    }
}
#endif
