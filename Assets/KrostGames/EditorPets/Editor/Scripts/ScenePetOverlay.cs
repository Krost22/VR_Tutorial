#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EditorPets
{
    // Runs and draws every visible PetData in all Scene Views. Starts by itself on editor load.
    [InitializeOnLoad]
    public static class ScenePetOverlay
    {
        // Package folder wherever it lives (Assets/... or Packages/...), located via EditorPets.Editor.asmdef's GUID.
        public static string Root => Dir(Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath("87942499440475e4c81881c5667d29b8")));
        public static string UIFolder => Root + "/Editor/UI";      // UXML, USS, icon
        public static string ItemsFolder => Root + "/Items";       // ball, heart, GlobalPetSettings
        public static string FoodFolder => ItemsFolder + "/Food";  // food library shown in the pet inspector

        // null = reload on next use. Right after a domain reload FindAssets can come back empty while the
        // AssetDatabase is still busy, so the list is loaded lazily once it's idle (and again on project changes).
        private static List<PetData> allPets;
        private static List<PetData> Pets => allPets ??= FindAll<PetData>();
        private static Dictionary<PetData, PetController> activePets = new Dictionary<PetData, PetController>();
        private static double lastUpdateTime;
        private const double MinRepaintInterval = 1.0 / 30.0;
        private static double lastRepaintTime;

        // Global UX Settings
        public static bool interactable = true;
        public static bool showNames = true;
        public static float globalOpacity = 1f;

        public static GlobalPetSettings settings;

        private class Ball
        {
            public Vector2 position;
            public Vector2 velocity;
            public bool isDragging;
            public bool active;
            public Vector2 dragOffset;
            public Vector2 lastMousePos;
        }
        private static Ball currentBall = new Ball();

        static ScenePetOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += EditorUpdate;
            EditorApplication.projectChanged += RefreshPetList;
            EditorApplication.delayCall += Initialize;
            lastUpdateTime = EditorApplication.timeSinceStartup;
        }

        private static void Initialize()
        {
            LoadSettings();
            LoadGlobalSettingsAsset();
            lastUpdateTime = EditorApplication.timeSinceStartup;
        }

        public static List<T> FindAll<T>() where T : Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .ToList();

        private static void RefreshPetList() => allPets = null;

        // Keeps the Scene View in sync with each PetData's isActive, however it changed (window, inspector, Undo, new/deleted asset).
        private static void SyncPets()
        {
            if (allPets == null && (EditorApplication.isUpdating || EditorApplication.isCompiling)) return;
            foreach (var data in activePets.Keys.Where(d => d == null || !d.isActive).ToList())
                activePets.Remove(data);
            foreach (var data in Pets)
            {
                if (data == null || !data.isActive || activePets.ContainsKey(data)) continue;
                // Spread across the whole view so a dozen pets don't pile up in one corner.
                float width = SceneView.sceneViews.Count > 0 ? ((SceneView)SceneView.sceneViews[0]).position.width : 450f;
                float x = Random.Range(20f, Mathf.Max(21f, width - data.size.x - 20f));
                activePets.Add(data, new PetController(data, new Vector2(x, 100f)));
            }
        }

        public static void LoadGlobalSettingsAsset()
        {
            if (settings != null) return;
            string root = Root;
            string path = ItemsFolder + "/GlobalPetSettings.asset";
            // The package's own asset wins over stray copies elsewhere in the project.
            settings = AssetDatabase.LoadAssetAtPath<GlobalPetSettings>(path) ?? FindAll<GlobalPetSettings>().FirstOrDefault();
            if (settings != null) return;

            if (!AssetDatabase.IsValidFolder(ItemsFolder)) AssetDatabase.CreateFolder(root, "Items");
            settings = ScriptableObject.CreateInstance<GlobalPetSettings>();
            settings.heartTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ItemsFolder + "/Heart.png");
            settings.foodTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FoodFolder + "/Bowl.png");
            settings.ballTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ItemsFolder + "/Ball.png");
            AssetDatabase.CreateAsset(settings, path);
        }

        public static void SetVisible(PetData data, bool visible)
        {
            Undo.RecordObject(data, visible ? "Show Pet" : "Hide Pet");
            data.isActive = visible;
            EditorUtility.SetDirty(data);
        }

        public static void SetAllVisible(bool visible) => ShowWhere(_ => visible);

        // Hides every other pet: handy once there are many.
        public static void ShowOnly(PetData only) => ShowWhere(data => data == only);

        private static void ShowWhere(System.Func<PetData, bool> visible)
        {
            int group = Undo.GetCurrentGroup();
            foreach (var data in Pets)
                if (data != null && data.isActive != visible(data)) SetVisible(data, visible(data));
            Undo.CollapseUndoOperations(group);
        }

        public static PetData Duplicate(PetData source)
        {
            string path = AssetDatabase.GetAssetPath(source);
            string copyPath = AssetDatabase.GenerateUniqueAssetPath(path);
            if (!AssetDatabase.CopyAsset(path, copyPath)) return null;
            var copy = AssetDatabase.LoadAssetAtPath<PetData>(copyPath);
            copy.petName = source.petName + " Copy";
            EditorUtility.SetDirty(copy);
            RefreshPetList();
            return copy;
        }

        private static void EditorUpdate()
        {
            SyncPets();

            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - lastUpdateTime);
            lastUpdateTime = currentTime;

            if (deltaTime > 0.05f) deltaTime = 0.05f; // Cap delta time
            if (deltaTime <= 0f) return;

            SceneView view = null;
            if (SceneView.sceneViews.Count > 0) view = SceneView.sceneViews[0] as SceneView;
            if (view == null) return;
            
            Rect bounds = view.position;
            Rect localBounds = new Rect(0, 0, bounds.width, bounds.height);

            // Update Ball
            if (currentBall.active)
            {
                float radius = settings != null ? settings.ballRadius : 16f;
                float gravity = settings != null ? settings.gravity : 1200f;

                if (!currentBall.isDragging)
                {
                    currentBall.velocity.y += gravity * deltaTime;
                    currentBall.position += currentBall.velocity * deltaTime;

                    float floorY = localBounds.height - 25f - radius * 2;
                    if (currentBall.position.y >= floorY)
                    {
                        currentBall.position.y = floorY;
                        currentBall.velocity.y *= -0.6f;
                        currentBall.velocity.x *= 0.95f;
                        if (Mathf.Abs(currentBall.velocity.y) < 20f && Mathf.Abs(currentBall.velocity.x) < 10f) currentBall.velocity = Vector2.zero;
                    }

                    if (currentBall.position.x <= 0)
                    {
                        currentBall.position.x = 0;
                        currentBall.velocity.x *= -0.7f;
                    }
                    else if (currentBall.position.x >= localBounds.width - radius * 2)
                    {
                        currentBall.position.x = localBounds.width - radius * 2;
                        currentBall.velocity.x *= -0.7f;
                    }

                    // Smoother Pet collisions (Pushing)
                    foreach (var pet in activePets.Values)
                    {
                        Rect petRect = new Rect(pet.position.x, pet.position.y, pet.data.size.x, pet.data.size.y);
                        Vector2 ballCenter = currentBall.position + new Vector2(radius, radius);
                        
                        if (ballCenter.y > petRect.y && ballCenter.y < petRect.yMax)
                        {
                            if (ballCenter.x > petRect.x - radius && ballCenter.x < petRect.xMax + radius)
                            {
                                float pushDir = (ballCenter.x < petRect.center.x) ? -1 : 1;
                                // Smooth velocity transfer instead of hard snap
                                currentBall.velocity.x = Mathf.Lerp(currentBall.velocity.x, pushDir * 200f, 15f * deltaTime);
                                currentBall.position.x += pushDir * 50f * deltaTime; // Nudge it away
                            }
                        }
                    }
                }
                
                foreach (var pet in activePets.Values)
                {
                    if (pet.currentState == PetState.Play)
                    {
                        pet.targetPosition = currentBall.position + new Vector2(radius, radius);
                    }
                }
            }

            // Update Pets
            foreach (var pet in activePets.Values)
            {
                pet.Update(deltaTime, localBounds);
            }

            // Repaint all scene views (throttled)
            if (activePets.Count > 0 || currentBall.active)
            {
                RequestRepaint();
            }
        }

        private static void RequestRepaint(bool force = false)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (!force && currentTime - lastRepaintTime < MinRepaintInterval) return;
            lastRepaintTime = currentTime;
            foreach (SceneView sv in SceneView.sceneViews)
            {
                sv.Repaint();
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (settings == null) LoadGlobalSettingsAsset();

            Rect bounds = sceneView.position;
            Rect localBounds = new Rect(0, 0, bounds.width, bounds.height);

            Event e = Event.current;

            if (interactable && (e.type == EventType.DragUpdated || e.type == EventType.DragPerform))
            {
                if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is PetData draggedPet)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        SetVisible(draggedPet, true);
                        e.Use();
                    }
                }
            }

            if (interactable) HandleBallInput(e, localBounds);

            if (activePets.Count == 0 && !currentBall.active) return;

            bool isInteracting = currentBall.isDragging;
            foreach (var pet in activePets.Values)
            {
                if (pet.currentState == PetState.Drag || pet.currentState == PetState.Interact)
                {
                    isInteracting = true;
                    break;
                }
            }
            if (isInteracting) RequestRepaint(force: true);

            Handles.BeginGUI();

            if (currentBall.active && settings != null && settings.ballTexture != null)
            {
                float radius = settings.ballRadius;
                float ballFloorY = localBounds.height - 25f - radius * 2;
                if (!currentBall.isDragging && currentBall.position.y > ballFloorY)
                {
                    currentBall.position.y = ballFloorY;
                }
                GUI.DrawTexture(new Rect(currentBall.position.x, currentBall.position.y, radius * 2, radius * 2), settings.ballTexture);
            }

            foreach (var pet in activePets.Values.ToList())
            {
                float petFloorY = localBounds.height - pet.data.size.y - 25f;
                pet.SnapToFloor(petFloorY);

                pet.Draw(globalOpacity, showNames);
                if (interactable) HandleInput(e, pet, localBounds);
            }
            Handles.EndGUI();
        }

        private static void HandleInput(Event e, PetController pet, Rect bounds)
        {
            if (e.isMouse && e.button == 0) 
            {
                Rect petRect = new Rect(pet.position.x, pet.position.y, pet.data.size.x, pet.data.size.y);
                if (e.type == EventType.MouseDown && petRect.Contains(e.mousePosition))
                {
                    pet.ChangeState(PetState.Interact);
                    pet.SpawnHeart();
                    pet.dragOffset = pet.position - e.mousePosition;
                    e.Use(); 
                }
                else if (e.type == EventType.MouseDrag && (pet.currentState == PetState.Interact || pet.currentState == PetState.Drag))
                {
                    pet.ChangeState(PetState.Drag);
                    pet.position = e.mousePosition + pet.dragOffset;
                    if (e.mousePosition.x < 10 || e.mousePosition.x > bounds.width - 10 || e.mousePosition.y < 10 || e.mousePosition.y > bounds.height - 10)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] { pet.data };
                        DragAndDrop.StartDrag(pet.data.petName);
                        pet.ChangeState(PetState.Idle);
                    }
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && (pet.currentState == PetState.Drag || pet.currentState == PetState.Interact))
                {
                    pet.ChangeState(PetState.Idle);
                    float petFloorY = bounds.height - pet.data.size.y - 25f;
                    pet.SnapToFloor(petFloorY);
                    e.Use();
                }
            }
        }

        private static void HandleBallInput(Event e, Rect bounds)
        {
            if (!currentBall.active || !e.isMouse || e.button != 0) return;
            float radius = settings != null ? settings.ballRadius : 16f;

            Rect ballRect = new Rect(currentBall.position.x, currentBall.position.y, radius * 2, radius * 2);

            if (e.type == EventType.MouseDown && ballRect.Contains(e.mousePosition))
            {
                currentBall.isDragging = true;
                currentBall.dragOffset = currentBall.position - e.mousePosition;
                currentBall.velocity = Vector2.zero;
                currentBall.lastMousePos = e.mousePosition;
                foreach (var pet in activePets.Values) if (pet.currentState != PetState.Play) pet.ChangeState(PetState.Play);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && currentBall.isDragging)
            {
                currentBall.position = e.mousePosition + currentBall.dragOffset;
                Vector2 delta = e.mousePosition - currentBall.lastMousePos;
                currentBall.velocity = delta / 0.02f;
                currentBall.lastMousePos = e.mousePosition;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && currentBall.isDragging)
            {
                currentBall.isDragging = false;
                e.Use();
            }
        }

        public static void LoadSettings()
        {
            interactable = EditorPrefs.GetBool("EditorPets_Interactable", true);
            showNames = EditorPrefs.GetBool("EditorPets_ShowNames", true);
            globalOpacity = EditorPrefs.GetFloat("EditorPets_Opacity", 1f);
        }

        public static void SaveSettings()
        {
            EditorPrefs.SetBool("EditorPets_Interactable", interactable);
            EditorPrefs.SetBool("EditorPets_ShowNames", showNames);
            EditorPrefs.SetFloat("EditorPets_Opacity", globalOpacity);
        }

        // Every visible pet eats its own food (PetData.food), or the default bowl.
        public static void FeedAll()
        {
            if (settings == null) LoadGlobalSettingsAsset();
            foreach (var pet in activePets.Values) pet.Feed(pet.data.food != null ? pet.data.food : settings.foodTexture);
        }

        public static void RandomizePetPosition(PetData data)
        {
            if (data == null) return;
            if (!activePets.TryGetValue(data, out PetController pet)) return;

            SceneView view = null;
            if (SceneView.sceneViews.Count > 0) view = SceneView.sceneViews[0] as SceneView;
            float maxX = view != null ? view.position.width - pet.data.size.x - 50 : 500f;
            float minX = 50f;
            float x = Random.Range(minX, Mathf.Max(minX + 1, maxX));
            pet.position = new Vector2(x, 100f);
        }

        public static void SpawnBall()
        {
            if (settings == null) LoadGlobalSettingsAsset();
            currentBall.active = true;
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null && SceneView.sceneViews.Count > 0) view = SceneView.sceneViews[0] as SceneView;
            if (view != null)
                currentBall.position = new Vector2(view.position.width / 2, 50);
            else
                currentBall.position = new Vector2(100, 50);
            currentBall.velocity = Vector2.zero;
            foreach (var pet in activePets.Values)
            {
                pet.ChangeState(PetState.Play);
                pet.targetPosition = currentBall.position;
            }
        }

        // Assets > Create > EditorPets > New Pet (also the window's "+ New Pet" button).
        // Uses the selected sprite sheets, assigned to states by file name: idle / walk|run / sleep / eat / happy|petted.
        [MenuItem("Assets/Create/EditorPets/New Pet", false, 0)]
        public static void CreatePetFromSelection()
        {
            var sheets = Selection.GetFiltered<Texture2D>(SelectionMode.Assets).Select(PrepareSpriteSheet).OrderBy(t => t.name).ToArray();
            var pet = ScriptableObject.CreateInstance<PetData>();
            foreach (var tex in sheets)
            {
                string n = tex.name.ToLowerInvariant();
                if (n.Contains("walk") || n.Contains("run")) pet.walkTexture = tex;
                else if (n.Contains("sleep")) pet.sleepTexture = tex;
                else if (n.Contains("eat")) pet.eatTexture = tex;
                else if (n.Contains("happy") || n.Contains("petted")) pet.pettedTexture = tex;
                else if (pet.idleTexture == null || n.Contains("idle")) pet.idleTexture = tex;
            }
            if (pet.idleTexture == null && sheets.Length > 0) pet.idleTexture = sheets[0];

            string folder;
            if (sheets.Length > 0)
            {
                folder = Dir(AssetDatabase.GetAssetPath(sheets[0]));
                pet.petName = Regex.Split(sheets[0].name, @"[\s_\-]")[0];
            }
            else
            {
                string selected = AssetDatabase.GetAssetPath(Selection.activeObject);
                folder = AssetDatabase.IsValidFolder(selected) ? selected : string.IsNullOrEmpty(selected) ? "Assets" : Dir(selected);
            }

            AssetDatabase.CreateAsset(pet, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{pet.petName}.asset"));
            RefreshPetList();
            Selection.activeObject = pet;
            EditorGUIUtility.PingObject(pet);
        }

        // Freshly imported PNGs get scaled to power-of-two and blurred, which breaks frame slicing.
        // Import them like the bundled sheets; textures the user already configured are left alone.
        private static Texture2D PrepareSpriteSheet(Texture2D tex)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer) || importer.textureType != TextureImporterType.Default) return tex;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static string Dir(string assetPath) => Path.GetDirectoryName(assetPath).Replace('\\', '/');
    }
}
#endif
