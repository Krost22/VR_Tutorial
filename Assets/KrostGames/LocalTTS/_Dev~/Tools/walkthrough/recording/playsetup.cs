var gvType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
var gv = UnityEditor.EditorWindow.GetWindow(gvType, false, null, false);
gvType.GetProperty("maximizeOnPlay", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(gv, true);
return "maximizeOnPlay set";
