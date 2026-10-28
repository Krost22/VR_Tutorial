using System.Collections;
using Autohand;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Final de la experiencia. Al resolver la sala de runas la puerta principal se hunde y aparece un portal
    /// dentro del marco (<see cref="Open"/>, conectado a PuzzleRoomManager.OnMainDoorOpened).
    /// Al cruzarlo: fundido a negro → el jugador aparece en una sala oscura con "¡Gracias por jugar!" →
    /// cuenta atrás → fundido → se vuelve a cargar la primera escena del build (el juego se reinicia).
    /// Nunca se mueve la cámara sin fundido (confort en VR).
    /// Para quitarlo: borrar este componente; la puerta se abre como antes y detrás no pasa nada.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EndPortal : MonoBehaviour
    {
        [Header("Aparición del portal")]
        [Tooltip("Visual del portal (brillo, partículas). Se enciende al abrir.")]
        public GameObject portalVisual;
        [Tooltip("Segundos tras abrirse la puerta antes de que aparezca el portal (deja terminar la animación de la puerta).")]
        public float openDelay = 1.6f;
        public FeelBurst appearFeedback;
        public AudioSource hum;

        [Header("Al cruzar")]
        public FeelBurst enterFeedback;
        public AutoHandPlayer player;
        public SceneFader fader;
        public float fadeOut = 1f;
        public float fadeIn = 1.2f;

        [Header("Sala final")]
        [Tooltip("Dónde aparece el jugador (pies), mirando hacia su eje Z.")]
        public Transform endPoint;
        [Tooltip("Textos y efectos de la sala final (apagados hasta llegar).")]
        public GameObject thanksRoot;
        public Transform thanksTitle;
        public TMP_Text countdownText;
        public FeelBurst arriveFeedback;
        [Tooltip("Segundos en la sala final antes de reiniciar.")]
        public float secondsBeforeRestart = 8f;

        bool _open, _used;
        Collider _trigger;

        void Awake()
        {
            _trigger = GetComponent<Collider>();
            _trigger.isTrigger = true;
            _trigger.enabled = false;
            if (portalVisual != null) portalVisual.SetActive(false);
            if (thanksRoot != null) thanksRoot.SetActive(false);
            if (player == null) player = FindFirstObjectByType<AutoHandPlayer>();
        }

        public void Open()
        {
            if (_open) return;
            _open = true;
            StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            yield return new WaitForSeconds(openDelay);
            if (portalVisual != null)
            {
                portalVisual.SetActive(true);
                FeelTween.PopIn(portalVisual.transform, 0.8f);
            }
            if (appearFeedback != null) appearFeedback.PlayAt(transform.position);
            if (hum != null) hum.Play();
            _trigger.enabled = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!_open || _used) return;
            bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<AutoHandPlayer>() != null;
            if (!isPlayer) return;
            _used = true;
            StartCoroutine(EndRoutine());
        }

        IEnumerator EndRoutine()
        {
            if (enterFeedback != null) enterFeedback.PlayAt(transform.position);
            FeelHaptics.Pulse(0.5f, 0.2f);
            var f = fader != null ? fader : SceneFader.Instance;
            if (f != null) yield return f.FadeTo(1f, fadeOut);
            if (hum != null) hum.Stop();

            // Sala final: el jugador aparece frente al mensaje.
            if (thanksRoot != null) thanksRoot.SetActive(true);
            if (player != null && endPoint != null)
                player.SetPosition(endPoint.position, Quaternion.Euler(0f, endPoint.eulerAngles.y, 0f));
            yield return null;
            yield return null;
            if (f != null) yield return f.FadeTo(0f, fadeIn);
            if (thanksTitle != null) FeelTween.PopIn(thanksTitle, 0.6f);
            if (arriveFeedback != null && thanksTitle != null) arriveFeedback.PlayAt(thanksTitle.position);
            FeelHaptics.SuccessPattern(this, 0.4f);

            // Cuenta atrás visible: el jugador sabe que el juego se reinicia solo.
            float left = secondsBeforeRestart;
            while (left > 0f)
            {
                if (countdownText != null) countdownText.text = "El juego se reiniciará en " + Mathf.CeilToInt(left);
                left -= Time.deltaTime;
                yield return null;
            }

            // Reinicio: primera escena del build.
            string first = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(0));
            if (f != null) f.FadeAndLoad(first);
            else SceneManager.LoadScene(0);
        }
    }
}
