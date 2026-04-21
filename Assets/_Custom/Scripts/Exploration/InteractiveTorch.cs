using UnityEngine;
using Autohand;

namespace VRTutorial.Exploration
{
    /// <summary>
    /// Gestiona la antorcha que va en el cinturón. Inicia apagada y se enciende mágicamente al ser agarrada.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class InteractiveTorch : MonoBehaviour
    {
        [Header("Efectos de Fuego")]
        [Tooltip("La luz dinámica que iluminará el camino oscuro.")]
        public Light TorchLight;
        [Tooltip("El sistema de partículas de las llamas.")]
        public ParticleSystem TorchParticles;
        [Tooltip("El sonido del fuego crepitando.")]
        public AudioSource TorchAudio;

        private Grabbable _grabbable;
        private bool _isIgnited = false;

        private void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            
            // Asegurarnos de que inicie apagada cuando carga la escena
            TurnOffTorch();
        }

        private void OnEnable()
        {
            // Suscribirnos al evento de AutoHand cuando la mano la agarra
            _grabbable.OnGrabEvent += HandleGrabbed;
        }

        private void OnDisable()
        {
            _grabbable.OnGrabEvent -= HandleGrabbed;
        }

        private void HandleGrabbed(Hand hand, Grabbable grab)
        {
            if (!_isIgnited)
            {
                IgniteTorch();
            }
        }

        private void IgniteTorch()
        {
            _isIgnited = true;
            
            if (TorchLight != null) TorchLight.enabled = true;
            if (TorchParticles != null) TorchParticles.Play();
            if (TorchAudio != null) TorchAudio.Play();

            Debug.Log($"[{gameObject.name}] Antorcha desenfundada y encendida mágicamente.");
        }

        private void TurnOffTorch()
        {
            _isIgnited = false;
            
            if (TorchLight != null) TorchLight.enabled = false;
            
            if (TorchParticles != null)
            {
                TorchParticles.Stop();
                TorchParticles.Clear(); // Limpia llamas antiguas
            }
            
            if (TorchAudio != null) TorchAudio.Stop();
        }
    }
}
