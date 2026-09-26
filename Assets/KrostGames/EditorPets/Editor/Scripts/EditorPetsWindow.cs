#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace EditorPets
{
    // Pets tab: searchable/filterable grid of every pet + the selected pet's inspector below.
    // Settings tab: Scene View options and the shared ball/food/heart settings.
    public class EditorPetsWindow : EditorWindow
    {
        public const string LANG_KEY = "EditorPets_IsEnglish";
        private enum Filter { All, Visible, Hidden }

        public static bool IsEnglish = true;
        public static string T(string en, string es) => IsEnglish ? en : es;

        // Built-in editor icon, dark-skin variant when available.
        public static Texture2D Icon(string name)
        {
            var dark = EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_" + name).image : null;
            return (Texture2D)(dark != null ? dark : EditorGUIUtility.IconContent(name).image);
        }

        // Survive domain reloads (serialized with the window).
        [SerializeField] private string _selectedGuid;
        [SerializeField] private string _search = "";
        [SerializeField] private Filter _filter;
        [SerializeField] private bool _settingsTab;

        private VisualElement _root;
        private VisualElement _grid;
        private readonly List<PetCardElement> _tiles = new List<PetCardElement>();
        private List<PetData> _pets = new List<PetData>();
        private PetData _selected;

        [MenuItem("Tools/Editor Pets/Settings", false, 0)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<EditorPetsWindow>("Editor Pets");
            wnd.minSize = new Vector2(360, 420);
        }

        private void OnEnable()
        {
            IsEnglish = EditorPrefs.GetBool(LANG_KEY, true);
            titleContent = new GUIContent("Editor Pets", Icon("UnityEditor.SceneView"));
            EditorApplication.projectChanged += RefreshPets;
        }

        private void OnDisable() => EditorApplication.projectChanged -= RefreshPets;

        public void CreateGUI()
        {
            _root = rootVisualElement;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ScenePetOverlay.UIFolder + "/EditorPetsWindow.uxml").CloneTree(_root);
            _root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(ScenePetOverlay.UIFolder + "/EditorPetsWindow.uss"));
            _grid = _root.Q("pets-grid");

            // Header + toolbar
            _root.Q<Button>("btn-lang-en").clicked += () => SetLanguage(true);
            _root.Q<Button>("btn-lang-es").clicked += () => SetLanguage(false);
            _root.Q<Button>("btn-help").clicked += ShowHelpDialog;
            _root.Q<Button>("btn-new-pet").clicked += () => { ScenePetOverlay.CreatePetFromSelection(); Select(Selection.activeObject as PetData); };
            _root.Q<Button>("btn-spawn-ball").clicked += ScenePetOverlay.SpawnBall;
            _root.Q<Button>("btn-feed-all").clicked += ScenePetOverlay.FeedAll;
            var interactable = _root.Q<Toggle>("toggle-interactable");
            interactable.SetValueWithoutNotify(ScenePetOverlay.interactable);
            interactable.RegisterValueChangedCallback(evt => { ScenePetOverlay.interactable = evt.newValue; ScenePetOverlay.SaveSettings(); });

            // Tabs
            _root.Q<Button>("tab-pets").clicked += () => ShowTab(false);
            _root.Q<Button>("tab-settings").clicked += () => ShowTab(true);

            // Filters
            var search = _root.Q<ToolbarSearchField>("search");
            search.SetValueWithoutNotify(_search);
            search.RegisterValueChangedCallback(evt => { _search = evt.newValue; ApplyFilter(); });
            _root.Q<Button>("filter-all").clicked += () => SetFilter(Filter.All);
            _root.Q<Button>("filter-visible").clicked += () => SetFilter(Filter.Visible);
            _root.Q<Button>("filter-hidden").clicked += () => SetFilter(Filter.Hidden);
            _root.Q<Button>("btn-show-all").clicked += () => ScenePetOverlay.SetAllVisible(true);
            _root.Q<Button>("btn-hide-all").clicked += () => ScenePetOverlay.SetAllVisible(false);

            // Selected pet
            _root.Q<Toggle>("toggle-detail-visible").RegisterValueChangedCallback(evt =>
            {
                if (_selected != null && _selected.isActive != evt.newValue) ScenePetOverlay.SetVisible(_selected, evt.newValue);
            });
            _root.Q<Button>("btn-solo").clicked += () => { if (_selected) ScenePetOverlay.ShowOnly(_selected); };
            _root.Q<Button>("btn-randomize").clicked += () => { if (_selected) ScenePetOverlay.RandomizePetPosition(_selected); };
            _root.Q<Button>("btn-duplicate").clicked += () => { if (_selected) Select(ScenePetOverlay.Duplicate(_selected)); };
            _root.Q<Button>("btn-select").clicked += () => { if (_selected) { Selection.activeObject = _selected; EditorGUIUtility.PingObject(_selected); } };

            // Settings tab
            var showNames = _root.Q<Toggle>("toggle-show-names");
            showNames.SetValueWithoutNotify(ScenePetOverlay.showNames);
            showNames.RegisterValueChangedCallback(evt => { ScenePetOverlay.showNames = evt.newValue; ScenePetOverlay.SaveSettings(); });
            var opacity = _root.Q<Slider>("slider-opacity");
            opacity.SetValueWithoutNotify(ScenePetOverlay.globalOpacity);
            opacity.RegisterValueChangedCallback(evt => { ScenePetOverlay.globalOpacity = evt.newValue; ScenePetOverlay.SaveSettings(); });

            // Dropping a pet asset (e.g. dragged out of the Scene View) onto this window hides it.
            _root.RegisterCallback<DragUpdatedEvent>(_ => { if (DraggedPet() != null) DragAndDrop.visualMode = DragAndDropVisualMode.Move; });
            _root.RegisterCallback<DragPerformEvent>(_ =>
            {
                var pet = DraggedPet();
                if (pet == null) return;
                DragAndDrop.AcceptDrag();
                ScenePetOverlay.SetVisible(pet, false);
            });

            // Polling keeps tiles, counters and thumbnails in sync with any change (Undo, inspector, Scene View drag...).
            _root.schedule.Execute(Tick).Every(120);

            SetLanguage(IsEnglish);
            ShowTab(_settingsTab);
        }

        private static PetData DraggedPet() =>
            DragAndDrop.objectReferences.Length > 0 ? DragAndDrop.objectReferences[0] as PetData : null;

        // ---------- pets grid ----------

        private void RefreshPets()
        {
            if (_grid == null) return;
            var pets = ScenePetOverlay.FindAll<PetData>().OrderBy(p => p.petName).ToList();
            if (pets.SequenceEqual(_pets)) return; // unrelated asset change: keep the UI as is
            _pets = pets;

            _grid.Clear();
            _tiles.Clear();
            foreach (var pet in pets)
            {
                var tile = new PetCardElement(pet, Select);
                _tiles.Add(tile);
                _grid.Add(tile);
            }

            var keep = _pets.FirstOrDefault(p => AssetGuid(p) == _selectedGuid) ?? _pets.FirstOrDefault();
            _selected = null; // force the inspector to rebuild
            Select(keep);
            ApplyFilter();
        }

        private void Select(PetData pet)
        {
            if (pet == null || pet == _selected) return;
            _selected = pet;
            _selectedGuid = AssetGuid(pet);
            var scroll = _root.Q<ScrollView>("details-scroll");
            scroll.Clear();
            scroll.Add(new InspectorElement(pet));
            Tick();
        }

        private static string AssetGuid(Object o) => o != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o)) : null;

        private void SetFilter(Filter filter)
        {
            _filter = filter;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            foreach (var tile in _tiles)
            {
                bool match = (_filter == Filter.All || tile.Pet.isActive == (_filter == Filter.Visible))
                             && (string.IsNullOrEmpty(_search) || tile.Pet.petName.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0);
                tile.style.display = match ? DisplayStyle.Flex : DisplayStyle.None;
            }
            _root.Q("filter-all").EnableInClassList("active", _filter == Filter.All);
            _root.Q("filter-visible").EnableInClassList("active", _filter == Filter.Visible);
            _root.Q("filter-hidden").EnableInClassList("active", _filter == Filter.Hidden);

            var empty = _root.Q<Label>("lbl-empty");
            bool anyShown = _tiles.Any(t => t.style.display == DisplayStyle.Flex);
            empty.style.display = anyShown ? DisplayStyle.None : DisplayStyle.Flex;
            empty.text = _pets.Count == 0
                ? T("No pets yet. Select one or more sprite sheets in the Project window and click + New Pet.",
                    "Aún no hay mascotas. Selecciona uno o más sprite sheets en la ventana Project y pulsa + Nueva Mascota.")
                : T("No pets match the search.", "Ninguna mascota coincide con la búsqueda.");
        }

        private void Tick()
        {
            if (_root == null) return;
            foreach (var tile in _tiles)
                if (tile.Pet != null) tile.Refresh(tile.Pet == _selected);
            if (_filter != Filter.All) ApplyFilter(); // visibility may have changed

            int visible = _pets.Count(p => p != null && p.isActive);
            _root.Q<Label>("lbl-count").text = T($"{_pets.Count} pet{(_pets.Count == 1 ? "" : "s")} · {visible} visible",
                                                  $"{_pets.Count} mascota{(_pets.Count == 1 ? "" : "s")} · {visible} visible{(visible == 1 ? "" : "s")}");

            bool has = _selected != null;
            _root.Q("details-header").SetEnabled(has);
            _root.Q<Label>("lbl-detail-name").text = has ? _selected.petName : T("No pet selected", "Ninguna mascota seleccionada");
            _root.Q<Toggle>("toggle-detail-visible").SetValueWithoutNotify(has && _selected.isActive);
            _root.Q("btn-randomize").SetEnabled(has && _selected.isActive);
        }

        // ---------- tabs, language, help ----------

        private void ShowTab(bool settings)
        {
            _settingsTab = settings;
            _root.Q("page-pets").style.display = settings ? DisplayStyle.None : DisplayStyle.Flex;
            _root.Q("page-settings").style.display = settings ? DisplayStyle.Flex : DisplayStyle.None;
            _root.Q("tab-pets").EnableInClassList("active", !settings);
            _root.Q("tab-settings").EnableInClassList("active", settings);

            var global = _root.Q("global-settings");
            if (settings && global.childCount == 0)
            {
                ScenePetOverlay.LoadGlobalSettingsAsset();
                if (ScenePetOverlay.settings == null) return;
                var inspector = new InspectorElement(ScenePetOverlay.settings);
                global.Add(inspector);
                inspector.schedule.Execute(() => inspector.Q("PropertyField:m_Script")?.RemoveFromHierarchy());
            }
        }

        private void SetLanguage(bool english)
        {
            IsEnglish = english;
            EditorPrefs.SetBool(LANG_KEY, english);
            _root.Q("btn-lang-en").EnableInClassList("active", english);
            _root.Q("btn-lang-es").EnableInClassList("active", !english);

            _root.Q<Button>("btn-help").tooltip = T("How to make your own pet", "Cómo crear tu propia mascota");
            SetButton("btn-new-pet", T("+ New Pet", "+ Nueva Mascota"), T("Creates a pet from the sprite sheets selected in the Project window", "Crea una mascota con los sprite sheets seleccionados en la ventana Project"));
            SetButton("btn-spawn-ball", T("Spawn Ball", "Crear Pelota"), T("Drop a ball to play with (drag it to throw)", "Suelta una pelota para jugar (arrástrala para lanzar)"));
            SetButton("btn-feed-all", T("Feed All", "Alimentar"), T("Every visible pet eats for a few seconds", "Todas las mascotas visibles comen unos segundos"));
            var interactable = _root.Q<Toggle>("toggle-interactable");
            interactable.label = T("Interactable", "Interactuable");
            interactable.tooltip = T("Click, drag and pet the pets in the Scene View. Turn off if they get in the way.", "Clic, arrastrar y acariciar en la Scene View. Desactívalo si estorban.");

            SetButton("tab-pets", T("Pets", "Mascotas"), null);
            SetButton("tab-settings", T("Settings", "Ajustes"), null);
            _root.Q<ToolbarSearchField>("search").tooltip = T("Search by name", "Buscar por nombre");
            SetButton("filter-all", T("All", "Todas"), null);
            SetButton("filter-visible", T("Visible", "Visibles"), T("Pets walking in the Scene View", "Mascotas paseando en la Scene View"));
            SetButton("filter-hidden", T("Hidden", "Ocultas"), null);
            SetButton("btn-show-all", T("Show all", "Mostrar todas"), T("Ctrl+Z to undo", "Ctrl+Z para deshacer"));
            SetButton("btn-hide-all", T("Hide all", "Ocultar todas"), T("Ctrl+Z to undo", "Ctrl+Z para deshacer"));

            _root.Q<Toggle>("toggle-detail-visible").label = T("Visible", "Visible");
            SetButton("btn-solo", T("Solo", "Solo"), T("Show only this pet", "Mostrar solo esta mascota"));
            SetButton("btn-randomize", T("Move", "Mover"), T("Move it to a random spot in the Scene View", "Moverla a un sitio aleatorio de la Scene View"));
            SetButton("btn-duplicate", T("Duplicate", "Duplicar"), T("Copy this pet to make a variant", "Copiar esta mascota para crear una variante"));
            SetButton("btn-select", T("Asset", "Asset"), T("Select the asset in the Project window", "Seleccionar el asset en la ventana Project"));

            _root.Q<Label>("lbl-scene-section").text = T("Scene View", "Scene View");
            _root.Q<Toggle>("toggle-show-names").label = T("Show Names", "Mostrar Nombres");
            _root.Q<Slider>("slider-opacity").label = T("Opacity", "Opacidad");
            _root.Q<Label>("lbl-items-section").text = T("Ball, Food & Heart", "Pelota, Comida y Corazón");

            _pets = new List<PetData>(); // rebuild tiles (context menus, empty hint) in the new language
            RefreshPets();
        }

        private void SetButton(string name, string text, string tooltip)
        {
            var btn = _root.Q<Button>(name);
            btn.text = text;
            btn.tooltip = tooltip;
        }

        private void ShowHelpDialog()
        {
            EditorUtility.DisplayDialog(T("Make your own pet", "Crea tu propia mascota"), T(
                "1. Draw a sprite sheet: frames side by side in ONE row, all frames square (e.g. 4 frames of 32x32 = 128x32 PNG).\n" +
                "2. Only Idle is required. Optional sheets: Walk, Sleep, Eat, Happy. Put those words in the file names (e.g. Cat_Walk.png) to auto-assign them.\n" +
                "3. Select the PNG(s) in the Project window and click + New Pet (or right-click > Create > EditorPets > New Pet).\n" +
                "4. Done! Frame counts are detected automatically. Tweak speed and size in the panel below the grid.\n\n" +
                "Many pets? Search and filter the grid, use the eye on each tile, Solo to show just one, or right-click a tile for more.\n" +
                "In the Scene View: click a pet to pet it, drag it to move it, drop it on this window to hide it.",
                "1. Dibuja un sprite sheet: frames uno al lado del otro en UNA fila, todos cuadrados (ej. 4 frames de 32x32 = PNG de 128x32).\n" +
                "2. Solo Idle es obligatorio. Opcionales: Walk, Sleep, Eat, Happy. Pon esas palabras en el nombre (ej. Gato_Walk.png) para asignarlos solos.\n" +
                "3. Selecciona los PNG en la ventana Project y pulsa + Nueva Mascota (o clic derecho > Create > EditorPets > New Pet).\n" +
                "4. ¡Listo! Los frames se detectan solos. Ajusta velocidad y tamaño en el panel bajo la cuadrícula.\n\n" +
                "¿Muchas mascotas? Busca y filtra la cuadrícula, usa el ojo de cada tarjeta, Solo para ver una sola, o clic derecho para más.\n" +
                "En la Scene View: clic para acariciar, arrastra para mover, suéltala en esta ventana para ocultarla."),
                T("Got it", "Entendido"));
        }
    }
}
#endif
