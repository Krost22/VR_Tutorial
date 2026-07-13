#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace EditorPets
{
    [CustomEditor(typeof(PetData))]
    public class PetDataEditor : Editor
    {
        private float _previewFrame;
        private bool _previewPlaying = true;
        private double _lastPreviewTime;
        private int _previewState = 0;
        private bool _initialized;
        private static readonly string[] StateNames = { "IDLE", "WALK", "SLEEP", "EAT", "PETTED" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            PetData pet = (PetData)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pet Preview", EditorStyles.boldLabel);

            if (!_initialized)
            {
                _lastPreviewTime = EditorApplication.timeSinceStartup;
                _initialized = true;
            }
            float deltaTime = (float)(EditorApplication.timeSinceStartup - _lastPreviewTime);
            _lastPreviewTime = EditorApplication.timeSinceStartup;
            if (_previewPlaying && deltaTime > 0f && deltaTime < 0.5f)
            {
                _previewFrame += pet.animationSpeed * deltaTime;
            }

            EditorGUILayout.BeginHorizontal();
            _previewState = GUILayout.Toolbar(_previewState, StateNames, GUILayout.Height(22));
            EditorGUILayout.EndHorizontal();

            Texture2D currentTex = GetTextureForState(pet, _previewState);
            int currentFrames = GetFramesForState(pet, _previewState);

            Rect previewRect = GUILayoutUtility.GetRect(192, 96, GUILayout.ExpandWidth(false));
            float previewX = previewRect.x + (previewRect.width - pet.size.x) / 2f;
            float previewY = previewRect.y + (previewRect.height - pet.size.y) / 2f;
            Rect drawRect = new Rect(previewX, previewY, pet.size.x, pet.size.y);

            if (currentTex != null && currentFrames > 0)
            {
                int frame = ((int)_previewFrame) % currentFrames;
                float uWidth = 1f / currentFrames;
                float uStart = frame * uWidth;
                Rect texCoords = new Rect(uStart, 0, uWidth, 1);
                GUI.DrawTextureWithTexCoords(drawRect, currentTex, texCoords, true);
            }
            else
            {
                EditorGUI.DrawRect(drawRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
                GUI.Label(drawRect, "No texture", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            _previewPlaying = GUILayout.Toggle(_previewPlaying, _previewPlaying ? "▶ Playing" : "⏸ Paused", EditorStyles.miniButton);
            if (GUILayout.Button("Reset", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                _previewFrame = 0f;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("petName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isActive"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("location"));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Movement & Size", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Animation States (Horizontal Spritesheets)", EditorStyles.boldLabel);
            DrawAnimationField("Idle", "idleTexture", "framesIdle");
            DrawAnimationField("Walk", "walkTexture", "framesWalk");
            DrawAnimationField("Sleep", "sleepTexture", "framesSleep");
            DrawAnimationField("Eat", "eatTexture", "framesEat");
            DrawAnimationField("Petted", "pettedTexture", "framesPetted");

            if (serializedObject.ApplyModifiedProperties())
            {
                ScenePetOverlay.UpdatePetInstance(pet);
            }

            if (Event.current.type == EventType.Repaint) Repaint();
        }

        private void DrawAnimationField(string label, string texProp, string framesProp)
        {
            SerializedProperty tex = serializedObject.FindProperty(texProp);
            SerializedProperty frames = serializedObject.FindProperty(framesProp);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(tex);
            int maxFrames = tex.objectReferenceValue is Texture2D t ? Mathf.Max(1, t.width / 8) : 32;
            frames.intValue = EditorGUILayout.IntSlider("Frame Count", frames.intValue, 1, maxFrames);
            EditorGUILayout.EndVertical();
        }

        private static Texture2D GetTextureForState(PetData pet, int state)
        {
            switch (state)
            {
                case 0: return pet.idleTexture;
                case 1: return pet.walkTexture;
                case 2: return pet.sleepTexture;
                case 3: return pet.eatTexture != null ? pet.eatTexture : pet.idleTexture;
                case 4: return pet.pettedTexture != null ? pet.pettedTexture : pet.idleTexture;
            }
            return pet.idleTexture;
        }

        private static int GetFramesForState(PetData pet, int state)
        {
            switch (state)
            {
                case 0: return pet.framesIdle;
                case 1: return pet.framesWalk;
                case 2: return pet.framesSleep;
                case 3: return pet.eatTexture != null ? pet.framesEat : pet.framesIdle;
                case 4: return pet.pettedTexture != null ? pet.framesPetted : pet.framesIdle;
            }
            return 1;
        }
    }
}
#endif
