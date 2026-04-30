using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRTutorial.Labyrinth
{
    /// <summary>
    /// Gestiona el laberinto de bucle estilo P.T.
    /// El jugador da 3 vueltas al anillo. En cada vuelta el ambiente
    /// cambia sutilmente. Al completar la 3ra vuelta, aparece el pedestal
    /// con la Runa de salida.
    /// </summary>
    public class LabyrinthManager : MonoBehaviour
    {
        [Header("Control de Vueltas")]
        [Tooltip("Número total de vueltas antes de revelar la salida.")]
        public int TotalLaps = 3;
        [Tooltip("Tiempo mínimo en segundos entre detecciones del trigger (evita doble conteo).")]
        public float LapCooldown = 3f;

        [Header("Cambios Ambientales — Vuelta 1")]
        [Tooltip("Objetos a ACTIVAR al completar la vuelta 1 (ej: runas azules en pared).")]
        public List<GameObject> Lap1Activate;
        [Tooltip("Objetos a DESACTIVAR al completar la vuelta 1.")]
        public List<GameObject> Lap1Deactivate;

        [Header("Cambios Ambientales — Vuelta 2")]
        [Tooltip("Objetos a ACTIVAR al completar la vuelta 2 (ej: antorchas de pared).")]
        public List<GameObject> Lap2Activate;
        [Tooltip("Objetos a DESACTIVAR al completar la vuelta 2.")]
        public List<GameObject> Lap2Deactivate;

        [Header("Cambios Ambientales — Vuelta 3 (Revelación)")]
        [Tooltip("Objetos a ACTIVAR al completar la vuelta 3 (ej: el pedestal con la Runa).")]
        public List<GameObject> Lap3Activate;
        [Tooltip("Objetos a DESACTIVAR al completar la vuelta 3.")]
        public List<GameObject> Lap3Deactivate;

        [Header("Salida del Laberinto")]
        [Tooltip("La pared o puerta que se desactiva al resolver el puzzle final.")]
        public GameObject ExitWall;
        [Tooltip("Evento disparado al abrir la salida (conectar audio + partículas de polvo).")]
        public UnityEvent OnLabyrinthSolved;

        // Estado interno
        private int _currentLap = 0;
        private float _lastLapTime = -999f;
        private bool _isSolved = false;

        private void Awake()
        {
            // Aseguramos que todos los cambios ambientales inicien APAGADOS
            SetListActive(Lap1Activate, false);
            SetListActive(Lap2Activate, false);
            SetListActive(Lap3Activate, false);
        }

        /// <summary>
        /// Llamado por el Trigger_VueltaControl cuando el jugador lo cruza.
        /// </summary>
        public void OnPlayerCrossedTrigger()
        {
            if (_isSolved) return;

            // Cooldown para evitar doble conteo
            if (Time.time - _lastLapTime < LapCooldown) return;
            _lastLapTime = Time.time;

            _currentLap++;
            Debug.Log($"[LabyrinthManager] Vuelta completada: {_currentLap} / {TotalLaps}");

            switch (_currentLap)
            {
                case 1:
                    ApplyLapChanges(Lap1Activate, Lap1Deactivate, "1 — El Corredor Olvidado");
                    break;
                case 2:
                    ApplyLapChanges(Lap2Activate, Lap2Deactivate, "2 — El Templo Despertando");
                    break;
                case 3:
                    ApplyLapChanges(Lap3Activate, Lap3Deactivate, "3 — La Llave se Revela");
                    break;
                default:
                    // El jugador sigue dando vueltas después de la 3ra.
                    // No pasa nada — el puzzle de la Runa está esperándolo.
                    Debug.Log("[LabyrinthManager] Vuelta extra. Esperando que el jugador coloque la Runa.");
                    break;
            }
        }

        /// <summary>
        /// Llamado por el PlacePoint del Altar de Salida cuando la Runa es colocada.
        /// Conectar desde el Inspector al evento OnPlace del PlacePoint del Altar.
        /// </summary>
        public void OnRunePlacedInAltar()
        {
            if (_isSolved) return;
            if (_currentLap < TotalLaps)
            {
                Debug.Log("[LabyrinthManager] La Runa fue colocada pero aún no se completaron las 3 vueltas. Ignorando.");
                return;
            }

            _isSolved = true;
            Debug.Log("[LabyrinthManager] ¡Laberinto Resuelto! Abriendo la salida...");

            StartCoroutine(OpenExitRoutine());
        }

        private IEnumerator OpenExitRoutine()
        {
            // Pequeña pausa dramática antes de abrir la puerta
            yield return new WaitForSeconds(0.5f);

            OnLabyrinthSolved?.Invoke();

            yield return new WaitForSeconds(0.3f);

            if (ExitWall != null)
                ExitWall.SetActive(false);
        }

        private void ApplyLapChanges(List<GameObject> toActivate, List<GameObject> toDeactivate, string lapName)
        {
            Debug.Log($"[LabyrinthManager] Aplicando cambios de Vuelta {lapName}");

            // Los cambios ocurren a espaldas del jugador (él acaba de doblar la esquina)
            // así que nunca los verá "aparecer"
            SetListActive(toActivate, true);
            SetListActive(toDeactivate, false);
        }

        private void SetListActive(List<GameObject> list, bool active)
        {
            if (list == null) return;
            foreach (var obj in list)
            {
                if (obj != null) obj.SetActive(active);
            }
        }

        /// <summary>
        /// Útil para debug en el Editor — simula que el jugador cruza el trigger.
        /// </summary>
        [ContextMenu("DEBUG: Simular cruce de trigger")]
        private void DebugSimulateLap()
        {
            _lastLapTime = -999f; // Resetea cooldown para poder simular
            OnPlayerCrossedTrigger();
        }
    }
}
