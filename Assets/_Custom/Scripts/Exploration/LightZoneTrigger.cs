using UnityEngine;
using UnityEngine.Events;

namespace VRTutorial.Exploration
{
    /// <summary>
    /// Recompensa la navegación del jugador en la Sección 2. 
    /// Se dispara cuando el jugador sale de las penumbras hacia la luz.
    /// </summary>
    public class LightZoneTrigger : MonoBehaviour
    {
        [Header("Configuración del Trigger")]
        [Tooltip("El Tag que debe tener el jugador (por defecto 'Player').")]
        public string TargetTag = "Player";
        
        [Tooltip("Audio triunfal / místico que se reproducirá al descubrir la zona.")]
        public AudioSource DiscoveryAudio;

        [Header("Eventos")]
        [Tooltip("Se lanza una vez cuando el jugador entra en esta zona iluminada.")]
        public UnityEvent OnZoneDiscovered;

        private bool _hasTriggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered) return;

            // Verificamos si el que entró fue el jugador. 
            // En AutoHand, el body collider suele tener el script AutoHandPlayer en sus padres.
            if (other.CompareTag(TargetTag) || other.GetComponentInParent<Autohand.AutoHandPlayer>() != null)
            {
                _hasTriggered = true;
                
                if (DiscoveryAudio != null)
                {
                    DiscoveryAudio.Play();
                }

                Debug.Log($"[{gameObject.name}] ¡El jugador ha cruzado la zona de penumbra con éxito!");
                OnZoneDiscovered?.Invoke();
            }
        }
    }
}
