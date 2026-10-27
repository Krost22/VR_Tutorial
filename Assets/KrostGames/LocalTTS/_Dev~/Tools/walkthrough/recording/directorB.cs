// Director B (Play Mode): the demo scene. Types in the input field, presses Speak (real time), then two baked speakers.
// uGUI buttons get real pointer events (enter / down / up / click), audio starts are read from the AudioSources.
var BF = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
string OUT = "C:/Users/eduar/AppData/Local/Temp/claude/C--Users-eduar-OneDrive-Documentos-Github-VR-Tutorial/9a572df9-bdb3-4a3b-84fc-8a5bada7436d/scratchpad/rec/";
System.IO.Directory.CreateDirectory(OUT + "audio");
var inv = System.Globalization.CultureInfo.InvariantCulture;
var events = new System.Collections.Generic.List<string>();
double Epoch() => (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
string F(double v) => v.ToString("F3", inv);
void Log(string type, double x, double y, string extra, double at = 0) => events.Add("{\"type\":\"" + type + "\",\"t\":" + F(at > 0 ? at : Epoch()) + ",\"x\":" + F(x) + ",\"y\":" + F(y) + extra + "}");

// Game view pixel -> screen point
var gvType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
UnityEditor.EditorWindow gv = null;
foreach (UnityEditor.EditorWindow w in UnityEngine.Resources.FindObjectsOfTypeAll(gvType)) { gv = w; break; }
UnityEngine.Vector2 ToScreen(UnityEngine.Vector2 px)
{
    var view = (UnityEngine.Rect)gvType.GetProperty("viewInWindow", BF).GetValue(gv);
    var target = (UnityEngine.Rect)gvType.GetProperty("targetInView", BF).GetValue(gv);
    float sx = target.width / UnityEngine.Screen.width, sy = target.height / UnityEngine.Screen.height;
    return new UnityEngine.Vector2(gv.position.x + view.x + target.x + px.x * sx, gv.position.y + 24 + view.y + target.y + (UnityEngine.Screen.height - px.y) * sy);
}
UnityEngine.Vector2 Center(UnityEngine.Component c)
{
    var rt = (UnityEngine.RectTransform)c.transform;
    return ToScreen(UnityEngine.RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center)));
}
UnityEngine.UI.Button Btn(string label)
{
    foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsSortMode.None))
        if (b.GetComponentInChildren<UnityEngine.UI.Text>()?.text == label) return b;
    return null;
}
var input = UnityEngine.Object.FindAnyObjectByType<UnityEngine.UI.InputField>();
UnityEngine.EventSystems.PointerEventData Ped(UnityEngine.Vector2 screen) => new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };

// audio: watch every AudioSource
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
    string file = OUT + "audio/B" + (audioN++) + ".wav";
    System.IO.File.WriteAllBytes(file, KrostGames.TTS.WavUtility.Encode(s, clip.channels, clip.frequency));
    return file;
}
var seen = new System.Collections.Generic.Dictionary<int, (UnityEngine.AudioClip clip, bool playing, float time)>();

// timeline
var steps = new System.Collections.Generic.List<(double t, System.Action a)>();
var tasks = new System.Collections.Generic.List<(double s, double e, System.Action<double> f)>();
void Press(double at, string label, string what)
{
    steps.Add((at - 0.25, () => { var b = Btn(label); UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject, Ped(Center(b)), UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler); }));
    steps.Add((at, () => { var b = Btn(label); var p = Center(b); Log("click", p.x, p.y, ",\"what\":\"" + what + "\""); UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject, Ped(p), UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler); }));
    steps.Add((at + 0.13, () => { var b = Btn(label); var ped = Ped(Center(b)); UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject, ped, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler); UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject, ped, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler); }));
    steps.Add((at + 1.2, () => { var b = Btn(label); UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject, Ped(Center(b)), UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler); }));
}
void Type(double at, string text, double cps)
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
        input.text = text.Substring(0, n);
        input.caretPosition = n;
    }));
}

steps.Add((0.0, () => Log("start", -1, -1, "")));
steps.Add((1.0, () => { var p = Center(input); Log("click", p.x, p.y, ",\"what\":\"input\""); input.ActivateInputField(); }));
steps.Add((1.4, () => { input.text = ""; input.caretPosition = 0; }));
Type(1.6, "Every word you type here is spoken right now.", 18);
Press(5.2, "▶  Speak", "speak");
Press(9.6, "Guía en español", "es");
Press(15.6, "Guia em português", "pt");
steps.Add((23.2, () => Log("end", -1, -1, "")));

double start = UnityEditor.EditorApplication.timeSinceStartup + 1.0;
int stepIndex = 0;
var taskDone = new bool[tasks.Count];
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    try
    {
        if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("left Play Mode");
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
        foreach (var src in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None))
        {
            int id = src.GetInstanceID();
            seen.TryGetValue(id, out var prev);
            bool playing = src.isPlaying && src.clip != null;
            if (playing && (!prev.playing || prev.clip != src.clip || src.time + 0.05f < prev.time))
                Log("audio", -1, -1, ",\"file\":\"" + SaveClip(src.clip) + "\",\"source\":\"" + src.name + "\"", Epoch() - src.time);
            seen[id] = (src.clip, playing, src.time);
        }
        if (stepIndex >= steps.Count)
        {
            UnityEditor.EditorApplication.update -= tick;
            System.IO.File.WriteAllText(OUT + "eventsB.json", "[\n" + string.Join(",\n", events) + "\n]");
        }
    }
    catch (System.Exception ex)
    {
        UnityEditor.EditorApplication.update -= tick;
        System.IO.File.WriteAllText(OUT + "eventsB.error.txt", ex.ToString() + "\n\n" + string.Join("\n", events));
    }
};
UnityEditor.EditorApplication.update += tick;
return "director B armed, starts in 1 s";
