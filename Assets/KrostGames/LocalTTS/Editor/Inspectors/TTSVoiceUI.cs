using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using L = KrostGames.TTS.EditorTools.TTSLocalization;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>Inspector controls for voice settings: installed-voice picker, language, rate, pitch, volume and a test button.</summary>
    public static class TTSVoiceUI
    {
        static StyleSheet _sheet;

        public static void ApplyStyle(VisualElement root)
        {
            if (_sheet == null) _sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(TTSEditorUtility.UIFolder + "/TTSInspector.uss");
            if (_sheet != null && !root.styleSheets.Contains(_sheet)) root.styleSheets.Add(_sheet);
        }

        /// <summary>
        /// Builds the controls for the five voice fields. <paramref name="prefix"/> is the property path that contains them
        /// ("" for top-level fields of a Voice Profile, "_voice." for a TTSVoiceOptions field...).
        /// </summary>
        public static VisualElement Create(SerializedObject serializedObject, string prefix, Func<string> testText = null)
        {
            var root = new VisualElement();
            ApplyStyle(root);

            string namePath = prefix + "voiceName", languagePath = prefix + "language";
            var voices = new List<TTSVoice>();

            var row = new VisualElement();
            row.AddToClassList("tts-row");
            var dropdown = new DropdownField(L.T("Voice", "Voz", "Voz"));
            dropdown.AddToClassList("tts-grow");
            dropdown.tooltip = L.T("Voices installed on this PC. Auto picks the first voice of the language below.",
                "Voces instaladas en este PC. Auto elige la primera voz del idioma de abajo.",
                "Vozes instaladas neste PC. Auto escolhe a primeira voz do idioma abaixo.");
            var test = new Button { text = "▶", tooltip = L.T("Listen to this voice", "Escuchar esta voz", "Ouvir esta voz") };
            test.AddToClassList("tts-icon-btn");
            row.Add(dropdown);
            row.Add(test);

            var info = new Label();
            info.AddToClassList("tts-muted");
            info.AddToClassList("tts-voice-info");

            root.Add(row);
            root.Add(info);
            root.Add(new PropertyField(serializedObject.FindProperty(languagePath), L.T("Language", "Idioma", "Idioma")));
            root.Add(new PropertyField(serializedObject.FindProperty(prefix + "rate"), L.T("Rate", "Velocidad", "Velocidade")));
            root.Add(new PropertyField(serializedObject.FindProperty(prefix + "pitch"), L.T("Pitch", "Tono", "Tom")));
            root.Add(new PropertyField(serializedObject.FindProperty(prefix + "volume"), L.T("Volume", "Volumen", "Volume")));

            void Refresh()
            {
                if (serializedObject.targetObject == null) return;
                serializedObject.Update();
                SerializedProperty nameProperty = serializedObject.FindProperty(namePath);
                SerializedProperty languageProperty = serializedObject.FindProperty(languagePath);
                if (nameProperty == null || languageProperty == null) return;

                string current = nameProperty.stringValue ?? "";
                var language = (TTSLanguage)languageProperty.intValue;
                voices.Clear();
                voices.AddRange(LocalTTS.Voices);

                var choices = new List<string> { L.T("Auto", "Auto", "Auto") + $" ({language})" };
                choices.AddRange(voices.Select(v => $"{v.DisplayName}  ·  {v.Culture}"));

                string value;
                TTSVoice named = string.IsNullOrWhiteSpace(current) ? null : FindByName(current);
                if (string.IsNullOrWhiteSpace(current)) value = choices[0];
                else if (named != null) value = choices[voices.IndexOf(named) + 1];
                else
                {
                    value = current + "  " + L.T("(not installed here)", "(no instalada aquí)", "(não instalada aqui)");
                    choices.Add(value);
                }
                dropdown.choices = choices;
                dropdown.SetValueWithoutNotify(value);

                if (!LocalTTS.IsRealtimeSupported)
                    info.text = L.T("Voices are listed in the Windows Editor.", "Las voces se listan en el Editor de Windows.", "As vozes são listadas no Editor do Windows.");
                else if (LocalTTS.Voices.Count == 0)
                    info.text = L.T("Loading voices…", "Cargando voces…", "Carregando vozes…");
                else
                {
                    TTSVoice used = LocalTTS.ResolveVoice(new TTSVoiceOptions(current, language));
                    info.text = used == null ? "" : "→ " + used.DisplayName + $" · {used.Culture} · {used.Gender}";
                }
            }

            TTSVoice FindByName(string name)
            {
                foreach (TTSVoice v in voices)
                    if (string.Equals(v.DisplayName, name, StringComparison.OrdinalIgnoreCase) || string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))
                        return v;
                foreach (TTSVoice v in voices)
                    if (v.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return v;
                return null;
            }

            dropdown.RegisterValueChangedCallback(e =>
            {
                if (e.target != dropdown) return; // ignore events bubbling from the label
                int index = dropdown.choices.IndexOf(e.newValue);
                if (index < 0 || index > voices.Count) return; // the "(not installed)" entry keeps the stored name
                serializedObject.Update();
                SerializedProperty nameProperty = serializedObject.FindProperty(namePath);
                SerializedProperty languageProperty = serializedObject.FindProperty(languagePath);
                if (index == 0) nameProperty.stringValue = "";
                else
                {
                    TTSVoice voice = voices[index - 1];
                    nameProperty.stringValue = voice.DisplayName;
                    if (TTSLanguageExtensions.TryFromCulture(voice.Culture, out TTSLanguage language)) languageProperty.intValue = (int)language;
                }
                serializedObject.ApplyModifiedProperties();
                Refresh();
            });

            test.clicked += () =>
            {
                serializedObject.Update();
                TTSVoiceOptions options = Read(serializedObject, prefix);
                string text = testText?.Invoke();
                if (string.IsNullOrWhiteSpace(text))
                {
                    TTSVoice used = LocalTTS.ResolveVoice(options);
                    text = used != null && TTSLanguageExtensions.TryFromCulture(used.Culture, out TTSLanguage l) ? l.SampleText() : options.language.SampleText();
                }
                LocalTTS.Speak(text, options);
            };
            test.SetEnabled(LocalTTS.IsRealtimeSupported);

            Action onEngine = () => root.schedule.Execute(Refresh);
            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                LocalTTS.StateChanged += onEngine;
                if (LocalTTS.Voices.Count == 0) LocalTTS.Prewarm();
                Refresh();
            });
            root.RegisterCallback<DetachFromPanelEvent>(_ => LocalTTS.StateChanged -= onEngine);
            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            return root;
        }

        /// <summary>Reads the five voice fields under <paramref name="prefix"/> into a TTSVoiceOptions.</summary>
        public static TTSVoiceOptions Read(SerializedObject serializedObject, string prefix)
        {
            return new TTSVoiceOptions(
                serializedObject.FindProperty(prefix + "voiceName").stringValue,
                (TTSLanguage)serializedObject.FindProperty(prefix + "language").intValue,
                serializedObject.FindProperty(prefix + "rate").intValue,
                serializedObject.FindProperty(prefix + "pitch").intValue,
                serializedObject.FindProperty(prefix + "volume").intValue);
        }

        /// <summary>Section header with a title and an optional info label on the right.</summary>
        public static VisualElement Section(string title, out Label info)
        {
            var header = new VisualElement();
            header.AddToClassList("tts-section");
            var label = new Label(title);
            label.AddToClassList("tts-section-title");
            info = new Label();
            info.AddToClassList("tts-section-info");
            header.Add(label);
            header.Add(info);
            return header;
        }
    }

    /// <summary>Draws any TTSVoiceOptions field (on your own components too) with the installed-voice picker.</summary>
    [CustomPropertyDrawer(typeof(TTSVoiceOptions))]
    public class TTSVoiceOptionsDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var foldout = new Foldout { text = property.displayName, value = true };
            foldout.Add(TTSVoiceUI.Create(property.serializedObject, property.propertyPath + "."));
            return foldout;
        }
    }

    /// <summary>Voice Profile asset: same controls plus a test sentence.</summary>
    [CustomEditor(typeof(TTSVoiceSettings))]
    public class TTSVoiceSettingsEditor : UnityEditor.Editor
    {
        string _testText = "";

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            TTSVoiceUI.ApplyStyle(root);
            root.AddToClassList("tts-inspector");

            var banner = new Label(L.T("A reusable voice. Assign it to TTS Speakers so they all sound the same; edit it here and they all change.",
                "Una voz reutilizable. Asígnala a varios TTS Speakers para que suenen igual; edítala aquí y cambian todos.",
                "Uma voz reutilizável. Atribua a vários TTS Speakers para que soem iguais; edite aqui e todos mudam."));
            banner.AddToClassList("tts-banner");
            root.Add(banner);

            root.Add(TTSVoiceUI.Section(L.T("Voice", "Voz", "Voz"), out _));
            root.Add(TTSVoiceUI.Create(serializedObject, "", () => _testText));

            root.Add(TTSVoiceUI.Section(L.T("Test", "Prueba", "Teste"), out _));
            var text = new TextField { multiline = true, value = _testText };
            text.AddToClassList("tts-line-text");
            text.RegisterValueChangedCallback(e => _testText = e.newValue);
            root.Add(text);
            var hint = new Label(L.T("Empty = a sample sentence in the voice's language. Press ▶ above.",
                "Vacío = una frase de ejemplo en el idioma de la voz. Pulsa ▶ arriba.",
                "Vazio = uma frase de exemplo no idioma da voz. Clique em ▶ acima."));
            hint.AddToClassList("tts-muted");
            root.Add(hint);
            return root;
        }
    }
}
