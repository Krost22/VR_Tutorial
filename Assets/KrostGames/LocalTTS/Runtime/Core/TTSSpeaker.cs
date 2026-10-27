using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace KrostGames.TTS
{
    /// <summary>A line a speaker can say, plus the clip baked for it (if any).</summary>
    [Serializable]
    public class TTSLine
    {
        [TextArea(1, 6)]
        public string text = "";

        [Tooltip("Pre-generated audio for this line. Plays on platforms without real-time speech and skips synthesis on Windows. Create it with Bake in the inspector.")]
        public AudioClip clip;

        [SerializeField, HideInInspector] string _bakedSignature = "";

        public TTSLine() { }
        public TTSLine(string text) { this.text = text ?? ""; }

        /// <summary>Settings signature the clip was baked with (see <see cref="TTSVoiceOptions.Signature"/>).</summary>
        public string BakedSignature => _bakedSignature;

        /// <summary>True when the clip matches the current text and voice.</summary>
        public bool IsBakedFor(TTSVoiceOptions voice) => clip != null && voice != null && _bakedSignature == voice.Signature(text);

        /// <summary>Stores a baked clip and remembers which text and voice produced it.</summary>
        public void SetBaked(AudioClip bakedClip, TTSVoiceOptions voice)
        {
            clip = bakedClip;
            _bakedSignature = bakedClip != null && voice != null ? voice.Signature(text) : "";
        }
    }

    /// <summary>
    /// Speaks lines through the AudioSource on this GameObject, so speech can be 3D, mixed and spatialized like any sound.
    /// Auto mode plays the baked clip when it matches the text and voice, and synthesizes in real time otherwise (Windows).
    /// Every public method can be wired from UnityEvents (UI buttons, XR interactables, Timeline signals...).
    /// </summary>
    [AddComponentMenu("KrostGames/Local TTS/TTS Speaker")]
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class TTSSpeaker : MonoBehaviour
    {
        public enum AudioMode
        {
            /// <summary>Baked clip when it is up to date, real-time speech otherwise (Windows), any baked clip as a last resort.</summary>
            Auto,
            /// <summary>Always synthesize (Windows). Handy while writing lines.</summary>
            RealtimeOnly,
            /// <summary>Only baked clips, exactly what non-Windows builds do.</summary>
            BakedOnly
        }

        public enum BusyBehaviour
        {
            /// <summary>A new line cuts the current one.</summary>
            Interrupt,
            /// <summary>New lines wait their turn.</summary>
            Queue,
            /// <summary>New lines are dropped while speaking.</summary>
            Ignore
        }

        [Serializable] public class SpeechEvent : UnityEvent<string> { }

        [Tooltip("Optional voice profile asset. When set, it replaces the voice settings below.")]
        [SerializeField] TTSVoiceSettings _voiceProfile;
        [SerializeField] TTSVoiceOptions _voice = new TTSVoiceOptions();
        [SerializeField] List<TTSLine> _lines = new List<TTSLine> { new TTSLine("Hello! I'm speaking with Local TTS.") };

        [Tooltip("Auto: baked clip when it matches, otherwise real-time speech (Windows).\nRealtime Only: always synthesize.\nBaked Only: only baked clips, like non-Windows builds.")]
        [SerializeField] AudioMode _audioMode = AudioMode.Auto;
        [Tooltip("What happens when a line is requested while another one is playing.")]
        [SerializeField] BusyBehaviour _whenBusy = BusyBehaviour.Interrupt;
        [SerializeField] bool _speakOnStart;
        [SerializeField, Min(0)] int _startLine;
        [Tooltip("Start the speech engine and synthesize the lines in the background on Start, so the first line plays without delay (Windows).")]
        [SerializeField] bool _prepareOnStart = true;
        [SerializeField] SpeechEvent _onSpeechStarted = new SpeechEvent();
        [SerializeField] SpeechEvent _onSpeechFinished = new SpeechEvent();

        internal static readonly List<TTSSpeaker> Active = new List<TTSSpeaker>();

        /// <summary>Every enabled speaker, including the shared one used by <see cref="LocalTTS.Speak"/>.</summary>
        public static IReadOnlyList<TTSSpeaker> ActiveSpeakers => Active;

        enum Phase { Idle, Preparing, Playing }

        struct Request
        {
            public string Text;
            public TTSVoiceOptions Voice;
            public int Line;
        }

        readonly Queue<Request> _queue = new Queue<Request>();
        AudioSource _audio;
        AudioClip _ownedClip;
        Phase _phase;
        int _requestId;
        bool _paused;
        float _playStartedAt;
        float[] _meter;
        float _loudness;
        int _loudnessFrame = -1;

        // ------------------------------------------------------------------ Properties

        public AudioSource AudioSource => _audio != null ? _audio : (_audio = GetComponent<AudioSource>());

        /// <summary>Optional voice profile. When set it overrides <see cref="InlineVoice"/>.</summary>
        public TTSVoiceSettings VoiceProfile
        {
            get => _voiceProfile;
            set => _voiceProfile = value;
        }

        /// <summary>Voice settings stored on this component (used when there is no profile).</summary>
        public TTSVoiceOptions InlineVoice
        {
            get => _voice;
            set => _voice = value ?? new TTSVoiceOptions();
        }

        /// <summary>The voice this speaker uses right now: the profile's if there is one, else the inline settings.</summary>
        public TTSVoiceOptions Voice => _voiceProfile != null ? _voiceProfile.ToOptions() : _voice;

        public List<TTSLine> Lines => _lines;

        public AudioMode Mode
        {
            get => _audioMode;
            set => _audioMode = value;
        }

        public BusyBehaviour WhenBusy
        {
            get => _whenBusy;
            set => _whenBusy = value;
        }

        /// <summary>True from the moment a line is requested until it finishes (synthesis included).</summary>
        public bool IsSpeaking => _phase != Phase.Idle;

        /// <summary>True while the current line is being synthesized.</summary>
        public bool IsPreparing => _phase == Phase.Preparing;

        public bool IsPaused => _paused;

        /// <summary>Text being spoken (or last spoken).</summary>
        public string CurrentText { get; private set; }

        /// <summary>Index of the line being spoken (or last spoken), -1 before the first line.</summary>
        public int CurrentLine { get; private set; } = -1;

        public SpeechEvent OnSpeechStarted => _onSpeechStarted;
        public SpeechEvent OnSpeechFinished => _onSpeechFinished;

        /// <summary>Loudness of the speech playing right now, 0..1. Drive a mouth, a light or a UI meter with it.</summary>
        public float Loudness
        {
            get
            {
                if (_phase != Phase.Playing || _paused) return 0f;
                if (_loudnessFrame == Time.frameCount) return _loudness;
                _loudnessFrame = Time.frameCount;
                if (_meter == null) _meter = new float[256];
                AudioSource.GetOutputData(_meter, 0);
                float sum = 0f;
                for (int i = 0; i < _meter.Length; i++) sum += _meter[i] * _meter[i];
                _loudness = Mathf.Clamp01(Mathf.Sqrt(sum / _meter.Length) * 4f);
                return _loudness;
            }
        }

        // ------------------------------------------------------------------ Speaking

        /// <summary>Speaks the next line (the start line the first time, then one after another, wrapping around).</summary>
        public void Speak() => SpeakNext();

        /// <summary>Speaks the next line (the start line the first time, then one after another, wrapping around).</summary>
        public void SpeakNext()
        {
            if (_lines.Count == 0) return;
            int next = CurrentLine < 0 ? Mathf.Clamp(_startLine, 0, _lines.Count - 1) : (CurrentLine + 1) % _lines.Count;
            SpeakLine(next);
        }

        /// <summary>Speaks the line at this index.</summary>
        public void SpeakLine(int index)
        {
            if (index < 0 || index >= _lines.Count || _lines[index] == null)
            {
                Debug.LogWarning($"[LocalTTS] {name} has no line {index}.", this);
                return;
            }
            if (string.IsNullOrWhiteSpace(_lines[index].text)) return;
            Enqueue(new Request { Text = _lines[index].text, Voice = Voice, Line = index });
        }

        /// <summary>Speaks any text with this speaker's voice. If it matches one of the lines, that line's baked clip is used.</summary>
        public void Speak(string text) => Speak(text, null);

        /// <summary>Speaks any text with the given voice (null = this speaker's voice).</summary>
        public void Speak(string text, TTSVoiceOptions voice)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Enqueue(new Request { Text = text, Voice = voice ?? Voice, Line = IndexOfLine(text) });
        }

        /// <summary>Stops the current line and clears the queue. OnSpeechFinished is not raised.</summary>
        public void Stop()
        {
            _requestId++;
            _queue.Clear();
            _phase = Phase.Idle;
            _paused = false;
            if (AudioSource != null) AudioSource.Stop();
        }

        public void Pause()
        {
            if (_phase != Phase.Playing || _paused) return;
            _paused = true;
            AudioSource.Pause();
        }

        public void Resume()
        {
            if (!_paused) return;
            _paused = false;
            AudioSource.UnPause();
        }

        /// <summary>Synthesizes every line in the background so they play without delay later (Windows).</summary>
        public void Prepare()
        {
            if (_audioMode == AudioMode.BakedOnly || !LocalTTS.IsRealtimeSupported) return;
            LocalTTS.Prewarm();
            TTSVoiceOptions voice = Voice;
            foreach (TTSLine line in _lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;
                if (_audioMode == AudioMode.Auto && line.IsBakedFor(voice)) continue;
                LocalTTS.Prepare(line.text, voice);
            }
        }

        // ------------------------------------------------------------------ Unity messages

        void Reset()
        {
            var source = GetComponent<AudioSource>();
            if (source != null) source.playOnAwake = false;
        }

        void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        void Start()
        {
            if (_prepareOnStart) Prepare();
            if (_speakOnStart && _lines.Count > 0) SpeakLine(Mathf.Clamp(_startLine, 0, _lines.Count - 1));
        }

        void Update()
        {
            if (_phase != Phase.Playing || _paused || AudioListener.pause) return;
            AudioSource source = AudioSource;
            if (source.isPlaying) return;
            // A clip that loads in the background is not "playing" for its first frames: give it time to start.
            AudioClip clip = source.clip;
            if (clip != null && clip.loadState == AudioDataLoadState.Loading) return;
            if (Time.unscaledTime - _playStartedAt < 0.1f) return;
            Finish();
        }

        void OnDisable()
        {
            Active.Remove(this);
            Stop();
        }

        void OnDestroy() => ReleaseOwnedClip();

        // ------------------------------------------------------------------ Internals

        internal void ConfigureAsShared()
        {
            _lines.Clear();
            _whenBusy = BusyBehaviour.Queue;
            _prepareOnStart = false;
            _speakOnStart = false;
        }

        void Enqueue(Request request)
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                LocalTTS.EditorSpeak?.Invoke(request.Text, request.Voice);
#endif
                return;
            }
            if (!isActiveAndEnabled)
            {
                Debug.LogWarning($"[LocalTTS] {name} is disabled and cannot speak.", this);
                return;
            }

            if (_phase != Phase.Idle)
            {
                if (_whenBusy == BusyBehaviour.Ignore) return;
                if (_whenBusy == BusyBehaviour.Queue)
                {
                    _queue.Enqueue(request);
                    return;
                }
                _requestId++;
                AudioSource.Stop();
                _phase = Phase.Idle;
                _paused = false;
            }
            Begin(request);
        }

        async void Begin(Request request)
        {
            int id = ++_requestId;
            _phase = Phase.Preparing;
            CurrentText = request.Text;
            if (request.Line >= 0) CurrentLine = request.Line;

            TTSLine line = request.Line >= 0 && request.Line < _lines.Count ? _lines[request.Line] : null;
            bool useBaked = _audioMode != AudioMode.RealtimeOnly;
            bool realtime = _audioMode != AudioMode.BakedOnly && LocalTTS.IsRealtimeSupported;
            AudioClip clip = null;
            bool owned = false;
            string problem = null;

            try
            {
                // 1. A baked clip that matches this text and voice (any baked clip when real-time is unavailable).
                if (useBaked && line != null && line.clip != null && (line.IsBakedFor(request.Voice) || !realtime)) clip = line.clip;
                if (clip == null && useBaked) TTSLibrary.TryGet(request.Text, request.Voice, realtime, out clip);

                // 2. Real-time synthesis (Windows).
                if (clip == null && realtime)
                {
                    if (line == null) LocalTTS.NotifyLineRequested(request.Text, request.Voice);
                    clip = await LocalTTS.SynthesizeAsync(request.Text, request.Voice);
                    owned = true;
                }
            }
            catch (Exception e)
            {
                problem = e.Message;
            }

            if (this == null || id != _requestId)
            {
                if (owned) DestroyClip(clip); // superseded or destroyed while synthesizing
                return;
            }

            // 3. Last resort: an outdated baked clip beats silence.
            if (clip == null && useBaked)
            {
                if (line != null && line.clip != null) clip = line.clip;
                else TTSLibrary.TryGet(request.Text, request.Voice, false, out clip);
            }

            if (clip == null)
            {
                Debug.LogWarning(problem != null
                    ? $"[LocalTTS] {name} could not speak \"{Short(request.Text)}\": {problem}"
                    : $"[LocalTTS] {name} has no audio for \"{Short(request.Text)}\". Bake it in the Editor: real-time speech is not available here.", this);
                _phase = Phase.Idle;
                PlayNextInQueue();
                return;
            }

            if (owned)
            {
                ReleaseOwnedClip();
                _ownedClip = clip;
            }

            AudioSource source = AudioSource;
            source.clip = clip;
            source.Play();
            _playStartedAt = Time.unscaledTime;
            _phase = Phase.Playing;
            _onSpeechStarted.Invoke(request.Text);
        }

        void Finish()
        {
            _phase = Phase.Idle;
            _onSpeechFinished.Invoke(CurrentText);
            PlayNextInQueue();
        }

        void PlayNextInQueue()
        {
            if (this == null) return; // an OnSpeechFinished listener may have destroyed us
            if (_phase == Phase.Idle && _queue.Count > 0 && isActiveAndEnabled) Begin(_queue.Dequeue());
        }

        int IndexOfLine(string text)
        {
            for (int i = 0; i < _lines.Count; i++)
                if (_lines[i] != null && _lines[i].text == text) return i;
            return -1;
        }

        void ReleaseOwnedClip()
        {
            if (_ownedClip == null) return;
            if (_audio != null && _audio.clip == _ownedClip)
            {
                _audio.Stop();
                _audio.clip = null;
            }
            DestroyClip(_ownedClip);
            _ownedClip = null;
        }

        static void DestroyClip(AudioClip clip)
        {
            if (clip == null) return;
            if (Application.isPlaying) Destroy(clip);
            else DestroyImmediate(clip);
        }

        static string Short(string text) => text.Length > 48 ? text.Substring(0, 48) + "..." : text;
    }
}
