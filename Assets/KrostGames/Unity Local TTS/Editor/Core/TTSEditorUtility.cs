#if UNITY_EDITOR_WIN
using System.IO;
using UnityEditor;
using UnityEngine;
using KrostGames.LocalTTS.Runtime.Core;

namespace KrostGames.LocalTTS.Editor.Core
{
    /// <summary>
    /// Utility helpers for the TTS editor tooling.
    /// </summary>
    public static class TTSEditorUtility
    {
        private const string GeneratedAudioFolder = "Assets/Unity Local TTS/GeneratedAudio";

        /// <summary>
        /// Opens the GeneratedAudio folder in the Project window.
        /// </summary>
        public static void PingGeneratedFolder()
        {
            string absoluteDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../", GeneratedAudioFolder));
            Directory.CreateDirectory(absoluteDir);
            AssetDatabase.Refresh();

            Object folder = AssetDatabase.LoadAssetAtPath<Object>(GeneratedAudioFolder);
            if (folder != null)
                EditorGUIUtility.PingObject(folder);
        }

        /// <summary>
        /// Deletes all generated .wav files in the output folder (including subfolders).
        /// </summary>
        public static int ClearGeneratedAudio()
        {
            string absoluteDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../", GeneratedAudioFolder));
            if (!Directory.Exists(absoluteDir))
                return 0;

            // Search recursively to include language subfolders
            string[] wavFiles = Directory.GetFiles(absoluteDir, "*.wav", SearchOption.AllDirectories);
            int count = 0;

            foreach (string file in wavFiles)
            {
                File.Delete(file);

                string metaFile = file + ".meta";
                if (File.Exists(metaFile))
                    File.Delete(metaFile);

                count++;
            }

            if (count > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[TTS] Cleared {count} generated audio file(s).");
            }

            return count;
        }

        /// <summary>
        /// Returns the number of .wav files in the generated folder (including subfolders).
        /// </summary>
        public static int GetGeneratedClipCount()
        {
            string absoluteDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../", GeneratedAudioFolder));
            if (!Directory.Exists(absoluteDir))
                return 0;

            return Directory.GetFiles(absoluteDir, "*.wav", SearchOption.AllDirectories).Length;
        }

        /// <summary>
        /// Tries to auto-assign a clip to the selected GameObject's AudioSource or TTSAudioPlayer.
        /// </summary>
        /// <returns>True if assignment was successful.</returns>
        public static bool TryAutoAssignClip(AudioClip clip)
        {
            if (clip == null || Selection.activeGameObject == null)
                return false;

            GameObject selected = Selection.activeGameObject;

            // Try TTSAudioPlayer first
            var player = selected.GetComponent<TTSAudioPlayer>();
            if (player != null)
            {
                Undo.RecordObject(player, "TTS Auto-Assign Clip");
                player.Clip = clip;
                EditorUtility.SetDirty(player);
                Debug.Log($"[TTS] Auto-assigned clip to TTSAudioPlayer on '{selected.name}'.");
                return true;
            }

            // Try AudioSource
            var audioSource = selected.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                Undo.RecordObject(audioSource, "TTS Auto-Assign Clip");
                audioSource.clip = clip;
                EditorUtility.SetDirty(audioSource);
                Debug.Log($"[TTS] Auto-assigned clip to AudioSource on '{selected.name}'.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Ensures language subfolders exist.
        /// </summary>
        public static void EnsureLanguageFolders()
        {
            string baseDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../", GeneratedAudioFolder));
            Directory.CreateDirectory(Path.Combine(baseDir, "EN"));
            Directory.CreateDirectory(Path.Combine(baseDir, "ES"));
            Directory.CreateDirectory(Path.Combine(baseDir, "PT"));
            AssetDatabase.Refresh();
        }
    }
}
#endif
