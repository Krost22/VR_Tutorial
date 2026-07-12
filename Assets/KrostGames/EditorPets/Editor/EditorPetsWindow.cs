#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace EditorPets
{
    public class EditorPetsWindow : EditorWindow
    {
        private List<PetData> allPets = new List<PetData>();
        private Vector2 scrollPos;
        private Dictionary<PetData, bool> foldouts = new Dictionary<PetData, bool>();
        private bool showGlobalItemSettings = false;

        [MenuItem("Tools/Editor Pets Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<EditorPetsWindow>("Editor Pets");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            RefreshPets();
        }

        private void RefreshPets()
        {
            allPets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:PetData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PetData data = AssetDatabase.LoadAssetAtPath<PetData>(path);
                if (data != null)
                {
                    allPets.Add(data);
                    if (!foldouts.ContainsKey(data)) foldouts[data] = false;
                }
            }
        }

        private void OnGUI()
        {
            // Toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Pet Customization & Management", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            bool wasInteractable = ScenePetOverlay.interactable;
            ScenePetOverlay.interactable = GUILayout.Toggle(ScenePetOverlay.interactable, "👆 Interactable", EditorStyles.toolbarButton, GUILayout.Width(100));
            if (wasInteractable != ScenePetOverlay.interactable) ScenePetOverlay.SaveSettings();

            if (GUILayout.Button("🎾 Spawn Ball", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                ScenePetOverlay.SpawnBall(); 
            }

            if (GUILayout.Button("🍖 Feed All", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ScenePetOverlay.FeedAll(); 
            }
            
            if (GUILayout.Button("🔄 Reload All", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                RefreshPets();
                ScenePetOverlay.InitializePets();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            
            // Global Scene Settings
            EditorGUILayout.BeginVertical("helpBox");
            GUILayout.Label("Global Scene View Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginChangeCheck();
            ScenePetOverlay.showNames = EditorGUILayout.ToggleLeft("Show Names", ScenePetOverlay.showNames, GUILayout.Width(100));
            ScenePetOverlay.globalOpacity = EditorGUILayout.Slider("Opacity", ScenePetOverlay.globalOpacity, 0.1f, 1f);
            
            if (EditorGUI.EndChangeCheck())
            {
                ScenePetOverlay.SaveSettings();
            }
            EditorGUILayout.EndHorizontal();

            // Global Item Settings (Food, Heart, Ball)
            EditorGUILayout.Space(2);
            showGlobalItemSettings = EditorGUILayout.Foldout(showGlobalItemSettings, "Global Item Settings (Food, Heart, Ball)", true);
            if (showGlobalItemSettings && ScenePetOverlay.settings != null)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                ScenePetOverlay.settings.foodTexture = (Texture2D)EditorGUILayout.ObjectField("Food Texture", ScenePetOverlay.settings.foodTexture, typeof(Texture2D), false);
                ScenePetOverlay.settings.heartTexture = (Texture2D)EditorGUILayout.ObjectField("Heart Texture", ScenePetOverlay.settings.heartTexture, typeof(Texture2D), false);
                ScenePetOverlay.settings.ballTexture = (Texture2D)EditorGUILayout.ObjectField("Ball Texture", ScenePetOverlay.settings.ballTexture, typeof(Texture2D), false);
                EditorGUILayout.EndVertical();
                
                if (ScenePetOverlay.settings.ballTexture != null)
                    GUILayout.Label(ScenePetOverlay.settings.ballTexture, GUILayout.Width(64), GUILayout.Height(64));
                EditorGUILayout.EndHorizontal();

                ScenePetOverlay.settings.ballRadius = EditorGUILayout.FloatField("Ball Radius", ScenePetOverlay.settings.ballRadius);
                ScenePetOverlay.settings.gravity = EditorGUILayout.FloatField("Ball Gravity", ScenePetOverlay.settings.gravity);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(ScenePetOverlay.settings);
                    AssetDatabase.SaveAssets();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var pet in allPets)
            {
                DrawPetSettings(pet);
            }
            EditorGUILayout.EndScrollView();

            HandleDragAndDrop();
        }

        private void DrawPetSettings(PetData pet)
        {
            if (pet == null) return;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            if (pet.idleTexture != null) GUILayout.Label(pet.idleTexture, GUILayout.Width(48), GUILayout.Height(48));
            else GUILayout.Box("?", GUILayout.Width(48), GUILayout.Height(48));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField(pet.petName, EditorStyles.boldLabel, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(pet, "Rename Pet");
                pet.petName = newName;
                EditorUtility.SetDirty(pet); AssetDatabase.SaveAssets(); ScenePetOverlay.UpdatePetInstance(pet);
            }
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            bool newActive = EditorGUILayout.ToggleLeft("Show in Scene", pet.isActive, GUILayout.Width(110));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(pet, "Toggle Pet Visibility");
                pet.isActive = newActive;
                EditorUtility.SetDirty(pet); AssetDatabase.SaveAssets(); ScenePetOverlay.UpdatePetInstance(pet);
            }
            EditorGUILayout.EndHorizontal();
            bool inScene = pet.location == PetLocation.Scene;
            string buttonText = inScene ? "🏠 Send to House (Hide)" : "🌳 Bring to Scene";
            if (GUILayout.Button(buttonText, GUILayout.Width(160)))
            {
                Undo.RecordObject(pet, "Change Pet Location");
                pet.location = inScene ? PetLocation.House : PetLocation.Scene;
                EditorUtility.SetDirty(pet); AssetDatabase.SaveAssets(); ScenePetOverlay.UpdatePetInstance(pet);
            }
            EditorGUILayout.EndVertical(); EditorGUILayout.EndHorizontal();
            foldouts[pet] = EditorGUILayout.Foldout(foldouts[pet], "Advanced Customization", true);
            if (foldouts[pet])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(); GUILayout.Label("General Movement", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                float newMoveSpeed = EditorGUILayout.Slider("Move Speed", pet.moveSpeed, 0f, 200f);
                float newAnimSpeed = EditorGUILayout.Slider("Anim Speed", pet.animationSpeed, 0.1f, 20f);
                Vector2 newSize = EditorGUILayout.Vector2Field("Draw Size", pet.size);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(pet, "Customize Pet Movement");
                    pet.moveSpeed = newMoveSpeed; pet.animationSpeed = newAnimSpeed; pet.size = newSize;
                    EditorUtility.SetDirty(pet); AssetDatabase.SaveAssets(); ScenePetOverlay.UpdatePetInstance(pet);
                }
                EditorGUILayout.Space(); GUILayout.Label("Animation States", EditorStyles.boldLabel);
                DrawAnimationSection(pet, "IDLE", ref pet.idleTexture, ref pet.framesIdle);
                DrawAnimationSection(pet, "WALK", ref pet.walkTexture, ref pet.framesWalk);
                DrawAnimationSection(pet, "SLEEP", ref pet.sleepTexture, ref pet.framesSleep);
                DrawAnimationSection(pet, "EAT", ref pet.eatTexture, ref pet.framesEat);
                DrawAnimationSection(pet, "PETTED", ref pet.pettedTexture, ref pet.framesPetted);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(); EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Asset", EditorStyles.miniButton, GUILayout.Width(80))) EditorGUIUtility.PingObject(pet);
            if (GUILayout.Button("Randomize Pos", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                pet.isActive = false; ScenePetOverlay.UpdatePetInstance(pet);
                pet.isActive = true; ScenePetOverlay.UpdatePetInstance(pet);
            }
            EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); EditorGUILayout.Space(5);
        }

        private void DrawAnimationSection(PetData pet, string title, ref Texture2D tex, ref int frames)
        {
            EditorGUILayout.BeginVertical("helpBox"); GUILayout.Label(title, EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            tex = (Texture2D)EditorGUILayout.ObjectField("Texture", tex, typeof(Texture2D), false);
            frames = EditorGUILayout.IntField("Frame Count", frames);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(pet, "Update Animation: " + title);
                EditorUtility.SetDirty(pet); AssetDatabase.SaveAssets(); ScenePetOverlay.UpdatePetInstance(pet);
            }
            EditorGUILayout.EndVertical(); EditorGUILayout.Space(2);
        }

        private void HandleDragAndDrop()
        {
            Event e = Event.current;
            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is PetData draggedPet)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        Undo.RecordObject(draggedPet, "Move Pet to House");
                        draggedPet.location = PetLocation.House;
                        EditorUtility.SetDirty(draggedPet); AssetDatabase.SaveAssets(); ScenePetOverlay.UpdatePetInstance(draggedPet);
                        e.Use();
                    }
                }
            }
        }
    }
}
#endif
