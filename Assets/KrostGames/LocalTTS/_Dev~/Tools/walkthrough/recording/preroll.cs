// Pre-roll for the recording in the clean project: English UI, demo scene, empty Generator, tidy windows.
KrostGames.TTS.EditorTools.TTSLocalization.Current = KrostGames.TTS.EditorTools.UILanguage.English;
// retakes: drop clips made by a previous take (this is the throwaway recording project) and reload the scene unsaved
foreach (var sub in UnityEditor.AssetDatabase.GetSubFolders("Assets/KrostGames/LocalTTS/GeneratedAudio")) UnityEditor.AssetDatabase.DeleteAsset(sub);
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/KrostGames/LocalTTS/GeneratedAudio" })) UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/KrostGames/LocalTTS/Demo/LocalTTS_Demo.unity");
var user = KrostGames.TTS.EditorTools.TTSUserSettings.instance;
user.text = ""; user.history.Clear(); user.lastClipGuid = "";
user.voice.voiceName = "Microsoft Zira Desktop"; user.voice.language = KrostGames.TTS.TTSLanguage.English;
user.voice.rate = 0; user.voice.pitch = 0; user.voice.volume = 100;
user.SaveNow();
KrostGames.TTS.EditorTools.TTSProjectSettings.instance.OrganizeByLanguage = true;
// collapse Transform / AudioSource on the speakers so the TTS Speaker fits
foreach (var sp in UnityEngine.Object.FindObjectsByType<KrostGames.TTS.TTSSpeaker>(UnityEngine.FindObjectsSortMode.None))
    foreach (var c in sp.GetComponents<UnityEngine.Component>())
        UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(c, c is KrostGames.TTS.TTSSpeaker);
UnityEditor.Selection.activeObject = null;
// rebuild the Generator so it shows the empty state, docked next to the Inspector and in front
foreach (var w in UnityEngine.Resources.FindObjectsOfTypeAll<KrostGames.TTS.EditorTools.TTSEditorWindow>()) w.Close();
var insp = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
UnityEditor.EditorWindow.GetWindow<KrostGames.TTS.EditorTools.TTSEditorWindow>("TTS Generator", true, insp);
// Project window: show the package folder
var folder = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/KrostGames/LocalTTS");
UnityEditor.AssetDatabase.OpenAsset(folder);
// clear the console
var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
logEntries?.GetMethod("Clear")?.Invoke(null, null);
KrostGames.TTS.LocalTTS.Prewarm();
return "preroll ok scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
