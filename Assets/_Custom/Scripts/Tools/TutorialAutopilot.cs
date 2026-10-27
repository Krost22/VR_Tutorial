#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VRTutorial.GameFeel;
using VRTutorial.Tutorial;
using Object = UnityEngine.Object;

namespace VRTutorial.DevTools
{
    /// <summary>
    /// Solo para el Editor: juega el tutorial completo sin visor, como lo haría una persona, para probarlo
    /// y grabarlo. Mira las 5 luces, pisa los círculos, arma la bandera (con un error a propósito), aplasta
    /// arañas con el martillo, encesta el balón y lleva al golem esquivando los pinchos hasta la meta.
    /// Termina cuando se abre la pared hacia la mazmorra.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class TutorialAutopilot : MonoBehaviour
    {
        public float walkSpeed = 1.5f;
        public float turnSpeed = 110f;
        public float golemSpeed = 1.7f;
        public string Phase { get; private set; } = "inicio";
        public bool Finished { get; private set; }
        public readonly List<string> Log = new List<string>();

        GazeSequenceTutorial _gaze;
        Camera _cam;
        Collider _playerCollider;
        Vector3 _pos = new Vector3(0.3f, 1.6f, -0.1f);
        Quaternion _rot = Quaternion.Euler(14f, -16f, 0f);
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public Pose HeadPose => new Pose(_pos, _rot * Jitter());

        void Start()
        {
            _gaze = Object.FindAnyObjectByType<GazeSequenceTutorial>();
            _cam = _gaze.playerCamera;
            foreach (var b in _cam.GetComponents<Behaviour>())
                if (b.GetType().Name.Contains("TrackedPoseDriver")) b.enabled = false;
            _playerCollider = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).FirstOrDefault(c => c.CompareTag("Player"));
            StartCoroutine(Run());
            StartCoroutine(PopupCloser());
        }

        void Update() => Apply();
        void LateUpdate() => Apply();
        void Apply() { if (_cam != null) _cam.transform.SetPositionAndRotation(_pos, _rot * Jitter()); }

        static Quaternion Jitter()
        {
            float t = Time.time;
            return Quaternion.Euler(Mathf.PerlinNoise(t * 0.6f, 0f) * 0.7f - 0.35f, Mathf.PerlinNoise(0f, t * 0.6f) * 0.7f - 0.35f, 0f);
        }

        void Note(string s) { Log.Add(Time.time.ToString("0.0") + "s " + Phase + ": " + s); }

        IEnumerator Run()
        {
            Phase = "mirada"; yield return GazePhase();
            // El autopilot mueve la cabeza; el cuerpo físico de AutoHand no debe arrastrarla.
            if (_gaze.movementScriptToDisable != null) _gaze.movementScriptToDisable.enabled = false;
            Phase = "movimiento"; yield return MovementPhase();
            Phase = "bandera"; yield return FlagPhase();
            Phase = "arañas"; yield return SpiderPhase();
            Phase = "canasta"; yield return BasketPhase();
            Phase = "golem"; yield return GolemPhase();
            Phase = "fin"; Finished = true;
            Note("terminado");
        }

        // ------------------------------------------------------------------ Cabeza

        IEnumerator LookAt(Vector3 point, float minDuration = 0.35f)
        {
            var to = Quaternion.LookRotation(point - _pos);
            float angle = Quaternion.Angle(_rot, to);
            float duration = Mathf.Max(minDuration, angle / turnSpeed);
            var from = _rot;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / duration);
                _rot = Quaternion.Slerp(from, to, u);
                yield return null;
            }
            _rot = to;
        }

        IEnumerator WalkTo(Vector3 target, Func<Vector3> lookAt = null)
        {
            var from = _pos;
            float duration = Mathf.Max(0.6f, Vector3.Distance(from, target) / walkSpeed);
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / duration);
                _pos = Vector3.Lerp(from, target, u);
                // Pasos suaves (sin balanceo fuerte: es VR).
                _pos.y = target.y + Mathf.Sin(u * duration * 8f) * 0.01f;
                Vector3 look = lookAt != null ? lookAt() : _pos + (target - from).normalized * 3f + Vector3.down * 0.4f;
                var want = Quaternion.LookRotation(look - _pos);
                _rot = Quaternion.RotateTowards(_rot, want, turnSpeed * Time.deltaTime);
                yield return null;
            }
            _pos = target;
        }

        IEnumerator Track(Func<Vector3> point, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                var want = Quaternion.LookRotation(point() - _pos);
                _rot = Quaternion.Slerp(_rot, want, 1f - Mathf.Exp(-6f * Time.deltaTime));
                yield return null;
            }
        }

        IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }

        // ------------------------------------------------------------------ Fase 1: mirada guiada

        IEnumerator GazePhase()
        {
            while (_gaze.CurrentTarget == null && !_gaze.Completed) yield return null;
            yield return Wait(0.9f); // lee el texto
            while (!_gaze.Completed)
            {
                var target = _gaze.CurrentTarget;
                if (target == null) { yield return null; continue; }
                float angle = Quaternion.Angle(_rot, Quaternion.LookRotation(target.position - _pos));
                if (angle > 35f) yield return Wait(0.6f); // ve la flecha y decide
                yield return LookAt(target.position);
                while (_gaze.CurrentTarget == target) yield return null;
                Note("luz " + target.name);
                yield return Wait(0.2f);
                yield return LookAt(new Vector3(1.4f, 1.25f, 3.3f), 0.8f); // sigue la luz hasta su gema
                yield return Wait(0.3f);
            }
            yield return Wait(1.6f);
            Note("barrera retirada: " + !(_gaze.barrier != null && _gaze.barrier.activeInHierarchy));
        }

        // ------------------------------------------------------------------ Fase 1b: círculos de movimiento

        IEnumerator MovementPhase()
        {
            var all = Resources.FindObjectsOfTypeAll<MovementTutorial>().Where(m => m.gameObject.scene.IsValid()).ToList();
            var current = all.FirstOrDefault(m => m.isFirstCircle);
            var trigger = typeof(MovementTutorial).GetMethod("OnTriggerEnter", Any);
            GameObject wall = null;
            while (current != null)
            {
                float wait = 0f;
                while (!current.gameObject.activeInHierarchy && wait < 3f) { wait += Time.deltaTime; yield return null; }
                var c = current.transform.position;
                yield return LookAt(c, 0.5f);
                yield return Wait(0.25f);
                yield return WalkTo(new Vector3(c.x, 1.62f, c.z));
                if (current.isLastCircle) wall = current.wallToDeactivate;
                var next = current.nextCircle != null && current.nextCircle != current.gameObject ? current.nextCircle.GetComponent<MovementTutorial>() : null;
                trigger.Invoke(current, new object[] { _playerCollider });
                Note("círculo " + current.name);
                current = next;
                yield return Wait(0.45f);
            }
            if (wall != null)
            {
                yield return LookAt(wall.GetComponent<Renderer>() != null ? wall.GetComponent<Renderer>().bounds.center : wall.transform.position, 0.5f);
                yield return Wait(1.4f);
                Note("láser de movimiento apagado: " + !wall.activeInHierarchy);
            }
        }

        // ------------------------------------------------------------------ Fase 2: bandera

        static GameObject DispenserSource(string color)
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                if (t.gameObject.scene.IsValid() && t.name == color && t.parent != null && t.parent.name == color + " dispenser") return t.gameObject;
            return null;
        }

        IEnumerator FlyTo(Transform obj, Vector3 to, float duration, float arc)
        {
            var from = obj.position;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / duration);
                obj.position = Vector3.Lerp(from, to, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * arc;
                yield return null;
            }
            obj.position = to;
        }

        IEnumerator CarryCube(FlagPuzzle puzzle, int slotIndex, string color)
        {
            var src = DispenserSource(color);
            var slot = puzzle.slots[slotIndex];
            var go = Object.Instantiate(src, src.transform.position + Vector3.up * 0.12f, src.transform.rotation);
            go.name = color + "(Clone)";
            go.SetActive(true);
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            yield return FlyTo(go.transform, slot.point.transform.position + new Vector3(0f, 0.05f, -0.12f), 0.85f, 0.35f);
            if (rb != null) rb.isKinematic = false;
            slot.point.Place(go.GetComponent<Autohand.Grabbable>());
        }

        IEnumerator FlagPhase()
        {
            var puzzle = Object.FindAnyObjectByType<FlagPuzzle>();
            yield return WalkTo(new Vector3(0.8f, 1.68f, 10.2f));
            var board = new Vector3(-2.2f, 1.15f, 13.4f);
            yield return LookAt(board, 0.6f);
            yield return Wait(0.6f);

            // Un error a propósito: rojo en un hueco que pide amarillo.
            yield return CarryCube(puzzle, 0, "Rojo");
            Note("cubo equivocado (rojo) en " + puzzle.slots[0].point.name);
            yield return Wait(1.6f);

            for (int i = 0; i < puzzle.slots.Count; i++)
            {
                var s = puzzle.slots[i];
                if (s.point.placedObject != null) continue;
                yield return CarryCube(puzzle, i, s.expected.ToString());
                yield return Wait(0.45f);
            }
            yield return Wait(0.8f);
            yield return LookAt(new Vector3(0f, 1.2f, 15.4f), 0.6f);
            yield return Wait(1.4f);
            Note("bandera: haces activos " + puzzle.beams.ActiveCount + ", barrera azul activa " + (puzzle.gate != null && puzzle.gate.activeInHierarchy));
        }

        // ------------------------------------------------------------------ Fase 2b: arañas

        IEnumerator SpiderPhase()
        {
            var spiders = Object.FindObjectsByType<Autohand.Demo.Smash>()
                .Where(s => s.name.Contains("Araña")).OrderBy(s => s.transform.position.x).Take(4).ToList();
            if (spiders.Count == 0) yield break;
            var center = spiders.Aggregate(Vector3.zero, (a, s) => a + s.transform.position) / spiders.Count;
            yield return WalkTo(new Vector3(-0.5f, 1.65f, 11.8f), () => center);
            yield return LookAt(center, 0.4f);

            // Martillo de utilería (copia sin física) para que se vea el golpe.
            Transform hammer = null;
            var src = Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t => t.gameObject.scene.IsValid() && t.parent != null && t.parent.name == "Hammer Dispenser");
            if (src != null)
            {
                hammer = Object.Instantiate(src.gameObject).transform;
                hammer.name = "__martillo_demo";
                foreach (var mb in hammer.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
                foreach (var col in hammer.GetComponentsInChildren<Collider>(true)) col.enabled = false;
                foreach (var body in hammer.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                hammer.gameObject.SetActive(true);
                hammer.rotation = Quaternion.Euler(0f, 90f, 0f);
                hammer.position = _pos + _rot * new Vector3(0.25f, -0.35f, 0.4f);
            }

            foreach (var s in spiders)
            {
                var p = s.transform.position;
                if (hammer != null)
                {
                    yield return FlyTo(hammer, p + Vector3.up * 0.45f, 0.35f, 0.05f);
                    yield return FlyTo(hammer, p + Vector3.up * 0.12f, 0.1f, 0f);
                }
                s.DoSmash();
                if (hammer != null) yield return FlyTo(hammer, p + Vector3.up * 0.4f, 0.2f, 0f);
                yield return Wait(0.35f);
            }
            Note("arañas aplastadas: " + spiders.Count);
            yield return Wait(0.8f);
            if (hammer != null) Destroy(hammer.gameObject);
        }

        // ------------------------------------------------------------------ Fase 2c: canasta

        IEnumerator BasketPhase()
        {
            var hoop = Object.FindAnyObjectByType<HoopDetector>();
            var highlight = Object.FindAnyObjectByType<HoldTargetHighlight>();
            // En Play mode el dispensador oculta el original y crea copias: se usa una copia visible o se crea una.
            var ball = Object.FindObjectsByType<Collider>().FirstOrDefault(c => c.CompareTag("BasketBall") && c.name.StartsWith("Basketball"));
            if (ball == null)
            {
                var src = Resources.FindObjectsOfTypeAll<Collider>().FirstOrDefault(c => c.gameObject.scene.IsValid() && c.name == "Basketball" && c.CompareTag("BasketBall"));
                if (src != null)
                {
                    var copy = Object.Instantiate(src.gameObject, src.transform.position, src.transform.rotation);
                    copy.name = "Basketball(Clone)";
                    copy.SetActive(true);
                    ball = copy.GetComponent<Collider>();
                }
            }
            yield return WalkTo(new Vector3(1.1f, 1.66f, 11.0f), () => ball != null ? ball.transform.position : hoop.transform.position);
            if (ball == null) { Note("sin balón"); yield break; }
            var rb = ball.attachedRigidbody;
            if (rb != null) rb.isKinematic = true;

            // Agarra el balón: se acerca a la mano y el aro se ilumina.
            yield return LookAt(ball.transform.position, 0.4f);
            Vector3 Hand() => _pos + _rot * new Vector3(0.18f, -0.32f, 0.42f);
            yield return FlyTo(ball.transform, Hand(), 0.45f, 0.05f);
            if (highlight != null) highlight.SetHolding(true);
            var hoopPos = hoop.transform.position;
            var to = Quaternion.LookRotation(hoopPos - _pos);
            var from = _rot;
            for (float t = 0; t < 1.3f; t += Time.deltaTime)
            {
                _rot = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / 0.9f));
                ball.transform.position = Hand();
                yield return null;
            }
            // Lanza en arco.
            if (highlight != null) highlight.SetHolding(false);
            yield return FlyTo(ball.transform, hoopPos + Vector3.up * 0.15f, 0.85f, 0.9f);
            typeof(HoopDetector).GetMethod("OnTriggerEnter", Any).Invoke(hoop, new object[] { ball });
            if (rb != null) { rb.isKinematic = false; rb.linearVelocity = Vector3.down * 1.5f; }
            Note("canasta: puntos " + hoop.score + ", láser rojo activo " + (hoop.Pared_Nivel != null && hoop.Pared_Nivel.activeInHierarchy));
            yield return Wait(1.0f);
            yield return LookAt(new Vector3(0f, 1.2f, 15.4f), 0.6f);
            yield return Wait(1.5f);
        }

        // ------------------------------------------------------------------ Fase 3: golem y final

        IEnumerator GolemPhase()
        {
            var carro = GameObject.Find("Carro");
            var golem = carro.transform.Find("Golem");
            var body = carro.GetComponent<Rigidbody>();
            yield return WalkTo(new Vector3(-0.5f, 1.7f, 16.3f), () => golem.position + Vector3.up);
            yield return Wait(0.6f);

            // Centro visual del golem y su desfase respecto al carro.
            Bounds b = new Bounds(golem.position, Vector3.zero);
            foreach (var r in golem.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            Vector3 visual0 = new Vector3(b.center.x, carro.transform.position.y, b.center.z);
            Vector3 offset = carro.transform.position - visual0;
            var rot0 = carro.transform.rotation;
            if (body != null) body.isKinematic = true;

            // Slalom entre las filas de pinchos (z 22,2 / 26,1 / 29,5 / 32,7 / 36,9).
            var pts = new List<Vector3>
            {
                visual0,
                new Vector3(0f, visual0.y, 20.6f),
                new Vector3(0f, visual0.y, 22.2f),
                new Vector3(-3.1f, visual0.y, 26.1f),
                new Vector3(0.2f, visual0.y, 29.5f),
                new Vector3(3.2f, visual0.y, 32.7f),
                new Vector3(0f, visual0.y, 36.9f),
                new Vector3(-0.3f, visual0.y, 40.5f),
                new Vector3(-0.3f, visual0.y, 42.6f),
            };
            var path = SamplePath(pts, 0.1f);
            float yaw = 0f;
            float s = 0f, total = path.Sum(p => p.Item2);
            int seg = 0; float segStart = 0f;
            var detector = Object.FindAnyObjectByType<FinishDetector>();
            var trigger = typeof(FinishDetector).GetMethod("OnTriggerEnter", Any);
            var carroCol = carro.GetComponent<Collider>();
            Vector3 VisualPos() => carro.transform.position - Quaternion.Euler(0f, yaw, 0f) * offset;
            while (s < total)
            {
                s += golemSpeed * Time.deltaTime;
                while (seg < path.Count - 2 && segStart + path[seg + 1].Item2 < s) { segStart += path[seg + 1].Item2; seg++; }
                var a = path[seg].Item1; var c = path[Mathf.Min(seg + 1, path.Count - 1)].Item1;
                float len = Mathf.Max(0.0001f, path[Mathf.Min(seg + 1, path.Count - 1)].Item2);
                var pos = Vector3.Lerp(a, c, Mathf.Clamp01((s - segStart) / len));
                var dir = c - a; dir.y = 0f;
                if (dir.sqrMagnitude > 1e-6f) yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 120f * Time.deltaTime);
                var q = Quaternion.Euler(0f, yaw, 0f);
                var cp = pos + q * offset;
                carro.transform.SetPositionAndRotation(cp, q * rot0);
                if (body != null) { body.position = cp; body.rotation = q * rot0; }
                var want = Quaternion.LookRotation(VisualPos() + Vector3.up * 0.8f - _pos);
                _rot = Quaternion.Slerp(_rot, want, 1f - Mathf.Exp(-5f * Time.deltaTime));
                yield return null;
            }
            trigger.Invoke(detector, new object[] { carroCol });
            Note("golem en la meta");

            // La runa vuela hasta la pared y la pared se hunde.
            var finale = Object.FindAnyObjectByType<TutorialFinale>();
            var wall = finale != null ? finale.doorToOpen : null;
            float wallY0 = wall != null ? wall.transform.position.y : 0f;
            yield return Track(() => VisualPos() + Vector3.up, 1.3f);
            if (finale != null && finale.reward != null) yield return Track(() => finale.reward.position, finale.deliverDuration + 0.4f);
            if (wall != null)
            {
                yield return Track(() => new Vector3(-4.94f, 1.6f, 16.64f), 4.2f);
                Note("pared hacia la mazmorra: activa " + wall.gameObject.activeInHierarchy + ", bajó " + (wallY0 - wall.transform.position.y).ToString("0.0") + " m");
            }
            yield return Wait(1.0f);
        }

        static List<(Vector3, float)> SamplePath(List<Vector3> pts, float step)
        {
            // Catmull-Rom: curva suave que pasa por todos los puntos.
            var outPts = new List<(Vector3, float)>();
            Vector3 prev = pts[0];
            outPts.Add((prev, 0f));
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p0 = pts[Mathf.Max(i - 1, 0)]; var p1 = pts[i]; var p2 = pts[i + 1]; var p3 = pts[Mathf.Min(i + 2, pts.Count - 1)];
                int n = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(p1, p2) / step));
                for (int k = 1; k <= n; k++)
                {
                    float t = k / (float)n;
                    var p = 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
                    outPts.Add((p, Vector3.Distance(prev, p)));
                    prev = p;
                }
            }
            return outPts;
        }

        // ------------------------------------------------------------------ Avisos

        IEnumerator PopupCloser()
        {
            // Los avisos flotantes siguen la cabeza; en el video se cierran solos tras 3 s (como pulsar "Continuar").
            var seen = new Dictionary<GameObject, float>();
            while (!Finished)
            {
                foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                {
                    var go = t.gameObject;
                    if (!go.scene.IsValid() || !go.name.StartsWith("POP-UP") || !go.activeInHierarchy) continue;
                    if (!seen.ContainsKey(go)) seen[go] = Time.time;
                    else if (Time.time - seen[go] > 3f) { go.SetActive(false); seen.Remove(go); }
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
#endif
