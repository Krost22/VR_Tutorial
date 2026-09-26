#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using static EditorPets.EditorPetsWindow;

namespace EditorPets
{
    // First-run "pick your starter" screen. Every pet is unlocked; choosing one just makes it
    // your first companion (the only visible one), the rest wait in the Editor Pets window.
    public class WelcomeWindow : EditorWindow
    {
        private const string PrefsKey = "EditorPets_Welcomed_v1";

        // The classic starter triangle.
        private static readonly (string pet, string typeEn, string typeEs, string cls, string en, string es)[] Starters =
        {
            ("Red Dragon", "FIRE", "FUEGO", "type-fire",
             "Tiny, proud, and breathes a little flame when you pet it.",
             "Pequeño y orgulloso, escupe una llamita cuando lo acaricias."),
            ("Turtle", "WATER", "AGUA", "type-water",
             "Slow and steady. Loves leaves and naps inside its shell.",
             "Lenta pero segura. Ama las hojas y duerme dentro de su caparazón."),
            ("Snake", "GRASS", "PLANTA", "type-grass",
             "Curious and wiggly, flicks its tongue at everything.",
             "Curiosa y ondulante, le saca la lengua a todo."),
        };

        private PetData _choice;
        private PetData _chosen; // set once confirmed: switches to the "joined" page
        private bool _showAll;
        private readonly List<(VisualElement element, PetData pet)> _selectable = new List<(VisualElement, PetData)>();

        [InitializeOnLoadMethod]
        private static void CheckFirstRun() =>
            EditorApplication.delayCall += () => { if (!EditorPrefs.GetBool(PrefsKey, false)) ShowWindow(); };

        [MenuItem("Tools/Editor Pets/Welcome", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<WelcomeWindow>(true, "Welcome to EditorPets");
            window.minSize = window.maxSize = new Vector2(600, 560);
            window.position = new Rect((Screen.currentResolution.width - 600) / 2, (Screen.currentResolution.height - 560) / 2, 600, 560);
            window.Show();
        }

        // A small pixel-art platformer level to watch the pets on, framed in a 2D Scene View.
        [MenuItem("Tools/Editor Pets/Open Demo Scene", false, 2)]
        public static void OpenDemoScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePetOverlay.Root + "/Demo/EditorPets_Demo.unity");
            var view = SceneView.lastActiveSceneView;
            if (view == null) return;
            view.in2DMode = true;
            view.LookAt(Vector3.zero, Quaternion.identity, 7.5f, true, true);
        }

        private void OnEnable() => IsEnglish = EditorPrefs.GetBool(LANG_KEY, true);

        public void CreateGUI()
        {
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(ScenePetOverlay.UIFolder + "/WelcomeWindow.uss"));
            Rebuild();
        }

        private void Rebuild()
        {
            var root = rootVisualElement;
            root.Clear();
            root.AddToClassList("welcome");
            _selectable.Clear();

            var lang = new VisualElement();
            lang.AddToClassList("lang");
            foreach (var (label, english) in new[] { ("EN", true), ("ES", false) })
            {
                var b = new Button(() => { IsEnglish = english; EditorPrefs.SetBool(LANG_KEY, english); Rebuild(); }) { text = label };
                b.AddToClassList("lang-btn");
                b.EnableInClassList("active", IsEnglish == english);
                lang.Add(b);
            }
            root.Add(lang);

            if (_chosen != null) BuildJoinedPage(root);
            else BuildChoosePage(root);
        }

        // ---------- page 1: choose ----------

        private void BuildChoosePage(VisualElement root)
        {
            root.Add(Text(T("Choose your first pet!", "¡Elige tu primera mascota!"), "title"));
            root.Add(Text(T("Your companion will live in your Scene View. Don't worry: every pet is unlocked and waits for you in the Editor Pets window.",
                            "Tu compañero vivirá en tu Scene View. Tranquilo: todas las mascotas están desbloqueadas y te esperan en la ventana Editor Pets."), "subtitle"));

            // Starters | All pets: each view gets the whole window instead of stacking both.
            var all = ScenePetOverlay.FindAll<PetData>().OrderBy(p => p.petName).ToList();
            var tabs = new VisualElement();
            tabs.AddToClassList("tabs");
            foreach (var (label, showAll) in new[] { (T("★ Starters", "★ Iniciales"), false), (T($"All pets ({all.Count})", $"Todas ({all.Count})"), true) })
            {
                var tab = new Button(() => { _showAll = showAll; Rebuild(); }) { text = label };
                tab.AddToClassList("tab");
                tab.EnableInClassList("active", _showAll == showAll);
                tabs.Add(tab);
            }
            root.Add(tabs);

            if (!_showAll)
            {
                root.Add(new VisualElement { style = { flexGrow = 1 } }); // keep the cards vertically centered
                var cards = new VisualElement();
                cards.AddToClassList("starters");
                foreach (var s in Starters)
                {
                    var pet = Load(s.pet);
                    if (pet == null) continue;
                    var card = new VisualElement();
                    card.AddToClassList("starter");
                    var badge = Text(IsEnglish ? s.typeEn : s.typeEs, "type-badge");
                    badge.AddToClassList(s.cls);
                    card.Add(badge);
                    card.Add(Sprite(pet, 96, card));
                    card.Add(Text(pet.petName, "starter-name"));
                    card.Add(Text(IsEnglish ? s.en : s.es, "starter-desc"));
                    MakeSelectable(card, pet);
                    cards.Add(card);
                }
                root.Add(cards);
                root.Add(new VisualElement { style = { flexGrow = 1 } });
            }
            else
            {
                var scroll = new ScrollView();
                scroll.AddToClassList("all-scroll");
                var grid = new VisualElement();
                grid.AddToClassList("all-grid");
                foreach (var pet in all)
                {
                    var tile = new VisualElement();
                    tile.AddToClassList("mini");
                    tile.Add(Sprite(pet, 64, tile));
                    tile.Add(Text(pet.petName, "mini-name"));
                    MakeSelectable(tile, pet);
                    grid.Add(tile);
                }
                scroll.Add(grid);
                root.Add(scroll);
            }

            var footer = new VisualElement();
            footer.AddToClassList("footer");
            var skip = new Button(Close) { text = T("Skip", "Omitir"), tooltip = T("Keep every pet as it is", "Deja todas las mascotas como están") };
            skip.AddToClassList("link-btn");
            footer.Add(skip);
            footer.Add(new VisualElement { style = { flexGrow = 1 } });
            var confirm = new Button(Confirm) { name = "confirm" };
            confirm.AddToClassList("primary-btn");
            footer.Add(confirm);
            root.Add(footer);
            RefreshSelection();
        }

        private void MakeSelectable(VisualElement element, PetData pet)
        {
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                _choice = pet;
                RefreshSelection();
                if (evt.clickCount == 2) Confirm();
            });
            element.tooltip = pet.petName;
            _selectable.Add((element, pet));
        }

        private void RefreshSelection()
        {
            foreach (var (element, pet) in _selectable) element.EnableInClassList("selected", pet == _choice);
            var confirm = rootVisualElement.Q<Button>("confirm");
            if (confirm == null) return;
            confirm.SetEnabled(_choice != null);
            confirm.text = _choice != null ? T($"I choose you, {_choice.petName}!", $"¡Te elijo a ti, {_choice.petName}!")
                                           : T("Pick a pet", "Elige una mascota");
        }

        private void Confirm()
        {
            if (_choice == null) return;
            ScenePetOverlay.ShowOnly(_choice);
            EditorPrefs.SetBool(PrefsKey, true);
            _chosen = _choice;
            Rebuild();
        }

        // ---------- page 2: joined ----------

        private void BuildJoinedPage(VisualElement root)
        {
            root.Add(new VisualElement { style = { flexGrow = 1 } });
            var hero = new VisualElement();
            hero.AddToClassList("hero");
            hero.Add(Sprite(_chosen, 160, null, PetState.Interact));
            root.Add(hero);
            root.Add(Text(T($"{_chosen.petName} joined your Scene View!", $"¡{_chosen.petName} se unió a tu Scene View!"), "title"));

            var tips = new VisualElement();
            tips.AddToClassList("tips");
            foreach (var tip in new[]
            {
                T("Click it to pet it, drag it to move it.", "Haz clic para acariciarla, arrástrala para moverla."),
                T("Open Editor Pets to feed it, throw a ball or make your own pet.", "Abre Editor Pets para darle de comer, lanzarle una pelota o crear tu propia mascota."),
                T("The other pets are in the grid, one click away (the eye shows them).", "Las demás están en la cuadrícula, a un clic (el ojo las muestra)."),
            }) tips.Add(Text("•  " + tip, "tip"));
            root.Add(tips);
            root.Add(new VisualElement { style = { flexGrow = 1 } });

            var footer = new VisualElement();
            footer.AddToClassList("footer");
            var dontShow = new Toggle(T("Don't show this again", "No volver a mostrar")) { value = EditorPrefs.GetBool(PrefsKey, false) };
            dontShow.RegisterValueChangedCallback(evt => EditorPrefs.SetBool(PrefsKey, evt.newValue));
            footer.Add(dontShow);
            footer.Add(new VisualElement { style = { flexGrow = 1 } });
            var close = new Button(Close) { text = T("Close", "Cerrar") };
            close.AddToClassList("secondary-btn");
            footer.Add(close);
            var demo = new Button(() => { OpenDemoScene(); Close(); }) { text = T("Open demo scene", "Abrir escena demo") };
            demo.AddToClassList("secondary-btn");
            footer.Add(demo);
            var open = new Button(() => { EditorPetsWindow.ShowWindow(); Close(); }) { text = T("Open Editor Pets", "Abrir Editor Pets") };
            open.AddToClassList("primary-btn");
            footer.Add(open);
            root.Add(footer);
        }

        // ---------- helpers ----------

        private static PetData Load(string name) =>
            AssetDatabase.LoadAssetAtPath<PetData>($"{ScenePetOverlay.Root}/Pets/{name}/{name}.asset");

        private static Label Text(string text, string cls)
        {
            var label = new Label(text);
            label.AddToClassList(cls);
            return label;
        }

        // Animated, pixel-crisp sprite. Walks while hovered, cheers once selected.
        private VisualElement Sprite(PetData pet, float size, VisualElement hoverTarget, PetState fixedState = PetState.Idle)
        {
            bool hover = false;
            hoverTarget?.RegisterCallback<PointerEnterEvent>(_ => hover = true);
            hoverTarget?.RegisterCallback<PointerLeaveEvent>(_ => hover = false);
            var box = new IMGUIContainer { pickingMode = PickingMode.Ignore, style = { width = size, height = size, alignSelf = Align.Center } };
            box.onGUIHandler = () =>
            {
                var state = fixedState != PetState.Idle ? fixedState : pet == _choice ? PetState.Interact : hover ? PetState.Walk : PetState.Idle;
                var (tex, frames) = pet.GetAnimation(state);
                if (tex == null) return;
                Rect r = box.contentRect;
                float frameW = (float)tex.width / frames, fit = Mathf.Min(r.width / frameW, r.height / tex.height);
                float scale = fit >= 2 ? Mathf.Floor(fit) : fit; // whole-pixel scaling when it doesn't leave the sprite tiny
                var drawSize = new Vector2(frameW, tex.height) * scale;
                var rect = new Rect(r.center.x - drawSize.x / 2, r.yMax - drawSize.y, drawSize.x, drawSize.y);
                int frame = (int)(EditorApplication.timeSinceStartup * pet.animationSpeed) % frames;
                GUI.DrawTextureWithTexCoords(rect, tex, new Rect((float)frame / frames, 0, 1f / frames, 1));
            };
            box.schedule.Execute(box.MarkDirtyRepaint).Every(80);
            return box;
        }
    }
}
#endif
