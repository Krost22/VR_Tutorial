// Trailer frame recorder (placeholders are filled by rec.sh).
string dir = @"OUTDIR"; System.IO.Directory.CreateDirectory(dir);
UnityEditor.EditorWindow win = WINDOW_EXPR;
int total = TOTAL; double interval = 1.0 / 20; double next = UnityEditor.EditorApplication.timeSinceStartup; int n = 0;
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var sf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
var parent = typeof(UnityEditor.EditorWindow).GetField("m_Parent", bf).GetValue(win);
var grab = parent.GetType().GetMethod("GrabPixels", bf);
int pw = (int)win.position.width, ph = (int)win.position.height;
var rt = new RenderTexture(pw, ph, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
var tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
var flip = new Color32[pw * ph];
var overlay = typeof(EditorPets.ScenePetOverlay);
System.Collections.IDictionary Pets() => (System.Collections.IDictionary)overlay.GetField("activePets", sf).GetValue(null);
object Ball() => overlay.GetField("currentBall", sf).GetValue(null);
void Throw(float x, float y, float vx, float vy) { var b = Ball(); var t = b.GetType(); t.GetField("position").SetValue(b, new Vector2(x, y)); t.GetField("velocity").SetValue(b, new Vector2(vx, vy)); }
void PetNth(int i) {
    var ctrls = new System.Collections.Generic.List<EditorPets.PetController>();
    foreach (System.Collections.DictionaryEntry e in Pets()) ctrls.Add((EditorPets.PetController)e.Value);
    ctrls.Sort((a, b) => a.position.x.CompareTo(b.position.x));
    if (ctrls.Count == 0) return;
    var c = ctrls[i % ctrls.Count]; c.ChangeState(EditorPets.PetState.Interact); c.SpawnHeart();
}
void Spread() { var ctrls = new System.Collections.Generic.List<EditorPets.PetController>(); foreach (System.Collections.DictionaryEntry e in Pets()) ctrls.Add((EditorPets.PetController)e.Value); float step = (pw - 320f) / System.Math.Max(1, ctrls.Count); for (int i = 0; i < ctrls.Count; i++) { ctrls[i].position.x = 160 + (i + 0.5f) * step - ctrls[i].data.size.x / 2; ctrls[i].ChangeState(EditorPets.PetState.Walk); } }
void Cast(params string[] names) { var set = new System.Collections.Generic.HashSet<string>(names); foreach (var p in EditorPets.ScenePetOverlay.FindAll<EditorPets.PetData>()) if (p.isActive != set.Contains(p.petName)) EditorPets.ScenePetOverlay.SetVisible(p, set.Contains(p.petName)); }
EditorPets.PetController Ctrl(string name) { foreach (System.Collections.DictionaryEntry e in Pets()) if (((EditorPets.PetData)e.Key).petName == name) return (EditorPets.PetController)e.Value; return null; }
void NoBall() { var b = Ball(); b.GetType().GetField("active").SetValue(b, false); }
System.Action<int> act = f => { ACTIONS };
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
    double now = UnityEditor.EditorApplication.timeSinceStartup;
    if (now < next) return;
    next += interval; if (next < now) next = now + interval;
    try { act(n); } catch (System.Exception ex) { Debug.LogWarning("trailer action: " + ex.Message); }
    win.Repaint();
    grab.Invoke(parent, new object[] { rt, new Rect(0, 0, pw, ph) });
    var prev = RenderTexture.active; RenderTexture.active = rt;
    tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0); RenderTexture.active = prev;
    var px = tex.GetPixels32();
    for (int y = 0; y < ph; y++) System.Array.Copy(px, y * pw, flip, (ph - 1 - y) * pw, pw);
    tex.SetPixels32(flip);
    System.IO.File.WriteAllBytes(dir + "/f_" + n.ToString("0000") + ".jpg", tex.EncodeToJPG(92));
    if (++n >= total) { UnityEditor.EditorApplication.update -= cb; rt.Release(); }
};
UnityEditor.EditorApplication.update += cb;
return "recording " + total + " frames of " + pw + "x" + ph;
