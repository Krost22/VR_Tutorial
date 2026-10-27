using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>Bakes speaker lines and Line Library entries into .wav clips, reporting progress in the Editor status bar.</summary>
    public static class TTSBakeUtility
    {
        /// <summary>Bakes the lines whose clip is missing or outdated (or every line). Returns how many lines were baked.</summary>
        public static async Task<int> BakeSpeakersAsync(IList<TTSSpeaker> speakers, bool onlyMissing = true)
        {
            var work = new List<(TTSSpeaker speaker, int line)>();
            foreach (TTSSpeaker speaker in speakers)
            {
                if (speaker == null) continue;
                TTSVoiceOptions voice = speaker.Voice;
                for (int i = 0; i < speaker.Lines.Count; i++)
                {
                    TTSLine line = speaker.Lines[i];
                    if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;
                    if (onlyMissing && line.IsBakedFor(voice)) continue;
                    work.Add((speaker, i));
                }
            }
            if (work.Count == 0) return 0;

            int progressId = Progress.Start("Local TTS", "Baking speaker lines");
            int baked = 0;
            try
            {
                foreach (var (speaker, index) in work)
                {
                    if (speaker == null || index >= speaker.Lines.Count) continue;
                    TTSLine line = speaker.Lines[index];
                    TTSVoiceOptions voice = speaker.Voice;
                    Progress.Report(progressId, baked, work.Count, Short(line.text));

                    AudioClip clip = await WindowsTTSEngine.BakeAsync(line.text, voice);
                    if (speaker == null) continue;
                    Undo.RecordObject(speaker, "Bake TTS Lines");
                    line.SetBaked(clip, voice);
                    TTSEditorUtility.MarkChanged(speaker);
                    baked++;
                }
                Progress.Finish(progressId);
            }
            catch (Exception e)
            {
                Progress.Finish(progressId, Progress.Status.Failed);
                Debug.LogWarning("[LocalTTS] Bake failed: " + e.Message);
            }
            return baked;
        }

        /// <summary>Bakes the Line Library entries whose clip is missing or outdated (or every entry).</summary>
        public static async Task<int> BakeLibraryAsync(TTSLibrary library, bool onlyMissing = true)
        {
            if (library == null) return 0;
            var work = new List<TTSLibrary.Entry>();
            foreach (TTSLibrary.Entry entry in library.Entries)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.text) && (!onlyMissing || !entry.IsBaked)) work.Add(entry);
            if (work.Count == 0) return 0;

            int progressId = Progress.Start("Local TTS", "Baking the Line Library");
            int baked = 0;
            try
            {
                foreach (TTSLibrary.Entry entry in work)
                {
                    Progress.Report(progressId, baked, work.Count, Short(entry.text));
                    AudioClip clip = await WindowsTTSEngine.BakeAsync(entry.text, entry.voice);
                    if (library == null) break;
                    Undo.RecordObject(library, "Bake Line Library");
                    entry.SetBaked(clip);
                    EditorUtility.SetDirty(library);
                    baked++;
                }
                AssetDatabase.SaveAssetIfDirty(library);
                Progress.Finish(progressId);
            }
            catch (Exception e)
            {
                Progress.Finish(progressId, Progress.Status.Failed);
                Debug.LogWarning("[LocalTTS] Bake failed: " + e.Message);
            }
            return baked;
        }

        static string Short(string text) => text.Length > 48 ? text.Substring(0, 48) + "..." : text;
    }
}
