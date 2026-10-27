using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using L = KrostGames.TTS.EditorTools.TTSLocalization;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>
    /// TTS Speaker inspector: voice, lines with per-line preview and bake, playback options, events,
    /// and a note on what the active build target will do with each line.
    /// </summary>
    [CustomEditor(typeof(TTSSpeaker))]
    public class TTSSpeakerEditor : UnityEditor.Editor
    {
        TTSSpeaker Speaker => (TTSSpeaker)target;

        Label _banner, _linesInfo;
        VisualElement _inlineVoice;
        Label _profileInfo;
        ListView _list;
        Button _bakeMissing, _bakeAll, _speakNext, _stop;
        bool _baking;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            TTSVoiceUI.ApplyStyle(root);
            root.AddToClassList("tts-inspector");

            _banner = new Label();
            _banner.AddToClassList("tts-banner");
            root.Add(_banner);

            // Voice
            root.Add(TTSVoiceUI.Section(L.T("Voice", "Voz", "Voz"), out _));
            root.Add(new PropertyField(serializedObject.FindProperty("_voiceProfile"), L.T("Voice Profile", "Perfil de voz", "Perfil de voz")));
            _profileInfo = new Label();
            _profileInfo.AddToClassList("tts-muted");
            root.Add(_profileInfo);
            _inlineVoice = TTSVoiceUI.Create(serializedObject, "_voice.", FirstLineText);
            root.Add(_inlineVoice);

            // Lines
            root.Add(TTSVoiceUI.Section(L.T("Lines", "Líneas", "Falas"), out _linesInfo));
            _list = new ListView
            {
                bindingPath = "_lines",
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.Single,
                makeItem = () => new LineRow(this),
                bindItem = (element, index) => ((LineRow)element).Bind(index),
                unbindItem = (element, _) => ((LineRow)element).Unbind()
            };
            _list.AddToClassList("tts-lines");
            root.Add(_list);

            var lineTools = new VisualElement();
            lineTools.AddToClassList("tts-row");
            lineTools.AddToClassList("tts-wrap");
            _bakeMissing = SmallButton(L.T("Bake missing", "Hornear faltantes", "Gerar faltantes"), () => Bake(true), true);
            _bakeMissing.tooltip = L.T("Create .wav clips for lines that have none, or whose text or voice changed. Builds without real-time speech play these clips.",
                "Crea clips .wav para las líneas que no tienen o cuyo texto o voz cambió. Las builds sin voz en tiempo real usan estos clips.",
                "Cria clipes .wav para as falas sem clipe ou cujo texto ou voz mudou. Builds sem voz em tempo real tocam esses clipes.");
            _bakeAll = SmallButton(L.T("Re-bake all", "Re-hornear todo", "Gerar tudo de novo"), () => Bake(false));
            lineTools.Add(_bakeMissing);
            lineTools.Add(_bakeAll);
            lineTools.Add(SmallButton(L.T("Import .txt/.csv…", "Importar .txt/.csv…", "Importar .txt/.csv…"), ImportLines));
            lineTools.Add(SmallButton(L.T("Clear clips", "Quitar clips", "Remover clipes"), ClearClips));
            root.Add(lineTools);

            // Playback
            root.Add(TTSVoiceUI.Section(L.T("Playback", "Reproducción", "Reprodução"), out _));
            root.Add(new PropertyField(serializedObject.FindProperty("_audioMode"), L.T("Audio", "Audio", "Áudio")));
            root.Add(new PropertyField(serializedObject.FindProperty("_whenBusy"), L.T("When busy", "Si está hablando", "Se estiver falando")));
            root.Add(new PropertyField(serializedObject.FindProperty("_speakOnStart"), L.T("Speak on Start", "Hablar al iniciar", "Falar ao iniciar")));
            root.Add(new PropertyField(serializedObject.FindProperty("_startLine"), L.T("Start line", "Línea inicial", "Fala inicial")));
            root.Add(new PropertyField(serializedObject.FindProperty("_prepareOnStart"), L.T("Prepare on Start", "Preparar al iniciar", "Preparar ao iniciar")));

            var events = new Foldout { text = L.T("Events", "Eventos", "Eventos"), value = false };
            events.Add(new PropertyField(serializedObject.FindProperty("_onSpeechStarted"), "On Speech Started (string)"));
            events.Add(new PropertyField(serializedObject.FindProperty("_onSpeechFinished"), "On Speech Finished (string)"));
            root.Add(events);

            // Test bar
            var bar = new VisualElement();
            bar.AddToClassList("tts-test-bar");
            _speakNext = new Button(SpeakNext);
            _speakNext.AddToClassList("tts-primary");
            _stop = new Button(() =>
            {
                if (Application.isPlaying) Speaker.Stop();
                TTSEditorAudio.Stop();
            }) { text = "■ " + L.T("Stop", "Detener", "Parar") };
            bar.Add(_speakNext);
            bar.Add(_stop);
            root.Add(bar);

            root.schedule.Execute(UpdateDynamic).Every(250);
            UpdateDynamic();
            return root;
        }

        Button SmallButton(string text, Action action, bool primary = false)
        {
            var button = new Button(action) { text = text };
            button.AddToClassList("tts-small-btn");
            if (primary) button.AddToClassList("tts-primary");
            return button;
        }

        string FirstLineText() => Speaker.Lines.FirstOrDefault(l => l != null && !string.IsNullOrWhiteSpace(l.text))?.text;

        // ------------------------------------------------------------------ Dynamic parts

        void UpdateDynamic()
        {
            if (target == null || _banner == null) return;
            TTSSpeaker speaker = Speaker;
            TTSVoiceOptions voice = speaker.Voice;
            bool hasProfile = speaker.VoiceProfile != null;

            _inlineVoice.style.display = hasProfile ? DisplayStyle.None : DisplayStyle.Flex;
            _profileInfo.style.display = hasProfile ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasProfile) _profileInfo.text = "→ " + voice;

            int total = 0, baked = 0, outdated = 0;
            foreach (TTSLine line in speaker.Lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;
                total++;
                if (line.IsBakedFor(voice)) baked++;
                else if (line.clip != null) outdated++;
            }
            _linesInfo.text = total == 0 ? "" : $"{baked}/{total} " + L.T("baked", "horneadas", "geradas") + (outdated > 0 ? $" · {outdated} " + L.T("outdated", "desactualizadas", "desatualizadas") : "");

            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            bool buildRealtime = TTSEditorUtility.BuildTargetHasRealtime(buildTarget);
            int silent = total - baked - outdated;
            string editor = LocalTTS.IsRealtimeSupported
                ? L.T("In this Editor every line plays in real time, no bake needed.", "En este Editor todas las líneas suenan en tiempo real, sin hornear.", "Neste Editor todas as falas tocam em tempo real, sem gerar.")
                : L.T("Real-time speech needs the Windows Editor; baked lines play here.", "La voz en tiempo real necesita el Editor de Windows; aquí suenan las líneas horneadas.", "A voz em tempo real precisa do Editor do Windows; aqui tocam as falas geradas.");
            string build;
            if (buildRealtime)
                build = L.T("Windows builds also speak in real time.", "Las builds de Windows también hablan en tiempo real.", "Builds para Windows também falam em tempo real.");
            else if (silent > 0 && speaker.Mode != TTSSpeaker.AudioMode.RealtimeOnly)
                build = string.Format(L.T("{0} builds have no real-time speech: {1} line(s) without a clip will be silent. Press Bake missing.",
                    "Las builds de {0} no tienen voz en tiempo real: {1} línea(s) sin clip quedarán en silencio. Pulsa Hornear faltantes.",
                    "Builds de {0} não têm voz em tempo real: {1} fala(s) sem clipe ficarão em silêncio. Clique em Gerar faltantes."), buildTarget, silent);
            else if (speaker.Mode == TTSSpeaker.AudioMode.RealtimeOnly)
                build = string.Format(L.T("Audio is set to Realtime Only, so {0} builds will be silent. Use Auto to play baked clips there.",
                    "Audio está en Realtime Only: las builds de {0} quedarán en silencio. Usa Auto para que suenen los clips horneados.",
                    "Áudio está em Realtime Only: builds de {0} ficarão em silêncio. Use Auto para tocar os clipes gerados."), buildTarget);
            else
                build = string.Format(L.T("{0} builds play the baked clips.", "Las builds de {0} usan los clips horneados.", "Builds de {0} tocam os clipes gerados."), buildTarget);
            _banner.text = editor + " " + build;
            _banner.EnableInClassList("tts-banner-warn", !buildRealtime && (silent > 0 || speaker.Mode == TTSSpeaker.AudioMode.RealtimeOnly));

            bool canBake = LocalTTS.IsRealtimeSupported && !_baking;
            _bakeMissing.SetEnabled(canBake && total > baked);
            _bakeAll.SetEnabled(canBake && total > 0);
            _bakeMissing.text = _baking ? L.T("Baking…", "Horneando…", "Gerando…") : L.T("Bake missing", "Hornear faltantes", "Gerar faltantes");

            bool playing = Application.isPlaying;
            _speakNext.text = playing ? "▶ " + L.T("Speak next line", "Decir la siguiente línea", "Falar a próxima fala") : "▶ " + L.T("Preview first line", "Escuchar la primera línea", "Ouvir a primeira fala");
            _speakNext.SetEnabled(total > 0 && (playing || LocalTTS.IsRealtimeSupported || speaker.Lines.Any(l => l?.clip != null)));
            _stop.SetEnabled(playing ? speaker.IsSpeaking : TTSEditorAudio.IsBusy);

            if (_list != null)
                foreach (LineRow row in _list.Query<LineRow>().ToList()) row.Refresh();
        }

        void SpeakNext()
        {
            if (Application.isPlaying)
            {
                Speaker.SpeakNext();
                return;
            }
            int first = Speaker.Lines.FindIndex(l => l != null && !string.IsNullOrWhiteSpace(l.text));
            if (first >= 0) PreviewLine(first);
        }

        internal void PreviewLine(int index)
        {
            TTSSpeaker speaker = Speaker;
            if (index < 0 || index >= speaker.Lines.Count || speaker.Lines[index] == null) return;
            if (Application.isPlaying)
            {
                speaker.SpeakLine(index);
                return;
            }
            TTSLine line = speaker.Lines[index];
            TTSVoiceOptions voice = speaker.Voice;
            bool useClip = line.clip != null && (line.IsBakedFor(voice) || !LocalTTS.IsRealtimeSupported || speaker.Mode == TTSSpeaker.AudioMode.BakedOnly);
            if (speaker.Mode == TTSSpeaker.AudioMode.RealtimeOnly && LocalTTS.IsRealtimeSupported) useClip = false;
            if (useClip) TTSEditorAudio.Play(line.clip);
            else if (LocalTTS.IsRealtimeSupported) LocalTTS.Speak(line.text, voice);
        }

        internal async void BakeLine(int index)
        {
            TTSSpeaker speaker = Speaker;
            if (index < 0 || index >= speaker.Lines.Count || speaker.Lines[index] == null || string.IsNullOrWhiteSpace(speaker.Lines[index].text)) return;
            _baking = true;
            try
            {
                TTSLine line = speaker.Lines[index];
                TTSVoiceOptions voice = speaker.Voice;
                AudioClip clip = await WindowsTTSEngine.BakeAsync(line.text, voice);
                if (speaker == null) return;
                Undo.RecordObject(speaker, "Bake TTS Line");
                line.SetBaked(clip, voice);
                TTSEditorUtility.MarkChanged(speaker);
                serializedObject.Update();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LocalTTS] " + e.Message, speaker);
            }
            finally
            {
                _baking = false;
            }
        }

        async void Bake(bool onlyMissing)
        {
            _baking = true;
            try
            {
                int count = await TTSBakeUtility.BakeSpeakersAsync(new[] { Speaker }, onlyMissing);
                if (target != null) serializedObject.Update();
                if (count > 0) Debug.Log($"[LocalTTS] Baked {count} line(s) on {Speaker.name}.", Speaker);
            }
            finally
            {
                _baking = false;
            }
        }

        void ImportLines()
        {
            string path = EditorUtility.OpenFilePanel(L.T("Import lines", "Importar líneas", "Importar falas"), Application.dataPath, "txt,csv");
            if (string.IsNullOrEmpty(path)) return;
            var entries = TTSBatchParser.Parse(File.ReadAllText(path), path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
            if (entries.Count == 0) return;
            Undo.RecordObject(Speaker, "Import TTS Lines");
            Speaker.Lines.RemoveAll(l => l == null || string.IsNullOrWhiteSpace(l.text));
            foreach (WindowsTTSEngine.BatchEntry entry in entries) Speaker.Lines.Add(new TTSLine(entry.Text));
            TTSEditorUtility.MarkChanged(Speaker);
            serializedObject.Update();
            Debug.Log($"[LocalTTS] Imported {entries.Count} line(s) into {Speaker.name}.", Speaker);
        }

        void ClearClips()
        {
            Undo.RecordObject(Speaker, "Clear TTS Clips");
            foreach (TTSLine line in Speaker.Lines) line?.SetBaked(null, null);
            TTSEditorUtility.MarkChanged(Speaker);
            serializedObject.Update();
        }

        // ------------------------------------------------------------------ One line in the list

        class LineRow : VisualElement
        {
            readonly TTSSpeakerEditor _editor;
            readonly Label _index, _status;
            readonly TextField _text;
            readonly ObjectField _clip;
            readonly Button _play, _bake;
            int _line = -1;

            public LineRow(TTSSpeakerEditor editor)
            {
                _editor = editor;
                AddToClassList("tts-line");

                var top = new VisualElement();
                top.AddToClassList("tts-line-top");
                _index = new Label();
                _index.AddToClassList("tts-line-index");
                _text = new TextField { multiline = true };
                _text.AddToClassList("tts-line-text");
                var buttons = new VisualElement();
                buttons.AddToClassList("tts-line-buttons");
                _play = new Button(() => _editor.PreviewLine(_line)) { text = "▶", tooltip = L.T("Listen", "Escuchar", "Ouvir") };
                _play.AddToClassList("tts-icon-btn");
                _bake = new Button(() => _editor.BakeLine(_line)) { text = "⬇", tooltip = L.T("Bake this line to a .wav clip", "Hornear esta línea a un clip .wav", "Gerar um clipe .wav desta fala") };
                _bake.AddToClassList("tts-icon-btn");
                buttons.Add(_play);
                buttons.Add(_bake);
                top.Add(_index);
                top.Add(_text);
                top.Add(buttons);

                var bottom = new VisualElement();
                bottom.AddToClassList("tts-line-bottom");
                _status = new Label();
                _status.AddToClassList("tts-line-status");
                _clip = new ObjectField { objectType = typeof(AudioClip), allowSceneObjects = false };
                _clip.AddToClassList("tts-line-clip");
                bottom.Add(_status);
                bottom.Add(_clip);

                Add(top);
                Add(bottom);
            }

            public void Bind(int index)
            {
                _line = index;
                _index.text = (index + 1).ToString();
                SerializedProperty element = _editor.serializedObject.FindProperty("_lines").GetArrayElementAtIndex(index);
                _text.BindProperty(element.FindPropertyRelative("text"));
                _clip.BindProperty(element.FindPropertyRelative("clip"));
                Refresh();
            }

            public void Unbind()
            {
                _text.Unbind();
                _clip.Unbind();
                _line = -1;
            }

            public void Refresh()
            {
                TTSSpeaker speaker = _editor.target as TTSSpeaker;
                if (speaker == null || _line < 0 || _line >= speaker.Lines.Count || speaker.Lines[_line] == null) return;
                TTSLine line = speaker.Lines[_line];
                bool hasText = !string.IsNullOrWhiteSpace(line.text);

                _status.RemoveFromClassList("tts-ok");
                _status.RemoveFromClassList("tts-warn");
                _status.RemoveFromClassList("tts-none");
                _status.tooltip = "";
                if (!hasText)
                {
                    _status.text = L.T("Empty", "Vacía", "Vazia");
                    _status.AddToClassList("tts-none");
                }
                else if (line.IsBakedFor(speaker.Voice))
                {
                    _status.text = "● " + L.T("Baked", "Horneada", "Gerada") + $" · {line.clip.length:F1}s";
                    _status.AddToClassList("tts-ok");
                }
                else if (line.clip != null)
                {
                    _status.text = "▲ " + L.T("Outdated", "Desactualizada", "Desatualizada");
                    _status.tooltip = L.T("The text or the voice changed after baking.", "El texto o la voz cambió después de hornear.", "O texto ou a voz mudou depois de gerar.");
                    _status.AddToClassList("tts-warn");
                }
                else
                {
                    _status.text = "○ " + L.T("Real-time only", "Solo tiempo real", "Só tempo real");
                    _status.tooltip = L.T("No clip yet: plays in real time on Windows, silent elsewhere until baked.",
                        "Sin clip: suena en tiempo real en Windows y en silencio en otras plataformas hasta hornearla.",
                        "Sem clipe: toca em tempo real no Windows e fica em silêncio nas outras plataformas até ser gerada.");
                    _status.AddToClassList("tts-none");
                }
                _play.SetEnabled(hasText && (LocalTTS.IsRealtimeSupported || line.clip != null || Application.isPlaying));
                _bake.SetEnabled(hasText && LocalTTS.IsRealtimeSupported && !_editor._baking);
            }
        }
    }
}
