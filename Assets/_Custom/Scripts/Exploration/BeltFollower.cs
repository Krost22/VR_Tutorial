using UnityEngine;

namespace VRTutorial.Exploration
{
    /// <summary>
    /// Hace que el objeto (como un cinturón) siga la posición física y la rotación (solo eje Y)
    /// de la cabeza del jugador, permitiendo caminar en el mundo real o girar la cabeza
    /// sin que el cinturón se quede atrás o mire hacia otro lado.
    /// </summary>
    public class BeltFollower : MonoBehaviour
    {
        [Tooltip("La cámara principal del jugador (Camera (head)).")]
        public Transform HeadCamera;

        [Tooltip("Distancia vertical desde los ojos hasta la cadera (negativo hacia abajo).")]
        public float HeightOffset = -0.65f;

        [Tooltip("Qué tan rápido el cinturón se ajusta al girar o caminar.")]
        public float SmoothSpeed = 8f;

        private void LateUpdate()
        {
            if (HeadCamera == null) return;

            // 1. Calcular la posición: Exactamente debajo de la cámara
            Vector3 targetPosition = HeadCamera.position;
            targetPosition.y += HeightOffset; // Bajar a la altura de la cadera

            // 2. Calcular la rotación: Copiar el ángulo Y (Yaw), ignorar X y Z (Pitch/Roll)
            // Esto evita que el cinturón se voltee si el jugador mira hacia el piso o el techo.
            Vector3 headEuler = HeadCamera.eulerAngles;
            Quaternion targetRotation = Quaternion.Euler(0, headEuler.y, 0);

            // 3. Aplicar los cambios suavemente para evitar temblores
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * SmoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * SmoothSpeed);
        }
    }
}
