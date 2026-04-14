using UnityEngine;
using UnityEngine.Events;
using Autohand;

namespace VRTutorial.RunePuzzle
{
    /// <summary>
    /// Se adjunta al Pilar Rotatorio cilíndrico (Hinge Joint).
    /// </summary>
    public class PillarProjector : MonoBehaviour
    {
        [Header("Conexión con el Altar")]
        [Tooltip("El PlacePoint del Altar que le corresponde a este pilar.")]
        public PlacePoint AltarSocket;

        [Header("Interacción y Feedback del Pilar")]
        [Tooltip("El componente Grabbable del pilar para bloquear/desbloquear la rotación.")]
        public Grabbable PillarGrabbable;
        [Tooltip("Sistema de partículas a emitir cuando el pilar se active.")]
        public ParticleSystem ActivationParticles;
        [Tooltip("Fuente de audio a reproducir cuando el pilar se active.")]
        public AudioSource ActivationAudio;

        [Header("Visualización del Proyector")]
        [Tooltip("Desde dónde sale el rayo de proyección (ej. un cubo vacío al frente del pilar).")]
        public Transform RaycastOrigin;
        
        [Tooltip("Componente LineRenderer para hacer visible el láser mágico al jugador.")]
        public LineRenderer LaserRenderer;

        [Tooltip("Hacia qué capa estamos disparando el raycast de los murales.")]
        public LayerMask MuralLayer;
        
        [Tooltip("La distancia máxima a la que llega el proyector.")]
        public float ProjectionDistance = 15f;

        [Header("Objetivo")]
        [Tooltip("Qué runa MÍNIMA o específica requiere este pilar. Si es 'Ninguno', funcionará con la del mural.")]
        public RunicSymbol RequiredSymbolInSocket;
        
        [Tooltip("El Tag de Unity que debe tener el Mural correcto para este pilar.")]
        public string TargetMuralTag = "MuralCorrecto";

        [Header("Eventos")]
        public UnityEvent OnProjectorMatched;
        public UnityEvent OnProjectorDisconnected;

        private bool _isActivatedByAltar = false;
        private bool _isMatched = false;
        private RuneData _currentSocketedRune;

        private void Start()
        {
            // Asegurarnos de que el pilar inicie bloqueado y sin láser
            if (PillarGrabbable != null)
            {
                PillarGrabbable.enabled = false;
            }
            if (LaserRenderer != null)
            {
                LaserRenderer.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (AltarSocket != null)
            {
                AltarSocket.OnPlace.AddListener(HandleRunePlaced);
                AltarSocket.OnRemove.AddListener(HandleRuneRemoved);
            }
        }

        private void OnDisable()
        {
            if (AltarSocket != null)
            {
                AltarSocket.OnPlace.RemoveListener(HandleRunePlaced);
                AltarSocket.OnRemove.RemoveListener(HandleRuneRemoved);
            }
        }

        private void FixedUpdate()
        {
            CheckProjectionAlignment();
        }

        private void HandleRunePlaced(PlacePoint point, Grabbable grab)
        {
            _currentSocketedRune = grab.GetComponent<RuneData>();

            if (RequiredSymbolInSocket != RunicSymbol.Ninguno)
            {
                if (_currentSocketedRune == null || _currentSocketedRune.CurrentSymbol != RequiredSymbolInSocket)
                {
                    Debug.Log($"[{gameObject.name}] Runa incorrecta colocada. No se activa.");
                    return;
                }
            }

            ActivatePillar();
        }

        private void HandleRuneRemoved(PlacePoint point, Grabbable grab)
        {
            _currentSocketedRune = null;
            DeactivatePillar();
        }

        private void ActivatePillar()
        {
            if (_isActivatedByAltar) return;
            _isActivatedByAltar = true;

            Debug.Log($"[{gameObject.name}] Pilar activado por el altar. Desbloqueando rotación.");

            // Desbloquear agarre
            if (PillarGrabbable != null)
            {
                PillarGrabbable.enabled = true;
            }

            // Reproducir feedback
            if (ActivationParticles != null) ActivationParticles.Play();
            if (ActivationAudio != null) ActivationAudio.Play();
            
            // Encender Láser
            if (LaserRenderer != null) LaserRenderer.enabled = true;
        }

        private void DeactivatePillar()
        {
            if (!_isActivatedByAltar) return;
            _isActivatedByAltar = false;

            Debug.Log($"[{gameObject.name}] Runa retirada del altar. Bloqueando pilar.");

            // Bloquear agarre
            if (PillarGrabbable != null)
            {
                PillarGrabbable.ForceHandsRelease();
                PillarGrabbable.enabled = false;
            }

            // Apagar Láser
            if (LaserRenderer != null) LaserRenderer.enabled = false;

            SetMatchedState(false);
        }

        /// <summary>
        /// Ejecuta un raycast físico hacia adelante para probar la alineación.
        /// </summary>
        private void CheckProjectionAlignment()
        {
            // Solo proyecta si el pilar ha sido activado (runa correcta en el altar)
            if (!_isActivatedByAltar)
            {
                if (LaserRenderer != null) LaserRenderer.enabled = false;
                SetMatchedState(false);
                return;
            }

            Ray ray = new Ray(RaycastOrigin.position, RaycastOrigin.forward);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * ProjectionDistance, Color.cyan);

            if (Physics.Raycast(ray, out hit, ProjectionDistance, MuralLayer))
            {
                // Actualizar posiciones del LineRenderer hasta el punto de impacto
                if (LaserRenderer != null)
                {
                    LaserRenderer.SetPosition(0, ray.origin);
                    LaserRenderer.SetPosition(1, hit.point);
                }

                if (hit.collider.CompareTag(TargetMuralTag))
                {
                    SetMatchedState(true);
                }
                else
                {
                    SetMatchedState(false);
                }
            }
            else
            {
                // Actualizar posiciones del LineRenderer apuntando al infinito (límite)
                if (LaserRenderer != null)
                {
                    LaserRenderer.SetPosition(0, ray.origin);
                    LaserRenderer.SetPosition(1, ray.origin + ray.direction * ProjectionDistance);
                }

                SetMatchedState(false);
            }
        }

        private void SetMatchedState(bool matched)
        {
            if (_isMatched == matched) return;

            _isMatched = matched;

            if (_isMatched)
            {
                Debug.Log($"[{gameObject.name}] ¡Proyector Alineado con el Mural!");
                OnProjectorMatched?.Invoke();
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Desconectado.");
                OnProjectorDisconnected?.Invoke();
            }
        }

        public bool IsMatched()
        {
            return _isMatched;
        }
    }
}
