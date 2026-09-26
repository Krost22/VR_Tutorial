// Trailer stage v2: unsaved 2D scene with an in-memory pixel-art adventure level, seen in a floating 2D Scene View.
string L = @"LEVELDIR";
UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
GameObject Place(string name, float x, float y, Vector2 pivot, int order, bool flip = false) {
    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
    tex.LoadImage(System.IO.File.ReadAllBytes(L + "/" + name + ".png"));
    tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
    var go = new GameObject(char.ToUpper(name[0]) + name.Substring(1));
    var sr = go.AddComponent<SpriteRenderer>();
    sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 16);
    sr.sortingOrder = order; sr.flipX = flip;
    go.transform.position = new Vector3(x, y, 0);
    return go;
}
var bottom = new Vector2(0.5f, 0); var top = new Vector2(0.5f, 1);
Place("background", 0, 0, new Vector2(0.5f, 0.5f), -100);
Place("ground", 0, -7f, top, 10);
Place("tree", -11f, -7.1f, bottom, 1);
Place("sign", -8.6f, -7.1f, bottom, 2);
Place("bush", -6.3f, -7.1f, bottom, 2);
Place("bush", 7.6f, -7.1f, bottom, 2, true);
Place("tree", 10.2f, -7.1f, bottom, 1, true);
Place("flag", 12.4f, -7.1f, bottom, 2);
Place("platform_a", -5f, -2.2f, top, 3);
var knight = Place("knight", -5.6f, -2.3f, bottom, 6);
Place("slime", -3.6f, -2.3f, bottom, 5);
Place("platform_b", 1.2f, 0.9f, top, 3);
Place("chest", 1.8f, 0.85f, bottom, 5);
Place("coin", -1.6f, -0.9f, new Vector2(0.5f, 0.5f), 4);
Place("coin", -0.8f, -0.3f, new Vector2(0.5f, 0.5f), 4);
Place("coin", 0f, 0.1f, new Vector2(0.5f, 0.5f), 4);
knight.name = "Hero";

var sv = ScriptableObject.CreateInstance<UnityEditor.SceneView>();
sv.titleContent = new GUIContent("Trailer View");
sv.Show();
sv.position = new Rect(60, 60, 1280, 720);
sv.in2DMode = true;
sv.showGrid = false;
sv.drawGizmos = true;
try { sv.overlayCanvas.overlaysEnabled = false; } catch { }
sv.LookAt(new Vector3(0, 0, 0), Quaternion.identity, 7.5f, true, true);
var list = UnityEditor.SceneView.sceneViews; list.Remove(sv); list.Insert(0, sv);
UnityEditor.Tools.current = UnityEditor.Tool.Move;
UnityEditor.Selection.activeGameObject = knight;
EditorPets.ScenePetOverlay.showNames = false;
return "ok ortho=" + sv.camera.orthographicSize;
