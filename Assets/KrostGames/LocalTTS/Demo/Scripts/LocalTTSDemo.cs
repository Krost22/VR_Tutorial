using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KrostGames.TTS.Demo
{
    /// <summary>
    /// Demo of Local TTS. Builds its UI at runtime (no prefabs, works with any render pipeline and input system).
    /// Left: type anything and hear it in real time (Windows). Right: TTS Speakers with baked lines (every platform).
    /// </summary>
    public class LocalTTSDemo : MonoBehaviour
    {
        [Tooltip("Speakers with baked lines, one per language.")]
        [SerializeField] TTSSpeaker[] _speakers = Array.Empty<TTSSpeaker>();

        [Tooltip("Plays the example clips below.")]
        [SerializeField] TTSAudioPlayer _clipPlayer;
        [SerializeField] AudioClip[] _exampleClips = Array.Empty<AudioClip>();

        static readonly Color Background = new Color32(14, 18, 17, 255);
        static readonly Color CardColor = new Color32(26, 32, 30, 255);
        static readonly Color Field = new Color32(38, 46, 43, 255);
        static readonly Color Mint = new Color32(29, 185, 146, 255);
        static readonly Color MintDark = new Color32(5, 42, 32, 255);
        static readonly Color Ink = new Color32(236, 242, 239, 255);
        static readonly Color Muted = new Color32(150, 165, 160, 255);

        Font _font;
        Sprite _rounded;
        InputField _input;
        Dropdown _voiceDropdown;
        Slider _rate, _pitch;
        Text _status, _faceCaption;
        RectTransform[] _mouthBars;
        RectTransform _leftEye, _rightEye;
        readonly List<TTSVoice> _voices = new List<TTSVoice>();
        float _blinkAt;

        // ------------------------------------------------------------------ Setup

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _rounded = CreateRoundedSprite(64, 16);
            EnsureEventSystem();
            BuildUI();
        }

        async void Start()
        {
            LocalTTS.StateChanged += UpdateStatus;
            UpdateStatus();
            if (!LocalTTS.IsRealtimeSupported) return;
            try
            {
                IReadOnlyList<TTSVoice> voices = await LocalTTS.GetVoicesAsync();
                if (this == null) return;
                _voices.Clear();
                _voices.AddRange(voices);
                _voiceDropdown.ClearOptions();
                _voiceDropdown.AddOptions(_voices.Select(v => $"{v.DisplayName}  ({v.Culture})").ToList());
                int english = _voices.FindIndex(v => v.Matches(TTSLanguage.English));
                _voiceDropdown.SetValueWithoutNotify(Mathf.Max(0, english));
                _voiceDropdown.RefreshShownValue();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LocalTTS Demo] " + e.Message);
            }
        }

        void OnDestroy() => LocalTTS.StateChanged -= UpdateStatus;

        // ------------------------------------------------------------------ Actions

        void SpeakTyped()
        {
            if (string.IsNullOrWhiteSpace(_input.text)) return;
            var voice = new TTSVoiceOptions();
            if (_voices.Count > 0) voice.voiceName = _voices[Mathf.Clamp(_voiceDropdown.value, 0, _voices.Count - 1)].DisplayName;
            voice.rate = Mathf.RoundToInt(_rate.value);
            voice.pitch = Mathf.RoundToInt(_pitch.value);
            LocalTTS.Speak(_input.text, voice);
        }

        void SpeakSample()
        {
            TTSVoice voice = _voices.Count > 0 ? _voices[Mathf.Clamp(_voiceDropdown.value, 0, _voices.Count - 1)] : null;
            TTSLanguage language = TTSLanguage.English;
            if (voice != null) TTSLanguageExtensions.TryFromCulture(voice.Culture, out language);
            _input.text = language.SampleText();
        }

        void StopAll()
        {
            LocalTTS.StopAll();
            if (_clipPlayer != null) _clipPlayer.Stop();
        }

        // ------------------------------------------------------------------ Frame update

        void Update()
        {
            float loudness = 0f;
            string speaking = null;
            // Every enabled speaker, including the shared one LocalTTS.Speak uses.
            foreach (TTSSpeaker speaker in TTSSpeaker.ActiveSpeakers)
            {
                if (speaker == null || !speaker.IsSpeaking) continue;
                loudness = Mathf.Max(loudness, speaker.Loudness);
                speaking = speaker.IsPreparing ? "…" : speaker.CurrentText;
            }
            if (_clipPlayer != null && _clipPlayer.IsPlaying)
            {
                loudness = Mathf.Max(loudness, 0.35f + 0.3f * Mathf.PerlinNoise(Time.time * 9f, 0f));
                speaking = _clipPlayer.Clip != null ? _clipPlayer.Clip.name : speaking;
            }

            for (int i = 0; i < _mouthBars.Length; i++)
            {
                float wave = 0.55f + 0.45f * Mathf.Sin(Time.time * 18f + i * 1.3f);
                float target = 8f + loudness * 120f * wave * (1f - Mathf.Abs(i - 2) * 0.18f);
                Vector2 size = _mouthBars[i].sizeDelta;
                size.y = Mathf.Lerp(size.y, target, Time.deltaTime * 20f);
                _mouthBars[i].sizeDelta = size;
            }

            // Blink every few seconds.
            float blink = Time.time >= _blinkAt && Time.time < _blinkAt + 0.12f ? 0.15f : 1f;
            if (Time.time > _blinkAt + 0.12f) _blinkAt = Time.time + UnityEngine.Random.Range(2.5f, 5f);
            _leftEye.localScale = _rightEye.localScale = new Vector3(1f, blink, 1f);

            _faceCaption.text = speaking ?? "";
        }

        void UpdateStatus()
        {
            if (_status == null) return;
            switch (LocalTTS.State)
            {
                case TTSEngineState.Ready:
                    _status.text = $"● Real-time speech ready · {LocalTTS.Voices.Count} Windows voices";
                    _status.color = Mint;
                    break;
                case TTSEngineState.Starting:
                case TTSEngineState.NotStarted:
                    _status.text = "● Starting the speech engine…";
                    _status.color = new Color32(251, 191, 36, 255);
                    break;
                case TTSEngineState.Unsupported:
                    _status.text = "● No real-time speech on this platform: baked lines still play";
                    _status.color = Muted;
                    break;
                default:
                    _status.text = "● " + LocalTTS.LastError;
                    _status.color = new Color32(248, 113, 113, 255);
                    break;
            }
            bool realtime = LocalTTS.IsRealtimeSupported;
            _input.interactable = realtime;
            _voiceDropdown.interactable = realtime && _voices.Count > 0;
        }

        // ------------------------------------------------------------------ UI construction

        void BuildUI()
        {
            var canvasGo = new GameObject("Demo UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform root = (RectTransform)canvasGo.transform;

            var bg = MakePanel(root, "Background", Background, false);
            Stretch(bg, 0, 0, 0, 0);

            MakeLabel(root, "Local TTS", 54, Ink, FontStyle.Bold, new Vector2(96, -70), new Vector2(900, 70));
            MakeLabel(root, "Offline text-to-speech with the voices installed in Windows", 26, Muted, FontStyle.Normal, new Vector2(98, -132), new Vector2(1100, 40));
            _status = MakeLabel(root, "", 22, Mint, FontStyle.Normal, new Vector2(98, -176), new Vector2(1300, 34));

            // Left card: real time
            RectTransform left = MakeCard(root, new Vector2(96, -236), new Vector2(820, 720));
            MakeLabel(left, "Say anything, in real time", 30, Ink, FontStyle.Bold, new Vector2(32, -28), new Vector2(740, 40));
            MakeLabel(left, "LocalTTS.Speak(text, voice) — Windows Editor and Windows builds", 20, Muted, FontStyle.Normal, new Vector2(32, -72), new Vector2(760, 30));

            _input = InputBox(left, new Vector2(32, -118), new Vector2(756, 190));
            _input.text = "Hello! Type anything here and press Speak. Nothing is baked: the voice is generated right now.";

            MakeLabel(left, "Voice", 20, Muted, FontStyle.Normal, new Vector2(32, -330), new Vector2(200, 30));
            _voiceDropdown = DropdownBox(left, new Vector2(32, -364), new Vector2(756, 52));

            MakeLabel(left, "Rate", 20, Muted, FontStyle.Normal, new Vector2(32, -440), new Vector2(200, 30));
            _rate = SliderBox(left, new Vector2(32, -474), new Vector2(360, 30), -10, 10, 0);
            MakeLabel(left, "Pitch", 20, Muted, FontStyle.Normal, new Vector2(428, -440), new Vector2(200, 30));
            _pitch = SliderBox(left, new Vector2(428, -474), new Vector2(360, 30), -10, 10, 0);

            MakeButton(left, "▶  Speak", new Vector2(32, -548), new Vector2(300, 64), true, SpeakTyped);
            MakeButton(left, "Stop", new Vector2(348, -548), new Vector2(160, 64), false, StopAll);
            MakeButton(left, "Sample text", new Vector2(524, -548), new Vector2(264, 64), false, SpeakSample);
            MakeLabel(left, "Every line is cached: say it again and it plays instantly.", 18, Muted, FontStyle.Italic, new Vector2(32, -640), new Vector2(760, 30));

            // Right card: speakers with baked lines + face
            RectTransform right = MakeCard(root, new Vector2(948, -236), new Vector2(876, 720));
            MakeLabel(right, "TTS Speakers with baked lines", 30, Ink, FontStyle.Bold, new Vector2(32, -28), new Vector2(800, 40));
            MakeLabel(right, "Baked to .wav in the Editor: they play on Android, iOS, WebGL…", 20, Muted, FontStyle.Normal, new Vector2(32, -72), new Vector2(800, 30));

            BuildFace(right, new Vector2(32, -118), new Vector2(812, 300));

            float x = 32;
            foreach (TTSSpeaker speaker in _speakers)
            {
                if (speaker == null) continue;
                TTSSpeaker target = speaker;
                MakeButton(right, speaker.name, new Vector2(x, -444), new Vector2(260, 64), true, () => target.SpeakNext());
                x += 276;
            }
            MakeLabel(right, "Each button says the next line of its speaker (SpeakNext).", 18, Muted, FontStyle.Italic, new Vector2(32, -522), new Vector2(800, 30));

            if (_exampleClips.Length > 0 && _clipPlayer != null)
            {
                MakeLabel(right, "Example clips (TTS Audio Player)", 20, Muted, FontStyle.Normal, new Vector2(32, -572), new Vector2(800, 30));
                float cx = 32;
                foreach (AudioClip clip in _exampleClips)
                {
                    if (clip == null) continue;
                    AudioClip c = clip;
                    MakeButton(right, clip.name.Replace("Example_", "Example "), new Vector2(cx, -608), new Vector2(200, 56), false, () => _clipPlayer.Play(c));
                    cx += 216;
                }
            }

            MakeLabel(root, "KrostGames · Local TTS " + LocalTTS.Version, 18, Muted, FontStyle.Normal, new Vector2(96, -1010), new Vector2(800, 30));
        }

        void BuildFace(RectTransform parent, Vector2 position, Vector2 size)
        {
            RectTransform face = MakePanel(parent, "Face", Field, true);
            Place(face, position, size);

            RectTransform head = MakePanel(face, "Head", new Color32(44, 54, 51, 255), true);
            Place(head, new Vector2(size.x / 2 - 130, -30), new Vector2(260, 200));
            _leftEye = MakePanel(head, "Eye L", Mint, true);
            Place(_leftEye, new Vector2(62, -52), new Vector2(38, 38));
            _rightEye = MakePanel(head, "Eye R", Mint, true);
            Place(_rightEye, new Vector2(160, -52), new Vector2(38, 38));

            _mouthBars = new RectTransform[5];
            for (int i = 0; i < _mouthBars.Length; i++)
            {
                RectTransform bar = MakePanel(head, "Mouth " + i, Mint, true);
                bar.anchorMin = bar.anchorMax = new Vector2(0, 0);
                bar.pivot = new Vector2(0.5f, 0.5f);
                bar.anchoredPosition = new Vector2(82 + i * 24, 58);
                bar.sizeDelta = new Vector2(14, 8);
                _mouthBars[i] = bar;
            }

            _faceCaption = MakeLabel(face, "", 22, Ink, FontStyle.Italic, new Vector2(24, -244), new Vector2(size.x - 48, 44));
            _faceCaption.alignment = TextAnchor.MiddleCenter;
        }

        // ------------------------------------------------------------------ Widgets (flat style, no sprites needed)

        RectTransform MakeCard(RectTransform parent, Vector2 position, Vector2 size)
        {
            RectTransform card = MakePanel(parent, "Card", CardColor, true);
            Place(card, position, size);
            return card;
        }

        RectTransform MakePanel(RectTransform parent, string name, Color color, bool rounded)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            if (rounded)
            {
                image.sprite = _rounded;
                image.type = Image.Type.Sliced;
            }
            return (RectTransform)go.transform;
        }

        Text MakeLabel(RectTransform parent, string text, int size, Color color, FontStyle style, Vector2 position, Vector2 box)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = _font;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.text = text;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            Place((RectTransform)go.transform, position, box);
            return label;
        }

        void MakeButton(RectTransform parent, string text, Vector2 position, Vector2 size, bool primary, Action onClick)
        {
            RectTransform rect = MakePanel(parent, "Button " + text, primary ? Mint : Field, true);
            Place(rect, position, size);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick());
            Text label = MakeLabel(rect, text, 24, primary ? MintDark : Ink, FontStyle.Bold, Vector2.zero, size);
            label.alignment = TextAnchor.MiddleCenter;
        }

        InputField InputBox(RectTransform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = MakePanel(parent, "Input", Field, true);
            Place(rect, position, size);
            Text text = MakeLabel(rect, "", 24, Ink, FontStyle.Normal, new Vector2(18, -14), size - new Vector2(36, 28));
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = true;
            Text placeholder = MakeLabel(rect, "Type a line…", 24, Muted, FontStyle.Italic, new Vector2(18, -14), size - new Vector2(36, 28));
            placeholder.alignment = TextAnchor.UpperLeft;
            var input = rect.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.MultiLineNewline;
            input.selectionColor = new Color(Mint.r, Mint.g, Mint.b, 0.4f);
            input.caretColor = Mint;
            return input;
        }

        Dropdown DropdownBox(RectTransform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = MakePanel(parent, "Voice", Field, true);
            Place(rect, position, size);
            Text caption = MakeLabel(rect, "Loading voices…", 22, Ink, FontStyle.Normal, new Vector2(18, 0), size - new Vector2(60, 0));
            Text arrow = MakeLabel(rect, "▼", 18, Mint, FontStyle.Normal, new Vector2(size.x - 44, 0), new Vector2(30, size.y));
            arrow.alignment = TextAnchor.MiddleCenter;

            // Template (the list that opens)
            RectTransform template = MakePanel(rect, "Template", CardColor, true);
            template.anchorMin = new Vector2(0, 0);
            template.anchorMax = new Vector2(1, 0);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0, -4);
            template.sizeDelta = new Vector2(0, 300);
            var scroll = template.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = MakePanel(template, "Viewport", Color.white, false);
            Stretch(viewport, 0, 0, 0, 0);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0, 48);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle)).GetComponent<RectTransform>();
            item.SetParent(content, false);
            item.anchorMin = new Vector2(0, 0.5f);
            item.anchorMax = new Vector2(1, 0.5f);
            item.sizeDelta = new Vector2(0, 48);
            RectTransform itemBackground = MakePanel(item, "Item Background", Field, false);
            Stretch(itemBackground, 0, 0, 0, 0);
            RectTransform check = MakePanel(item, "Item Checkmark", Mint, false);
            check.anchorMin = new Vector2(0, 0);
            check.anchorMax = new Vector2(0, 1);
            check.sizeDelta = new Vector2(6, 0);
            check.anchoredPosition = new Vector2(3, 0);
            Text itemLabel = MakeLabel(item, "", 22, Ink, FontStyle.Normal, Vector2.zero, Vector2.zero);
            Stretch((RectTransform)itemLabel.transform, 18, 0, 10, 0);
            var toggle = item.GetComponent<Toggle>();
            toggle.targetGraphic = itemBackground.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = true;
            var toggleColors = toggle.colors;
            toggleColors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            toggle.colors = toggleColors;

            scroll.viewport = viewport;
            scroll.content = content;
            template.gameObject.SetActive(false);

            var dropdown = rect.gameObject.AddComponent<Dropdown>();
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            dropdown.ClearOptions();
            return dropdown;
        }

        Slider SliderBox(RectTransform parent, Vector2 position, Vector2 size, float min, float max, float value)
        {
            var root = new GameObject("Slider", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            Place(root, position, size);

            RectTransform track = MakePanel(root, "Track", Field, true);
            Stretch(track, 0, 10, 0, 10);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(root, false);
            Stretch(fillArea, 0, 10, 0, 10);
            RectTransform fill = MakePanel(fillArea, "Fill", Mint, true);
            Stretch(fill, 0, 0, 0, 0);
            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(root, false);
            Stretch(handleArea, 12, 0, 12, 0);
            RectTransform handle = MakePanel(handleArea, "Handle", Ink, true);
            handle.sizeDelta = new Vector2(24, 0);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            slider.value = value;
            return slider;
        }

        static void Place(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
        }

        static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        // A white rounded square, 9-sliced, so the flat UI gets soft corners without any texture asset.
        static Sprite CreateRoundedSprite(int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        // uGUI needs an EventSystem. Use the Input System module when that package is active, the legacy one otherwise.
        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            // Found by name so the demo compiles whether or not the Input System package is installed.
            Type module = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module != null)
            {
                go.AddComponent(module);
                return;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
