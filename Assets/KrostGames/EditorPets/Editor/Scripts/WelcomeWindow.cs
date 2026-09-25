#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace EditorPets
{
    public class WelcomeWindow : EditorWindow
    {
        private const string PrefsKey = "EditorPets_Welcomed_v1";
        private static Texture2D _logo;

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

        [MenuItem("Tools/Editor Pets/Welcome", false, 1)]
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
            _logo = AssetDatabase.LoadAssetAtPath<Texture2D>(ScenePetOverlay.UIFolder + "/icon.png");
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

            DrawStep("1.", "Meet your pets",
                "They already live in your Scene View. Click one to pet it, drag it to move it.");
            EditorGUILayout.Space(2);
            DrawStep("2.", "Open the Editor Pets window",
                "Tools → Editor Pets → Settings: show/hide pets, spawn a ball, feed them.");
            EditorGUILayout.Space(2);
            DrawStep("3.", "Make your own pet",
                "Draw a sprite sheet (square frames in one row, e.g. 4 × 64px = 256×64 PNG). " +
                "Select it in the Project window and click + New Pet. Frames are detected automatically.");
            EditorGUILayout.EndVertical();
        }

        private static void DrawStep(string number, string title, string description)
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
            if (GUILayout.Button("Open Editor Pets", GUILayout.Height(32)))
            {
                EditorPetsWindow.ShowWindow();
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
