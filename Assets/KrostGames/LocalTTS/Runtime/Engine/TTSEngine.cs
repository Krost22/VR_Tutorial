using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KrostGames.TTS.Engine
{
    /// <summary>
    /// Thin layer over the PowerShell worker: voice listing and synthesis requests.
    /// On platforms without Windows speech it compiles to stubs that report "unsupported".
    /// </summary>
    internal static class TTSEngine
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        static readonly object Gate = new object();
        static TTSWorker _worker;
        static int _available = -1;

        public static bool IsAvailable
        {
            get
            {
                if (_available < 0)
                {
                    var platform = Application.platform;
                    _available = platform == RuntimePlatform.WindowsEditor || platform == RuntimePlatform.WindowsPlayer ? 1 : 0;
                }
                return _available == 1;
            }
        }

        static TTSWorker Worker
        {
            get
            {
                lock (Gate) return _worker ?? (_worker = new TTSWorker());
            }
        }

        public static Task<TTSVoice[]> GetVoicesAsync() => Worker.Send("VOICES", null, 30000, ParseVoices);

        public static Task<TTSAudioData> SynthesizeAsync(TTSVoice voice, string text, int rate, int pitch, int volume, bool ssml, bool trimSilence)
        {
            string args = string.Join("\t",
                voice.Engine == TTSEngineKind.OneCore ? "W" : "S",
                Base64(voice.Id),
                string.IsNullOrEmpty(voice.Culture) ? "en-US" : voice.Culture,
                rate.ToString(CultureInfo.InvariantCulture),
                pitch.ToString(CultureInfo.InvariantCulture),
                volume.ToString(CultureInfo.InvariantCulture),
                ssml ? "1" : "0",
                Base64(text));

            // Synthesis runs far faster than real time; the limit only catches a hung engine.
            int timeoutMs = 20000 + Math.Min(text.Length, 200000) * 25;

            return Worker.Send("SPEAK", args, timeoutMs, payload =>
            {
                var data = WavUtility.Decode(Convert.FromBase64String(payload));
                if (trimSilence) data = data.Trimmed();
                data.Text = text;
                data.Voice = voice;
                return data;
            });
        }

        public static void Shutdown()
        {
            TTSWorker worker;
            lock (Gate)
            {
                worker = _worker;
                _worker = null;
            }
            worker?.Dispose();
        }

        static TTSVoice[] ParseVoices(string payload)
        {
            string list = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var voices = new List<TTSVoice>();
            foreach (string line in list.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] f = line.Split('|');
                if (f.Length < 5) continue;
                var engine = f[0] == "W" ? TTSEngineKind.OneCore : TTSEngineKind.Sapi;
                voices.Add(new TTSVoice(engine, f[1], f[2], f[3], f[4]));
            }
            return voices.ToArray();
        }

        static string Base64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? ""));
#else
        public static bool IsAvailable => false;

        static TTSException NotSupported() =>
            new TTSException("Real-time speech needs Windows. Bake your lines in the Editor to play them on this platform.");

        public static Task<TTSVoice[]> GetVoicesAsync()
        {
            var tcs = new TaskCompletionSource<TTSVoice[]>();
            tcs.SetException(NotSupported());
            return tcs.Task;
        }

        public static Task<TTSAudioData> SynthesizeAsync(TTSVoice voice, string text, int rate, int pitch, int volume, bool ssml, bool trimSilence)
        {
            var tcs = new TaskCompletionSource<TTSAudioData>();
            tcs.SetException(NotSupported());
            return tcs.Task;
        }

        public static void Shutdown() { }
#endif
    }
}
