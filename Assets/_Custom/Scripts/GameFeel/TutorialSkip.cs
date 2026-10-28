using System.Collections;
using System.Collections.Generic;
using Autohand;
using UnityEngine;
using VRTutorial.Tutorial;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRCharacteristics = UnityEngine.XR.InputDeviceCharacteristics;
using XRUsages = UnityEngine.XR.CommonUsages;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Atajo de prueba: mantener el botón A del control derecho salta todo el tutorial y deja al jugador
    /// frente al pasillo de la mazmorra, con la pared ya abierta.
    ///
    /// Mientras se mantiene, un anillo delante de la vista se llena y la vibración se acelera; soltar antes
    /// cancela. Al completarse: fundido a negro → fase de mirada terminada, pared abierta, jugador movido →
    /// fundido de vuelta. Nunca se mueve la cámara sin fundido (confort en VR).
    /// En el Editor sin visor también funciona manteniendo la tecla K.
    /// Para quitarlo: borrar este componente (el tutorial queda como antes).
    /// </summary>
    public class TutorialSkip : MonoBehaviour
    {
        [Header("Entrada")]
        [Tooltip("Segundos que hay que mantener A.")]
        public float holdSeconds = 2f;
        [Tooltip("Segundos que hay que soltar A antes de poder volver a empezar (evita saltos accidentales).")]
        public float rearmDelay = 0.3f;
        public bool onlyOnce = true;

        [Header("Qué se salta")]
        public GazeSequenceTutorial gaze;
        [Tooltip("Pared que se abre al final del tutorial (Wall_A (11)).")]
        public FeelDisappear wallToOpen;
        [Tooltip("Objetos que se apagan al saltar (barreras, letreros del tutorial...). Opcional.")]
        public List<GameObject> deactivateOnSkip = new List<GameObject>();

        [Header("Destino")]
        public AutoHandPlayer player;
        [Tooltip("Dónde queda el jugador: pies en la posición, mirando hacia su eje Z.")]
        public Transform destination;

        [Header("Feedback")]
        public SceneFader fader;
        public float fadeOut = 0.35f;
        public float fadeIn = 0.6f;
        [Tooltip("Material aditivo para el anillo de progreso (Feel_ParticleAdditive).")]
        public Material progressMaterial;
        public Color progressColor = new Color(0.35f, 0.9f, 1f, 1f);
        public FeelBurst confirmFeedback;

        /// <summary>Para pruebas automáticas sin visor: actúa como si A estuviera pulsado.</summary>
        [System.NonSerialized] public bool simulateHold;

        float _held;
        float _releasedFor = 1f;
        bool _running, _used;
        float _hapticTimer;
        LineRenderer _ring;
        Camera _cam;
        const int RingSegments = 48;

        void Update()
        {
            if (_running || (onlyOnce && _used)) { HideRing(); return; }

            bool pressed = IsPressed();
            if (!pressed)
            {
                _releasedFor += Time.unscaledDeltaTime;
                if (_held > 0f) { _held = 0f; HideRing(); }
                return;
            }
            if (_releasedFor < rearmDelay) return;

            _held += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_held / holdSeconds);
            DrawRing(k);

            // Vibración que se acelera: el jugador siente que "carga".
            _hapticTimer -= Time.unscaledDeltaTime;
            if (_hapticTimer <= 0f)
            {
                FeelHaptics.Pulse(Mathf.Lerp(0.08f, 0.4f, k), 0.02f);
                _hapticTimer = Mathf.Lerp(0.2f, 0.05f, k);
            }

            if (_held >= holdSeconds)
            {
                _held = 0f;
                _releasedFor = 0f;
                HideRing();
                StartCoroutine(SkipRoutine());
            }
        }

        static readonly List<XRInputDevice> _devices = new List<XRInputDevice>();

        bool IsPressed()
        {
            if (simulateHold) return true;
            // A = botón primario del control derecho (OpenXR, perfil Oculus Touch).
            _devices.Clear();
            XRInputDevices.GetDevicesWithCharacteristics(XRCharacteristics.Right | XRCharacteristics.Controller, _devices);
            foreach (var d in _devices)
                if (d.TryGetFeatureValue(XRUsages.primaryButton, out bool a) && a) return true;
#if ENABLE_INPUT_SYSTEM
            var right = UnityEngine.InputSystem.XR.XRController.rightHand;
            if (right != null && right.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton") is { } button && button.isPressed) return true;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (Application.isEditor && keyboard != null && keyboard.kKey.isPressed) return true;
#endif
            return false;
        }

        IEnumerator SkipRoutine()
        {
            _running = true;
            _used = true;
            FeelHaptics.Pulse(0.6f, 0.12f);
            var activeFader = fader != null ? fader : SceneFader.Instance;
            if (activeFader != null) yield return activeFader.FadeTo(1f, fadeOut);

            if (gaze != null) gaze.Skip();
            foreach (var go in deactivateOnSkip) if (go != null) go.SetActive(false);
            if (wallToOpen != null && wallToOpen.gameObject.activeInHierarchy) wallToOpen.HideInstant();

            if (player != null && destination != null)
            {
                var yaw = Quaternion.Euler(0f, destination.eulerAngles.y, 0f);
                player.SetPosition(destination.position, yaw);
            }
            // Un par de frames con la pantalla negra para que la física y el rig se asienten.
            yield return null;
            yield return null;

            if (activeFader != null) yield return activeFader.FadeTo(0f, fadeIn);
            if (confirmFeedback != null && destination != null) confirmFeedback.PlayAt(destination.position + Vector3.up * 1.2f);
            _running = false;
        }

        void DrawRing(float k)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null || progressMaterial == null) return;
            if (_ring == null)
            {
                var go = new GameObject("[SaltarTutorial_Progreso]");
                _ring = go.AddComponent<LineRenderer>();
                _ring.sharedMaterial = progressMaterial;
                _ring.useWorldSpace = false;
                _ring.loop = false;
                _ring.widthMultiplier = 0.006f;
                _ring.numCapVertices = 2;
                _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _ring.receiveShadows = false;
            }
            var t = _ring.transform;
            if (t.parent != _cam.transform)
            {
                t.SetParent(_cam.transform, false);
                t.localPosition = new Vector3(0f, -0.06f, 0.7f); // un poco bajo el centro de la vista
                t.localRotation = Quaternion.identity;
            }
            _ring.gameObject.SetActive(true);
            int count = Mathf.Max(2, Mathf.CeilToInt(RingSegments * k) + 1);
            _ring.positionCount = count;
            const float radius = 0.035f;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.PI * 0.5f - (i / (float)(count - 1)) * k * Mathf.PI * 2f;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
            var c = progressColor;
            c.a = Mathf.Lerp(0.4f, 1f, k);
            _ring.startColor = _ring.endColor = c;
        }

        void HideRing()
        {
            if (_ring != null && _ring.gameObject.activeSelf) _ring.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_ring != null) Destroy(_ring.gameObject);
        }
    }
}
