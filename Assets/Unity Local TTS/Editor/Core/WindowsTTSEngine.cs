#if UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using KrostGames.LocalTTS.Runtime.Core;
using Debug = UnityEngine.Debug;

namespace KrostGames.LocalTTS.Editor.Core
{
    /// <summary>
    /// Generates .wav audio files from text using Windows SAPI via PowerShell.
    /// Editor-only: System.Speech is unavailable in CoreCLR builds.
    /// </summary>
    public static class WindowsTTSEngine
    {
        private const string GeneratedAudioFolder = "Assets/Unity Local TTS/GeneratedAudio";
        private const int ProcessTimeoutMs = 30_000;

        /// <summary>
        /// Cached voice data: voice name → culture code (e.g. "en-US", "es-MX").
        /// </summary>
        private static Dictionary<string, string> _voiceCultureMap;

        /// <summary>
        /// Returns the language subfolder name for organizing generated files.
        /// </summary>
        public static string GetLanguageSubfolder(TTSLanguage language)
        {
            return language switch
            {
                TTSLanguage.English => "EN",
                TTSLanguage.Spanish => "ES",
                TTSLanguage.Portuguese => "PT",
                _ => "EN"
            };
        }

        /// <summary>
        /// Generates a .wav file from the given text using Windows SAPI.
        /// Files are organized into language subfolders.
        /// </summary>
        public static AudioClip Generate(
            string text,
            TTSLanguage language = TTSLanguage.English,
            string fileName = null,
            int rate = 0,
            int volume = 100,
            string voiceName = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogError("[TTS] Text cannot be empty.");
                return null;
            }

            // Auto-select voice by language if none specified
            if (string.IsNullOrEmpty(voiceName))
            {
                voiceName = FindVoiceForLanguage(language);
                if (string.IsNullOrEmpty(voiceName))
                {
                    Debug.LogError($"[TTS] No SAPI voice found for language: {language}. " +
                                   "Install a voice pack for this language in Windows Settings > Time & Language > Speech.");
                    return null;
                }

                Debug.Log($"[TTS] Auto-selected voice: {voiceName} (language: {language})");
            }

            // Build path with language subfolder
            string langFolder = GetLanguageSubfolder(language);
            string relativeDir = $"{GeneratedAudioFolder}/{langFolder}";
            string absoluteDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../", relativeDir));
            Directory.CreateDirectory(absoluteDir);

            // Sanitize file name
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = SanitizeFileName(text);

            string absolutePath = Path.Combine(absoluteDir, $"{fileName}.wav");
            string relativePath = $"{relativeDir}/{fileName}.wav";

            // Build PowerShell script
            string psScript = BuildPowerShellScript(text, absolutePath, rate, volume, voiceName);

            // Execute
            bool success = ExecutePowerShell(psScript);
            if (!success)
                return null;

            // Verify file was created
            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"[TTS] Expected file not found: {absolutePath}");
                return null;
            }

            // Import into Unity
            AssetDatabase.Refresh();
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);

            if (clip == null)
                Debug.LogError($"[TTS] Failed to load generated clip at: {relativePath}");
            else
                Debug.Log($"[TTS] Generated: {relativePath} ({clip.length:F1}s)");

            return clip;
        }

        /// <summary>
        /// Tracked process for live preview, allowing stop/cancel.
        /// </summary>
        private static Process _livePreviewProcess;

        /// <summary>
        /// Whether a live preview is currently playing.
        /// </summary>
        public static bool IsLivePreviewPlaying =>
            _livePreviewProcess != null && !_livePreviewProcess.HasExited;

        /// <summary>
        /// Speaks text through Windows speakers without generating a file (Live Preview).
        /// Runs asynchronously so the editor remains responsive.
        /// </summary>
        public static void LivePreview(string text, int rate = 0, int volume = 100, string voiceName = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            // Stop any previous live preview
            StopLivePreview();

            string escapedText = text.Replace("'", "''");

            var sb = new StringBuilder();
            sb.Append("Add-Type -AssemblyName System.Speech; ");
            sb.Append("$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer; ");

            if (!string.IsNullOrEmpty(voiceName))
            {
                string escapedVoice = voiceName.Replace("'", "''");
                sb.Append($"$synth.SelectVoice('{escapedVoice}'); ");
            }

            sb.Append($"$synth.Rate = {Mathf.Clamp(rate, -10, 10)}; ");
            sb.Append($"$synth.Volume = {Mathf.Clamp(volume, 0, 100)}; ");
            sb.Append("$synth.SetOutputToDefaultAudioDevice(); ");
            sb.Append($"$synth.Speak('{escapedText}'); ");
            sb.Append("$synth.Dispose();");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{sb}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                _livePreviewProcess = Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TTS] Live preview failed: {ex.Message}");
                _livePreviewProcess = null;
            }
        }

        /// <summary>
        /// Stops any currently playing live preview.
        /// </summary>
        public static void StopLivePreview()
        {
            if (_livePreviewProcess == null) return;

            try
            {
                if (!_livePreviewProcess.HasExited)
                    _livePreviewProcess.Kill();
            }
            catch (Exception)
            {
                // Process may have already exited
            }
            finally
            {
                _livePreviewProcess.Dispose();
                _livePreviewProcess = null;
            }
        }

        /// <summary>
        /// Batch generates multiple clips from a list of text entries.
        /// </summary>
        /// <param name="entries">List of (text, fileName) tuples.</param>
        /// <param name="language">Target language.</param>
        /// <param name="rate">Speech rate.</param>
        /// <param name="volume">Volume.</param>
        /// <param name="voiceName">Explicit voice name, or null for auto.</param>
        /// <param name="namingTemplate">Naming template. Tokens: {lang}, {id}, {name}. Null = default.</param>
        /// <returns>List of generated AudioClips (nulls for failures).</returns>
        public static List<AudioClip> BatchGenerate(
            List<(string text, string fileName)> entries,
            TTSLanguage language,
            int rate = 0,
            int volume = 100,
            string voiceName = null,
            string namingTemplate = null)
        {
            var results = new List<AudioClip>();

            for (int i = 0; i < entries.Count; i++)
            {
                var (text, rawName) = entries[i];

                if (string.IsNullOrWhiteSpace(text))
                {
                    results.Add(null);
                    continue;
                }

                // Apply naming template
                string finalName = ApplyNamingTemplate(namingTemplate, language, i + 1, rawName, text);

                float progress = (float)(i + 1) / entries.Count;
                bool cancel = EditorUtility.DisplayCancelableProgressBar(
                    "Batch TTS Generation",
                    $"[{i + 1}/{entries.Count}] {(text.Length > 50 ? text.Substring(0, 50) + "..." : text)}",
                    progress);

                if (cancel)
                {
                    Debug.LogWarning($"[TTS] Batch generation cancelled at {i + 1}/{entries.Count}.");
                    EditorUtility.ClearProgressBar();
                    break;
                }

                AudioClip clip = Generate(text, language, finalName, rate, volume, voiceName);
                results.Add(clip);
            }

            EditorUtility.ClearProgressBar();
            int successCount = results.FindAll(c => c != null).Count;
            Debug.Log($"[TTS] Batch complete: {successCount}/{entries.Count} clips generated.");
            return results;
        }

        /// <summary>
        /// Applies a naming template to generate file names.
        /// Tokens: {lang}, {id}, {name}
        /// </summary>
        public static string ApplyNamingTemplate(
            string template, TTSLanguage language, int id, string name, string text)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                // If name is provided use it, otherwise sanitize from text
                return string.IsNullOrWhiteSpace(name) ? SanitizeFileName(text) : name;
            }

            string langCode = GetLanguageSubfolder(language);
            string safeName = string.IsNullOrWhiteSpace(name) ? SanitizeFileName(text) : name;

            string result = template
                .Replace("{lang}", langCode)
                .Replace("{id}", id.ToString("D3"))
                .Replace("{name}", safeName);

            // Sanitize the final result
            foreach (char c in Path.GetInvalidFileNameChars())
                result = result.Replace(c, '_');

            return result;
        }

        /// <summary>
        /// Returns the list of installed SAPI voices with culture codes.
        /// </summary>
        public static Dictionary<string, string> GetInstalledVoicesWithCulture()
        {
            const string script =
                "Add-Type -AssemblyName System.Speech; " +
                "$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                "$synth.GetInstalledVoices() | ForEach-Object { " +
                "  $v = $_.VoiceInfo; " +
                "  Write-Output ('{0}|{1}' -f $v.Name, $v.Culture.Name) " +
                "}; " +
                "$synth.Dispose();";

            string output = ExecutePowerShellWithOutput(script);
            var map = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(output))
                return map;

            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                int sep = line.IndexOf('|');
                if (sep > 0 && sep < line.Length - 1)
                {
                    string voName = line.Substring(0, sep).Trim();
                    string culture = line.Substring(sep + 1).Trim();
                    map[voName] = culture;
                }
            }

            _voiceCultureMap = map;
            return map;
        }

        /// <summary>
        /// Returns only the voice names that match the given language.
        /// </summary>
        public static string[] GetVoicesForLanguage(TTSLanguage language)
        {
            if (_voiceCultureMap == null || _voiceCultureMap.Count == 0)
                GetInstalledVoicesWithCulture();

            if (_voiceCultureMap == null)
                return Array.Empty<string>();

            string prefix = TTSVoiceSettings.GetCulturePrefix(language);
            var filtered = new List<string>();

            foreach (var kvp in _voiceCultureMap)
            {
                if (kvp.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    filtered.Add(kvp.Key);
            }

            return filtered.ToArray();
        }

        /// <summary>
        /// Returns all installed voice names (unfiltered).
        /// </summary>
        public static string[] GetInstalledVoices()
        {
            if (_voiceCultureMap == null || _voiceCultureMap.Count == 0)
                GetInstalledVoicesWithCulture();

            if (_voiceCultureMap == null)
                return Array.Empty<string>();

            var names = new string[_voiceCultureMap.Count];
            _voiceCultureMap.Keys.CopyTo(names, 0);
            return names;
        }

        /// <summary>
        /// Forces a refresh of the cached voice list.
        /// </summary>
        public static void RefreshVoiceCache()
        {
            _voiceCultureMap = null;
            GetInstalledVoicesWithCulture();
        }

        /// <summary>
        /// Gets the culture code for a specific voice name from the cache.
        /// </summary>
        public static string GetCultureForVoice(string voiceName)
        {
            if (_voiceCultureMap != null && _voiceCultureMap.TryGetValue(voiceName, out string culture))
                return culture;
            return "unknown";
        }

        #region Internal

        private static string FindVoiceForLanguage(TTSLanguage language)
        {
            string[] voices = GetVoicesForLanguage(language);
            return voices.Length > 0 ? voices[0] : null;
        }

        private static string BuildPowerShellScript(
            string text, string outputPath, int rate, int volume, string voiceName)
        {
            string escapedText = text.Replace("'", "''");
            string escapedPath = outputPath.Replace("'", "''");

            var sb = new StringBuilder();
            sb.Append("Add-Type -AssemblyName System.Speech; ");
            sb.Append("$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer; ");

            if (!string.IsNullOrEmpty(voiceName))
            {
                string escapedVoice = voiceName.Replace("'", "''");
                sb.Append($"$synth.SelectVoice('{escapedVoice}'); ");
            }

            sb.Append($"$synth.Rate = {Mathf.Clamp(rate, -10, 10)}; ");
            sb.Append($"$synth.Volume = {Mathf.Clamp(volume, 0, 100)}; ");
            sb.Append($"$synth.SetOutputToWaveFile('{escapedPath}'); ");
            sb.Append($"$synth.Speak('{escapedText}'); ");
            sb.Append("$synth.Dispose();");

            return sb.ToString();
        }

        private static bool ExecutePowerShell(string script)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        Debug.LogError("[TTS] Failed to start PowerShell process.");
                        return false;
                    }

                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit(ProcessTimeoutMs);

                    if (!process.HasExited)
                    {
                        process.Kill();
                        Debug.LogError("[TTS] PowerShell process timed out.");
                        return false;
                    }

                    if (process.ExitCode != 0)
                    {
                        Debug.LogError($"[TTS] PowerShell error (exit {process.ExitCode}):\n{stderr}");
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TTS] Exception: {ex.Message}");
                return false;
            }
        }

        private static string ExecutePowerShellWithOutput(string script)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return null;

                    string stdout = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(ProcessTimeoutMs);

                    return process.HasExited && process.ExitCode == 0 ? stdout.Trim() : null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TTS] Exception querying voices: {ex.Message}");
                return null;
            }
        }

        public static string SanitizeFileName(string text)
        {
            string sanitized = text.Length > 40 ? text.Substring(0, 40) : text;

            foreach (char c in Path.GetInvalidFileNameChars())
                sanitized = sanitized.Replace(c, '_');

            sanitized = sanitized.Replace(' ', '_').ToLowerInvariant();
            sanitized += $"_{DateTime.Now:yyyyMMdd_HHmmss}";
            return sanitized;
        }

        #endregion
    }
}
#endif
