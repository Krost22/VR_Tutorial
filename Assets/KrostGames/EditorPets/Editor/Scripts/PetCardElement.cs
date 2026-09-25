#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using static EditorPets.EditorPetsWindow;

namespace EditorPets
{
    // One tile of the pets grid: animated thumbnail (walks on hover), name, and an eye to show/hide.
    // Click selects, double-click pings the asset, right-click opens more actions.
    public class PetCardElement : VisualElement
    {
        public readonly PetData Pet;
        private readonly Label _name;
        private readonly Button _eye;
        private readonly IMGUIContainer _thumb;
        private bool _hover;
        private bool? _shownActive;

        public PetCardElement(PetData pet, Action<PetData> onSelect)
        {
            Pet = pet;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ScenePetOverlay.UIFolder + "/PetCardElement.uxml").CloneTree(this);
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(ScenePetOverlay.UIFolder + "/PetCardElement.uss"));
            AddToClassList("pet-tile");

            _name = this.Q<Label>("lbl-name");
            _eye = this.Q<Button>("btn-eye");
            _thumb = new IMGUIContainer(DrawThumbnail) { pickingMode = PickingMode.Ignore };
            _thumb.AddToClassList("tile-thumb");
            this.Q("thumb").Add(_thumb);

            _eye.clicked += () => ScenePetOverlay.SetVisible(Pet, !Pet.isActive);
            RegisterCallback<PointerEnterEvent>(_ => _hover = true);
            RegisterCallback<PointerLeaveEvent>(_ => _hover = false);
            RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                onSelect(Pet);
                if (evt.clickCount == 2) { Selection.activeObject = Pet; EditorGUIUtility.PingObject(Pet); }
            });
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                onSelect(Pet);
                evt.menu.AppendAction(Pet.isActive ? T("Hide", "Ocultar") : T("Show", "Mostrar"), _ => ScenePetOverlay.SetVisible(Pet, !Pet.isActive));
                evt.menu.AppendAction(T("Show only this one", "Mostrar solo esta"), _ => ScenePetOverlay.ShowOnly(Pet));
                evt.menu.AppendAction(T("Move to a random spot", "Mover a un sitio aleatorio"), _ => ScenePetOverlay.RandomizePetPosition(Pet),
                    Pet.isActive ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendSeparator();
                evt.menu.AppendAction(T("Duplicate", "Duplicar"), _ => onSelect(ScenePetOverlay.Duplicate(Pet)));
                evt.menu.AppendAction(T("Select asset", "Seleccionar asset"), _ => { Selection.activeObject = Pet; EditorGUIUtility.PingObject(Pet); });
            }));
            Refresh(false);
        }

        // Polled by the window: picks up renames, Undo, Hide All... without extra bookkeeping.
        public void Refresh(bool selected)
        {
            if (_name.text != Pet.petName)
            {
                _name.text = Pet.petName; // long names are cut with "…", so the full one goes in the tooltip
                tooltip = Pet.petName + "\n" + T("Click: edit · Double-click: select asset · Right-click: more",
                                                 "Clic: editar · Doble clic: seleccionar asset · Clic derecho: más");
            }
            EnableInClassList("selected", selected);
            if (_shownActive != Pet.isActive)
            {
                _shownActive = Pet.isActive;
                EnableInClassList("hidden", !Pet.isActive);
                _eye.style.backgroundImage = new StyleBackground(Icon(Pet.isActive ? "scenevis_visible_hover" : "scenevis_hidden_hover"));
                _eye.tooltip = Pet.isActive ? T("Visible in the Scene View (click to hide)", "Visible en la Scene View (clic para ocultar)")
                                            : T("Hidden (click to show)", "Oculta (clic para mostrar)");
            }
            _thumb.MarkDirtyRepaint();
        }

        private void DrawThumbnail()
        {
            var (tex, frames) = Pet.GetAnimation(_hover ? PetState.Walk : PetState.Idle);
            if (tex == null) return;
            // Fit one frame into the box, keeping its aspect (Point filtering keeps pixel art crisp).
            Rect box = _thumb.contentRect;
            float frameW = (float)tex.width / frames, scale = Mathf.Min(box.width / frameW, box.height / tex.height);
            var size = new Vector2(frameW, tex.height) * scale;
            var rect = new Rect(box.center.x - size.x / 2, box.yMax - size.y, size.x, size.y);
            int frame = (int)(EditorApplication.timeSinceStartup * Pet.animationSpeed) % frames;
            GUI.color = new Color(1, 1, 1, Pet.isActive ? 1f : 0.35f);
            GUI.DrawTextureWithTexCoords(rect, tex, new Rect((float)frame / frames, 0, 1f / frames, 1));
            GUI.color = Color.white;
        }
    }
}
#endif
