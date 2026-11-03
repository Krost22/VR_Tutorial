using System.Collections;
using System.Collections.Generic;
using Autohand;
using UnityEngine;
using UnityEngine.Events;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Si el objeto queda en el suelo (soltado o caído), desaparece con un destello y vuelve a su posición
    /// inicial: el jugador nunca tiene que agacharse a recoger nada (comodidad en VR).
    /// "Suelo" = colliders cuyo nombre empieza por Tile, Suelo, Step, Plane, Floor o Piso. Si cae fuera del
    /// mapa también vuelve. No actúa mientras se sostiene o mientras está colocado en un PlacePoint.
    /// Para quitarlo: borrar el componente.
    /// Sin [RequireComponent(Rigidbody)]: los PlacePoint de AutoHand que cuelgan de un objeto agarrable (los
    /// pilares) destruyen el Rigidbody al colocar y crean otro al soltar. Con el atributo, Unity impedía
    /// destruirlo ("Can't remove Rigidbody...") y la runa quedaba con física dentro del pilar.
    /// </summary>
    public class ReturnHomeWhenDropped : MonoBehaviour
    {
        [Tooltip("Inicio del nombre de los colliders que cuentan como suelo.")]
        public string[] floorPrefixes = { "Tile", "Suelo", "Step", "Plane", "Floor", "Piso" };
        [Tooltip("Segundos quieto en el suelo antes de volver (deja ver que cayó).")]
        public float restSeconds = 0.6f;
        [Tooltip("Velocidad por debajo de la cual se considera quieto (m/s).")]
        public float restSpeed = 0.4f;
        [Tooltip("Si baja más que esto por debajo de su posición inicial (fuera del mapa), vuelve al instante.")]
        public float fallLimit = 6f;

        [Header("Feedback")]
        public FeelBurst vanishFeedback;
        public FeelBurst appearFeedback;
        public UnityEvent onReturned = new UnityEvent();

        Grabbable _grab;
        Rigidbody _rbCache;
        // AutoHand puede quitar el Rigidbody y crear uno nuevo: se vuelve a buscar cuando hace falta.
        Rigidbody _rb => _rbCache != null ? _rbCache : (_rbCache = GetComponent<Rigidbody>());
        Vector3 _homePos, _homeScale;
        Quaternion _homeRot;
        Transform _homeParent;
        bool _homeKinematic, _homeGravity;
        float _restTimer;
        bool _returning, _armed, _frozen;
        // Contactos con el suelo por entrada/salida: siguen valiendo cuando el cuerpo se "duerme" (quieto, la
        // física deja de mandar OnCollisionStay y antes nunca se llegaba a contar el tiempo en el suelo).
        readonly HashSet<Collider> _floorContacts = new HashSet<Collider>();
        readonly List<float> _recentReturns = new List<float>();

        public Vector3 HomePosition => _homePos;

        IEnumerator Start()
        {
            _grab = GetComponent<Grabbable>();
            _homeParent = transform.parent;
            if (_rb != null) { _homeKinematic = _rb.isKinematic; _homeGravity = _rb.useGravity; }
            else _homeGravity = true;
            SaveHome();
            // Si quedó congelado por la protección, al agarrarlo vuelve a ser un objeto físico normal.
            if (_grab != null) _grab.OnBeforeGrabEvent += (h, g) => { if (_frozen) { _frozen = false; if (_rb != null) _rb.isKinematic = _homeKinematic; } };

            // El sitio de origen es donde queda en reposo al empezar (una runa que aparece en el aire cae sobre
            // el altar o su pedestal), no el punto del aire. Si al asentarse quedó en el suelo, se conserva el
            // punto inicial.
            float t = 0f;
            while (t < 2f && _rb != null && !_rb.isKinematic && !_rb.IsSleeping()) { t += Time.deltaTime; yield return null; }
            if (_floorContacts.Count == 0) SaveHome();
            _armed = true;
        }

        void SaveHome()
        {
            _homePos = transform.position;
            _homeRot = transform.rotation;
            _homeScale = transform.localScale;
        }

        void OnCollisionEnter(Collision c) { if (IsFloor(c.collider)) _floorContacts.Add(c.collider); }
        void OnCollisionStay(Collision c) { if (IsFloor(c.collider)) _floorContacts.Add(c.collider); }
        void OnCollisionExit(Collision c) { _floorContacts.Remove(c.collider); }

        bool IsFloor(Collider c)
        {
            var n = c.name;
            foreach (var p in floorPrefixes) if (n.StartsWith(p)) return true;
            return false;
        }

        void Update()
        {
            if (_returning || !_armed) return;
            bool busy = _grab != null && (_grab.IsHeld() || _grab.BeingGrabbed() || _grab.placePoint != null);
            if (busy) { _restTimer = 0f; _floorContacts.Clear(); return; }
            var rb = _rb;
            if (rb == null) return; // sin Rigidbody (AutoHand lo quitó al colocarlo): nada que vigilar

            if (transform.position.y < _homePos.y - fallLimit) { StartCoroutine(ReturnRoutine()); return; }

            _floorContacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            bool onFloor = _floorContacts.Count > 0;
            bool still = rb.isKinematic || rb.IsSleeping() || rb.linearVelocity.magnitude < restSpeed;
            _restTimer = onFloor && still ? _restTimer + Time.deltaTime : 0f;
            if (_restTimer >= restSeconds) StartCoroutine(ReturnRoutine());
        }

        /// <summary>Lo devuelve a su sitio ya (lo usan también otros scripts).</summary>
        public void ReturnNow()
        {
            if (!_returning && isActiveAndEnabled) StartCoroutine(ReturnRoutine());
        }

        IEnumerator ReturnRoutine()
        {
            _returning = true;
            _restTimer = 0f;
            _floorContacts.Clear();

            // Protección: si vuelve una y otra vez sin que nadie lo toque (su sitio no lo sostiene), se queda
            // flotando quieto en su sitio en vez de caer y volver en bucle.
            _recentReturns.RemoveAll(x => Time.time - x > 10f);
            _recentReturns.Add(Time.time);
            bool freeze = _recentReturns.Count >= 3;
            if (vanishFeedback != null) vanishFeedback.PlayAt(transform.position);

            // Se encoge donde cayó...
            Vector3 from = transform.localScale;
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(from, _homeScale * 0.01f, t / 0.18f);
                yield return null;
            }

            // ...y reaparece en su sitio con un pequeño rebote.
            var rb = _rb;
            if (rb != null) rb.isKinematic = true;
            transform.SetParent(_homeParent, true);
            transform.SetPositionAndRotation(_homePos, _homeRot);
            if (rb != null) { rb.position = _homePos; rb.rotation = _homeRot; }
            yield return new WaitForFixedUpdate();
            rb = _rb;
            _frozen = freeze && !_homeKinematic;
            if (rb != null)
            {
                rb.isKinematic = _homeKinematic || freeze;
                rb.useGravity = _homeGravity;
                if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            }
            if (appearFeedback != null) appearFeedback.PlayAt(_homePos);
            onReturned?.Invoke();

            t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float k = FeelTween.Evaluate(FeelEase.OutBack, Mathf.Clamp01(t / 0.35f));
                transform.localScale = _homeScale * Mathf.Max(0.01f, k);
                yield return null;
            }
            transform.localScale = _homeScale;
            _returning = false;
        }
    }
}
