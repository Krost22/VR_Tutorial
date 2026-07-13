#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace EditorPets
{
    [InitializeOnLoad]
    public static class ScenePetOverlay
    {
        private static Dictionary<PetData, PetController> activePets = new Dictionary<PetData, PetController>();
        private static double lastUpdateTime;
        private const double MinRepaintInterval = 1.0 / 30.0;
        private static double lastRepaintTime;

        // Global UX Settings
        public static bool interactable = true;
        public static bool showNames = true;
        public static float globalOpacity = 1f;

        public static GlobalPetSettings settings;

        public class Ball
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
            EditorApplication.delayCall += InitializePets;
            lastUpdateTime = EditorApplication.timeSinceStartup;
        }

        public static void InitializePets()
        {
            LoadSettings();
            LoadGlobalSettingsAsset();
            activePets.Clear();

            string[] guids = AssetDatabase.FindAssets("t:PetData");
            if (guids.Length == 0)
            {
                CreateDefaultPet();
                guids = AssetDatabase.FindAssets("t:PetData");
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PetData data = AssetDatabase.LoadAssetAtPath<PetData>(path);
                if (data != null)
                {
                    UpdatePetInstance(data);
                }
            }

            lastUpdateTime = EditorApplication.timeSinceStartup;
        }

        private static void LoadGlobalSettingsAsset()
        {
            if (settings != null) return;

            string path = "Assets/KrostGames/EditorPets/Data/GlobalPetSettings.asset";
            settings = AssetDatabase.LoadAssetAtPath<GlobalPetSettings>(path);

            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/KrostGames/EditorPets/Data"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/KrostGames")) AssetDatabase.CreateFolder("Assets", "KrostGames");
                    if (!AssetDatabase.IsValidFolder("Assets/KrostGames/EditorPets")) AssetDatabase.CreateFolder("Assets/KrostGames", "EditorPets");
                    AssetDatabase.CreateFolder("Assets/KrostGames/EditorPets", "Data");
                }

                settings = ScriptableObject.CreateInstance<GlobalPetSettings>();
                
                // Assign defaults
                settings.heartTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/KrostGames/EditorPets/Textures/Heart.png");
                settings.foodTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/KrostGames/EditorPets/Textures/Food.png");
                settings.ballTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/KrostGames/EditorPets/Textures/Ball.png");

                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
            }
        }

        public static void UpdatePetInstance(PetData data)
        {
            if (data == null) return;
            if (settings == null) LoadGlobalSettingsAsset();

            bool shouldBeInScene = data.isActive && data.location == PetLocation.Scene;

            if (shouldBeInScene)
            {
                if (!activePets.ContainsKey(data))
                {
                    float startX = Random.Range(50, 400);
                    PetController pet = new PetController(data, new Vector2(startX, 100));
                    pet.heartTexture = settings.heartTexture;
                    activePets.Add(data, pet);
                }
                else
                {
                    activePets[data].data = data;
                    activePets[data].heartTexture = settings.heartTexture;
                }
            }
            else
            {
                if (activePets.ContainsKey(data)) activePets.Remove(data);
            }
        }

        private static void EditorUpdate()
        {
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
                        Undo.RecordObject(draggedPet, "Move Pet to Scene");
                        draggedPet.location = PetLocation.Scene;
                        draggedPet.isActive = true;
                        EditorUtility.SetDirty(draggedPet);
                        AssetDatabase.SaveAssets();
                        UpdatePetInstance(draggedPet);
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

        public static void FeedAll()
        {
            if (settings == null) LoadGlobalSettingsAsset();
            foreach (var pet in activePets.Values) pet.Feed(settings.foodTexture);
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

        public static void HideAllPets()
        {
            foreach (var kvp in activePets)
            {
                Undo.RecordObject(kvp.Key, "Hide All Pets");
                kvp.Key.isActive = false;
                EditorUtility.SetDirty(kvp.Key);
            }
            if (activePets.Count > 0) AssetDatabase.SaveAssets();
            activePets.Clear();
        }

        public static void ShowAllPets()
        {
            string[] guids = AssetDatabase.FindAssets("t:PetData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PetData data = AssetDatabase.LoadAssetAtPath<PetData>(path);
                if (data != null)
                {
                    Undo.RecordObject(data, "Show All Pets");
                    data.isActive = true;
                    data.location = PetLocation.Scene;
                    EditorUtility.SetDirty(data);
                    UpdatePetInstance(data);
                }
            }
            AssetDatabase.SaveAssets();
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

        private static void CreateCorgiPet()
        {
            TextureImporter importer = AssetImporter.GetAtPath("Assets/KrostGames/EditorPets/Textures/Corgi.png") as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Texture2D corgiTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/KrostGames/EditorPets/Textures/Corgi.png");
            if (corgiTex == null) return;
            
            PetData newData = ScriptableObject.CreateInstance<PetData>();
            newData.petName = "Corgi";
            newData.idleTexture = corgiTex;
            newData.walkTexture = corgiTex;
            newData.sleepTexture = corgiTex;
            newData.eatTexture = corgiTex;
            newData.framesIdle = 1; newData.framesWalk = 1; newData.framesSleep = 1; newData.framesEat = 1;
            AssetDatabase.CreateAsset(newData, "Assets/KrostGames/EditorPets/Data/Corgi.asset");
            AssetDatabase.SaveAssets();
        }

        private static PetData CreateDefaultPet()
        {
            string rootPath = "Assets/KrostGames";
            string folderPath = rootPath + "/EditorPets";
            if (!AssetDatabase.IsValidFolder(rootPath)) AssetDatabase.CreateFolder("Assets", "KrostGames");
            if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder(rootPath, "EditorPets");
            if (!AssetDatabase.IsValidFolder(folderPath + "/Editor")) AssetDatabase.CreateFolder(folderPath, "Editor");
            if (!AssetDatabase.IsValidFolder(folderPath + "/Data")) AssetDatabase.CreateFolder(folderPath, "Data");
            if (!AssetDatabase.IsValidFolder(folderPath + "/Textures")) AssetDatabase.CreateFolder(folderPath, "Textures");

            PetData newData = ScriptableObject.CreateInstance<PetData>();
            newData.petName = "Pixel Dog";
            newData.idleTexture = CreateProceduralDogTexture(Color.white, "Idle");
            newData.walkTexture = CreateProceduralDogTexture(Color.white, "Walk");
            newData.sleepTexture = CreateProceduralDogTexture(Color.gray, "Sleep");
            newData.framesIdle = 1; newData.framesWalk = 2; newData.framesSleep = 1;
            AssetDatabase.CreateAsset(newData, "Assets/KrostGames/EditorPets/Data/DefaultDog.asset");
            AssetDatabase.SaveAssets();
            return newData;
        }

        private static Texture2D CreateProceduralDogTexture(Color tint, string state)
        {
            bool isWalk = state == "Walk";
            int width = isWalk ? 128 : 64; 
            int height = 64;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; 
            Color clear = new Color(0, 0, 0, 0);
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) tex.SetPixel(x, y, clear);
            bool isSleep = state == "Sleep";
            DrawDogBody(tex, 0, 0, tint, false, isSleep);
            if (isWalk) DrawDogBody(tex, 64, 0, tint, true, false);
            tex.Apply();
            string path = $"Assets/KrostGames/EditorPets/Textures/Dog_{state}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.Refresh();
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void DrawDogBody(Texture2D tex, int offsetX, int offsetY, Color tint, bool walkPose, bool sleepPose)
        {
            int bodyY = sleepPose ? 4 : 12; DrawRect(tex, offsetX + 16, offsetY + bodyY, 32, 20, tint);
            int headY = sleepPose ? 4 : 24; DrawRect(tex, offsetX + 36, offsetY + headY, 20, 20, tint);
            DrawRect(tex, offsetX + 40, offsetY + headY + 16, 8, 12, tint); DrawRect(tex, offsetX + 56, offsetY + headY + 4, 8, 8, tint);
            DrawRect(tex, offsetX + 60, offsetY + headY + 8, 4, 4, Color.black); DrawRect(tex, offsetX + 48, offsetY + headY + 10, 4, 4, sleepPose ? tint : Color.black);
            int tailY = sleepPose ? 4 : 24; DrawRect(tex, offsetX + 8, offsetY + tailY, 8, 8, tint);
            if (sleepPose) { DrawRect(tex, offsetX + 16, offsetY + 0, 8, 8, tint); DrawRect(tex, offsetX + 36, offsetY + 0, 8, 8, tint); }
            else if (walkPose) { DrawRect(tex, offsetX + 16, offsetY + 4, 8, 8, tint); DrawRect(tex, offsetX + 36, offsetY + 4, 8, 8, tint); }
            else { DrawRect(tex, offsetX + 20, offsetY + 4, 8, 8, tint); DrawRect(tex, offsetX + 32, offsetY + 4, 8, 8, tint); }
        }

        private static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color c) { for (int i = 0; i < w; i++) for (int j = 0; j < h; j++) tex.SetPixel(x + i, y + j, c); }
    }
}
#endif
