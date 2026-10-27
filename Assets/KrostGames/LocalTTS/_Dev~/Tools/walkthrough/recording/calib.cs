var gen = UnityEngine.Resources.FindObjectsOfTypeAll<KrostGames.TTS.EditorTools.TTSEditorWindow>()[0];
var root = gen.rootVisualElement;
string R(UnityEngine.Rect r) => $"({r.x:F0},{r.y:F0},{r.width:F0},{r.height:F0})";
var sb = new System.Text.StringBuilder();
sb.Append("pos=" + R(gen.position) + " rootWB=" + R(root.worldBound));
foreach (var n in new[] { "btn-preview", "btn-generate", "text", "voice", "pitch" })
{
    var e = UnityEngine.UIElements.UQueryExtensions.Q(root, n);
    sb.Append("\n" + n + " wb=" + R(e.worldBound));
}
// the text field empty, skybox off in the scene view, project shows the package folder
var tf = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.TextField>(root, "text");
tf.value = "";
foreach (UnityEditor.SceneView sv in UnityEditor.SceneView.sceneViews) { var st = sv.sceneViewState; st.showSkybox = false; sv.sceneViewState = st; sv.Repaint(); }
UnityEditor.AssetDatabase.OpenAsset(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/KrostGames/LocalTTS/Demo"));
UnityEditor.Selection.activeObject = null;
return sb.ToString();
