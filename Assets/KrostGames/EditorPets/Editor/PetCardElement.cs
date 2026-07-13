#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;

namespace EditorPets
{
    public class PetCardElement : VisualElement
    {
        private PetData _pet;
        private bool _isEnglish;
        private System.Action _onPetChanged;

        private VisualElement _thumbnail;
        private TextField _nameField;
        private Toggle _activeToggle;
        private Button _btnLocate;
        private Button _btnPing;
        private Button _btnRandomize;
        private Button _btnDuplicate;
        private Foldout _foldoutAdvanced;
        private Slider _sliderMoveSpeed;
        private Slider _sliderAnimSpeed;
        private Vector2Field _fieldSize;

        private ObjectField _texIdle;
        private ObjectField _texWalk;
        private ObjectField _texSleep;
        private ObjectField _texEat;
        private IntegerField _framesIdle;
        private IntegerField _framesWalk;
        private IntegerField _framesSleep;
        private IntegerField _framesEat;

        private Label _lblAnimIdle;
        private Label _lblAnimWalk;
        private Label _lblAnimSleep;
        private Label _lblAnimEat;
        private List<Label> _lblFramesList = new List<Label>();

        public PetCardElement()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/KrostGames/EditorPets/Editor/PetCardElement.uxml");
            if (tree != null)
                tree.CloneTree(this);
            else
                Debug.LogError("EditorPets: No se encontró PetCardElement.uxml");

            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/KrostGames/EditorPets/Editor/PetCardElement.uss");
            if (style != null)
                styleSheets.Add(style);

            QueryElements();
            RegisterCallbacks();
        }

        private void QueryElements()
        {
            _thumbnail = this.Q<VisualElement>("pet-thumbnail");
            _nameField = this.Q<TextField>("field-pet-name");
            _activeToggle = this.Q<Toggle>("toggle-active");
            _btnLocate = this.Q<Button>("btn-locate");
            _btnPing = this.Q<Button>("btn-ping");
            _btnRandomize = this.Q<Button>("btn-randomize");
            _btnDuplicate = this.Q<Button>("btn-duplicate");
            _foldoutAdvanced = this.Q<Foldout>("foldout-advanced");
            _sliderMoveSpeed = this.Q<Slider>("slider-move-speed");
            _sliderAnimSpeed = this.Q<Slider>("slider-anim-speed");
            _fieldSize = this.Q<Vector2Field>("field-size");

            _texIdle = this.Q<ObjectField>("field-anim-idle");
            _texWalk = this.Q<ObjectField>("field-anim-walk");
            _texSleep = this.Q<ObjectField>("field-anim-sleep");
            _texEat = this.Q<ObjectField>("field-anim-eat");
            _framesIdle = this.Q<IntegerField>("field-frames-idle");
            _framesWalk = this.Q<IntegerField>("field-frames-walk");
            _framesSleep = this.Q<IntegerField>("field-frames-sleep");
            _framesEat = this.Q<IntegerField>("field-frames-eat");

            _lblAnimIdle = this.Q<Label>("lbl-anim-idle");
            _lblAnimWalk = this.Q<Label>("lbl-anim-walk");
            _lblAnimSleep = this.Q<Label>("lbl-anim-sleep");
            _lblAnimEat = this.Q<Label>("lbl-anim-eat");

            _lblFramesList = this.Query<Label>(className: "anim-frames-label").ToList();

            if (_texIdle != null) _texIdle.objectType = typeof(Texture2D);
            if (_texWalk != null) _texWalk.objectType = typeof(Texture2D);
            if (_texSleep != null) _texSleep.objectType = typeof(Texture2D);
            if (_texEat != null) _texEat.objectType = typeof(Texture2D);
        }

        public void Bind(PetData pet, bool isEnglish, System.Action onPetChanged)
        {
            _pet = pet;
            _isEnglish = isEnglish;
            _onPetChanged = onPetChanged;

            RefreshValues();
            UpdateLocalization(isEnglish);
        }

        public void UpdateLocalization(bool isEnglish)
        {
            _isEnglish = isEnglish;
            if (_pet == null) return;

            _activeToggle.label = isEnglish ? "Show in Scene" : "Mostrar en Escena";
            _foldoutAdvanced.text = isEnglish ? "Advanced Customization" : "Personalización Avanzada";
            _sliderMoveSpeed.label = isEnglish ? "Move Speed" : "Velocidad Movimiento";
            _sliderAnimSpeed.label = isEnglish ? "Anim Speed" : "Velocidad Animación";
            _fieldSize.label = isEnglish ? "Draw Size" : "Tamaño Dibujo";

            _lblAnimIdle.text = isEnglish ? "IDLE" : "IDLE";
            _lblAnimWalk.text = isEnglish ? "WALK" : "CAMINAR";
            _lblAnimSleep.text = isEnglish ? "SLEEP" : "DORMIR";
            _lblAnimEat.text = isEnglish ? "EAT" : "COMER";

            _btnPing.text = isEnglish ? "Ping" : "Ping";
            _btnRandomize.text = isEnglish ? "Randomize" : "Aleatorio";
            _btnDuplicate.text = isEnglish ? "Duplicate" : "Duplicar";

            string framesLabel = isEnglish ? "frames" : "cuadros";
            foreach (var lbl in _lblFramesList)
                lbl.text = framesLabel;

            UpdateLocateButton();
        }

        private void RefreshValues()
        {
            if (_pet == null) return;

            _nameField.SetValueWithoutNotify(_pet.petName);
            _activeToggle.SetValueWithoutNotify(_pet.isActive);
            _sliderMoveSpeed.SetValueWithoutNotify(_pet.moveSpeed);
            _sliderAnimSpeed.SetValueWithoutNotify(_pet.animationSpeed);
            _fieldSize.SetValueWithoutNotify(_pet.size);

            _texIdle.SetValueWithoutNotify(_pet.idleTexture);
            _texWalk.SetValueWithoutNotify(_pet.walkTexture);
            _texSleep.SetValueWithoutNotify(_pet.sleepTexture);
            _texEat.SetValueWithoutNotify(_pet.eatTexture);
            _framesIdle.SetValueWithoutNotify(_pet.framesIdle);
            _framesWalk.SetValueWithoutNotify(_pet.framesWalk);
            _framesSleep.SetValueWithoutNotify(_pet.framesSleep);
            _framesEat.SetValueWithoutNotify(_pet.framesEat);

            UpdateThumbnail();
            UpdateLocateButton();
        }

        private void UpdateThumbnail()
        {
            if (_thumbnail == null) return;
            Texture2D tex = _pet != null ? _pet.idleTexture : null;
            _thumbnail.style.backgroundImage = tex != null ? new StyleBackground(tex) : StyleKeyword.None;
            _thumbnail.style.unityBackgroundImageTintColor = tex != null ? Color.white : new Color(0.27f, 0.27f, 0.27f);
        }

        private void UpdateLocateButton()
        {
            if (_btnLocate == null || _pet == null) return;
            bool inScene = _pet.location == PetLocation.Scene;
            _btnLocate.text = inScene
                ? (_isEnglish ? "Send to House" : "Enviar a Casa")
                : (_isEnglish ? "Bring to Scene" : "Traer a Escena");
        }

        private void RegisterCallbacks()
        {
            _nameField.RegisterValueChangedCallback(evt =>
            {
                if (_pet == null) return;
                Undo.RecordObject(_pet, "Rename Pet");
                _pet.petName = evt.newValue;
                MarkDirtyAndSave();
            });

            _activeToggle.RegisterValueChangedCallback(evt =>
            {
                if (_pet == null) return;
                Undo.RecordObject(_pet, "Toggle Pet Visibility");
                _pet.isActive = evt.newValue;
                MarkDirtyAndSave();
            });

            _sliderMoveSpeed.RegisterValueChangedCallback(evt =>
            {
                if (_pet == null) return;
                Undo.RecordObject(_pet, "Customize Pet Movement");
                _pet.moveSpeed = evt.newValue;
                MarkDirtyAndSave();
            });

            _sliderAnimSpeed.RegisterValueChangedCallback(evt =>
            {
                if (_pet == null) return;
                Undo.RecordObject(_pet, "Customize Pet Animation");
                _pet.animationSpeed = evt.newValue;
                MarkDirtyAndSave();
            });

            _fieldSize.RegisterValueChangedCallback(evt =>
            {
                if (_pet == null) return;
                Undo.RecordObject(_pet, "Customize Pet Size");
                _pet.size = evt.newValue;
                MarkDirtyAndSave();
            });

            _texIdle.RegisterValueChangedCallback(evt => UpdateAnim(ref _pet.idleTexture, evt.newValue as Texture2D, "IDLE", ref _pet.framesIdle, _framesIdle.value));
            _texWalk.RegisterValueChangedCallback(evt => UpdateAnim(ref _pet.walkTexture, evt.newValue as Texture2D, "WALK", ref _pet.framesWalk, _framesWalk.value));
            _texSleep.RegisterValueChangedCallback(evt => UpdateAnim(ref _pet.sleepTexture, evt.newValue as Texture2D, "SLEEP", ref _pet.framesSleep, _framesSleep.value));
            _texEat.RegisterValueChangedCallback(evt => UpdateAnim(ref _pet.eatTexture, evt.newValue as Texture2D, "EAT", ref _pet.framesEat, _framesEat.value));

            _framesIdle.RegisterValueChangedCallback(evt => UpdateAnim(ref _pet.idleTexture, _pet.idleTexture, "IDLE", ref _pet.framesIdle, evt.newValue));
            _framesWalk.RegisterValueChangedCallback(evt => UpdateAnim(ref _pet.walkTexture, _pet.walkTexture, "WALK", ref _pet.framesWalk, evt.newValue));
            _framesSleep.RegisterValueChangedCallback(evt => UpdateAnim(ref _pet.sleepTexture, _pet.sleepTexture, "SLEEP", ref _pet.framesSleep, evt.newValue));
            _framesEat.RegisterValueChangedCallback(evt => UpdateAnim(ref _pet.eatTexture, _pet.eatTexture, "EAT", ref _pet.framesEat, evt.newValue));

            _btnLocate.clicked += OnLocateClicked;
            _btnPing.clicked += OnPingClicked;
            _btnRandomize.clicked += OnRandomizeClicked;
            _btnDuplicate.clicked += OnDuplicateClicked;
        }

        private void UpdateAnim(ref Texture2D texField, Texture2D newTex, string title, ref int framesField, int newFrames)
        {
            if (_pet == null) return;
            Undo.RecordObject(_pet, "Update Animation: " + title);
            texField = newTex;
            framesField = newFrames;
            MarkDirtyAndSave();
            if (title == "IDLE") UpdateThumbnail();
        }

        private void OnLocateClicked()
        {
            if (_pet == null) return;
            Undo.RecordObject(_pet, "Change Pet Location");
            _pet.location = (_pet.location == PetLocation.Scene) ? PetLocation.House : PetLocation.Scene;
            MarkDirtyAndSave();
            UpdateLocateButton();
        }

        private void OnPingClicked()
        {
            if (_pet == null) return;
            EditorGUIUtility.PingObject(_pet);
        }

        private void OnRandomizeClicked()
        {
            if (_pet == null) return;
            ScenePetOverlay.RandomizePetPosition(_pet);
        }

        private void OnDuplicateClicked()
        {
            if (_pet == null) return;
            string sourcePath = AssetDatabase.GetAssetPath(_pet);
            if (string.IsNullOrEmpty(sourcePath)) return;
            string newPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);

            EditorApplication.delayCall += () =>
            {
                if (_pet == null) return;
                PetData copy = Object.Instantiate(_pet);
                copy.name = _pet.petName + " (Copy)";
                AssetDatabase.CreateAsset(copy, newPath);
                AssetDatabase.SaveAssets();
                _onPetChanged?.Invoke();
                EditorGUIUtility.PingObject(copy);
            };
        }

        private void MarkDirtyAndSave()
        {
            if (_pet == null) return;
            EditorUtility.SetDirty(_pet);
            AssetDatabase.SaveAssets();
            ScenePetOverlay.UpdatePetInstance(_pet);
        }
    }
}
#endif
