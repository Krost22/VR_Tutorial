#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace EditorPets
{
    public class WelcomeWindow : EditorWindow
    {
        private const string PrefsKey = "EditorPets_Welcomed_v1";
        private static Texture2D _logo;
        private static Texture2D _iconSettings;
        private static Texture2D _iconBall;
        private static Texture2D _iconPet;

        [InitializeOnLoadMethod]
        private static void CheckFirstRun()
        {
            EditorApplication.delayCall += ShowIfFirstRun;
        }

        private static void ShowIfFirstRun()
        {
            if (EditorPrefs.GetBool(PrefsKey, false)) return;
            ShowWindow();
        }

        [MenuItem("Tools/Editor Pets/Welcome")]
        public static void ShowWindow()
        {
            var window = GetWindow<WelcomeWindow>("Welcome to EditorPets");
            window.minSize = new Vector2(520, 460);
            window.maxSize = new Vector2(520, 460);
            window.position = new Rect(
                (Screen.currentResolution.width - 520) / 2,
                (Screen.currentResolution.height - 460) / 2,
                520, 460);
            window.Show();
        }

        private void OnEnable()
        {
            _logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/KrostGames/EditorPets/icon.png");
        }

        private void OnDisable()
        {
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(8);
            DrawSteps();
            EditorGUILayout.Space(8);
            DrawActions();
            EditorGUILayout.Space(4);
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (_logo != null)
            {
                Rect logoRect = GUILayoutUtility.GetRect(96, 96, GUILayout.Width(96), GUILayout.Height(96));
                GUI.DrawTexture(logoRect, _logo, ScaleMode.ScaleToFit);
            }
            else
            {
                GUILayout.Box("🐾", GUILayout.Width(96), GUILayout.Height(96));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("Welcome to EditorPets", headerStyle);
            var subStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("Interactive pets in your Scene View — version 1.0.0", subStyle);
        }

        private void DrawSteps()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Quick Start", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawStep("1.", "Open the settings window",
                "Tools → Editor Pets Settings to manage pets and global textures.",
                ref _iconSettings);
            EditorGUILayout.Space(2);
            DrawStep("2.", "Spawn the ball",
                "Click 🎾 Spawn Ball to give your pets something to play with.",
                ref _iconBall);
            EditorGUILayout.Space(2);
            DrawStep("3.", "Add pets to the scene",
                "Drag a PetData from the Project window into the Scene View, " +
                "or create one via Assets → Create → EditorPets → Pet Data.",
                ref _iconPet);
            EditorGUILayout.EndVertical();
        }

        private static void DrawStep(string number, string title, string description, ref Texture2D icon)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(number, GUILayout.Width(20));
            EditorGUILayout.BeginVertical();
            GUILayout.Label(title, EditorStyles.boldLabel);
            var descStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label(description, descStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Settings", GUILayout.Height(32)))
            {
                EditorPetsWindow.ShowWindow();
            }
            if (GUILayout.Button("Open Sample Scene", GUILayout.Height(32)))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    "Assets/KrostGames/EditorPets/Example scene/Editor Pets Sample.unity",
                    UnityEditor.SceneManagement.OpenSceneMode.Single);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            bool dontShow = GUILayout.Toggle(
                EditorPrefs.GetBool(PrefsKey, false),
                " Don't show this again");
            if (dontShow != EditorPrefs.GetBool(PrefsKey, false))
            {
                EditorPrefs.SetBool(PrefsKey, dontShow);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(80)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
