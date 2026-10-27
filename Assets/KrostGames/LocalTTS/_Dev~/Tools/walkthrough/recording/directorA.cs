// Director A (edit mode): Generator window, then a TTS Speaker in the Inspector. Runs on EditorApplication.update,
// performs every action through the real UI, and logs clicks / drags / audio starts with wall-clock times.
var BF = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
string OUT = "C:/Users/eduar/AppData/Local/Temp/claude/C--Users-eduar-OneDrive-Documentos-Github-VR-Tutorial/9a572df9-bdb3-4a3b-84fc-8a5bada7436d/scratchpad/rec/";
System.IO.Directory.CreateDirectory(OUT + "audio");
var inv = System.Globalization.CultureInfo.InvariantCulture;
var events = new System.Collections.Generic.List<string>();
double Epoch() => (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
string F(double v) => v.ToString("F3", inv);
void Log(string type, double x, double y, string extra) => events.Add("{\"type\":\"" + type + "\",\"t\":" + F(Epoch()) + ",\"x\":" + F(x) + ",\"y\":" + F(y) + extra + "}");

var gen = UnityEngine.Resources.FindObjectsOfTypeAll<KrostGames.TTS.EditorTools.TTSEditorWindow>()[0];
UnityEngine.UIElements.VisualElement G(string n) => UnityEngine.UIElements.UQueryExtensions.Q(gen.rootVisualElement, n);
var inspType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
UnityEditor.EditorWindow Insp() { foreach (UnityEditor.EditorWindow w in UnityEngine.Resources.FindObjectsOfTypeAll(inspType)) if (w.docked) return w; return null; }
UnityEngine.Vector2 Ctr(UnityEditor.EditorWindow w, UnityEngine.UIElements.VisualElement e) { var r = e.worldBound; return new UnityEngine.Vector2(w.position.x + r.center.x, w.position.y + r.center.y); }
var simulate = typeof(UnityEngine.UIElements.Clickable).GetMethod("SimulateSingleClick", BF);
void Press(UnityEngine.UIElements.Button b) { using (var ev = UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()) simulate.Invoke(b.clickable, new object[] { ev, 160 }); }
void Click(UnityEditor.EditorWindow w, UnityEngine.UIElements.VisualElement e, string what)
{
    var p = Ctr(w, e);
    Log("click", p.x, p.y, ",\"what\":\"" + what + "\"");
    if (e is UnityEngine.UIElements.Button b) Press(b);
}
void ClickXY(double x, double y, string what) => Log("click", x, y, ",\"what\":\"" + what + "\"");
System.Collections.Generic.List<UnityEngine.UIElements.Button> Plays() =>
    UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Button>(UnityEngine.UIElements.UQueryExtensions.Q(Insp().rootVisualElement, null, "tts-lines"), null, "tts-icon-btn").Where(b => b.text == "▶").ToList();
UnityEngine.UIElements.TextField LastLineText() =>
    UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.TextField>(Insp().rootVisualElement, null, "tts-line-text").Last();
UnityEngine.UIElements.Button ButtonByText(UnityEditor.EditorWindow w, string text) =>
    UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Button>(w.rootVisualElement).Where(b => b.text == text).First();

// audio capture: the clip the preview is playing, or the asset clip we expect
var ownedField = typeof(KrostGames.TTS.EditorTools.TTSEditorAudio).GetField("_owned", BF);
string projectRoot = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
int audioN = 0;
string SaveClip(UnityEngine.AudioClip clip)
{
    string path = UnityEditor.AssetDatabase.GetAssetPath(clip);
    if (!string.IsNullOrEmpty(path)) return System.IO.Path.GetFullPath(System.IO.Path.Combine(projectRoot, path)).Replace('\\', '/');
    var data = new float[clip.samples * clip.channels];
    clip.GetData(data, 0);
    var s = new short[data.Length];
    for (int i = 0; i < data.Length; i++) s[i] = (short)UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(data[i] * 32767f), -32768, 32767);
    string file = OUT + "audio/A" + (audioN++) + ".wav";
    System.IO.File.WriteAllBytes(file, KrostGames.TTS.WavUtility.Encode(s, clip.channels, clip.frequency));
    return file;
}

// timeline
var steps = new System.Collections.Generic.List<(double t, System.Action a)>();
var tasks = new System.Collections.Generic.List<(double s, double e, System.Action<double> f)>();
UnityEngine.AudioClip expected = null;
void Type(double at, System.Func<UnityEngine.UIElements.TextField> field, string text, double cps)
{
    var times = new double[text.Length];
    var rnd = new System.Random(text.Length);
    double tt = at;
    for (int i = 0; i < text.Length; i++) { times[i] = tt; char c = text[i]; tt += (1.0 / cps) * (0.65 + 0.7 * rnd.NextDouble()) * (c == ' ' ? 1.3 : c == '.' || c == ',' || c == '!' ? 2.2 : 1.0); }
    int shown = -1;
    tasks.Add((at, tt, p =>
    {
        double now = at + p * (tt - at);
        int n = 0; while (n < text.Length && times[n] <= now) n++;
        if (p >= 1) n = text.Length;
        if (n == shown) return;
        shown = n;
        var tf = field();
        tf.value = text.Substring(0, n);
        tf.SelectRange(n, n);
    }));
}
UnityEngine.UIElements.TextField genText() => (UnityEngine.UIElements.TextField)G("text");

// 1. English line: type, preview, save, show in Project
steps.Add((0.0, () => { gen.Focus(); Log("start", -1, -1, ""); }));
steps.Add((1.3, () => { var tf = genText(); Click(gen, tf, "text"); tf.Focus(); }));
Type(1.6, genText, "Welcome to the training room. Grab the red cube to begin.", 17);
steps.Add((6.0, () => Click(gen, G("btn-preview"), "preview")));
steps.Add((10.6, () => Click(gen, G("btn-generate"), "generate")));
steps.Add((12.6, () => Click(gen, G("btn-result-ping"), "ping")));
// 2. Spanish voice, pitch, preview, save
steps.Add((15.0, () => { var dd = (UnityEngine.UIElements.DropdownField)G("voice"); Click(gen, dd, "voice"); dd.value = dd.choices.Find(c => c.Contains("Raul")); }));
steps.Add((16.6, () => { var tf = genText(); Click(gen, tf, "text"); tf.Focus(); tf.value = ""; }));
Type(16.9, genText, "¡Bien hecho! Ahora presiona el botón verde.", 17);
UnityEngine.UIElements.SliderInt pitch() => (UnityEngine.UIElements.SliderInt)G("pitch");
UnityEngine.Vector2 Handle() => Ctr(gen, UnityEngine.UIElements.UQueryExtensions.Q(pitch(), "unity-dragger"));
steps.Add((20.4, () => { var p = Handle(); Log("dragstart", p.x, p.y, ""); }));
tasks.Add((20.6, 21.5, p => { pitch().value = UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.SmoothStep(0, 2, (float)p)); if (p >= 1) { var h = Handle(); Log("dragend", h.x, h.y, ""); } }));
steps.Add((22.3, () => Click(gen, G("btn-preview"), "preview")));
steps.Add((26.2, () => Click(gen, G("btn-generate"), "generate")));
// 3. TTS Speaker in the Inspector
steps.Add((28.4, () => { ClickXY(112, 163, "hierarchy"); UnityEditor.Selection.activeGameObject = UnityEngine.GameObject.Find("English guide"); }));
steps.Add((29.5, () => { ClickXY(1512, 91, "inspector-tab"); Insp().Focus(); }));
steps.Add((31.3, () => { var sp = UnityEngine.GameObject.Find("English guide").GetComponent<KrostGames.TTS.TTSSpeaker>(); expected = sp.Lines[1].clip; Click(Insp(), Plays()[1], "play-line-2"); }));
steps.Add((36.2, () => Click(Insp(), UnityEngine.UIElements.UQueryExtensions.Q(Insp().rootVisualElement, "unity-list-view__add-button"), "add-line")));
steps.Add((37.3, () => { var tf = LastLineText(); Click(Insp(), tf, "line-text"); tf.Focus(); tf.value = ""; }));
Type(37.6, LastLineText, "Checkpoint reached. Well done!", 17);
steps.Add((40.4, () => Click(Insp(), Plays()[3], "play-line-4")));
steps.Add((43.2, () => Click(Insp(), ButtonByText(Insp(), "Bake missing"), "bake")));
steps.Add((47.0, () => Log("end", -1, -1, "")));

double start = UnityEditor.EditorApplication.timeSinceStartup + 1.0;
int stepIndex = 0;
var taskDone = new bool[tasks.Count];
bool wasPlaying = false;
object lastOwned = ownedField.GetValue(null);
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    try
    {
        double t = UnityEditor.EditorApplication.timeSinceStartup - start;
        if (t < 0) return;
        while (stepIndex < steps.Count && t >= steps[stepIndex].t) { steps[stepIndex].a(); stepIndex++; }
        for (int i = 0; i < tasks.Count; i++)
        {
            if (taskDone[i] || t < tasks[i].s) continue;
            double p = System.Math.Min(1, (t - tasks[i].s) / System.Math.Max(0.001, tasks[i].e - tasks[i].s));
            tasks[i].f(p);
            if (p >= 1) taskDone[i] = true;
        }
        bool playing = KrostGames.TTS.EditorTools.TTSEditorAudio.IsPlaying;
        if (playing && !wasPlaying)
        {
            object owned = ownedField.GetValue(null);
            var clip = owned != null && !ReferenceEquals(owned, lastOwned) ? (UnityEngine.AudioClip)owned : expected;
            lastOwned = owned; expected = null;
            Log("audio", -1, -1, ",\"file\":\"" + (clip != null ? SaveClip(clip) : "") + "\"");
        }
        wasPlaying = playing;
        gen.Repaint(); Insp()?.Repaint();
        if (stepIndex >= steps.Count)
        {
            UnityEditor.EditorApplication.update -= tick;
            System.IO.File.WriteAllText(OUT + "eventsA.json", "[\n" + string.Join(",\n", events) + "\n]");
        }
    }
    catch (System.Exception ex)
    {
        UnityEditor.EditorApplication.update -= tick;
        System.IO.File.WriteAllText(OUT + "eventsA.error.txt", ex.ToString() + "\n\n" + string.Join("\n", events));
    }
};
UnityEditor.EditorApplication.update += tick;
return "director A armed, starts in 1 s";
