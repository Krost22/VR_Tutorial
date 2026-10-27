using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KrostGames.TTS.Engine;
using UnityEngine;

namespace KrostGames.TTS
{
    public enum TTSEngineState
    {
        /// <summary>Real-time speech is available but the engine has not been started yet.</summary>
        NotStarted,
        /// <summary>The engine is starting or reading the installed voices.</summary>
        Starting,
        /// <summary>Ready to speak.</summary>
        Ready,
        /// <summary>The engine failed (see <see cref="LocalTTS.LastError"/>). The next request tries again.</summary>
        Error,
        /// <summary>This platform has no real-time speech. Baked clips still play.</summary>
        Unsupported
    }

    /// <summary>
    /// Offline text-to-speech in one line:
    /// <code>LocalTTS.Speak("Hello!");</code>
    /// Real-time speech uses the voices installed in Windows and works in the Windows Editor and Windows builds.
    /// On other platforms (Android, iOS, WebGL...) lines play from clips baked in the Editor.
    /// Call it from Unity's main thread.
    /// </summary>
    public static class LocalTTS
    {
        public const string Version = "1.1.0";

        const string NoVoicesMessage = "No Windows voices are installed. Add one in Windows Settings > Time & language > Speech.";
        const double RetryDelaySeconds = 10;

        static TTSEngineState? _state;
        static DateTime _failedAtUtc = DateTime.MinValue;
        static TTSVoiceOptions _defaultVoice = new TTSVoiceOptions();
        static TTSVoice[] _voices = Array.Empty<TTSVoice>();
        static Task<TTSVoice[]> _voicesTask;
        static TTSSpeaker _shared;
        static int _cacheSize = 64;

        static readonly Dictionary<string, Task<TTSAudioData>> Inflight = new Dictionary<string, Task<TTSAudioData>>();
        static readonly Dictionary<string, LinkedListNode<KeyValuePair<string, TTSAudioData>>> CacheMap = new Dictionary<string, LinkedListNode<KeyValuePair<string, TTSAudioData>>>();
        static readonly LinkedList<KeyValuePair<string, TTSAudioData>> CacheOrder = new LinkedList<KeyValuePair<string, TTSAudioData>>();
        static readonly HashSet<string> Warned = new HashSet<string>();

        // ------------------------------------------------------------------ State

        /// <summary>True on Windows (Editor and Windows builds), where lines can be synthesized at runtime.</summary>
        public static bool IsRealtimeSupported => TTSEngine.IsAvailable;

        public static TTSEngineState State =>
            _state ?? (IsRealtimeSupported ? TTSEngineState.NotStarted : TTSEngineState.Unsupported);

        public static bool IsReady => State == TTSEngineState.Ready;

        /// <summary>Why the engine is in the Error state.</summary>
        public static string LastError { get; private set; }

        /// <summary>Raised on the main thread whenever <see cref="State"/> changes.</summary>
        public static event Action StateChanged;

        // ------------------------------------------------------------------ Settings

        /// <summary>Voice used by <see cref="Speak(string, TTSVoiceOptions)"/> and friends when none is given.</summary>
        public static TTSVoiceOptions DefaultVoice
        {
            get => _defaultVoice;
            set => _defaultVoice = value ?? new TTSVoiceOptions();
        }

        /// <summary>Cut the silence Windows voices add before and after speech (on by default).</summary>
        public static bool TrimSilence { get; set; } = true;

        /// <summary>How many synthesized lines stay in memory, so repeated lines play instantly.</summary>
        public static int CacheSize
        {
            get => _cacheSize;
            set
            {
                _cacheSize = Math.Max(0, value);
                lock (CacheMap) TrimCache();
            }
        }

        // ------------------------------------------------------------------ Voices

        /// <summary>Voices found the last time the engine was queried (empty until then).</summary>
        public static IReadOnlyList<TTSVoice> Voices => _voices;

        /// <summary>Starts the engine in the background, so the first line does not wait for it.</summary>
        public static void Prewarm()
        {
            if (IsRealtimeSupported) Observe(GetVoicesAsync(), "start the speech engine");
        }

        /// <summary>Lists the voices installed in Windows (SAPI 5 Desktop voices and OneCore voices).</summary>
        public static async Task<IReadOnlyList<TTSVoice>> GetVoicesAsync(bool refresh = false)
        {
            if (!IsRealtimeSupported)
            {
                SetState(TTSEngineState.Unsupported, null);
                return _voices;
            }

            // After a failure (PowerShell blocked by policy, for example) wait a little before starting the engine again,
            // so a burst of lines fails fast instead of each one waiting for a new start.
            var task = _voicesTask;
            bool failed = task != null && (task.IsFaulted || task.IsCanceled);
            bool coolingDown = failed && (DateTime.UtcNow - _failedAtUtc).TotalSeconds < RetryDelaySeconds;
            bool started = false;
            if (refresh || task == null || (failed && !coolingDown))
            {
                task = _voicesTask = TTSEngine.GetVoicesAsync();
                started = true;
                SetState(TTSEngineState.Starting, null);
            }

            try
            {
                TTSVoice[] voices = await task;
                if (task == _voicesTask)
                {
                    _voices = voices;
                    if (voices.Length > 0) SetState(TTSEngineState.Ready, null);
                    else SetState(TTSEngineState.Error, NoVoicesMessage);
                }
                return voices;
            }
            catch (Exception e)
            {
                if (started && task == _voicesTask)
                {
                    _failedAtUtc = DateTime.UtcNow;
                    SetState(TTSEngineState.Error, e.Message);
                }
                throw;
            }
        }

        /// <summary>
        /// The installed voice these options select: the named voice (full or partial name), otherwise the first voice
        /// of the language, otherwise the first installed voice. Null until the voices are loaded.
        /// </summary>
        public static TTSVoice ResolveVoice(TTSVoiceOptions options) => ResolveVoice(options, _voices, false);

        internal static TTSVoice ResolveVoice(TTSVoiceOptions options, IReadOnlyList<TTSVoice> voices, bool warn)
        {
            if (voices == null || voices.Count == 0) return null;
            options = options ?? DefaultVoice;
            string name = (options.voiceName ?? "").Trim();

            if (name.Length > 0)
            {
                foreach (var v in voices)
                    if (string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(v.DisplayName, name, StringComparison.OrdinalIgnoreCase)) return v;
                foreach (var v in voices)
                    if (v.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return v;
            }

            TTSVoice byLanguage = null;
            foreach (var v in voices)
                if (v.Matches(options.language)) { byLanguage = v; break; }

            TTSVoice chosen = byLanguage ?? voices[0];
            if (warn && (name.Length > 0 || byLanguage == null) && Warned.Add(name + "|" + options.language))
            {
                Debug.LogWarning(name.Length > 0
                    ? $"[LocalTTS] Voice \"{name}\" is not installed on this PC. Using {chosen.DisplayName}."
                    : $"[LocalTTS] No {options.language} voice is installed on this PC. Using {chosen.DisplayName}. Add voices in Windows Settings > Time & language > Speech.");
            }
            return chosen;
        }

        // ------------------------------------------------------------------ Synthesis

        /// <summary>
        /// Synthesizes a line and returns a new AudioClip (you own it: Destroy it when done).
        /// Text starting with &lt;speak&gt; is read as SSML. Windows only; see <see cref="IsRealtimeSupported"/>.
        /// </summary>
        public static async Task<AudioClip> SynthesizeAsync(string text, TTSVoiceOptions options = null, CancellationToken cancellationToken = default)
        {
            TTSAudioData data = await SynthesizeDataAsync(text, options, cancellationToken);
            return data.ToAudioClip(ClipName(text));
        }

        /// <summary>Synthesizes a line and returns the raw 16-bit PCM (also kept in the memory cache).</summary>
        public static async Task<TTSAudioData> SynthesizeDataAsync(string text, TTSVoiceOptions options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("The text is empty.", nameof(text));
            if (!IsRealtimeSupported)
                throw new TTSException("Real-time speech needs Windows. Bake the line in the Editor to play it on this platform.");

            options = options ?? DefaultVoice;
            IReadOnlyList<TTSVoice> voices = await GetVoicesAsync();
            TTSVoice voice = ResolveVoice(options, voices, true) ?? throw new TTSException(NoVoicesMessage);

            int rate = Mathf.Clamp(options.rate, -10, 10);
            int pitch = Mathf.Clamp(options.pitch, -10, 10);
            int volume = Mathf.Clamp(options.volume, 0, 100);
            bool trim = TrimSilence;
            string key = string.Join("|", voice.Engine, voice.Id, rate, pitch, volume, trim ? 1 : 0, text);

            if (TryGetCached(key, out TTSAudioData cached)) return cached;

            Task<TTSAudioData> task;
            lock (Inflight)
            {
                if (!Inflight.TryGetValue(key, out task) || task.IsFaulted || task.IsCanceled)
                    Inflight[key] = task = TTSEngine.SynthesizeAsync(voice, text, rate, pitch, volume, LooksLikeSsml(text), trim);
            }

            try
            {
                TTSAudioData data = await WithCancellation(task, cancellationToken);
                AddToCache(key, data);
                return data;
            }
            finally
            {
                if (task.IsCompleted)
                    lock (Inflight)
                        if (Inflight.TryGetValue(key, out var current) && current == task) Inflight.Remove(key);
            }
        }

        /// <summary>Synthesizes a line in the background so it plays instantly later. Errors are ignored.</summary>
        public static void Prepare(string text, TTSVoiceOptions options = null)
        {
            if (IsRealtimeSupported && !string.IsNullOrWhiteSpace(text)) PrepareQuietly(text, options);
        }

        static async void PrepareQuietly(string text, TTSVoiceOptions options)
        {
            try { await SynthesizeDataAsync(text, options); }
            catch (Exception) { /* Speak reports problems when the line is actually used */ }
        }

        /// <summary>Forgets every synthesized line kept in memory.</summary>
        public static void ClearCache()
        {
            lock (CacheMap)
            {
                CacheMap.Clear();
                CacheOrder.Clear();
            }
        }

        // ------------------------------------------------------------------ Speaking

        /// <summary>
        /// Speaks a line through a shared 2D speaker that survives scene loads. Lines queue up.
        /// Uses a baked clip from the Line Library when there is one; otherwise synthesizes it (Windows).
        /// Returns the speaker, so you can listen to its events or stop it.
        /// </summary>
        public static TTSSpeaker Speak(string text, TTSVoiceOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                if (EditorSpeak != null)
                {
                    EditorSpeak(text, options ?? DefaultVoice);
                    return null;
                }
#endif
                Debug.LogWarning("[LocalTTS] LocalTTS.Speak plays in Play Mode and in builds. In edit mode, use Tools > Local TTS > Generator.");
                return null;
            }

            TTSSpeaker speaker = SharedSpeaker;
            speaker.Speak(text, options ?? DefaultVoice);
            return speaker;
        }

        /// <summary>Stops every TTSSpeaker (including the shared one) and the Editor preview.</summary>
        public static void StopAll()
        {
            foreach (var speaker in TTSSpeaker.Active.ToArray())
                if (speaker != null) speaker.Stop();
#if UNITY_EDITOR
            EditorStop?.Invoke();
#endif
        }

        /// <summary>The speaker used by <see cref="Speak(string, TTSVoiceOptions)"/>. Created on first use (Play Mode only).</summary>
        public static TTSSpeaker SharedSpeaker
        {
            get
            {
                if (_shared != null) return _shared;
                if (!Application.isPlaying)
                    throw new InvalidOperationException("The shared speaker only exists in Play Mode and in builds.");

                var go = new GameObject("[LocalTTS] Speaker");
                UnityEngine.Object.DontDestroyOnLoad(go);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _shared = go.AddComponent<TTSSpeaker>();
                _shared.ConfigureAsShared();
                return _shared;
            }
        }

        // ------------------------------------------------------------------ Lifecycle

        /// <summary>Stops the background engine process. It starts again on the next request.</summary>
        public static void Shutdown()
        {
            TTSEngine.Shutdown();
            _voicesTask = null;
            lock (Inflight) Inflight.Clear();
            if (IsRealtimeSupported) SetState(TTSEngineState.NotStarted, null);
        }

#if UNITY_EDITOR
        // Set by the Editor assembly: plays lines in edit mode through the Editor audio preview.
        internal static Action<string, TTSVoiceOptions> EditorSpeak;
        internal static Action EditorStop;

        [UnityEditor.InitializeOnLoadMethod]
        static void InitEditor()
        {
            // A domain reload must not leave a process (or a blocked thread) behind.
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
            UnityEditor.EditorApplication.quitting += Shutdown;
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitRuntime()
        {
            _shared = null; // also correct when "Enter Play Mode Options" skip the domain reload
#if !UNITY_EDITOR
            Application.quitting -= Shutdown;
            Application.quitting += Shutdown;
#endif
        }

        /// <summary>Raised when a speaker synthesizes text that is not one of its own lines (the Editor records these).</summary>
        internal static event Action<string, TTSVoiceOptions> LineRequested;

        internal static void NotifyLineRequested(string text, TTSVoiceOptions voice)
        {
            try { LineRequested?.Invoke(text, voice); }
            catch (Exception e) { Debug.LogException(e); }
        }

        // ------------------------------------------------------------------ Helpers

        static void SetState(TTSEngineState state, string error)
        {
            if (_state == state && LastError == error) return;
            _state = state;
            LastError = error;
            try { StateChanged?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        static bool TryGetCached(string key, out TTSAudioData data)
        {
            lock (CacheMap)
            {
                if (CacheMap.TryGetValue(key, out var node))
                {
                    CacheOrder.Remove(node);
                    CacheOrder.AddFirst(node);
                    data = node.Value.Value;
                    return true;
                }
            }
            data = null;
            return false;
        }

        static void AddToCache(string key, TTSAudioData data)
        {
            lock (CacheMap)
            {
                if (_cacheSize <= 0) return;
                if (CacheMap.TryGetValue(key, out var existing)) CacheOrder.Remove(existing);
                CacheMap[key] = CacheOrder.AddFirst(new KeyValuePair<string, TTSAudioData>(key, data));
                TrimCache();
            }
        }

        static void TrimCache()
        {
            while (CacheOrder.Count > _cacheSize)
            {
                CacheMap.Remove(CacheOrder.Last.Value.Key);
                CacheOrder.RemoveLast();
            }
        }

        static async Task<T> WithCancellation<T>(Task<T> task, CancellationToken token)
        {
            if (!token.CanBeCanceled || task.IsCompleted) return await task;
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (token.Register(() => cancelled.TrySetResult(true)))
            {
                if (await Task.WhenAny(task, cancelled.Task) != task) throw new OperationCanceledException(token);
            }
            return await task;
        }

        static async void Observe(Task task, string what)
        {
            try { await task; }
            catch (Exception e) { Debug.LogWarning($"[LocalTTS] Could not {what}: {e.Message}"); }
        }

        internal static bool LooksLikeSsml(string text) =>
            text.TrimStart().StartsWith("<speak", StringComparison.OrdinalIgnoreCase);

        internal static string ClipName(string text)
        {
            string flat = text.Trim().Replace('\n', ' ').Replace('\r', ' ');
            return "TTS " + (flat.Length > 40 ? flat.Substring(0, 40) + "..." : flat);
        }
    }
}
