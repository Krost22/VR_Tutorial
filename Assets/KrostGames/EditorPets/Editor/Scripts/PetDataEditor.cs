#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace EditorPets
{
    // Animated preview on top of the regular fields. Also embedded in each pet card of the Editor Pets window.
    [CustomEditor(typeof(PetData))]
    public class PetDataEditor : Editor
    {
        private static readonly string[] StateNames = { "IDLE", "WALK", "SLEEP", "EAT", "PETTED" };
        private static readonly PetState[] States = { PetState.Idle, PetState.Walk, PetState.Sleep, PetState.Eat, PetState.Interact };
        private int _previewState;
        private bool _playing = true;
        private int _frame;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var preview = new IMGUIContainer(DrawPreview);
            preview.schedule.Execute(preview.MarkDirtyRepaint).Every(33); // keep the animation running
            root.Add(preview);
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            root.Q("PropertyField:m_Script")?.RemoveFromHierarchy(); // noise for people just making a pet
            var foodField = root.Q("PropertyField:food");
            foodField?.parent.Insert(foodField.parent.IndexOf(foodField) + 1, FoodPicker());
            return root;
        }

        // One click to pick the food from Items/Food. Any PNG dropped in that folder shows up here.
        private VisualElement FoodPicker()
        {
            var prop = serializedObject.FindProperty("food");
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginLeft = 3, marginBottom = 6 } };
            var buttons = new List<(Button button, Texture2D food)>();
            foreach (var food in FoodLibrary())
            {
                var button = new Button(() => { prop.objectReferenceValue = food; serializedObject.ApplyModifiedProperties(); }) { tooltip = food.name };
                var s = button.style;
                s.width = 36; s.height = 36; s.marginLeft = s.marginRight = s.marginTop = s.marginBottom = 1;
                s.paddingLeft = s.paddingRight = s.paddingTop = s.paddingBottom = 2;
                s.borderLeftWidth = s.borderRightWidth = s.borderTopWidth = s.borderBottomWidth = 2;
                s.backgroundImage = food;
                s.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                row.Add(button);
                buttons.Add((button, food));
            }
            void Highlight(SerializedProperty p)
            {
                foreach (var (button, food) in buttons)
                {
                    var color = p.objectReferenceValue == food ? new Color(1f, 0.51f, 0.27f) : new Color(0, 0, 0, 0.25f);
                    var s = button.style;
                    s.borderLeftColor = s.borderRightColor = s.borderTopColor = s.borderBottomColor = color;
                }
            }
            row.TrackPropertyValue(prop, Highlight);
            Highlight(prop);
            return row;
        }

        private static IEnumerable<Texture2D> FoodLibrary() =>
            !AssetDatabase.IsValidFolder(ScenePetOverlay.FoodFolder) ? Enumerable.Empty<Texture2D>() :
            AssetDatabase.FindAssets("t:Texture2D", new[] { ScenePetOverlay.FoodFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(t => t != null).OrderBy(t => t.name);

        private void DrawPreview()
        {
            var pet = (PetData)target;
            _previewState = GUILayout.Toolbar(_previewState, StateNames, GUILayout.Height(22));
            var (tex, frames) = pet.GetAnimation(States[_previewState]);

            Rect area = GUILayoutUtility.GetRect(96, 96, GUILayout.ExpandWidth(true));
            float scale = Mathf.Min(1f, area.width / pet.size.x, area.height / pet.size.y);
            Vector2 drawSize = pet.size * scale;
            Rect drawRect = new Rect(area.center - drawSize / 2f, drawSize);

            if (tex != null)
            {
                if (_playing) _frame = (int)(EditorApplication.timeSinceStartup * pet.animationSpeed);
                GUI.DrawTextureWithTexCoords(drawRect, tex, new Rect((float)(_frame % frames) / frames, 0, 1f / frames, 1));
                var food = pet.food != null ? pet.food : ScenePetOverlay.settings != null ? ScenePetOverlay.settings.foodTexture : null;
                if (States[_previewState] == PetState.Eat && food != null)
                    GUI.DrawTexture(new Rect(drawRect.x + drawRect.width * 0.75f, drawRect.yMax - 32 * scale, 32 * scale, 32 * scale), food);
            }
            else
            {
                EditorGUI.DrawRect(drawRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
                GUI.Label(area, "Drop a sprite sheet in Idle Texture", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _playing = GUILayout.Toggle(_playing, _playing ? "▶ Playing" : "⏸ Paused", EditorStyles.miniButton, GUILayout.Width(80));
            GUILayout.Label($"{frames} frame(s)", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }
    }
}
#endif
