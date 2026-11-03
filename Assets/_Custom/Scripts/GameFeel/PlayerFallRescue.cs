using System.Collections;
using Autohand;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Rescate por caída: si el jugador se sale del mapa y cae, la pantalla se funde a negro y aparece en el
    /// último sitio donde estuvo apoyado. Sin esto caía para siempre y había que reiniciar el juego.
    /// - "Sitio seguro" = posición del jugador tras estar apoyado en el suelo un momento.
    /// - Los teletransportes del juego (círculos, atajo de salto, portal final) no cuentan como caída: el
    ///   destino pasa a ser el nuevo sitio seguro.
    /// - No gira la vista ni mueve la cámara por su cuenta: solo fundido y reaparición (confort en VR).
    /// Para quitarlo: borrar el componente (objeto GameFeel/RescateCaida).
    /// </summary>
    public class PlayerFallRescue : MonoBehaviour
    {
        public AutoHandPlayer player;
        [Tooltip("Vacío = SceneFader.Instance.")]
        public SceneFader fader;

        [Tooltip("Metros por debajo del último sitio seguro a partir de los cuales se considera que cayó fuera del mapa.")]
        public float fallDistance = 6f;
        [Tooltip("Segundos apoyado en el suelo antes de guardar la posición como sitio seguro.")]
        public float groundedTimeToSave = 0.4f;
        [Tooltip("Un salto mayor que esto en un solo frame es un teletransporte, no una caída.")]
        public float teleportJump = 3f;
        public float fadeOut = 0.25f;
        public float fadeIn = 0.45f;

        [Header("Respuesta (opcional)")]
        [Tooltip("Sonido/partículas al reaparecer.")]
        public FeelBurst rescueFeedback;

        Vector3 _safePos, _lastPos;
        Vector3 _fallRef; // altura desde la que se mide la caída (último suelo o destino de teletransporte)
        bool _hasSafe, _rescuing;
        float _groundedTime;

        public Vector3 SafePosition => _safePos;
        /// <summary>Veces que ha rescatado al jugador en esta partida (para pruebas).</summary>
        public int RescueCount { get; private set; }

        void Start()
        {
            if (player == null) player = FindAnyObjectByType<AutoHandPlayer>();
            if (player == null) { enabled = false; return; }
            player.OnTeleported += OnTeleported;
            _safePos = _fallRef = _lastPos = player.transform.position;
            _hasSafe = true;
        }

        void OnDestroy()
        {
            if (player != null) player.OnTeleported -= OnTeleported;
        }

        void OnTeleported(AutoHandPlayer p) => Teleported(p.transform.position);

        /// <summary>
        /// Tras un teletransporte la caída se mide desde el destino (si no, ir a la sala final a -60 m contaría como
        /// caída). El destino solo pasa a ser el sitio seguro si tiene suelo debajo: un teletransporte al vacío
        /// conserva el último sitio bueno (antes lo sobrescribía y el rescate devolvía al vacío una y otra vez).
        /// </summary>
        void Teleported(Vector3 pos)
        {
            _fallRef = _lastPos = pos;
            if (GroundBelow(pos, 3f)) { _safePos = pos; _hasSafe = true; }
            _groundedTime = 0f;
        }

        void Update()
        {
            if (_rescuing || player == null) return;
            Vector3 pos = player.transform.position;

            // Movido de golpe por otro script sin avisar: se trata como teletransporte.
            if ((pos - _lastPos).sqrMagnitude > teleportJump * teleportJump) Teleported(pos);
            _lastPos = pos;

            if (IsOnSolidGround(pos))
            {
                _groundedTime += Time.deltaTime;
                if (_groundedTime >= groundedTimeToSave) { _safePos = _fallRef = pos; _hasSafe = true; }
            }
            else _groundedTime = 0f;

            if (_hasSafe && pos.y < _fallRef.y - fallDistance) StartCoroutine(RescueRoutine());
        }

        readonly RaycastHit[] _hits = new RaycastHit[8];

        // AutoHandPlayer.IsGrounded() casi nunca es true en este proyecto (el cuerpo se apoya por física), así que
        // además se mira con un rayo corto: cuenta como suelo cualquier collider fijo bajo los pies, pero no el
        // propio jugador, las manos ni objetos sueltos (carro del golem, cubos): sobre ellos no es un sitio seguro.
        bool IsOnSolidGround(Vector3 pos)
        {
            if (player.IsGrounded()) return true;
            if (player.body != null && Mathf.Abs(player.body.linearVelocity.y) > 0.5f) return false;
            return GroundBelow(pos, 0.3f);
        }

        /// <summary>Hay suelo fijo a menos de <paramref name="distance"/> bajo los pies (ni el jugador ni objetos sueltos).</summary>
        bool GroundBelow(Vector3 pos, float distance)
        {
            int n = Physics.RaycastNonAlloc(pos + Vector3.up * 0.3f, Vector3.down, _hits, 0.3f + distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = _hits[i].collider;
                if (c.transform.IsChildOf(player.transform)) continue;
                var rb = c.attachedRigidbody;
                if (rb != null && !rb.isKinematic) continue;
                return true;
            }
            return false;
        }

        IEnumerator RescueRoutine()
        {
            _rescuing = true;
            RescueCount++;
            FeelHaptics.Pulse(0.3f, 0.1f);
            var f = fader != null ? fader : SceneFader.Instance;
            if (f != null) yield return f.FadeTo(1f, fadeOut);

            var target = _safePos;
            player.SetPosition(target);
            if (player.body != null && !player.body.isKinematic)
            {
                player.body.linearVelocity = Vector3.zero;
                player.body.angularVelocity = Vector3.zero;
            }
            _lastPos = target;
            _groundedTime = 0f;
            // Un par de frames a oscuras para que la física y el rig se asienten.
            yield return null;
            yield return null;

            if (f != null) yield return f.FadeTo(0f, fadeIn);
            if (rescueFeedback != null) rescueFeedback.PlayAt(target + Vector3.up * 1f);
            _rescuing = false;
        }
    }
}
