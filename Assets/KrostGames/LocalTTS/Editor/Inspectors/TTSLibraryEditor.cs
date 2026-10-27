using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using L = KrostGames.TTS.EditorTools.TTSLocalization;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>Line Library inspector: recorded lines, their status, preview, and one-click bake.</summary>
    [CustomEditor(typeof(TTSLibrary))]
    public class TTSLibraryEditor : UnityEditor.Editor
    {
        TTSLibrary Library => (TTSLibrary)target;

        Label _info;
        Button _bakePending, _rebakeAll, _removeUnbaked;
        ListView _list;
        bool _baking;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            TTSVoiceUI.ApplyStyle(root);
            root.AddToClassList("tts-inspector");

            var banner = new Label(L.T(
                "Lines spoken from code (LocalTTS.Speak, TTSSpeaker.Speak(text)) in Play Mode are recorded here. Bake them and the same code also speaks on Android, iOS, WebGL and consoles. This asset is loaded from Resources, so builds need no setup.",
                "Aquí se registran las líneas habladas desde código (LocalTTS.Speak, TTSSpeaker.Speak(texto)) en Play Mode. Hornéalas y el mismo código también habla en Android, iOS, WebGL y consolas. Este asset se carga desde Resources, así que las builds no necesitan configuración.",
                "Aqui ficam registradas as falas ditas por código (LocalTTS.Speak, TTSSpeaker.Speak(texto)) no Play Mode. Gere os áudios e o mesmo código também fala em Android, iOS, WebGL e consoles. Este asset é carregado de Resources, então as builds não precisam de configuração."));
            banner.AddToClassList("tts-banner");
            root.Add(banner);

            var record = new Toggle(L.T("Record lines in Play Mode", "Registrar líneas en Play Mode", "Registrar falas no Play Mode"))
            {
                value = TTSProjectSettings.instance.RecordPlayModeLines
            };
            record.RegisterValueChangedCallback(e =>
            {
                if (e.target == record) TTSProjectSettings.instance.RecordPlayModeLines = e.newValue;
            });
            root.Add(record);

            root.Add(TTSVoiceUI.Section(L.T("Lines", "Líneas", "Falas"), out _info));

            var tools = new VisualElement();
            tools.AddToClassList("tts-row");
            tools.AddToClassList("tts-wrap");
            _bakePending = new Button(() => Bake(true));
            _bakePending.AddToClassList("tts-small-btn");
            _bakePending.AddToClassList("tts-primary");
            _rebakeAll = new Button(() => Bake(false)) { text = L.T("Re-bake all", "Re-hornear todo", "Gerar tudo de novo") };
            _rebakeAll.AddToClassList("tts-small-btn");
            _removeUnbaked = new Button(RemoveUnbaked) { text = L.T("Remove lines without clip", "Quitar líneas sin clip", "Remover falas sem clipe") };
            _removeUnbaked.AddToClassList("tts-small-btn");
            tools.Add(_bakePending);
            tools.Add(_rebakeAll);
            tools.Add(_removeUnbaked);
            root.Add(tools);

            _list = new ListView
            {
                bindingPath = "_entries",
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                makeItem = () => new EntryRow(this),
                bindItem = (element, index) => ((EntryRow)element).Bind(index),
                unbindItem = (element, _) => ((EntryRow)element).Unbind()
            };
            _list.AddToClassList("tts-lines");
            root.Add(_list);

            root.schedule.Execute(UpdateDynamic).Every(300);
            UpdateDynamic();
            return root;
        }

        void UpdateDynamic()
        {
            if (target == null || _info == null) return;
            int total = Library.Entries.Count(e => e != null && !string.IsNullOrWhiteSpace(e.text));
            int baked = Library.Entries.Count(e => e != null && e.IsBaked);
            _info.text = $"{baked}/{total} " + L.T("baked", "horneadas", "geradas");
            _bakePending.text = _baking ? L.T("Baking…", "Horneando…", "Gerando…") : L.T("Bake pending", "Hornear pendientes", "Gerar pendentes") + $" ({total - baked})";
            _bakePending.SetEnabled(!_baking && LocalTTS.IsRealtimeSupported && total > baked);
            _rebakeAll.SetEnabled(!_baking && LocalTTS.IsRealtimeSupported && total > 0);
            _removeUnbaked.SetEnabled(!_baking && Library.Entries.Any(e => e == null || e.clip == null));
            foreach (EntryRow row in _list.Query<EntryRow>().ToList()) row.Refresh();
        }

        async void Bake(bool onlyMissing)
        {
            _baking = true;
            try
            {
                int count = await TTSBakeUtility.BakeLibraryAsync(Library, onlyMissing);
                if (target != null) serializedObject.Update();
                if (count > 0) Debug.Log($"[LocalTTS] Baked {count} Line Library line(s).", Library);
            }
            finally
            {
                _baking = false;
            }
        }

        void RemoveUnbaked()
        {
            Undo.RecordObject(Library, "Remove Unbaked Lines");
            Library.Entries.RemoveAll(e => e == null || e.clip == null);
            EditorUtility.SetDirty(Library);
            serializedObject.Update();
        }

        internal void Preview(int index)
        {
            if (index < 0 || index >= Library.Entries.Count || Library.Entries[index] == null) return;
            TTSLibrary.Entry entry = Library.Entries[index];
            if (entry.IsBaked || (entry.clip != null && !LocalTTS.IsRealtimeSupported)) TTSEditorAudio.Play(entry.clip);
            else LocalTTS.Speak(entry.text, entry.voice);
        }

        internal async void BakeOne(int index)
        {
            if (index < 0 || index >= Library.Entries.Count || Library.Entries[index] == null) return;
            TTSLibrary.Entry entry = Library.Entries[index];
            _baking = true;
            try
            {
                AudioClip clip = await WindowsTTSEngine.BakeAsync(entry.text, entry.voice);
                Undo.RecordObject(Library, "Bake Line");
                entry.SetBaked(clip);
                EditorUtility.SetDirty(Library);
                AssetDatabase.SaveAssetIfDirty(Library);
                serializedObject.Update();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LocalTTS] " + e.Message, Library);
            }
            finally
            {
                _baking = false;
            }
        }

        class EntryRow : VisualElement
        {
            readonly TTSLibraryEditor _editor;
            readonly Label _index, _status;
            readonly TextField _text;
            readonly ObjectField _clip;
            readonly Foldout _voiceFoldout;
            readonly Button _play, _bake;
            int _entry = -1;

            public EntryRow(TTSLibraryEditor editor)
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
                _play = new Button(() => _editor.Preview(_entry)) { text = "▶", tooltip = L.T("Listen", "Escuchar", "Ouvir") };
                _play.AddToClassList("tts-icon-btn");
                _bake = new Button(() => _editor.BakeOne(_entry)) { text = "⬇", tooltip = L.T("Bake this line", "Hornear esta línea", "Gerar esta fala") };
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

                _voiceFoldout = new Foldout { value = false };
                _voiceFoldout.style.marginLeft = 18;

                Add(top);
                Add(bottom);
                Add(_voiceFoldout);
            }

            public void Bind(int index)
            {
                _entry = index;
                _index.text = (index + 1).ToString();
                SerializedObject so = _editor.serializedObject;
                SerializedProperty element = so.FindProperty("_entries").GetArrayElementAtIndex(index);
                _text.BindProperty(element.FindPropertyRelative("text"));
                _clip.BindProperty(element.FindPropertyRelative("clip"));
                _voiceFoldout.Clear();
                _voiceFoldout.Add(TTSVoiceUI.Create(so, element.propertyPath + ".voice."));
                Refresh();
            }

            public void Unbind()
            {
                _text.Unbind();
                _clip.Unbind();
                _voiceFoldout.Clear();
                _entry = -1;
            }

            public void Refresh()
            {
                TTSLibrary library = _editor.target as TTSLibrary;
                if (library == null || _entry < 0 || _entry >= library.Entries.Count || library.Entries[_entry] == null) return;
                TTSLibrary.Entry entry = library.Entries[_entry];
                _voiceFoldout.text = L.T("Voice: ", "Voz: ", "Voz: ") + entry.voice;

                _status.RemoveFromClassList("tts-ok");
                _status.RemoveFromClassList("tts-warn");
                _status.RemoveFromClassList("tts-none");
                if (entry.IsBaked)
                {
                    _status.text = "● " + L.T("Baked", "Horneada", "Gerada") + $" · {entry.clip.length:F1}s";
                    _status.AddToClassList("tts-ok");
                }
                else if (entry.clip != null)
                {
                    _status.text = "▲ " + L.T("Outdated", "Desactualizada", "Desatualizada");
                    _status.AddToClassList("tts-warn");
                }
                else
                {
                    _status.text = "○ " + L.T("Pending", "Pendiente", "Pendente");
                    _status.AddToClassList("tts-none");
                }
                bool hasText = !string.IsNullOrWhiteSpace(entry.text);
                _play.SetEnabled(hasText && (LocalTTS.IsRealtimeSupported || entry.clip != null));
                _bake.SetEnabled(hasText && LocalTTS.IsRealtimeSupported && !_editor._baking);
            }
        }
    }
}
