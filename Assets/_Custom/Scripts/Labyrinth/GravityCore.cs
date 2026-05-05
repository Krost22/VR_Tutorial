using UnityEngine;
using UnityEngine.Events;
using Autohand;

namespace VRTutorial.Labyrinth
{
    public class GravityCore : MonoBehaviour
    {
        [Header("Configuración del Puzzle")]
        public int RequiredCrystals = 3;
        public float RotationSpeedBase = 20f;
        public float SpeedIncreasePerCrystal = 30f;

        [Header("Referencias")]
        public PlacePoint[] CrystalSlots;
        public GameObject SuccessEffect; // Partículas o luz al completar
        
        [Header("Eventos")]
        public UnityEvent OnPuzzleComplete;

        private int _insertedCrystals = 0;
        private bool _isComplete = false;

        private void Start()
        {
            // Suscribirse a los eventos de los PlacePoints
            foreach (var slot in CrystalSlots)
            {
                slot.OnPlaceEvent += (point, grab) => OnCrystalInserted();
            }
        }

        private void Update()
        {
            if (_isComplete) return;

            // Rotar el núcleo constantemente
            float currentSpeed = RotationSpeedBase + (_insertedCrystals * SpeedIncreasePerCrystal);
            transform.Rotate(Vector3.up, currentSpeed * Time.deltaTime);
        }

        private void OnCrystalInserted()
        {
            _insertedCrystals++;
            Debug.Log($"[GravityCore] Cristal insertado: {_insertedCrystals}/{RequiredCrystals}");

            if (_insertedCrystals >= RequiredCrystals)
            {
                CompletePuzzle();
            }
        }

        private void CompletePuzzle()
        {
            _isComplete = true;
            Debug.Log("[GravityCore] ¡Puzzle Completado!");
            
            if (SuccessEffect != null) SuccessEffect.SetActive(true);
            
            OnPuzzleComplete?.Invoke();
        }
    }
}
