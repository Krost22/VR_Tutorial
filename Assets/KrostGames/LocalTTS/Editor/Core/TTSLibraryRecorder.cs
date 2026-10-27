using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>
    /// Records the lines spoken from code in Play Mode (LocalTTS.Speak, TTSSpeaker.Speak(string)) into the Line Library,
    /// so they can be baked for platforms without real-time speech.
    /// </summary>
    [InitializeOnLoad]
    static class TTSLibraryRecorder
    {
        static int _recorded;
        static TTSLibrary _library;

        static TTSLibraryRecorder()
        {
            LocalTTS.LineRequested += OnLineRequested;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode) Flush();
            };
        }

        static void OnLineRequested(string text, TTSVoiceOptions voice)
        {
            if (!EditorApplication.isPlaying || !TTSProjectSettings.instance.RecordPlayModeLines || string.IsNullOrWhiteSpace(text)) return;
            voice = voice ?? LocalTTS.DefaultVoice;

            TTSLibrary library = TTSEditorUtility.GetLibrary(true);
            if (library == null || library.Find(text, voice) != null) return;

            library.Entries.Add(new TTSLibrary.Entry { text = text, voice = voice.Clone() });
            EditorUtility.SetDirty(library);
            _library = library;
            _recorded++;
        }

        static void Flush()
        {
            if (_recorded == 0 || _library == null) return;
            AssetDatabase.SaveAssetIfDirty(_library);
            Debug.Log(TTSLocalization.T(
                $"[LocalTTS] {_recorded} new line(s) spoken in Play Mode were added to the Line Library. Bake them (Tools > Local TTS > Line Library) to hear them on platforms without real-time speech.",
                $"[LocalTTS] Se agregaron {_recorded} línea(s) nuevas habladas en Play Mode a la Line Library. Hornéalas (Tools > Local TTS > Line Library) para oírlas en plataformas sin voz en tiempo real.",
                $"[LocalTTS] {_recorded} nova(s) fala(s) dita(s) no Play Mode foram adicionadas à Line Library. Gere os áudios (Tools > Local TTS > Line Library) para ouvi-las em plataformas sem voz em tempo real."), _library);
            _recorded = 0;
        }
    }

    /// <summary>Warns before a build when Line Library lines are not baked and the target cannot synthesize speech.</summary>
    class TTSBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (TTSEditorUtility.BuildTargetHasRealtime(report.summary.platform)) return;
            TTSLibrary library = TTSEditorUtility.GetLibrary(false);
            if (library == null) return;

            int pending = 0;
            foreach (TTSLibrary.Entry entry in library.Entries)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.text) && entry.clip == null) pending++;
            if (pending == 0) return;

            Debug.LogWarning($"[LocalTTS] {pending} line(s) in the Line Library have no baked clip. {report.summary.platform} has no real-time speech, " +
                             "so they will be silent in this build. Bake them in Tools > Local TTS > Line Library.", library);
        }
    }
}
