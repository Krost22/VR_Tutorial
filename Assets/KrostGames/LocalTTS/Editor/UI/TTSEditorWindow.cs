using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using L = KrostGames.TTS.EditorTools.TTSLocalization;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>
    /// Tools > Local TTS > Generator. Type a line, pick a voice, preview it instantly from memory
    /// and save it as a .wav asset. Also: batch files, history, voice profiles and quick assignment to the selection.
    /// </summary>
    public class TTSEditorWindow : EditorWindow
    {
        const int MaxTextLength = 20000;

        // ------------------------------------------------------------------ UI references

        VisualElement _root;
        VisualElement _logo, _statusDot, _resultCard, _historyList;
        Label _subtitle, _statusLabel, _platformNote, _charCount, _textHint, _voiceInfo, _voiceHint, _namePreview,
              _resultInfo, _batchInfo, _batchHint, _historyEmpty, _toolsInfo, _actionStatus, _footer;
        TextField _text, _folder, _fileName, _template;
        DropdownField _languageFilter, _voice;
        SliderInt _rate, _pitch, _volume;
        ObjectField _profile, _resultClip, _batchFile;
        Toggle _useTemplate, _byLanguage, _autoAssign, _trimSilence, _recordLines;
        Foldout _batch, _history, _tools;
        ProgressBar _batchProgress;
        Button _uiEn, _uiEs, _uiPt, _btnSample, _btnRefresh, _btnSaveProfile, _btnResetVoice, _btnFolder, _btnOpenFolder,
               _btnResultPlay, _btnResultPing, _btnResultAssign, _btnBatch, _btnBatchCancel, _btnClearHistory,
               _btnLibrary, _btnBakeScenes, _btnDocs, _btnDeleteAll, _btnPreview, _btnStop, _btnGenerate;

        // ------------------------------------------------------------------ State

        readonly List<TTSVoice> _listedVoices = new List<TTSVoice>();
        readonly List<string> _languageCodes = new List<string>();
        List<WindowsTTSEngine.BatchEntry> _batchEntries = new List<WindowsTTSEngine.BatchEntry>();
        CancellationTokenSource _batchCancel;
        AudioClip _lastClip;
        string _status = "";
        bool _statusIsError;
        bool _generating;
        bool _settingsDirty;
        double _dirtySince;

        static TTSUserSettings User => TTSUserSettings.instance;
        static TTSProjectSettings Project => TTSProjectSettings.instance;

        [MenuItem("Tools/Local TTS/Generator", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<TTSEditorWindow>();
            window.minSize = new Vector2(380, 520);
            window.Show();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("TTS Generator", Icon("AudioSource Icon"));
            L.Changed += ApplyTexts;
            LocalTTS.StateChanged += OnEngineStateChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            Selection.selectionChanged += UpdateAssignTargets;
        }

        void OnDisable()
        {
            L.Changed -= ApplyTexts;
            LocalTTS.StateChanged -= OnEngineStateChanged;
            EditorApplication.projectChanged -= OnProjectChanged;
            Selection.selectionChanged -= UpdateAssignTargets;
            _batchCancel?.Cancel();
            SaveUserSettings();
        }

        public void CreateGUI()
        {
            _root = rootVisualElement;
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TTSEditorUtility.UIFolder + "/TTSEditorWindow.uxml");
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(TTSEditorUtility.UIFolder + "/TTSEditorWindow.uss");
            if (tree == null)
            {
                _root.Add(new HelpBox("Local TTS: UI files not found in " + TTSEditorUtility.UIFolder, HelpBoxMessageType.Error));
                return;
            }
            tree.CloneTree(_root);
            if (sheet != null) _root.styleSheets.Add(sheet);

            QueryElements();
            BuildLogo();
            LoadState();
            WireEvents();
            ApplyTexts();
            RefreshVoiceControls();
            RebuildHistory();
            UpdateResult();
            UpdateOutputControls();
            OnEngineStateChanged();

            if (LocalTTS.IsRealtimeSupported) LocalTTS.Prewarm();
            _root.schedule.Execute(Tick).Every(100);
        }

        // ------------------------------------------------------------------ Setup

        void QueryElements()
        {
            _logo = _root.Q("logo");
            _statusDot = _root.Q("status-dot");
            _resultCard = _root.Q("result-card");
            _historyList = _root.Q("history-list");

            _subtitle = _root.Q<Label>("subtitle");
            _statusLabel = _root.Q<Label>("status-label");
            _platformNote = _root.Q<Label>("platform-note");
            _charCount = _root.Q<Label>("char-count");
            _textHint = _root.Q<Label>("text-hint");
            _voiceInfo = _root.Q<Label>("voice-info");
            _voiceHint = _root.Q<Label>("voice-hint");
            _namePreview = _root.Q<Label>("name-preview");
            _resultInfo = _root.Q<Label>("result-info");
            _batchInfo = _root.Q<Label>("batch-info");
            _batchHint = _root.Q<Label>("batch-hint");
            _historyEmpty = _root.Q<Label>("history-empty");
            _toolsInfo = _root.Q<Label>("tools-info");
            _actionStatus = _root.Q<Label>("action-status");
            _footer = _root.Q<Label>("footer");

            _text = _root.Q<TextField>("text");
            _folder = _root.Q<TextField>("folder");
            _fileName = _root.Q<TextField>("file-name");
            _template = _root.Q<TextField>("template");
            _languageFilter = _root.Q<DropdownField>("language-filter");
            _voice = _root.Q<DropdownField>("voice");
            _rate = _root.Q<SliderInt>("rate");
            _pitch = _root.Q<SliderInt>("pitch");
            _volume = _root.Q<SliderInt>("volume");
            _profile = _root.Q<ObjectField>("profile");
            _resultClip = _root.Q<ObjectField>("result-clip");
            _batchFile = _root.Q<ObjectField>("batch-file");
            _useTemplate = _root.Q<Toggle>("use-template");
            _byLanguage = _root.Q<Toggle>("by-language");
            _autoAssign = _root.Q<Toggle>("auto-assign");
            _trimSilence = _root.Q<Toggle>("trim-silence");
            _recordLines = _root.Q<Toggle>("record-lines");
            _batch = _root.Q<Foldout>("batch");
            _history = _root.Q<Foldout>("history");
            _tools = _root.Q<Foldout>("tools");
            _batchProgress = _root.Q<ProgressBar>("batch-progress");

            _uiEn = _root.Q<Button>("ui-en");
            _uiEs = _root.Q<Button>("ui-es");
            _uiPt = _root.Q<Button>("ui-pt");
            _btnSample = _root.Q<Button>("btn-sample");
            _btnRefresh = _root.Q<Button>("btn-refresh");
            _btnSaveProfile = _root.Q<Button>("btn-save-profile");
            _btnResetVoice = _root.Q<Button>("btn-reset-voice");
            _btnFolder = _root.Q<Button>("btn-folder");
            _btnOpenFolder = _root.Q<Button>("btn-open-folder");
            _btnResultPlay = _root.Q<Button>("btn-result-play");
            _btnResultPing = _root.Q<Button>("btn-result-ping");
            _btnResultAssign = _root.Q<Button>("btn-result-assign");
            _btnBatch = _root.Q<Button>("btn-batch");
            _btnBatchCancel = _root.Q<Button>("btn-batch-cancel");
            _btnClearHistory = _root.Q<Button>("btn-clear-history");
            _btnLibrary = _root.Q<Button>("btn-library");
            _btnBakeScenes = _root.Q<Button>("btn-bake-scenes");
            _btnDocs = _root.Q<Button>("btn-docs");
            _btnDeleteAll = _root.Q<Button>("btn-delete-all");
            _btnPreview = _root.Q<Button>("btn-preview");
            _btnStop = _root.Q<Button>("btn-stop");
            _btnGenerate = _root.Q<Button>("btn-generate");

            _profile.objectType = typeof(TTSVoiceSettings);
            _profile.allowSceneObjects = false;
            _resultClip.objectType = typeof(AudioClip);
            _resultClip.allowSceneObjects = false;
            _batchFile.objectType = typeof(TextAsset);
            _batchFile.allowSceneObjects = false;
            _folder.isReadOnly = true;
            _text.maxLength = MaxTextLength;
        }

        void BuildLogo()
        {
            foreach (float h in new[] { 0.35f, 0.7f, 1f, 0.6f, 0.3f })
            {
                var bar = new VisualElement();
                bar.AddToClassList("tts-logo-bar");
                bar.style.height = Length.Percent(h * 62f);
                _logo.Add(bar);
            }
        }

        void LoadState()
        {
            _text.SetValueWithoutNotify(string.IsNullOrEmpty(User.text) ? TTSLanguage.English.SampleText() : User.text);
            _fileName.SetValueWithoutNotify(User.fileName);
            _rate.SetValueWithoutNotify(User.voice.rate);
            _pitch.SetValueWithoutNotify(User.voice.pitch);
            _volume.SetValueWithoutNotify(User.voice.volume);
            _useTemplate.SetValueWithoutNotify(Project.UseNamingTemplate);
            _template.SetValueWithoutNotify(Project.NamingTemplate);
            _byLanguage.SetValueWithoutNotify(Project.OrganizeByLanguage);
            _autoAssign.SetValueWithoutNotify(User.autoAssign);
            _trimSilence.SetValueWithoutNotify(Project.TrimSilence);
            _recordLines.SetValueWithoutNotify(Project.RecordPlayModeLines);
            if (!string.IsNullOrEmpty(User.lastClipGuid))
                _lastClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(User.lastClipGuid));
        }

        void WireEvents()
        {
            _uiEn.clicked += () => L.Current = UILanguage.English;
            _uiEs.clicked += () => L.Current = UILanguage.Spanish;
            _uiPt.clicked += () => L.Current = UILanguage.Portuguese;
            _root.Q("status").RegisterCallback<ClickEvent>(_ =>
            {
                if (LocalTTS.State == TTSEngineState.NotStarted || LocalTTS.State == TTSEngineState.Error) RefreshVoices();
            });

            // Text
            OnChange(_text, e =>
            {
                User.text = e.newValue;
                MarkDirty();
                UpdateTextInfo();
                UpdateOutputControls();
            });
            _text.RegisterCallback<KeyDownEvent>(e =>
            {
                if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && e.actionKey)
                {
                    Preview();
                    e.StopPropagation();
                }
            }, TrickleDown.TrickleDown);
            _btnSample.clicked += () => _text.value = SampleTextForCurrentVoice();

            // Voice
            _btnRefresh.clicked += RefreshVoices;
            OnChange(_languageFilter, _ => OnLanguageFilterChanged());
            OnChange(_voice, _ => OnVoiceChanged());
            OnChange(_rate, e => { User.voice.rate = e.newValue; MarkDirty(); UpdateVoiceInfo(); });
            OnChange(_pitch, e => { User.voice.pitch = e.newValue; MarkDirty(); UpdateVoiceInfo(); });
            OnChange(_volume, e => { User.voice.volume = e.newValue; MarkDirty(); UpdateVoiceInfo(); });
            OnChange(_profile, e => LoadProfile(e.newValue as TTSVoiceSettings));
            _btnSaveProfile.clicked += SaveProfile;
            _btnResetVoice.clicked += () =>
            {
                _rate.value = 0;
                _pitch.value = 0;
                _volume.value = 100;
            };

            // Output
            _btnFolder.clicked += ChooseFolder;
            _btnOpenFolder.clicked += () => TTSEditorUtility.PingFolder(Project.OutputFolder);
            OnChange(_fileName, e => { User.fileName = e.newValue; MarkDirty(); UpdateOutputControls(); });
            OnChange(_useTemplate, e => { Project.UseNamingTemplate = e.newValue; UpdateOutputControls(); });
            OnChange(_template, e => { Project.NamingTemplate = e.newValue; UpdateOutputControls(); });
            OnChange(_byLanguage, e => { Project.OrganizeByLanguage = e.newValue; UpdateOutputControls(); });
            OnChange(_autoAssign, e => { User.autoAssign = e.newValue; MarkDirty(); });

            // Last clip
            _btnResultPlay.clicked += () => { if (_lastClip != null) TTSEditorAudio.Play(_lastClip); };
            _btnResultPing.clicked += () => Ping(_lastClip);
            _btnResultAssign.clicked += () => Assign(_lastClip, LastClipText());
            MakeDraggable(_resultCard, () => _lastClip);

            // Batch
            OnChange(_batchFile, _ => ParseBatchFile());
            _btnBatch.clicked += RunBatch;
            _btnBatchCancel.clicked += () => _batchCancel?.Cancel();

            // History and tools
            _btnClearHistory.clicked += () =>
            {
                User.history.Clear();
                User.SaveNow();
                RebuildHistory();
            };
            OnChange(_trimSilence, e => Project.TrimSilence = e.newValue);
            OnChange(_recordLines, e => Project.RecordPlayModeLines = e.newValue);
            _btnLibrary.clicked += TTSMenus.OpenLibrary;
            _btnBakeScenes.clicked += TTSMenus.BakeSpeakersInOpenScenes;
            _btnDocs.clicked += TTSMenus.OpenDocumentation;
            _btnDeleteAll.clicked += DeleteAllGenerated;

            // Actions
            _btnPreview.clicked += Preview;
            _btnStop.clicked += () =>
            {
                TTSEditorAudio.Stop();
                SetStatus("");
            };
            _btnGenerate.clicked += Generate;
        }

        // ------------------------------------------------------------------ Texts

        void ApplyTexts()
        {
            if (_root == null || _text == null) return;
            foreach (var (button, lang) in new[] { (_uiEn, UILanguage.English), (_uiEs, UILanguage.Spanish), (_uiPt, UILanguage.Portuguese) })
                button.EnableInClassList("tts-active", L.Current == lang);

            _subtitle.text = L.T("Offline text-to-speech · Windows voices", "Texto a voz offline · voces de Windows", "Texto para fala offline · vozes do Windows");
            _root.Q<Label>("h-text").text = L.T("TEXT", "TEXTO", "TEXTO");
            _root.Q<Label>("h-voice").text = L.T("VOICE", "VOZ", "VOZ");
            _root.Q<Label>("h-output").text = L.T("OUTPUT", "SALIDA", "SAÍDA");
            _root.Q<Label>("h-result").text = L.T("LAST CLIP", "ÚLTIMO CLIP", "ÚLTIMO CLIPE");
            _btnSample.text = L.T("Sample text", "Texto de ejemplo", "Texto de exemplo");
            _textHint.text = L.T("Ctrl+Enter to preview. Text that starts with <speak> is read as SSML.",
                "Ctrl+Enter para escuchar. Un texto que empieza con <speak> se lee como SSML.",
                "Ctrl+Enter para ouvir. Um texto que começa com <speak> é lido como SSML.");
            _btnRefresh.tooltip = L.T("Reload installed voices", "Recargar voces instaladas", "Recarregar vozes instaladas");
            _rate.label = L.T("Rate", "Velocidad", "Velocidade");
            _pitch.label = L.T("Pitch", "Tono", "Tom");
            _volume.label = L.T("Volume", "Volumen", "Volume");
            _profile.label = L.T("Voice profile", "Perfil de voz", "Perfil de voz");
            _profile.tooltip = L.T("Load the settings of a Voice Profile asset.", "Carga los ajustes de un asset Voice Profile.", "Carrega os ajustes de um asset Voice Profile.");
            _btnSaveProfile.text = L.T("Save as profile…", "Guardar como perfil…", "Salvar como perfil…");
            _btnResetVoice.text = L.T("Reset", "Restablecer", "Redefinir");
            _voiceHint.text = L.T("Missing a language? Windows Settings > Time & language > Speech > Add voices, then press ↻.",
                "¿Falta un idioma? Configuración de Windows > Hora e idioma > Voz > Agregar voces, y luego pulsa ↻.",
                "Falta um idioma? Configurações do Windows > Hora e idioma > Fala > Adicionar vozes e depois clique em ↻.");

            _folder.label = L.T("Folder", "Carpeta", "Pasta");
            _btnFolder.tooltip = L.T("Choose the output folder", "Elegir la carpeta de salida", "Escolher a pasta de saída");
            _btnOpenFolder.text = L.T("Show", "Mostrar", "Mostrar");
            _fileName.label = L.T("File name", "Nombre", "Nome do arquivo");
            _fileName.tooltip = L.T("Optional. Empty = taken from the text. A name that already exists is replaced and keeps its references.",
                "Opcional. Vacío = se toma del texto. Si el nombre ya existe se reemplaza y conserva sus referencias.",
                "Opcional. Vazio = vem do texto. Um nome que já existe é substituído e mantém as referências.");
            _useTemplate.label = L.T("Naming template", "Plantilla de nombre", "Modelo de nome");
            _template.label = " ";
            _template.tooltip = "{lang} {id} {name} {voice}";
            _byLanguage.label = L.T("Sub-folder per language", "Subcarpeta por idioma", "Subpasta por idioma");
            _autoAssign.label = L.T("Assign to the selected object", "Asignar al objeto seleccionado", "Atribuir ao objeto selecionado");
            _autoAssign.tooltip = L.T("After generating, put the clip on the selected TTS Speaker (as a line), TTS Audio Player or Audio Source.",
                "Al generar, pone el clip en el TTS Speaker (como línea), TTS Audio Player o Audio Source seleccionado.",
                "Ao gerar, coloca o clipe no TTS Speaker (como fala), TTS Audio Player ou Audio Source selecionado.");

            _btnResultPlay.text = "▶ " + L.T("Play", "Reproducir", "Tocar");
            _btnResultPing.text = L.T("Show in Project", "Ver en Project", "Ver no Project");
            _btnResultAssign.text = L.T("Assign to selection", "Asignar a la selección", "Atribuir à seleção");
            _resultCard.tooltip = L.T("Drag this card onto any AudioClip field or object.", "Arrastra esta tarjeta a cualquier campo AudioClip u objeto.", "Arraste este cartão para qualquer campo AudioClip ou objeto.");

            _batch.text = L.T("Batch generation", "Generación por lotes", "Geração em lote");
            _batchFile.label = L.T("Text file", "Archivo de texto", "Arquivo de texto");
            _batchHint.text = L.T(".txt: one clip per line. .csv: text,file name (quotes allowed; , or ;). Lines starting with # or // are skipped.",
                ".txt: un clip por línea. .csv: texto,nombre (admite comillas; , o ;). Las líneas que empiezan con # o // se omiten.",
                ".txt: um clipe por linha. .csv: texto,nome (aceita aspas; , ou ;). Linhas que começam com # ou // são ignoradas.");
            _btnBatchCancel.text = L.T("Cancel", "Cancelar", "Cancelar");

            _history.text = L.T("History", "Historial", "Histórico") + (User.history.Count > 0 ? $" ({User.history.Count})" : "");
            _historyEmpty.text = L.T("Generated clips appear here. Drag them onto any AudioClip field.",
                "Aquí aparecen los clips generados. Arrástralos a cualquier campo AudioClip.",
                "Os clipes gerados aparecem aqui. Arraste-os para qualquer campo AudioClip.");
            _btnClearHistory.text = L.T("Clear history", "Limpiar historial", "Limpar histórico");

            _tools.text = L.T("Tools", "Herramientas", "Ferramentas");
            _trimSilence.label = L.T("Trim silence", "Recortar silencio", "Cortar silêncio");
            _trimSilence.tooltip = L.T("Cut the pause Windows voices add before and after speech.", "Quita la pausa que las voces de Windows agregan antes y después.", "Remove a pausa que as vozes do Windows adicionam antes e depois.");
            _recordLines.label = L.T("Record Play Mode lines", "Registrar líneas de Play Mode", "Registrar falas do Play Mode");
            _recordLines.tooltip = L.T("Lines spoken from code in Play Mode are added to the Line Library, ready to bake for other platforms.",
                "Las líneas habladas desde código en Play Mode se agregan a la Line Library, listas para hornear para otras plataformas.",
                "As falas ditas por código no Play Mode são adicionadas à Line Library, prontas para gerar para outras plataformas.");
            _btnLibrary.text = "Line Library";
            _btnBakeScenes.text = L.T("Bake speakers in open scenes", "Hornear speakers de las escenas abiertas", "Gerar speakers das cenas abertas");
            _btnDocs.text = L.T("Documentation", "Documentación", "Documentação");
            _btnDeleteAll.text = L.T("Delete generated audio…", "Borrar audio generado…", "Apagar áudio gerado…");

            _btnPreview.text = "▶  " + L.T("Preview", "Escuchar", "Ouvir");
            _btnPreview.tooltip = L.T("Plays from memory: nothing is saved (Ctrl+Enter).", "Suena desde memoria: no se guarda nada (Ctrl+Enter).", "Toca da memória: nada é salvo (Ctrl+Enter).");
            _btnStop.text = "■";
            _btnStop.tooltip = L.T("Stop", "Detener", "Parar");
            _btnGenerate.text = L.T("Generate .wav", "Generar .wav", "Gerar .wav");
            _btnGenerate.tooltip = L.T("Save the line as an audio asset that plays on every platform.",
                "Guarda la línea como asset de audio que suena en cualquier plataforma.",
                "Salva a fala como um asset de áudio que toca em qualquer plataforma.");
            _footer.text = $"Local TTS {LocalTTS.Version} · " + L.T("Developed by KrostGames · 2026", "Desarrollado por KrostGames · 2026", "Desenvolvido por KrostGames · 2026");

            RefreshVoiceControls();
            UpdateTextInfo();
            UpdateOutputControls();
            UpdateResult();
            ParseBatchFile();
            RebuildHistory();
            RefreshToolsInfo();
            OnEngineStateChanged();
        }

        // ------------------------------------------------------------------ Engine and voices

        void OnEngineStateChanged()
        {
            if (_statusLabel == null) return;
            TTSEngineState state = LocalTTS.State;
            _statusDot.EnableInClassList("tts-ready", state == TTSEngineState.Ready);
            _statusDot.EnableInClassList("tts-starting", state == TTSEngineState.Starting);
            _statusDot.EnableInClassList("tts-error", state == TTSEngineState.Error);

            switch (state)
            {
                case TTSEngineState.Ready:
                    _statusLabel.text = L.T("Ready", "Listo", "Pronto") + $" · {LocalTTS.Voices.Count} " + L.T("voices", "voces", "vozes");
                    break;
                case TTSEngineState.Starting:
                    _statusLabel.text = L.T("Starting…", "Iniciando…", "Iniciando…");
                    break;
                case TTSEngineState.Error:
                    _statusLabel.text = L.T("Error", "Error", "Erro");
                    break;
                case TTSEngineState.Unsupported:
                    _statusLabel.text = L.T("Windows only", "Solo Windows", "Só Windows");
                    break;
                default:
                    _statusLabel.text = L.T("Idle", "En espera", "Inativo");
                    break;
            }
            _root.Q("status").tooltip = state == TTSEngineState.Error ? LocalTTS.LastError : "";

            string note = null;
            if (state == TTSEngineState.Unsupported)
                note = L.T("Speech synthesis needs the Windows Editor. Clips generated on Windows play on every platform.",
                    "La síntesis de voz necesita el Editor de Windows. Los clips generados en Windows suenan en cualquier plataforma.",
                    "A síntese de voz precisa do Editor do Windows. Os clipes gerados no Windows tocam em qualquer plataforma.");
            else if (state == TTSEngineState.Error)
                note = L.T("The speech engine reported a problem: ", "El motor de voz informó un problema: ", "O motor de voz informou um problema: ") + LocalTTS.LastError;
            _platformNote.text = note ?? "";
            _platformNote.style.display = note == null ? DisplayStyle.None : DisplayStyle.Flex;

            RefreshVoiceControls();
            UpdateButtons();
        }

        void RefreshVoices()
        {
            if (!LocalTTS.IsRealtimeSupported) return;
            ObserveVoices();
        }

        async void ObserveVoices()
        {
            try { await LocalTTS.GetVoicesAsync(true); }
            catch (Exception e) { SetStatus(e.Message, true); }
        }

        void RefreshVoiceControls()
        {
            if (_voice == null) return;
            IReadOnlyList<TTSVoice> voices = LocalTTS.Voices;

            _languageCodes.Clear();
            _languageCodes.Add("");
            foreach (string code in voices.Select(v => v.LanguageCode).Distinct().OrderBy(c => c)) _languageCodes.Add(code);
            if (!_languageCodes.Contains(User.languageFilter)) User.languageFilter = "";

            var filterChoices = _languageCodes.Select(code => code.Length == 0
                ? L.T("All languages", "Todos los idiomas", "Todos os idiomas") + $" ({voices.Count})"
                : $"{TTSEditorUtility.LanguageName(code)} ({voices.Count(v => v.LanguageCode == code)})").ToList();
            _languageFilter.choices = filterChoices;
            _languageFilter.SetValueWithoutNotify(filterChoices[_languageCodes.IndexOf(User.languageFilter)]);

            _listedVoices.Clear();
            _listedVoices.AddRange(voices.Where(v => User.languageFilter.Length == 0 || v.LanguageCode == User.languageFilter));

            if (_listedVoices.Count == 0)
            {
                string empty = LocalTTS.State == TTSEngineState.Starting || LocalTTS.State == TTSEngineState.NotStarted
                    ? L.T("Loading voices…", "Cargando voces…", "Carregando vozes…")
                    : L.T("No voices installed", "No hay voces instaladas", "Nenhuma voz instalada");
                _voice.choices = new List<string> { empty };
                _voice.SetValueWithoutNotify(empty);
                _voice.SetEnabled(false);
            }
            else
            {
                _voice.choices = _listedVoices.Select(VoiceLabel).ToList();
                _voice.SetEnabled(true);
                TTSVoice current = _listedVoices.FirstOrDefault(v => string.Equals(v.DisplayName, User.voice.voiceName, StringComparison.OrdinalIgnoreCase))
                                   ?? _listedVoices.FirstOrDefault(v => v.Matches(User.voice.language))
                                   ?? _listedVoices[0];
                SelectVoice(current, false);
                _voice.SetValueWithoutNotify(VoiceLabel(current));
            }
            UpdateVoiceInfo();
        }

        void OnLanguageFilterChanged()
        {
            int index = _languageFilter.choices.IndexOf(_languageFilter.value);
            User.languageFilter = index >= 0 && index < _languageCodes.Count ? _languageCodes[index] : "";
            MarkDirty();
            RefreshVoiceControls();
        }

        void OnVoiceChanged()
        {
            int index = _voice.choices.IndexOf(_voice.value);
            if (index < 0 || index >= _listedVoices.Count) return;
            SelectVoice(_listedVoices[index], true);
            UpdateVoiceInfo();
            UpdateOutputControls();
        }

        void SelectVoice(TTSVoice voice, bool userChoice)
        {
            if (voice == null) return;
            bool changed = User.voice.voiceName != voice.DisplayName;
            User.voice.voiceName = voice.DisplayName;
            if (TTSLanguageExtensions.TryFromCulture(voice.Culture, out TTSLanguage language)) User.voice.language = language;
            if (changed || userChoice) MarkDirty();
        }

        void UpdateVoiceInfo()
        {
            if (_voiceInfo == null) return;
            TTSVoice voice = LocalTTS.ResolveVoice(User.voice);
            if (voice == null)
            {
                _voiceInfo.text = "";
                return;
            }
            string engine = voice.Engine == TTSEngineKind.OneCore ? "OneCore" : "SAPI 5";
            _voiceInfo.text = $"{voice.Culture} · {GenderLabel(voice.Gender)} · {engine}";
        }

        static string VoiceLabel(TTSVoice voice) => $"{voice.DisplayName}  ·  {voice.Culture}";

        static string GenderLabel(string gender)
        {
            switch (gender)
            {
                case "Female": return L.T("Female", "Femenina", "Feminina");
                case "Male": return L.T("Male", "Masculina", "Masculina");
                default: return gender;
            }
        }

        string SampleTextForCurrentVoice()
        {
            TTSVoice voice = LocalTTS.ResolveVoice(User.voice);
            return voice != null && TTSLanguageExtensions.TryFromCulture(voice.Culture, out TTSLanguage language)
                ? language.SampleText()
                : User.voice.language.SampleText();
        }

        // ------------------------------------------------------------------ Profiles

        void LoadProfile(TTSVoiceSettings profile)
        {
            if (profile == null) return;
            User.voice.CopyFrom(profile.ToOptions());
            TTSVoice resolved = LocalTTS.ResolveVoice(User.voice);
            if (resolved != null)
            {
                User.voice.voiceName = resolved.DisplayName;
                User.languageFilter = "";
            }
            _rate.SetValueWithoutNotify(User.voice.rate);
            _pitch.SetValueWithoutNotify(User.voice.pitch);
            _volume.SetValueWithoutNotify(User.voice.volume);
            MarkDirty();
            RefreshVoiceControls();
            SetStatus(L.T("Loaded profile ", "Perfil cargado: ", "Perfil carregado: ") + profile.name);
        }

        void SaveProfile()
        {
            string folder = AssetDatabase.IsValidFolder(TTSEditorUtility.PackageRoot) && !TTSEditorUtility.IsReadOnlyPackage ? TTSEditorUtility.PackageRoot : "Assets";
            string path = EditorUtility.SaveFilePanelInProject(L.T("Save voice profile", "Guardar perfil de voz", "Salvar perfil de voz"),
                "TTS Voice Profile", "asset", L.T("Name of the voice profile", "Nombre del perfil de voz", "Nome do perfil de voz"), folder);
            if (string.IsNullOrEmpty(path)) return;
            var profile = CreateInstance<TTSVoiceSettings>();
            profile.voiceName = User.voice.voiceName;
            profile.language = User.voice.language;
            profile.rate = User.voice.rate;
            profile.pitch = User.voice.pitch;
            profile.volume = User.voice.volume;
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            _profile.SetValueWithoutNotify(profile);
            EditorGUIUtility.PingObject(profile);
        }

        // ------------------------------------------------------------------ Output

        void UpdateTextInfo()
        {
            if (_charCount == null) return;
            _charCount.text = $"{_text.value.Length} " + L.T("chars", "caracteres", "caracteres");
        }

        void UpdateOutputControls()
        {
            if (_folder == null) return;
            _folder.SetValueWithoutNotify(ShortPath(Project.OutputFolder));
            _folder.tooltip = Project.OutputFolder;
            _template.style.display = Project.UseNamingTemplate ? DisplayStyle.Flex : DisplayStyle.None;

            TTSVoice voice = LocalTTS.ResolveVoice(User.voice);
            string name = BuildFileName(_text.value, voice, false) ?? TTSEditorUtility.Slug(_text.value);
            string folder = Project.OrganizeByLanguage && voice != null ? $"{Project.OutputFolder}/{voice.LanguageCode}" : Project.OutputFolder;
            _namePreview.text = "→ " + ShortPath(folder) + "/" + name + ".wav";
            UpdateAssignTargets();
        }

        string BuildFileName(string text, TTSVoice voice, bool consumeId)
        {
            string typed = TTSEditorUtility.CleanFileName(User.fileName);
            if (!Project.UseNamingTemplate) return typed.Length > 0 ? typed : null;
            string name = typed.Length > 0 ? typed : TTSEditorUtility.Slug(text);
            int id = consumeId ? Project.TakeNextId() : Project.PeekNextId();
            return TTSEditorUtility.ApplyTemplate(Project.NamingTemplate, voice?.LanguageCode ?? User.voice.language.Code(), id, name, voice?.Name ?? User.voice.voiceName);
        }

        void ChooseFolder()
        {
            string start = Directory.Exists(TTSEditorUtility.ToAbsolute(Project.OutputFolder)) ? TTSEditorUtility.ToAbsolute(Project.OutputFolder) : Application.dataPath;
            string picked = EditorUtility.OpenFolderPanel(L.T("Output folder", "Carpeta de salida", "Pasta de saída"), start, "");
            if (string.IsNullOrEmpty(picked)) return;
            string assetPath = TTSEditorUtility.ToAssetPath(picked);
            if (assetPath == null)
            {
                EditorUtility.DisplayDialog("Local TTS", L.T("Choose a folder inside this project's Assets folder.",
                    "Elige una carpeta dentro de la carpeta Assets de este proyecto.",
                    "Escolha uma pasta dentro da pasta Assets deste projeto."), "OK");
                return;
            }
            Project.OutputFolder = assetPath;
            UpdateOutputControls();
            RefreshToolsInfo();
        }

        void UpdateAssignTargets()
        {
            if (_autoAssign == null) return;
            string target = DescribeSelectionTarget();
            _btnResultAssign.SetEnabled(target != null && _lastClip != null);
            _btnResultAssign.tooltip = target ?? L.T("Select a GameObject with a TTS Speaker, TTS Audio Player or Audio Source.",
                "Selecciona un GameObject con TTS Speaker, TTS Audio Player o Audio Source.",
                "Selecione um GameObject com TTS Speaker, TTS Audio Player ou Audio Source.");
            _autoAssign.tooltip = target != null
                ? "→ " + target
                : L.T("After generating, put the clip on the selected TTS Speaker (as a line), TTS Audio Player or Audio Source.",
                    "Al generar, pone el clip en el TTS Speaker (como línea), TTS Audio Player o Audio Source seleccionado.",
                    "Ao gerar, coloca o clipe no TTS Speaker (como fala), TTS Audio Player ou Audio Source selecionado.");
        }

        static string DescribeSelectionTarget()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null) return null;
            if (go.GetComponent<TTSSpeaker>() != null) return $"TTS Speaker · {go.name}";
            if (go.GetComponent<TTSAudioPlayer>() != null) return $"TTS Audio Player · {go.name}";
            if (go.GetComponent<AudioSource>() != null) return $"Audio Source · {go.name}";
            return null;
        }

        // ------------------------------------------------------------------ Actions

        async void Preview()
        {
            string text = _text.value;
            if (string.IsNullOrWhiteSpace(text) || !LocalTTS.IsRealtimeSupported) return;
            SetStatus(L.T("Synthesizing…", "Sintetizando…", "Sintetizando…"));
            try
            {
                await TTSEditorAudio.SpeakAsync(text, User.voice.Clone());
                SetStatus("");
            }
            catch (Exception e)
            {
                SetStatus(e.Message, true);
            }
        }

        async void Generate()
        {
            string text = _text.value;
            if (_generating || string.IsNullOrWhiteSpace(text) || !LocalTTS.IsRealtimeSupported) return;
            _generating = true;
            UpdateButtons();
            SetStatus(L.T("Generating…", "Generando…", "Gerando…"));
            try
            {
                TTSVoiceOptions voice = User.voice.Clone();
                IReadOnlyList<TTSVoice> voices = await LocalTTS.GetVoicesAsync();
                TTSVoice resolved = LocalTTS.ResolveVoice(voice, voices, false);
                AudioClip clip = await WindowsTTSEngine.GenerateAsync(text, voice, BuildFileName(text, resolved, true));
                OnClipCreated(clip, text);

                string message = L.T("Saved ", "Guardado: ", "Salvo: ") + Path.GetFileName(AssetDatabase.GetAssetPath(clip));
                if (User.autoAssign)
                {
                    string assigned = TTSEditorUtility.AssignToSelection(clip, text);
                    if (assigned != null) message += " → " + assigned;
                }
                SetStatus(message);
            }
            catch (Exception e)
            {
                SetStatus(e.Message, true);
                Debug.LogWarning("[LocalTTS] " + e.Message);
            }
            finally
            {
                _generating = false;
                UpdateButtons();
            }
        }

        void OnClipCreated(AudioClip clip, string text)
        {
            if (clip == null) return;
            _lastClip = clip;
            User.AddToHistory(clip, text);
            RebuildHistory();
            UpdateResult();
            RefreshToolsInfo();
        }

        void UpdateResult()
        {
            if (_resultCard == null) return;
            bool has = _lastClip != null;
            _resultCard.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            if (!has) return;
            _resultClip.SetValueWithoutNotify(_lastClip);
            string channels = _lastClip.channels == 1 ? "mono" : "stereo";
            _resultInfo.text = $"{_lastClip.length:F2} s · {_lastClip.frequency} Hz · {channels} · {ShortPath(AssetDatabase.GetAssetPath(_lastClip))}";
            UpdateAssignTargets();
        }

        string LastClipText()
        {
            if (_lastClip == null) return "";
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_lastClip));
            return User.history.LastOrDefault(h => h.guid == guid)?.text ?? _text.value;
        }

        void Assign(AudioClip clip, string text)
        {
            if (clip == null) return;
            string target = TTSEditorUtility.AssignToSelection(clip, text);
            SetStatus(target != null
                ? L.T("Assigned to ", "Asignado a ", "Atribuído a ") + target
                : L.T("Select a GameObject with a TTS Speaker, TTS Audio Player or Audio Source.", "Selecciona un GameObject con TTS Speaker, TTS Audio Player o Audio Source.", "Selecione um GameObject com TTS Speaker, TTS Audio Player ou Audio Source."),
                target == null);
        }

        static void Ping(UnityEngine.Object asset)
        {
            if (asset == null) return;
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        // ------------------------------------------------------------------ Batch

        void ParseBatchFile()
        {
            if (_batchInfo == null) return;
            var asset = _batchFile.value as TextAsset;
            _batchEntries = asset == null
                ? new List<WindowsTTSEngine.BatchEntry>()
                : TTSBatchParser.Parse(asset.text, AssetDatabase.GetAssetPath(asset).EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

            int named = _batchEntries.Count(e => !string.IsNullOrEmpty(e.FileName));
            _batchInfo.text = asset == null
                ? L.T("Drop a .txt or .csv file here.", "Arrastra aquí un archivo .txt o .csv.", "Arraste aqui um arquivo .txt ou .csv.")
                : $"{_batchEntries.Count} " + L.T("lines", "líneas", "linhas") + (named > 0 ? $" · {named} " + L.T("with file name", "con nombre", "com nome") : "");
            _btnBatch.text = L.T("Generate", "Generar", "Gerar") + $" {_batchEntries.Count} " + L.T("clips", "clips", "clipes");
            if (_batchCancel == null) _batchProgress.style.display = DisplayStyle.None;
            UpdateButtons();
        }

        async void RunBatch()
        {
            if (_generating || _batchEntries.Count == 0 || !LocalTTS.IsRealtimeSupported) return;
            _generating = true;
            _batchCancel = new CancellationTokenSource();
            _batchProgress.style.display = DisplayStyle.Flex;
            _batchProgress.value = 0;
            UpdateButtons();

            var entries = _batchEntries.ToList();
            TTSVoiceOptions voice = User.voice.Clone();
            try
            {
                List<WindowsTTSEngine.BatchResult> results = await WindowsTTSEngine.BatchGenerateAsync(entries, voice,
                    (entry, index, resolved) =>
                    {
                        string name = TTSEditorUtility.CleanFileName(entry.FileName);
                        if (Project.UseNamingTemplate)
                            name = TTSEditorUtility.ApplyTemplate(Project.NamingTemplate, resolved?.LanguageCode ?? "XX", Project.TakeNextId(),
                                name.Length > 0 ? name : TTSEditorUtility.Slug(entry.Text), resolved?.Name ?? "");
                        return name.Length > 0 ? name : null;
                    },
                    (done, total, text) =>
                    {
                        _batchProgress.value = total == 0 ? 100 : 100f * done / total;
                        _batchProgress.title = $"{done}/{total}";
                        SetStatus(done < total ? $"[{done + 1}/{total}] {text}" : "");
                    },
                    _batchCancel.Token);

                foreach (WindowsTTSEngine.BatchResult result in results) User.AddToHistory(result.Clip, result.Text);
                if (results.Count > 0) _lastClip = results[results.Count - 1].Clip;
                SetStatus($"{results.Count} " + L.T("clips generated", "clips generados", "clipes gerados"));
            }
            catch (OperationCanceledException)
            {
                SetStatus(L.T("Batch cancelled. Finished clips were kept.", "Lote cancelado. Se conservaron los clips terminados.", "Lote cancelado. Os clipes prontos foram mantidos."));
            }
            catch (Exception e)
            {
                SetStatus(e.Message, true);
                Debug.LogWarning("[LocalTTS] " + e.Message);
            }
            finally
            {
                _batchCancel.Dispose();
                _batchCancel = null;
                _generating = false;
                _batchProgress.style.display = DisplayStyle.None;
                RebuildHistory();
                UpdateResult();
                RefreshToolsInfo();
                UpdateButtons();
            }
        }

        // ------------------------------------------------------------------ History and tools

        void RebuildHistory()
        {
            if (_historyList == null) return;
            _historyList.Clear();
            User.history.RemoveAll(h => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(h.guid)));
            for (int i = User.history.Count - 1; i >= 0; i--)
            {
                TTSUserSettings.HistoryItem item = User.history[i];
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(item.guid));
                if (clip != null) _historyList.Add(HistoryRow(clip, item.text));
            }
            _historyEmpty.style.display = _historyList.childCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _btnClearHistory.style.display = _historyList.childCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _history.text = L.T("History", "Historial", "Histórico") + (_historyList.childCount > 0 ? $" ({_historyList.childCount})" : "");
        }

        VisualElement HistoryRow(AudioClip clip, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("tts-history-item");
            row.tooltip = text;

            string folder = Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(clip)) ?? "");
            var badge = new Label(folder != null && folder.Length == 2 ? folder.ToUpperInvariant() : "WAV");
            badge.AddToClassList("tts-badge");
            var name = new Label(clip.name);
            name.AddToClassList("tts-history-name");
            var length = new Label($"{clip.length:F1}s");
            length.AddToClassList("tts-history-len");

            row.Add(badge);
            row.Add(name);
            row.Add(length);
            row.Add(IconButton("▶", L.T("Play", "Reproducir", "Tocar"), () => TTSEditorAudio.Play(clip)));
            row.Add(IconButton("◎", L.T("Show in Project", "Ver en Project", "Ver no Project"), () => Ping(clip)));
            row.Add(IconButton("+", L.T("Assign to selection", "Asignar a la selección", "Atribuir à seleção"), () => Assign(clip, text)));
            MakeDraggable(row, () => clip);
            return row;
        }

        // Fields also receive ChangeEvents that bubble up from their children (changing a field's label sends one),
        // so only react to events the field itself sends.
        static void OnChange<T>(BaseField<T> field, EventCallback<ChangeEvent<T>> callback) =>
            field.RegisterValueChangedCallback(e =>
            {
                if (e.target == field) callback(e);
            });

        static Button IconButton(string text, string tooltip, Action action)
        {
            var button = new Button(action) { text = text, tooltip = tooltip };
            button.AddToClassList("tts-icon-btn");
            return button;
        }

        // Dragging a history row (or the last-clip card) drops the clip on any AudioClip field, AudioSource or object.
        static void MakeDraggable(VisualElement element, Func<AudioClip> clip)
        {
            Vector2 start = Vector2.zero;
            bool armed = false;
            element.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button != 0 || e.target is Button) return;
                start = e.mousePosition;
                armed = true;
            });
            element.RegisterCallback<MouseUpEvent>(_ => armed = false);
            element.RegisterCallback<MouseLeaveEvent>(_ => armed = false);
            element.RegisterCallback<MouseMoveEvent>(e =>
            {
                if (!armed || (e.mousePosition - start).sqrMagnitude < 36f) return;
                armed = false;
                AudioClip c = clip();
                if (c == null) return;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new UnityEngine.Object[] { c };
                DragAndDrop.paths = new[] { AssetDatabase.GetAssetPath(c) };
                DragAndDrop.StartDrag(c.name);
            });
        }

        void OnProjectChanged()
        {
            if (_lastClip == null && !string.IsNullOrEmpty(User.lastClipGuid))
                _lastClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(User.lastClipGuid));
            RebuildHistory();
            UpdateResult();
            RefreshToolsInfo();
        }

        void RefreshToolsInfo()
        {
            if (_toolsInfo == null) return;
            int clips = TTSEditorUtility.FindGeneratedClips().Length;
            TTSLibrary library = TTSEditorUtility.GetLibrary(false);
            string libraryText = library == null
                ? L.T("Line Library: not created yet", "Line Library: aún no creada", "Line Library: ainda não criada")
                : $"Line Library: {library.Entries.Count} " + L.T("lines", "líneas", "linhas") + $", {library.Entries.Count(e => e != null && !e.IsBaked)} " + L.T("to bake", "por hornear", "para gerar");
            _toolsInfo.text = $"{clips} " + L.T("clips in ", "clips en ", "clipes em ") + ShortPath(Project.OutputFolder) + "\n" + libraryText;
            _btnDeleteAll.SetEnabled(clips > 0);
        }

        void DeleteAllGenerated()
        {
            int count = TTSEditorUtility.FindGeneratedClips().Length;
            if (count == 0) return;
            if (!EditorUtility.DisplayDialog("Local TTS",
                    L.T($"Move the {count} clip(s) in {Project.OutputFolder} to the trash?\nObjects that use them will lose their clip.",
                        $"¿Mover a la papelera los {count} clip(s) de {Project.OutputFolder}?\nLos objetos que los usan perderán su clip.",
                        $"Mover para a lixeira os {count} clipe(s) de {Project.OutputFolder}?\nOs objetos que os usam perderão o clipe."),
                    L.T("Move to trash", "Mover a la papelera", "Mover para a lixeira"), L.T("Cancel", "Cancelar", "Cancelar")))
                return;
            int moved = TTSEditorUtility.TrashGeneratedClips();
            _lastClip = null;
            User.history.Clear();
            User.SaveNow();
            RebuildHistory();
            UpdateResult();
            RefreshToolsInfo();
            SetStatus($"{moved} " + L.T("clips moved to the trash", "clips movidos a la papelera", "clipes movidos para a lixeira"));
        }

        // ------------------------------------------------------------------ Frame update

        void Tick()
        {
            UpdateButtons();
            AnimateLogo();
            if (_settingsDirty && EditorApplication.timeSinceStartup - _dirtySince > 1.0) SaveUserSettings();
        }

        void UpdateButtons()
        {
            if (_btnPreview == null) return;
            bool supported = LocalTTS.IsRealtimeSupported;
            bool hasText = !string.IsNullOrWhiteSpace(_text.value);
            _btnPreview.SetEnabled(supported && hasText && !TTSEditorAudio.IsPreparing);
            _btnStop.SetEnabled(TTSEditorAudio.IsBusy);
            _btnGenerate.SetEnabled(supported && hasText && !_generating);
            _btnBatch.SetEnabled(supported && !_generating && _batchEntries.Count > 0);
            _btnBatchCancel.style.display = _batchCancel != null ? DisplayStyle.Flex : DisplayStyle.None;
            _btnRefresh.SetEnabled(supported && LocalTTS.State != TTSEngineState.Starting);
            _btnBakeScenes.SetEnabled(supported);

            string text = _status;
            bool error = _statusIsError;
            if (string.IsNullOrEmpty(text))
            {
                if (TTSEditorAudio.IsPreparing) text = L.T("Synthesizing…", "Sintetizando…", "Sintetizando…");
                else if (TTSEditorAudio.IsPlaying) text = L.T("Playing", "Reproduciendo", "Tocando");
            }
            _actionStatus.text = text;
            _actionStatus.tooltip = text;
            _actionStatus.EnableInClassList("tts-error-text", error);
        }

        void AnimateLogo()
        {
            if (_logo == null) return;
            bool playing = TTSEditorAudio.IsPlaying;
            float[] rest = { 0.35f, 0.7f, 1f, 0.6f, 0.3f };
            double t = EditorApplication.timeSinceStartup;
            for (int i = 0; i < _logo.childCount && i < rest.Length; i++)
            {
                float h = playing ? 0.25f + 0.75f * Mathf.Abs(Mathf.Sin((float)(t * (5.0 + i * 1.7) + i))) : rest[i];
                _logo[i].style.height = Length.Percent(h * 62f);
            }
        }

        void SetStatus(string text, bool error = false)
        {
            _status = text ?? "";
            _statusIsError = error;
            UpdateButtons();
        }

        void MarkDirty()
        {
            if (!_settingsDirty) _dirtySince = EditorApplication.timeSinceStartup;
            _settingsDirty = true;
        }

        void SaveUserSettings()
        {
            if (!_settingsDirty) return;
            _settingsDirty = false;
            User.SaveNow();
        }

        static string ShortPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string root = TTSEditorUtility.PackageRoot;
            return path.StartsWith(root + "/", StringComparison.Ordinal) ? path.Substring(root.Length + 1) : path;
        }

        static Texture Icon(string name)
        {
            Texture dark = EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_" + name).image : null;
            return dark != null ? dark : EditorGUIUtility.IconContent(name).image;
        }
    }
}
