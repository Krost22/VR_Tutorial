using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using L = KrostGames.TTS.EditorTools.TTSLocalization;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>Menu entries: Tools > Local TTS, GameObject > Audio > TTS Speaker and the AudioSource context menu.</summary>
    public static class TTSMenus
    {
        [MenuItem("Tools/Local TTS/Line Library", false, 110)]
        public static void OpenLibrary()
        {
            TTSLibrary library = TTSEditorUtility.GetLibrary(true);
            Selection.activeObject = library;
            EditorGUIUtility.PingObject(library);
        }

        [MenuItem("Tools/Local TTS/Bake Speakers in Open Scenes", false, 111)]
        public static async void BakeSpeakersInOpenScenes()
        {
            if (!LocalTTS.IsRealtimeSupported)
            {
                Debug.LogWarning("[LocalTTS] Baking needs the Windows Editor.");
                return;
            }
            // FindObjectsOfTypeAll also returns prefab assets: keep only objects that live in a loaded scene.
            TTSSpeaker[] speakers = Resources.FindObjectsOfTypeAll<TTSSpeaker>()
                .Where(s => !EditorUtility.IsPersistent(s) && s.gameObject.scene.isLoaded && (s.hideFlags & HideFlags.HideInHierarchy) == 0)
                .ToArray();
            if (speakers.Length == 0)
            {
                Debug.Log("[LocalTTS] There are no TTS Speakers in the open scenes.");
                return;
            }
            int baked = await TTSBakeUtility.BakeSpeakersAsync(speakers);
            if (baked > 0) EditorSceneManager.MarkAllScenesDirty();
            Debug.Log(baked == 0
                ? $"[LocalTTS] All lines of the {speakers.Length} TTS Speaker(s) in the open scenes were already baked."
                : $"[LocalTTS] Baked {baked} line(s) on {speakers.Length} TTS Speaker(s). Save the scene to keep the references.");
        }

        [MenuItem("Tools/Local TTS/Open Demo Scene", false, 130)]
        public static void OpenDemoScene()
        {
            string path = TTSEditorUtility.PackageRoot + "/Demo/LocalTTS_Demo.unity";
            if (!File.Exists(TTSEditorUtility.ToAbsolute(path)))
            {
                Debug.LogWarning("[LocalTTS] Demo scene not found: " + path);
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }

        [MenuItem("Tools/Local TTS/Documentation", false, 131)]
        public static void OpenDocumentation()
        {
            string language = L.Current == UILanguage.Spanish ? "ES" : "EN";
            string pdf = TTSEditorUtility.ToAbsolute($"{TTSEditorUtility.PackageRoot}/Documentation/LocalTTS_Manual_{language}.pdf");
            string readme = TTSEditorUtility.ToAbsolute($"{TTSEditorUtility.PackageRoot}/{(language == "ES" ? "LEEME.md" : "README.md")}");
            if (File.Exists(pdf)) Application.OpenURL("file:///" + pdf.Replace('\\', '/'));
            else if (File.Exists(readme)) Application.OpenURL("file:///" + readme.Replace('\\', '/'));
            else Debug.LogWarning("[LocalTTS] Documentation not found in " + TTSEditorUtility.PackageRoot);
        }

        [MenuItem("GameObject/Audio/TTS Speaker", false, 10)]
        static void CreateSpeaker(MenuCommand command)
        {
            var go = new GameObject("TTS Speaker");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            go.AddComponent<TTSSpeaker>();
            Undo.RegisterCreatedObjectUndo(go, "Create TTS Speaker");
            Selection.activeObject = go;
        }

        [MenuItem("CONTEXT/AudioSource/Add TTS Speaker")]
        static void AddSpeakerToAudioSource(MenuCommand command)
        {
            var source = (AudioSource)command.context;
            if (source.GetComponent<TTSSpeaker>() != null) return;
            source.playOnAwake = false;
            Undo.AddComponent<TTSSpeaker>(source.gameObject);
        }

        [MenuItem("CONTEXT/AudioSource/Add TTS Speaker", true)]
        static bool CanAddSpeakerToAudioSource(MenuCommand command) =>
            command.context is AudioSource source && source.GetComponent<TTSSpeaker>() == null;
    }
}
