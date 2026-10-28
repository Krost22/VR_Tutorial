using System.Collections;
using System.Linq;
using Autohand;
using TMPro;
using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Pergamino de pistas de la sala de runas. Flota enrollado sobre su atril, con un sello de cera.
    /// - Al agarrarlo por primera vez, el sello se rompe y el pergamino se desenrolla.
    /// - Una "tinta mágica" escribe la pista del paso en que va el jugador (cambia con el progreso del puzzle).
    /// - Una chispa de luz sale volando del pergamino hasta lo siguiente que hay que hacer (la runa, el pilar
    ///   o el portal) y se queda girando encima un momento.
    /// - Si hay una pista nueva mientras descansa en el atril, brilla para avisar.
    /// - Si cae al suelo vuelve al atril (ReturnHomeWhenDropped) y se enrolla otra vez.
    /// Para quitarlo: borrar el objeto Pergamino_Atril.
    /// </summary>
    public class HintScroll : MonoBehaviour
    {
        [Header("Partes")]
        public Grabbable grabbable;
        public Rigidbody body;
        [Tooltip("Hoja: pivote arriba, se estira en -Y al desenrollarse.")]
        public Transform sheet;
        public Transform bottomRod;
        public BoxCollider sheetCollider;
        public GameObject content;
        public GameObject seal;
        public GameObject restingSparkles;
        public FeelIdleMotion idleMotion;
        public float openLength = 0.42f;
        public float rolledLength = 0.035f;

        [Header("Texto (frente y dorso)")]
        public TMP_Text[] titleTexts = new TMP_Text[0];
        public TMP_Text[] bodyTexts = new TMP_Text[0];
        public float charactersPerSecond = 55f;

        [Header("Guía (chispa)")]
        public Transform wisp;
        public TrailRenderer wispTrail;
        public Renderer wispRenderer;
        public float wispCooldown = 3f;

        [Header("Sonido y efectos")]
        public AudioClip paperClip;
        public AudioClip launchClip;
        public AudioClip arriveClip;
        public FeelBurst sealBreakFeedback;
        public FeelBurst newHintFeedback;

        [Header("Puzzle")]
        public PuzzleRoomManager manager;
        public Transform portal;

        struct Hint { public int id; public string title, text; public Transform target; public Color color; }

        float _open;               // 0 enrollado → 1 abierto
        bool _sealed = true, _onStand = true;
        int _shownId = -1, _standId = -1;
        float _nextCheck, _lastWisp = -99f;
        Coroutine _unroll, _typing, _wispRoutine;
        MaterialPropertyBlock _mpb;
        RuneData[] _runes;

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
            if (body == null) body = GetComponent<Rigidbody>();
            SetOpen(0f);
            if (wisp != null) wisp.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (grabbable == null) return;
            grabbable.OnBeforeGrabEvent += OnBeforeGrab;
            grabbable.OnGrabEvent += OnGrab;
        }

        void OnDisable()
        {
            if (grabbable == null) return;
            grabbable.OnBeforeGrabEvent -= OnBeforeGrab;
            grabbable.OnGrabEvent -= OnGrab;
        }

        void Start() => _runes = FindObjectsByType<RuneData>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // ---------------------------------------------------------------- Agarre

        void OnBeforeGrab(Hand hand, Grabbable g)
        {
            // Deja de flotar en el atril: a partir de aquí es un objeto físico normal.
            _onStand = false;
            if (idleMotion != null) idleMotion.enabled = false;
            if (restingSparkles != null) restingSparkles.SetActive(false);
            body.isKinematic = false;
            body.useGravity = true;
        }

        void OnGrab(Hand hand, Grabbable g)
        {
            if (_open < 0.99f) Unroll();
            else RefreshHint(force: false, launchWisp: Time.time - _lastWisp > wispCooldown);
        }

        void Unroll()
        {
            if (_unroll != null) StopCoroutine(_unroll);
            _unroll = StartCoroutine(UnrollRoutine());
        }

        IEnumerator UnrollRoutine()
        {
            if (_sealed)
            {
                _sealed = false;
                if (seal != null)
                {
                    if (sealBreakFeedback != null) sealBreakFeedback.PlayAt(seal.transform.position);
                    seal.SetActive(false);
                }
                FeelHaptics.Pulse(0.45f, 0.08f);
                yield return new WaitForSeconds(0.12f);
            }
            if (paperClip != null) FeelAudio.PlayVaried(paperClip, transform.position, 0.8f, new Vector2(0.95f, 1.08f), 1f);
            float from = _open, t = 0f;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                SetOpen(Mathf.Lerp(from, 1f, FeelTween.Evaluate(FeelEase.OutBack, Mathf.Clamp01(t / 0.55f))));
                yield return null;
            }
            SetOpen(1f);
            RefreshHint(force: true, launchWisp: true);
        }

        /// <summary>0 = enrollado, 1 = abierto. Estira la hoja, baja el rodillo y enciende el texto.</summary>
        void SetOpen(float k)
        {
            _open = k;
            float len = Mathf.LerpUnclamped(rolledLength, openLength, k);
            if (sheet != null) sheet.localScale = new Vector3(1f, Mathf.Max(0.001f, len), 1f);
            if (bottomRod != null) bottomRod.localPosition = new Vector3(0f, -len, 0f);
            if (sheetCollider != null)
            {
                sheetCollider.enabled = k > 0.5f;
                sheetCollider.size = new Vector3(sheetCollider.size.x, len, sheetCollider.size.z);
                sheetCollider.center = new Vector3(0f, -len * 0.5f, 0f);
            }
            if (content != null) content.SetActive(k > 0.95f);
        }

        /// <summary>Lo llama ReturnHomeWhenDropped al volver al atril: se enrolla y vuelve a flotar.</summary>
        public void OnReturnedHome()
        {
            if (_unroll != null) StopCoroutine(_unroll);
            if (_typing != null) StopCoroutine(_typing);
            SetOpen(0f);
            _onStand = true;
            _standId = _shownId;
            body.isKinematic = true;
            if (idleMotion != null) { idleMotion.enabled = true; idleMotion.Rebase(); }
            if (restingSparkles != null) restingSparkles.SetActive(true);
        }

        // ---------------------------------------------------------------- Pistas

        void Update()
        {
            if (Time.time < _nextCheck || manager == null) return;
            _nextCheck = Time.time + 0.5f;
            var hint = Evaluate();
            if (grabbable != null && grabbable.IsHeld() && _open >= 0.99f && hint.id != _shownId)
                RefreshHint(force: true, launchWisp: true);
            else if (_onStand && _shownId >= 0 && hint.id != _standId)
            {
                // Nueva pista mientras descansa: brilla y salta un poco para llamar la atención.
                _standId = hint.id;
                if (newHintFeedback != null) newHintFeedback.PlayAt(transform.position);
                FeelTween.Punch(transform, 0.25f, 0.4f);
            }
        }

        void RefreshHint(bool force, bool launchWisp)
        {
            var hint = Evaluate();
            if (force || hint.id != _shownId)
            {
                _shownId = hint.id;
                _standId = hint.id;
                foreach (var t in titleTexts) if (t != null) t.text = hint.title;
                foreach (var t in bodyTexts) if (t != null) t.text = hint.text;
                if (_typing != null) StopCoroutine(_typing);
                _typing = StartCoroutine(TypeRoutine());
            }
            if (launchWisp && hint.target != null && wisp != null)
            {
                if (_wispRoutine != null) StopCoroutine(_wispRoutine);
                _wispRoutine = StartCoroutine(WispRoutine(hint.target, hint.color));
            }
        }

        IEnumerator TypeRoutine()
        {
            // Tinta mágica: el texto se escribe solo, letra a letra.
            foreach (var t in bodyTexts) if (t != null) t.maxVisibleCharacters = 0;
            foreach (var t in titleTexts) if (t != null) t.maxVisibleCharacters = 99999;
            yield return null;
            int total = bodyTexts.Where(t => t != null).Select(t => t.textInfo.characterCount).DefaultIfEmpty(0).Max();
            float shown = 0f;
            while (shown < total)
            {
                shown += charactersPerSecond * Time.deltaTime;
                foreach (var t in bodyTexts) if (t != null) t.maxVisibleCharacters = (int)shown;
                yield return null;
            }
            foreach (var t in bodyTexts) if (t != null) t.maxVisibleCharacters = 99999;
        }

        Hint Evaluate()
        {
            var all = manager.PhaseTwoProjectors.Where(p => p != null).ToList();
            var first = manager.PhaseOneProjectors.Where(p => p != null).ToList();

            if (all.Count > 0 && all.All(p => p.IsLocked()))
                return new Hint { id = 900, title = "Pista final", text = "La puerta grande se abrió.\nCruza el portal para terminar tu aventura.", target = portal, color = new Color(0.55f, 0.5f, 1f) };

            // Un pilar despierto que aún no apunta a su mural.
            var awake = all.FirstOrDefault(p => p.IsActivatedByAltar && !p.IsLocked());
            if (awake != null)
            {
                var e = awake.RequiredSymbolInSocket;
                return new Hint
                {
                    id = 200 + (int)e, title = "Pista", color = ElementPalette.Glow(e), target = awake.transform,
                    text = "El pilar de " + ElementPalette.InkTag(e, ElementPalette.Name(e)) + " despertó.\nAgárralo y gíralo hasta que su rayo " +
                           ElementPalette.InkTag(e, ElementPalette.ColorName(e)) + " toque el mural " + ElementPalette.InkTag(e, ElementPalette.ColorName(e)) + "."
                };
            }

            bool firstDone = first.Count > 0 && first.All(p => p.IsLocked());
            var pending = (firstDone ? all : first).FirstOrDefault(p => !p.IsActivatedByAltar);
            if (pending != null)
            {
                var e = pending.RequiredSymbolInSocket;
                var rune = _runes?.FirstOrDefault(r => r != null && r.CurrentSymbol == e);
                var runeGrab = rune != null ? rune.GetComponent<Grabbable>() : null;
                // Si ya la tiene en la mano, la chispa señala el hueco del altar.
                Transform target = rune != null && !(runeGrab != null && runeGrab.IsHeld()) ? rune.transform : pending.AltarSocket != null ? pending.AltarSocket.transform : null;
                string name = ElementPalette.InkTag(e, ElementPalette.Name(e)), color = ElementPalette.InkTag(e, ElementPalette.ColorName(e));
                return new Hint
                {
                    id = (firstDone ? 300 : 100) + (int)e, title = "Pista", color = ElementPalette.Glow(e), target = target,
                    text = firstDone
                        ? "¡Se abrió un pasadizo secreto!\nTrae la runa de " + name + " y ponla en el hueco " + color + " del altar."
                        : "Cada runa brilla con su color.\nPon la runa de " + name + " en el hueco " + color + " del altar."
                };
            }
            return new Hint { id = 0, title = "Pista", text = "Observa los murales y los pilares.\nCada elemento tiene su color.", target = null, color = Color.white };
        }

        // ---------------------------------------------------------------- Chispa guía

        IEnumerator WispRoutine(Transform target, Color color)
        {
            _lastWisp = Time.time;
            wisp.gameObject.SetActive(true);
            wisp.position = sheet != null ? sheet.position + sheet.up * -openLength * 0.5f : transform.position;
            if (wispTrail != null) { wispTrail.Clear(); wispTrail.startColor = color; wispTrail.endColor = new Color(color.r, color.g, color.b, 0f); }
            if (wispRenderer != null) { wispRenderer.GetPropertyBlock(_mpb); _mpb.SetColor("_BaseColor", color); wispRenderer.SetPropertyBlock(_mpb); }
            Vector3 baseScale = Vector3.one * 0.06f;
            if (launchClip != null) FeelAudio.PlayVaried(launchClip, wisp.position, 0.6f, new Vector2(1.1f, 1.25f), 1f);

            // Vuelo en arco hasta encima del objetivo (lo sigue si se mueve).
            Vector3 p0 = wisp.position;
            float dist = Vector3.Distance(p0, target.position);
            float dur = Mathf.Clamp(dist / 3.5f, 0.9f, 2.4f), t = 0f;
            while (t < dur && target != null)
            {
                t += Time.deltaTime;
                float u = FeelTween.Evaluate(FeelEase.InOutSine, Mathf.Clamp01(t / dur));
                Vector3 p2 = target.position + Vector3.up * 0.45f;
                Vector3 p1 = (p0 + p2) * 0.5f + Vector3.up * (0.6f + dist * 0.08f);
                wisp.position = (1 - u) * (1 - u) * p0 + 2 * (1 - u) * u * p1 + u * u * p2;
                wisp.localScale = baseScale * (1f + 0.25f * Mathf.Sin(t * 18f));
                yield return null;
            }
            if (arriveClip != null && target != null) FeelAudio.PlayVaried(arriveClip, target.position, 0.7f, new Vector2(1.15f, 1.3f), 1f);

            // Gira encima del objetivo, latiendo, para que el jugador lo encuentre.
            t = 0f;
            while (t < 2.8f && target != null)
            {
                t += Time.deltaTime;
                float a = t * 4f;
                wisp.position = target.position + Vector3.up * (0.45f + 0.05f * Mathf.Sin(t * 5f)) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.22f;
                wisp.localScale = baseScale * (1.1f + 0.35f * Mathf.Sin(t * 9f));
                yield return null;
            }
            t = 0f;
            while (t < 0.3f) { t += Time.deltaTime; wisp.localScale = baseScale * (1f - t / 0.3f); yield return null; }
            wisp.gameObject.SetActive(false);
        }
    }
}
