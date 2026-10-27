using System.Collections;
using System.Collections.Generic;
using Autohand;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Puzzle de la bandera de Colombia (4 filas x 3 columnas, proporción 2:1:1 amarillo/azul/rojo).
    /// Diseño (UX):
    /// - El tablero está pegado a la barrera azul: lo que armas es lo que abre el paso (mapeo directo).
    /// - Cada fila completa apaga el haz láser de su misma altura (estado visible, de arriba hacia abajo).
    /// - Los huecos vacíos laten (señal de "aquí va algo"); las celdas ya puestas muestran el patrón.
    /// - Un cubo del color equivocado no se queda: suena un error y sale disparado suavemente (error sin castigo).
    /// - Tras varios errores en un hueco, el hueco se tiñe del color que pide (pista progresiva).
    /// </summary>
    public class FlagPuzzle : MonoBehaviour
    {
        public enum FlagColor { Amarillo, Azul, Rojo }

        [System.Serializable]
        public class Slot
        {
            public PlacePoint point;
            public FlagColor expected;
            public int row;
            public Renderer frame;
            [HideInInspector] public bool filled;
            [HideInInspector] public int errors;
        }

        public List<Slot> slots = new List<Slot>();
        public int rowCount = 4;

        [Header("Progreso en la barrera")]
        [Tooltip("Haz que se apaga al completar cada fila (fila 0 = arriba).")]
        public LaserBeamSet beams;
        [Tooltip("Barrera completa que se retira al terminar (lleva FeelDisappear).")]
        public GameObject gate;
        public GameObject popupOnComplete;

        [Header("Respuesta")]
        public FeelBurst correctFeedback;
        public FeelBurst wrongFeedback;
        public FeelBurst rowFeedback;
        public FeelBurst completeFeedback;
        [Tooltip("Errores en un mismo hueco antes de mostrar su color como pista.")]
        public int errorsBeforeHint = 2;
        public float ejectSpeed = 1.2f;

        [Header("Colores")]
        public Color emptyColor = new Color(1f, 1f, 1f, 0.55f);
        public Color amarillo = new Color(1f, 0.85f, 0.1f);
        public Color azul = new Color(0.15f, 0.45f, 1f);
        public Color rojo = new Color(1f, 0.15f, 0.15f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _block;
        private int _placedCount;
        private bool _completed;
        private readonly HashSet<int> _rowsDone = new HashSet<int>();

        private void Awake() => _block = new MaterialPropertyBlock();

        private readonly Dictionary<Slot, UnityEngine.Events.UnityAction<PlacePoint, Grabbable>> _listeners =
            new Dictionary<Slot, UnityEngine.Events.UnityAction<PlacePoint, Grabbable>>();

        private void OnEnable()
        {
            foreach (var slot in slots)
            {
                var s = slot;
                if (s.point == null) continue;
                UnityEngine.Events.UnityAction<PlacePoint, Grabbable> action = (p, g) => HandlePlace(s, g);
                _listeners[s] = action;
                s.point.OnPlace.AddListener(action);
            }
        }

        private void OnDisable()
        {
            foreach (var kv in _listeners) if (kv.Key.point != null) kv.Key.point.OnPlace.RemoveListener(kv.Value);
            _listeners.Clear();
        }

        private void Update()
        {
            // Los huecos vacíos laten suavemente; con pista, laten en el color que piden.
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 4f);
            foreach (var s in slots)
            {
                if (s.frame == null || s.filled) continue;
                Color c = s.errors >= errorsBeforeHint ? ColorOf(s.expected) : emptyColor;
                c.a *= pulse;
                SetFrame(s, c);
            }
        }

        private void HandlePlace(Slot slot, Grabbable grab)
        {
            if (slot.filled || grab == null) return;
            if (TryGetColor(grab.name, out var color) && color == slot.expected)
            {
                slot.filled = true;
                grab.isGrabbable = false;
                StartCoroutine(CenterInSlot(grab, slot));
                SetFrame(slot, ColorOf(slot.expected) * 1.4f);
                if (correctFeedback != null) correctFeedback.PlayStepAt(slot.point.transform.position, _placedCount);
                FeelTween.Punch(grab.transform, 0.2f, 0.3f);
                _placedCount++;
                CheckRow(slot.row);
            }
            else
            {
                slot.errors++;
                if (wrongFeedback != null) wrongFeedback.PlayAt(slot.point.transform.position);
                FeelHaptics.ErrorPattern(this);
                StartCoroutine(Eject(slot, grab));
            }
        }

        /// <summary>El cubo tiene el pivote en una esquina: se mueve para que su centro visual quede en el hueco.</summary>
        private IEnumerator CenterInSlot(Grabbable grab, Slot slot)
        {
            for (int i = 0; i < 2; i++)
            {
                if (grab == null) yield break;
                var r = grab.GetComponentInChildren<Renderer>();
                if (r != null) grab.transform.position += slot.point.transform.position - r.bounds.center;
                yield return null;
            }
        }

        private IEnumerator Eject(Slot slot, Grabbable grab)
        {
            yield return new WaitForSeconds(0.3f);
            if (grab == null || slot.point == null) yield break;
            slot.point.Remove(grab);
            grab.isGrabbable = true;
            if (grab.body != null)
            {
                grab.body.isKinematic = false;
                // Sale hacia el jugador y un poco hacia arriba, para que sea fácil volver a agarrarlo.
                Vector3 dir = (-slot.point.transform.forward + Vector3.up * 0.6f).normalized;
                grab.body.linearVelocity = dir * ejectSpeed;
            }
        }

        private void CheckRow(int row)
        {
            if (_rowsDone.Contains(row)) return;
            foreach (var s in slots) if (s.row == row && !s.filled) return;
            _rowsDone.Add(row);
            if (beams != null) beams.TurnOff(row);
            if (rowFeedback != null)
            {
                Vector3 center = Vector3.zero; int n = 0;
                foreach (var s in slots) if (s.row == row) { center += s.point.transform.position; n++; }
                rowFeedback.PlayStepAt(n > 0 ? center / n : transform.position, _rowsDone.Count * 2);
            }
            if (_rowsDone.Count >= rowCount) StartCoroutine(Complete());
        }

        private IEnumerator Complete()
        {
            if (_completed) yield break;
            _completed = true;
            yield return new WaitForSeconds(0.5f);
            if (completeFeedback != null) completeFeedback.Play();
            if (popupOnComplete != null) popupOnComplete.SetActive(true);
            FeelDisappear.HideOrDeactivate(gate);
        }

        private void SetFrame(Slot s, Color c)
        {
            if (s.frame == null) return;
            s.frame.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, c);
            s.frame.SetPropertyBlock(_block);
        }

        private Color ColorOf(FlagColor c) => c == FlagColor.Amarillo ? amarillo : (c == FlagColor.Azul ? azul : rojo);

        public static bool TryGetColor(string objectName, out FlagColor color)
        {
            color = FlagColor.Amarillo;
            if (objectName.Contains("Amarillo")) { color = FlagColor.Amarillo; return true; }
            if (objectName.Contains("Azul")) { color = FlagColor.Azul; return true; }
            if (objectName.Contains("Rojo")) { color = FlagColor.Rojo; return true; }
            return false;
        }
    }
}
