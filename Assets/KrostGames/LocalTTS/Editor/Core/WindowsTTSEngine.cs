using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KrostGames.TTS.Engine;
using UnityEditor;
using UnityEngine;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>
    /// Bakes speech into .wav assets from the Editor, with the same engine the runtime uses.
    /// Prefer the async methods: they keep the Editor responsive. The synchronous v1.0 methods are kept for existing scripts.
    /// </summary>
    public static class WindowsTTSEngine
    {
        /// <summary>One line of a batch: the text and an optional file name (without extension).</summary>
        public struct BatchEntry
        {
            public string Text;
            public string FileName;

            public BatchEntry(string text, string fileName = null)
            {
                Text = text;
                FileName = fileName;
            }
        }

        /// <summary>A clip produced by a batch and the text it says.</summary>
        public struct BatchResult
        {
            public AudioClip Clip;
            public string Text;
        }

        // ------------------------------------------------------------------ Async API

        /// <summary>
        /// Synthesizes the text and saves it as a .wav asset in <paramref name="folder"/>
        /// (default: the output folder, plus a language sub-folder when that option is on).
        /// With a file name, an existing file of that name is replaced and keeps its references;
        /// without one, the name comes from the text and never overwrites anything.
        /// </summary>
        public static async Task<AudioClip> GenerateAsync(string text, TTSVoiceOptions voice = null, string fileName = null, string folder = null)
        {
            voice = voice ?? LocalTTS.DefaultVoice;
            TTSAudioData data = await LocalTTS.SynthesizeDataAsync(text, voice);
            string directory = string.IsNullOrEmpty(folder) ? FolderFor(data.Voice) : folder.TrimEnd('/');
            string name = TTSEditorUtility.CleanFileName(fileName);
            string path = name.Length > 0 ? $"{directory}/{name}.wav" : TTSEditorUtility.UniquePath(directory, TTSEditorUtility.Slug(text));
            return Save(data, path);
        }

        /// <summary>
        /// Bakes a line for a speaker or the Line Library. The file name comes from the text and the voice settings,
        /// so baking the same line twice reuses the existing clip, and identical lines share one file.
        /// </summary>
        public static async Task<AudioClip> BakeAsync(string text, TTSVoiceOptions voice)
        {
            voice = voice ?? LocalTTS.DefaultVoice;
            IReadOnlyList<TTSVoice> voices = await LocalTTS.GetVoicesAsync();
            TTSVoice resolved = LocalTTS.ResolveVoice(voice, voices, false);
            string path = $"{FolderFor(resolved)}/{TTSEditorUtility.Slug(text, 32)}_{voice.Signature(text).Substring(0, 8)}.wav";

            var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (existing != null) return existing;

            TTSAudioData data = await LocalTTS.SynthesizeDataAsync(text, voice);
            return Save(data, path);
        }

        /// <summary>
        /// Generates many clips one after another. <paramref name="nameFor"/> receives (entry, index, voice) and returns
        /// the file name (null = from the text). Files are imported together at the end, even when cancelled.
        /// </summary>
        public static async Task<List<BatchResult>> BatchGenerateAsync(IList<BatchEntry> entries, TTSVoiceOptions voice,
            Func<BatchEntry, int, TTSVoice, string> nameFor = null, Action<int, int, string> progress = null,
            CancellationToken cancellationToken = default)
        {
            voice = voice ?? LocalTTS.DefaultVoice;
            var paths = new List<string>();
            var texts = new List<string>();
            var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string text = entries[i].Text;
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    progress?.Invoke(i, entries.Count, text);

                    TTSAudioData data = await LocalTTS.SynthesizeDataAsync(text, voice, cancellationToken);
                    string directory = FolderFor(data.Voice);
                    TTSEditorUtility.EnsureFolder(directory);

                    string name = TTSEditorUtility.CleanFileName(nameFor?.Invoke(entries[i], i, data.Voice) ?? entries[i].FileName);
                    string path = name.Length > 0 ? $"{directory}/{name}.wav" : TTSEditorUtility.UniquePath(directory, TTSEditorUtility.Slug(text));
                    for (int n = 2; !reserved.Add(path); n++) path = $"{directory}/{(name.Length > 0 ? name : TTSEditorUtility.Slug(text))}_{n}.wav";

                    File.WriteAllBytes(TTSEditorUtility.ToAbsolute(path), data.ToWav());
                    paths.Add(path);
                    texts.Add(text);
                }
                progress?.Invoke(entries.Count, entries.Count, "");
            }
            finally
            {
                ImportAll(paths);
            }

            var results = new List<BatchResult>();
            for (int i = 0; i < paths.Count; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
                if (clip != null) results.Add(new BatchResult { Clip = clip, Text = texts[i] });
            }
            return results;
        }

        // ------------------------------------------------------------------ v1.0 API (synchronous, blocks the Editor)

        /// <summary>v1.0 API. Generates a .wav and returns the imported clip, or null on error. Blocks the Editor; prefer <see cref="GenerateAsync"/>.</summary>
        public static AudioClip Generate(string text, TTSLanguage language = TTSLanguage.English, string fileName = null,
            int rate = 0, int volume = 100, string voiceName = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogError("[LocalTTS] Text cannot be empty.");
                return null;
            }
            try
            {
                var voice = new TTSVoiceOptions(voiceName, language, rate, 0, volume);
                TTSAudioData data = SynthesizeBlocking(text, voice);
                string directory = FolderFor(data.Voice);
                string name = TTSEditorUtility.CleanFileName(fileName);
                string path = name.Length > 0 ? $"{directory}/{name}.wav" : TTSEditorUtility.UniquePath(directory, TTSEditorUtility.Slug(text));
                AudioClip clip = Save(data, path);
                Debug.Log($"[LocalTTS] Generated {path} ({clip.length:F1}s)");
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError("[LocalTTS] " + e.Message);
                return null;
            }
        }

        /// <summary>v1.0 API. Generates one clip per entry with a cancelable progress bar. Blocks the Editor.</summary>
        public static List<AudioClip> BatchGenerate(List<(string text, string fileName)> entries, TTSLanguage language,
            int rate = 0, int volume = 100, string voiceName = null, string namingTemplate = null)
        {
            var results = new List<AudioClip>();
            var voice = new TTSVoiceOptions(voiceName, language, rate, 0, volume);
            var paths = new List<string>();
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var (text, rawName) = entries[i];
                    if (string.IsNullOrWhiteSpace(text)) { paths.Add(null); continue; }
                    if (EditorUtility.DisplayCancelableProgressBar("Local TTS", $"[{i + 1}/{entries.Count}] {Short(text)}", (float)i / entries.Count))
                        break;

                    TTSAudioData data = SynthesizeBlocking(text, voice);
                    string directory = FolderFor(data.Voice);
                    TTSEditorUtility.EnsureFolder(directory);
                    string name = string.IsNullOrWhiteSpace(namingTemplate)
                        ? TTSEditorUtility.CleanFileName(rawName)
                        : ApplyNamingTemplate(namingTemplate, language, i + 1, rawName, text);
                    string path = name.Length > 0 ? $"{directory}/{name}.wav" : TTSEditorUtility.UniquePath(directory, TTSEditorUtility.Slug(text));
                    File.WriteAllBytes(TTSEditorUtility.ToAbsolute(path), data.ToWav());
                    paths.Add(path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[LocalTTS] " + e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                ImportAll(paths.Where(p => p != null).ToList());
            }
            foreach (string path in paths) results.Add(path == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            Debug.Log($"[LocalTTS] Batch complete: {results.Count(c => c != null)}/{entries.Count} clips generated.");
            return results;
        }

        /// <summary>v1.0 API. Speaks the text in the Editor without writing a file.</summary>
        public static void LivePreview(string text, int rate = 0, int volume = 100, string voiceName = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            LocalTTS.Speak(text, new TTSVoiceOptions(voiceName, LocalTTS.DefaultVoice.language, rate, 0, volume));
        }

        public static void StopLivePreview() => TTSEditorAudio.Stop();

        public static bool IsLivePreviewPlaying => TTSEditorAudio.IsBusy;

        /// <summary>v1.0 API. Installed voice names (both engines).</summary>
        public static string[] GetInstalledVoices() => LoadVoicesBlocking().Select(v => v.DisplayName).ToArray();

        /// <summary>v1.0 API. Installed voice names for a language.</summary>
        public static string[] GetVoicesForLanguage(TTSLanguage language) =>
            LoadVoicesBlocking().Where(v => v.Matches(language)).Select(v => v.DisplayName).ToArray();

        /// <summary>v1.0 API. Voice name to culture code.</summary>
        public static Dictionary<string, string> GetInstalledVoicesWithCulture()
        {
            var map = new Dictionary<string, string>();
            foreach (TTSVoice v in LoadVoicesBlocking()) map[v.DisplayName] = v.Culture;
            return map;
        }

        public static void RefreshVoiceCache() => _ = LocalTTS.GetVoicesAsync(true);

        public static string GetCultureForVoice(string voiceName)
        {
            foreach (TTSVoice v in LocalTTS.Voices)
                if (string.Equals(v.DisplayName, voiceName, StringComparison.OrdinalIgnoreCase) || string.Equals(v.Name, voiceName, StringComparison.OrdinalIgnoreCase))
                    return v.Culture;
            return "unknown";
        }

        public static string GetLanguageSubfolder(TTSLanguage language) => language.Code();

        /// <summary>v1.0 API. Tokens: {lang} {id} {name} (plus {voice}).</summary>
        public static string ApplyNamingTemplate(string template, TTSLanguage language, int id, string name, string text)
        {
            string baseName = string.IsNullOrWhiteSpace(name) ? TTSEditorUtility.Slug(text) : TTSEditorUtility.CleanFileName(name);
            if (string.IsNullOrWhiteSpace(template)) return baseName;
            return TTSEditorUtility.ApplyTemplate(template, language.Code(), id, baseName, LocalTTS.DefaultVoice.voiceName);
        }

        /// <summary>v1.0 API. File-safe name from a text.</summary>
        public static string SanitizeFileName(string text) => TTSEditorUtility.Slug(text);

        // ------------------------------------------------------------------ Internals

        static string FolderFor(TTSVoice voice)
        {
            string root = TTSEditorUtility.OutputFolder;
            return TTSProjectSettings.instance.OrganizeByLanguage && voice != null ? root + "/" + voice.LanguageCode : root;
        }

        static AudioClip Save(TTSAudioData data, string path)
        {
            TTSEditorUtility.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            File.WriteAllBytes(TTSEditorUtility.ToAbsolute(path), data.ToWav());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new TTSException("The .wav was written but Unity could not import it: " + path);
            return clip;
        }

        static void ImportAll(List<string> paths)
        {
            if (paths.Count == 0) return;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in paths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        // The worker completes its tasks on its own thread, so waiting on them here cannot deadlock.
        static TTSAudioData SynthesizeBlocking(string text, TTSVoiceOptions voice)
        {
            if (!TTSEngine.IsAvailable) throw new TTSException("Speech synthesis needs the Windows Editor.");
            TTSVoice resolved = LocalTTS.ResolveVoice(voice, LoadVoicesBlocking(), true) ??
                                throw new TTSException("No Windows voices are installed. Add one in Windows Settings > Time & language > Speech.");
            return Wait(TTSEngine.SynthesizeAsync(resolved, text, Mathf.Clamp(voice.rate, -10, 10), Mathf.Clamp(voice.pitch, -10, 10),
                Mathf.Clamp(voice.volume, 0, 100), LocalTTS.LooksLikeSsml(text), LocalTTS.TrimSilence), 120000);
        }

        static IReadOnlyList<TTSVoice> LoadVoicesBlocking()
        {
            if (LocalTTS.Voices.Count > 0 || !TTSEngine.IsAvailable) return LocalTTS.Voices;
            return Wait(TTSEngine.GetVoicesAsync(), 30000);
        }

        static T Wait<T>(Task<T> task, int timeoutMs)
        {
            try
            {
                if (!task.Wait(timeoutMs)) throw new TTSException("The speech engine did not answer in time.");
                return task.Result;
            }
            catch (AggregateException e)
            {
                throw e.InnerException ?? e;
            }
        }

        static string Short(string text) => text.Length > 50 ? text.Substring(0, 50) + "..." : text;
    }
}
