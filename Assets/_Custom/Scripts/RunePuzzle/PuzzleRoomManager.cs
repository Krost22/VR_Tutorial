using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRTutorial.RunePuzzle
{
    /// <summary>
    /// Escucha a los proyectores y administra las fases del puzzle.
    /// </summary>
    public class PuzzleRoomManager : MonoBehaviour
    {
        [Header("Fase 1: Los Primeros Dos Pilares")]
        [Tooltip("Los dos primeros pilares que deben alinearse para revelar el cuarto oculto.")]
        public List<PillarProjector> PhaseOneProjectors;
        public UnityEvent OnSecretCompartmentOpened;
        private bool _isPhaseOneComplete = false;

        [Header("Fase 2: Acertijo Final")]
        [Tooltip("Lista de TODOS los proyectores (los 4) que deben estar alineados para salir.")]
        public List<PillarProjector> PhaseTwoProjectors;
        public UnityEvent OnMainDoorOpened;
        private bool _isPhaseTwoComplete = false;

        /// <summary>
        /// Evalúa el estado general para ver si desencadenar Fase 1 o Fase 2.
        /// Se sugiere conectar este método a los eventos 'OnProjectorMatched' y 'OnProjectorDisconnected' 
        /// de todos los PillarProjectors.
        /// </summary>
        public void EvaluateGameState()
        {
            EvaluatePhaseOne();
            EvaluatePhaseTwo();
        }

        private void EvaluatePhaseOne()
        {
            if (_isPhaseOneComplete) return;

            bool allPhaseOneMatched = true;
            foreach (var projector in PhaseOneProjectors)
            {
                if (!projector.IsLocked())
                {
                    allPhaseOneMatched = false;
                    break;
                }
            }

            if (allPhaseOneMatched)
            {
                _isPhaseOneComplete = true;
                Debug.Log("¡Fase 1 Completada! Abriendo puerta secreta para revelar cubos extra...");
                OnSecretCompartmentOpened?.Invoke();
            }
        }

        private void EvaluatePhaseTwo()
        {
            if (_isPhaseTwoComplete) return;

            // La fase 2 requiere que PhaseTwoProjectors tenga los 4 pilares a validar.
            bool allPhaseTwoMatched = true;
            foreach (var projector in PhaseTwoProjectors)
            {
                if (!projector.IsLocked())
                {
                    allPhaseTwoMatched = false;
                    break;
                }
            }

            if (allPhaseTwoMatched)
            {
                _isPhaseTwoComplete = true;
                Debug.Log("¡Fase 2 Completada! Abriendo puerta principal...");
                OnMainDoorOpened?.Invoke();
            }
        }
    }
}
