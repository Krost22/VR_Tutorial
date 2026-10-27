using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>Paths, file naming and small helpers shared by the Editor tools.</summary>
    public static class TTSEditorUtility
    {
        // GUID of "Editor/KrostGames.LocalTTS.Editor.asmdef": finds the package wherever it was moved to.
        const string EditorAssemblyGuid = "7b1cd4736065711448c6c908f7eb31f4";
        static string _root;

        /// <summary>Folder that holds the package, e.g. "Assets/KrostGames/LocalTTS".</summary>
        public static string PackageRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(_root)) return _root;
                string asmdef = AssetDatabase.GUIDToAssetPath(EditorAssemblyGuid);
                _root = string.IsNullOrEmpty(asmdef)
                    ? "Assets/KrostGames/LocalTTS"
                    : Path.GetDirectoryName(Path.GetDirectoryName(asmdef)).Replace('\\', '/');
                return _root;
            }
        }

        /// <summary>True when the package sits in Packages/ (read-only), so generated files must go to Assets/.</summary>
        public static bool IsReadOnlyPackage => PackageRoot.StartsWith("Packages/", StringComparison.Ordinal);

        public static string DefaultOutputFolder => IsReadOnlyPackage ? "Assets/Local TTS Audio" : PackageRoot + "/GeneratedAudio";

        public static string OutputFolder => TTSProjectSettings.instance.OutputFolder;

        public static string UIFolder => PackageRoot + "/Editor/UI";

        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        public static string ToAbsolute(string assetPath) => Path.GetFullPath(Path.Combine(ProjectRoot, assetPath));

        /// <summary>"C:/.../Assets/Foo" to "Assets/Foo". Null when the folder is outside this project's Assets.</summary>
        public static string ToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;
            string full = Path.GetFullPath(absolutePath).Replace('\\', '/').TrimEnd('/');
            string assets = Application.dataPath.Replace('\\', '/');
            if (string.Equals(full, assets, StringComparison.OrdinalIgnoreCase)) return "Assets";
            return full.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase) ? "Assets" + full.Substring(assets.Length) : null;
        }

        /// <summary>Creates a folder and its parents through the AssetDatabase (so they get .meta files).</summary>
        public static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        public static void PingFolder(string folder)
        {
            EnsureFolder(folder);
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
            if (asset == null) return;
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        // ------------------------------------------------------------------ Naming

        /// <summary>File-safe slug: lowercase ASCII letters, digits and underscores, accents removed ("¡Hola, Raúl!" becomes "hola_raul").</summary>
        public static string Slug(string text, int maxLength = 40)
        {
            if (string.IsNullOrWhiteSpace(text)) return "tts";
            var sb = new StringBuilder();
            bool underscore = false;
            foreach (char c in text.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                char lower = char.ToLowerInvariant(c);
                if ((lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9'))
                {
                    sb.Append(lower);
                    underscore = false;
                }
                else if (!underscore && sb.Length > 0)
                {
                    sb.Append('_');
                    underscore = true;
                }
                if (sb.Length >= maxLength) break;
            }
            string slug = sb.ToString().Trim('_');
            return slug.Length == 0 ? "tts" : slug;
        }

        /// <summary>Removes characters file systems reject, folder separators and a typed ".wav" extension.</summary>
        public static string CleanFileName(string name)
        {
            name = (name ?? "").Trim();
            if (name.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 4);
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Replace('/', '_').Replace('\\', '_').Trim().Trim('.');
            return name;
        }

        /// <summary>Fills a naming template. Tokens: {lang} {id} {name} {voice}.</summary>
        public static string ApplyTemplate(string template, string languageCode, int id, string name, string voiceName)
        {
            string result = (string.IsNullOrWhiteSpace(template) ? "{name}" : template)
                .Replace("{lang}", string.IsNullOrEmpty(languageCode) ? "XX" : languageCode)
                .Replace("{id}", id.ToString("D3", CultureInfo.InvariantCulture))
                .Replace("{name}", name ?? "")
                .Replace("{voice}", Slug(voiceName, 24));
            result = CleanFileName(result);
            return result.Length == 0 ? "tts" : result;
        }

        /// <summary>"folder/name.wav", or "folder/name_2.wav", "_3"... when the file already exists.</summary>
        public static string UniquePath(string folder, string name)
        {
            string path = $"{folder}/{name}.wav";
            for (int i = 2; File.Exists(ToAbsolute(path)); i++) path = $"{folder}/{name}_{i}.wav";
            return path;
        }

        // ------------------------------------------------------------------ Generated clips

        public static string[] FindGeneratedClips()
        {
            string folder = OutputFolder;
            if (!AssetDatabase.IsValidFolder(folder)) return Array.Empty<string>();
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++) paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            return paths;
        }

        /// <summary>Moves every clip of the output folder to the OS trash (recoverable). Returns how many were moved.</summary>
        public static int TrashGeneratedClips()
        {
            string[] paths = FindGeneratedClips();
            if (paths.Length == 0) return 0;
            var failed = new List<string>();
            AssetDatabase.MoveAssetsToTrash(paths, failed);
            return paths.Length - failed.Count;
        }

        // ------------------------------------------------------------------ Assigning

        /// <summary>
        /// Puts a clip on the selected GameObject: as a line of its TTSSpeaker, the clip of its TTSAudioPlayer,
        /// or the clip of its AudioSource. Returns a short description, or null when nothing was assigned.
        /// </summary>
        public static string AssignToSelection(AudioClip clip, string text)
        {
            GameObject go = Selection.activeGameObject;
            if (clip == null || go == null) return null;

            var speaker = go.GetComponent<TTSSpeaker>();
            if (speaker != null)
            {
                Undo.RecordObject(speaker, "Assign TTS Clip");
                TTSLine line = speaker.Lines.Find(l => l != null && l.text == text);
                if (line == null)
                {
                    line = new TTSLine(text);
                    speaker.Lines.Add(line);
                }
                // The user picked this clip on purpose: treat it as baked for the speaker's current voice.
                line.SetBaked(clip, speaker.Voice);
                MarkChanged(speaker);
                return $"TTS Speaker \"{go.name}\" (line {speaker.Lines.IndexOf(line) + 1})";
            }

            var player = go.GetComponent<TTSAudioPlayer>();
            if (player != null)
            {
                Undo.RecordObject(player, "Assign TTS Clip");
                player.Clip = clip;
                MarkChanged(player);
                return $"TTS Audio Player \"{go.name}\"";
            }

            var source = go.GetComponent<AudioSource>();
            if (source != null)
            {
                Undo.RecordObject(source, "Assign TTS Clip");
                source.clip = clip;
                MarkChanged(source);
                return $"Audio Source \"{go.name}\"";
            }
            return null;
        }

        public static void MarkChanged(UnityEngine.Object target)
        {
            if (target == null) return;
            EditorUtility.SetDirty(target);
            if (target is Component component && PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        // ------------------------------------------------------------------ Line Library

        /// <summary>The project's Line Library, created in the output folder's Resources/ when <paramref name="create"/> is true.</summary>
        public static TTSLibrary GetLibrary(bool create)
        {
            TTSLibrary library = TTSLibrary.Instance;
            if (library == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:" + nameof(TTSLibrary));
                if (guids.Length > 0) library = AssetDatabase.LoadAssetAtPath<TTSLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (library != null || !create) return library;

            string folder = OutputFolder + "/Resources";
            EnsureFolder(folder);
            library = ScriptableObject.CreateInstance<TTSLibrary>();
            AssetDatabase.CreateAsset(library, $"{folder}/{TTSLibrary.ResourceName}.asset");
            AssetDatabase.SaveAssets();
            TTSLibrary.ResetInstance();
            return library;
        }

        // ------------------------------------------------------------------ Build targets

        /// <summary>True when the active build target can synthesize speech at runtime.</summary>
        public static bool BuildTargetHasRealtime(BuildTarget target) =>
            target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64;

        public static string LanguageName(string code)
        {
            if (string.IsNullOrEmpty(code)) return "?";
            try
            {
                string name = CultureInfo.GetCultureInfo(code.ToLowerInvariant()).NativeName;
                return name.Length > 0 ? char.ToUpper(name[0], CultureInfo.InvariantCulture) + name.Substring(1) : code;
            }
            catch (CultureNotFoundException) { return code; }
        }
    }
}
