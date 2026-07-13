#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace EditorPets
{
    public class EditorPetsWindow : EditorWindow
    {
        private const string LANG_KEY = "EditorPets_IsEnglish";

        private bool _isEnglish = true;
        private List<PetData> _allPets = new List<PetData>();

        // UI References
        private VisualElement _root;
        private VisualElement _petsList;
        private Button _btnLangEn;
        private Button _btnLangEs;
        private Label _lblTitle;
        private Label _lblGlobalSettings;
        private Label _lblPets;
        private Label _lblFooter;
        private Button _btnHelp;

        private Button _btnInteractable;
        private Button _btnSpawnBall;
        private Button _btnFeedAll;
        private Button _btnReloadAll;
        private Button _btnHideAll;
        private Button _btnShowAll;

        private Toggle _toggleShowNames;
        private Slider _sliderOpacity;
        private Foldout _foldoutGlobalItems;
        private ObjectField _fieldFood;
        private ObjectField _fieldHeart;
        private ObjectField _fieldBall;
        private FloatField _fieldBallRadius;
        private FloatField _fieldBallGravity;
        private VisualElement _ballPreview;

        [MenuItem("Tools/Editor Pets Settings")]
        public static void ShowWindow()
        {
            EditorPetsWindow wnd = GetWindow<EditorPetsWindow>("Editor Pets");
            wnd.minSize = new Vector2(480, 400);
        }

        private void OnEnable()
        {
            _isEnglish = EditorPrefs.GetBool(LANG_KEY, true);
            RefreshPets();
        }

        public void CreateGUI()
        {
            _root = rootVisualElement;
            _root.Clear();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/KrostGames/EditorPets/Editor/EditorPetsWindow.uxml");
            if (visualTree == null)
            {
                Debug.LogError("EditorPets: No se encontró EditorPetsWindow.uxml");
                return;
            }
            visualTree.CloneTree(_root);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/KrostGames/EditorPets/Editor/EditorPetsWindow.uss");
            if (styleSheet != null)
                _root.styleSheets.Add(styleSheet);

            QueryElements();
            BindEvents();
            RefreshLocalization();
            RefreshToolbarState();
            RefreshGlobalSettings();
            RefreshPets();
        }

        private void QueryElements()
        {
            _btnLangEn = _root.Q<Button>("btn-lang-en");
            _btnLangEs = _root.Q<Button>("btn-lang-es");
            _lblTitle = _root.Q<Label>("lbl-title");
            _lblGlobalSettings = _root.Q<Label>("lbl-global-settings");
            _lblPets = _root.Q<Label>("lbl-pets");
            _lblFooter = _root.Q<Label>("lbl-footer");
            _btnHelp = _root.Q<Button>("btn-help");

            _btnInteractable = _root.Q<Button>("btn-interactable");
            _btnSpawnBall = _root.Q<Button>("btn-spawn-ball");
            _btnFeedAll = _root.Q<Button>("btn-feed-all");
            _btnReloadAll = _root.Q<Button>("btn-reload-all");
            _btnHideAll = _root.Q<Button>("btn-hide-all");
            _btnShowAll = _root.Q<Button>("btn-show-all");

            _toggleShowNames = _root.Q<Toggle>("toggle-show-names");
            _sliderOpacity = _root.Q<Slider>("slider-opacity");
            _foldoutGlobalItems = _root.Q<Foldout>("foldout-global-items");
            _fieldFood = _root.Q<ObjectField>("field-food");
            _fieldHeart = _root.Q<ObjectField>("field-heart");
            _fieldBall = _root.Q<ObjectField>("field-ball");
            _fieldBallRadius = _root.Q<FloatField>("field-ball-radius");
            _fieldBallGravity = _root.Q<FloatField>("field-ball-gravity");
            _ballPreview = _root.Q<VisualElement>("ball-preview");
            _petsList = _root.Q<VisualElement>("pets-list");

            if (_fieldFood != null) _fieldFood.objectType = typeof(Texture2D);
            if (_fieldHeart != null) _fieldHeart.objectType = typeof(Texture2D);
            if (_fieldBall != null) _fieldBall.objectType = typeof(Texture2D);
        }

        private void BindEvents()
        {
            _btnLangEn.clicked += () => SetLanguage(true);
            _btnLangEs.clicked += () => SetLanguage(false);
            _btnHelp.clicked += ShowHelpDialog;

            _btnInteractable.clicked += () =>
            {
                ScenePetOverlay.interactable = !ScenePetOverlay.interactable;
                ScenePetOverlay.SaveSettings();
                RefreshToolbarState();
            };
            _btnSpawnBall.clicked += ScenePetOverlay.SpawnBall;
            _btnFeedAll.clicked += ScenePetOverlay.FeedAll;
            _btnReloadAll.clicked += () =>
            {
                RefreshPets();
                ScenePetOverlay.InitializePets();
            };
            _btnHideAll.clicked += () =>
            {
                ScenePetOverlay.HideAllPets();
                RefreshPets();
            };
            _btnShowAll.clicked += () =>
            {
                ScenePetOverlay.ShowAllPets();
                RefreshPets();
            };

            _toggleShowNames.RegisterValueChangedCallback(evt =>
            {
                ScenePetOverlay.showNames = evt.newValue;
                ScenePetOverlay.SaveSettings();
            });

            _sliderOpacity.RegisterValueChangedCallback(evt =>
            {
                ScenePetOverlay.globalOpacity = evt.newValue;
                ScenePetOverlay.SaveSettings();
            });

            _fieldFood.RegisterValueChangedCallback(evt => UpdateGlobalTexture(0, evt.newValue as Texture2D));
            _fieldHeart.RegisterValueChangedCallback(evt => UpdateGlobalTexture(1, evt.newValue as Texture2D));
            _fieldBall.RegisterValueChangedCallback(evt => UpdateGlobalTexture(2, evt.newValue as Texture2D));
            _fieldBallRadius.RegisterValueChangedCallback(evt => UpdateGlobalSettings());
            _fieldBallGravity.RegisterValueChangedCallback(evt => UpdateGlobalSettings());

            _root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            _root.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is PetData)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is PetData draggedPet)
            {
                DragAndDrop.AcceptDrag();
                Undo.RecordObject(draggedPet, "Move Pet to House");
                draggedPet.location = PetLocation.House;
                EditorUtility.SetDirty(draggedPet);
                AssetDatabase.SaveAssets();
                ScenePetOverlay.UpdatePetInstance(draggedPet);
                RefreshPets();
            }
        }

        private void SetLanguage(bool english)
        {
            _isEnglish = english;
            EditorPrefs.SetBool(LANG_KEY, english);
            if (english)
            {
                _btnLangEn.AddToClassList("active");
                _btnLangEs.RemoveFromClassList("active");
            }
            else
            {
                _btnLangEs.AddToClassList("active");
                _btnLangEn.RemoveFromClassList("active");
            }
            RefreshLocalization();
            RefreshPets();
        }

        private void RefreshLocalization()
        {
            _lblTitle.text = _isEnglish ? "Editor Pets Settings" : "Ajustes de Editor Pets";
            _lblGlobalSettings.text = _isEnglish ? "Global Scene View Settings" : "Ajustes Globales de Scene View";
            _lblPets.text = _isEnglish ? "Pets" : "Mascotas";
            _lblFooter.text = _isEnglish ? "Everything you need in one window" : "Todo lo que necesitas en una ventana";
            _btnHelp.tooltip = _isEnglish ? "View Shortcuts" : "Ver Atajos";

            _btnInteractable.text = _isEnglish ? "Interactable" : "Interactuable";
            _btnSpawnBall.text = _isEnglish ? "Spawn Ball" : "Crear Pelota";
            _btnFeedAll.text = _isEnglish ? "Feed All" : "Alimentar Todas";
            _btnReloadAll.text = _isEnglish ? "Reload All" : "Recargar Todas";
            _btnHideAll.text = _isEnglish ? "Hide All" : "Ocultar Todas";
            _btnShowAll.text = _isEnglish ? "Show All" : "Mostrar Todas";

            _toggleShowNames.label = _isEnglish ? "Show Names" : "Mostrar Nombres";
            _sliderOpacity.label = _isEnglish ? "Opacity" : "Opacidad";
            _foldoutGlobalItems.text = _isEnglish ? "Global Item Settings (Food, Heart, Ball)" : "Ajustes de Objetos Globales (Comida, Corazón, Pelota)";

            if (ScenePetOverlay.settings != null)
            {
                _fieldFood.label = _isEnglish ? "Food Texture" : "Textura Comida";
                _fieldHeart.label = _isEnglish ? "Heart Texture" : "Textura Corazón";
                _fieldBall.label = _isEnglish ? "Ball Texture" : "Textura Pelota";
                _fieldBallRadius.label = _isEnglish ? "Ball Radius" : "Radio Pelota";
                _fieldBallGravity.label = _isEnglish ? "Ball Gravity" : "Gravedad Pelota";
            }
        }

        private void RefreshToolbarState()
        {
            if (ScenePetOverlay.interactable)
                _btnInteractable.AddToClassList("active");
            else
                _btnInteractable.RemoveFromClassList("active");
        }

        private void RefreshGlobalSettings()
        {
            _toggleShowNames.SetValueWithoutNotify(ScenePetOverlay.showNames);
            _sliderOpacity.SetValueWithoutNotify(ScenePetOverlay.globalOpacity);

            if (ScenePetOverlay.settings != null)
            {
                _fieldFood.SetValueWithoutNotify(ScenePetOverlay.settings.foodTexture);
                _fieldHeart.SetValueWithoutNotify(ScenePetOverlay.settings.heartTexture);
                _fieldBall.SetValueWithoutNotify(ScenePetOverlay.settings.ballTexture);
                _fieldBallRadius.SetValueWithoutNotify(ScenePetOverlay.settings.ballRadius);
                _fieldBallGravity.SetValueWithoutNotify(ScenePetOverlay.settings.gravity);
                UpdateBallPreview();
            }
        }

        private void UpdateGlobalTexture(int index, Texture2D value)
        {
            if (ScenePetOverlay.settings == null) return;
            switch (index)
            {
                case 0: ScenePetOverlay.settings.foodTexture = value; break;
                case 1: ScenePetOverlay.settings.heartTexture = value; break;
                case 2: ScenePetOverlay.settings.ballTexture = value; break;
            }
            MarkGlobalSettingsDirty();
            UpdateBallPreview();
        }

        private void UpdateGlobalSettings()
        {
            if (ScenePetOverlay.settings == null) return;
            ScenePetOverlay.settings.ballRadius = _fieldBallRadius.value;
            ScenePetOverlay.settings.gravity = _fieldBallGravity.value;
            MarkGlobalSettingsDirty();
        }

        private void MarkGlobalSettingsDirty()
        {
            if (ScenePetOverlay.settings == null) return;
            EditorUtility.SetDirty(ScenePetOverlay.settings);
            AssetDatabase.SaveAssets();
        }

        private void UpdateBallPreview()
        {
            if (_ballPreview == null) return;
            Texture2D tex = ScenePetOverlay.settings != null ? ScenePetOverlay.settings.ballTexture : null;
            _ballPreview.style.backgroundImage = tex != null ? new StyleBackground(tex) : StyleKeyword.None;
            _ballPreview.style.unityBackgroundImageTintColor = tex != null ? Color.white : new Color(0.2f, 0.2f, 0.2f);
        }

        private void RefreshPets()
        {
            _allPets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:PetData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PetData data = AssetDatabase.LoadAssetAtPath<PetData>(path);
                if (data != null)
                    _allPets.Add(data);
            }

            if (_petsList == null) return;
            _petsList.Clear();
            foreach (var pet in _allPets)
            {
                var card = new PetCardElement();
                card.Bind(pet, _isEnglish, RefreshPets);
                _petsList.Add(card);
            }
        }

        private void ShowHelpDialog()
        {
            string title = _isEnglish ? "Editor Pets - Shortcuts & Tips" : "Editor Pets - Atajos y Consejos";
            string content = _isEnglish ?
                "• Drag a Pet asset here to move it to House.\n" +
                "• Interactable: click pets in Scene View.\n" +
                "• Use Advanced Customization to tweak movement, size and animations." :
                "• Arrastra un asset Pet aquí para enviarlo a Casa.\n" +
                "• Interactuable: haz clic en las mascotas en Scene View.\n" +
                "• Usa Personalización Avanzada para ajustar movimiento, tamaño y animaciones.";
            string ok = _isEnglish ? "Got it" : "Entendido";
            EditorUtility.DisplayDialog(title, content, ok);
        }
    }
}
#endif
