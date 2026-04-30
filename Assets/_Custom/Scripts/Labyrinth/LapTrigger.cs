using UnityEngine;

namespace VRTutorial.Labyrinth
{
    /// <summary>
    /// Trigger invisible que detecta al jugador y avisa al LabyrinthManager.
    /// Colocar un BoxCollider (Is Trigger = true) en el pasillo Sur del anillo,
    /// justo al inicio, mirando hacia la dirección de entrada del jugador.
    /// </summary>
    public class LapTrigger : MonoBehaviour
    {
        [Tooltip("El LabyrinthManager de la escena que recibirá el evento.")]
        public LabyrinthManager Manager;

        [Tooltip("Tag del jugador para ignorar otras colisiones (ej: 'Player').")]
        public string PlayerTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(PlayerTag))
            {
                Manager?.OnPlayerCrossedTrigger();
            }
        }

        private void OnDrawGizmos()
        {
            // Dibujar en la vista de Scene para recordar dónde está el trigger
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            var col = GetComponent<BoxCollider>();
            if (col != null)
                Gizmos.DrawCube(col.center, col.size);
        }
    }
}
