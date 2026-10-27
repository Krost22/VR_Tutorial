using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>Plays audio in the Editor without entering Play Mode, through Unity's own clip preview.</summary>
    [InitializeOnLoad]
    public static class TTSEditorAudio
    {
        static readonly MethodInfo PlayMethod;
        static readonly MethodInfo StopMethod;
        static readonly MethodInfo IsPlayingMethod;
        static AudioClip _owned;
        static int _request;

        static TTSEditorAudio()
        {
            Type util = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            PlayMethod = util?.GetMethod("PlayPreviewClip", flags, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            StopMethod = util?.GetMethod("StopAllPreviewClips", flags);
            IsPlayingMethod = util?.GetMethod("IsPreviewClipPlaying", flags);

            LocalTTS.EditorSpeak = (text, voice) => SpeakQuietly(text, voice);
            LocalTTS.EditorStop = Stop;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
        }

        /// <summary>True while a clip is playing in the preview.</summary>
        public static bool IsPlaying
        {
            get
            {
                try { return IsPlayingMethod != null && (bool)IsPlayingMethod.Invoke(null, null); }
                catch (Exception) { return false; }
            }
        }

        /// <summary>True while a line is being synthesized for the preview.</summary>
        public static bool IsPreparing { get; private set; }

        /// <summary>True while preparing or playing.</summary>
        public static bool IsBusy => IsPreparing || IsPlaying;

        /// <summary>Plays a clip. With <paramref name="destroyWhenReplaced"/> the clip is destroyed when another one plays or on Stop.</summary>
        public static void Play(AudioClip clip, bool destroyWhenReplaced = false)
        {
            StopPlayback();
            if (clip == null) return;
            if (destroyWhenReplaced) _owned = clip;
            if (PlayMethod == null)
            {
                Debug.LogWarning("[LocalTTS] This Unity version does not expose the Editor audio preview.");
                return;
            }
            PlayMethod.Invoke(null, new object[] { clip, 0, false });
        }

        /// <summary>Stops the preview and cancels a line that is still being synthesized.</summary>
        public static void Stop()
        {
            _request++;
            IsPreparing = false;
            StopPlayback();
        }

        /// <summary>Synthesizes the text and plays it right away. Nothing is written to disk.</summary>
        public static async Task SpeakAsync(string text, TTSVoiceOptions voice)
        {
            int request = ++_request;
            IsPreparing = true;
            try
            {
                AudioClip clip = await LocalTTS.SynthesizeAsync(text, voice);
                if (request != _request)
                {
                    UnityEngine.Object.DestroyImmediate(clip);
                    return;
                }
                Play(clip, true);
            }
            finally
            {
                if (request == _request) IsPreparing = false;
            }
        }

        static async void SpeakQuietly(string text, TTSVoiceOptions voice)
        {
            try { await SpeakAsync(text, voice); }
            catch (Exception e) { Debug.LogWarning("[LocalTTS] " + e.Message); }
        }

        static void StopPlayback()
        {
            try { StopMethod?.Invoke(null, null); }
            catch (Exception) { /* preview already stopped */ }
            if (_owned != null)
            {
                UnityEngine.Object.DestroyImmediate(_owned);
                _owned = null;
            }
        }
    }
}
