using UnityEngine;

namespace VRTutorial.Labyrinth
{
    /// <summary>
    /// Activa una pared a espaldas del jugador y desactiva la anterior 
    /// para evitar que camine hacia atrás en el laberinto.
    /// </summary>
    public class BlockerTrigger : MonoBehaviour
    {
        [Tooltip("La pared que debe APARECER detrás del jugador (fuera de su vista).")]
        public GameObject WallToActivate;

        [Tooltip("La pared que debe DESAPARECER (la que lo bloqueaba en el pasillo anterior).")]
        public GameObject WallToDeactivate;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (WallToActivate != null) WallToActivate.SetActive(true);
                if (WallToDeactivate != null) WallToDeactivate.SetActive(false);
            }
        }
    }
}
