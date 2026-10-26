using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    public enum FeelEase { Linear, OutCubic, InCubic, InOutSine, OutBack, InBack, OutElastic }

    /// <summary>
    /// Animaciones con suavizado (tweening/easing): nada aparece, desaparece ni cambia de golpe.
    /// Incluye "punch" de escala (squash & stretch) y sacudida de objetos (en VR se sacude el objeto, nunca la cámara).
    /// </summary>
    public class FeelTween : MonoBehaviour
    {
        private static FeelTween _instance;
        private readonly Dictionary<Transform, Vector3> _baseScales = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Coroutine> _punches = new Dictionary<Transform, Coroutine>();
        private readonly Dictionary<Transform, Vector3> _basePositions = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Coroutine> _shakes = new Dictionary<Transform, Coroutine>();

        public static FeelTween Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[FeelTween]");
                    _instance = go.AddComponent<FeelTween>();
                }
                return _instance;
            }
        }

        public static float Evaluate(FeelEase ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case FeelEase.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case FeelEase.InCubic: return t * t * t;
                case FeelEase.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case FeelEase.OutBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                case FeelEase.InBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return c3 * t * t * t - c1 * t * t;
                }
                case FeelEase.OutElastic:
                {
                    if (t <= 0f || t >= 1f) return t;
                    const float c4 = (2f * Mathf.PI) / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                default: return t;
            }
        }

        /// <summary>Anima un valor de <paramref name="from"/> a <paramref name="to"/>.</summary>
        public static Coroutine Value(float from, float to, float duration, FeelEase ease, Action<float> setter, Action onDone = null)
        {
            return Instance.StartCoroutine(Instance.ValueRoutine(from, to, duration, ease, setter, onDone));
        }

        public static Coroutine ScaleTo(Transform target, Vector3 to, float duration, FeelEase ease, Action onDone = null)
        {
            if (target == null) return null;
            Vector3 from = target.localScale;
            return Value(0f, 1f, duration, ease, k => { if (target) target.localScale = Vector3.LerpUnclamped(from, to, k); }, onDone);
        }

        public static Coroutine MoveTo(Transform target, Vector3 worldTo, float duration, FeelEase ease, Action onDone = null)
        {
            if (target == null) return null;
            Vector3 from = target.position;
            return Value(0f, 1f, duration, ease, k => { if (target) target.position = Vector3.LerpUnclamped(from, worldTo, k); }, onDone);
        }

        /// <summary>Aparece con un pequeño rebote (escala 0 → escala original con OutBack).</summary>
        public static Coroutine PopIn(Transform target, float duration = 0.35f)
        {
            if (target == null) return null;
            Vector3 baseScale = Instance.BaseScale(target);
            target.localScale = baseScale * 0.01f;
            return ScaleTo(target, baseScale, duration, FeelEase.OutBack);
        }

        /// <summary>Squash &amp; stretch: un "golpe" de escala que rebota y vuelve a su tamaño.</summary>
        public static void Punch(Transform target, float amount = 0.15f, float duration = 0.3f)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;
            var inst = Instance;
            Vector3 baseScale = inst.BaseScale(target);
            if (inst._punches.TryGetValue(target, out var running) && running != null) inst.StopCoroutine(running);
            inst._punches[target] = inst.StartCoroutine(inst.PunchRoutine(target, baseScale, amount, duration));
        }

        /// <summary>Sacude un objeto (no la cámara) con amplitud decreciente, en metros del mundo.</summary>
        public static void Shake(Transform target, float amplitude = 0.02f, float duration = 0.3f, float frequency = 35f)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;
            var inst = Instance;
            if (!inst._basePositions.ContainsKey(target) || !inst._shakes.ContainsKey(target) || inst._shakes[target] == null)
                inst._basePositions[target] = target.localPosition;
            if (inst._shakes.TryGetValue(target, out var running) && running != null) inst.StopCoroutine(running);
            inst._shakes[target] = inst.StartCoroutine(inst.ShakeRoutine(target, inst._basePositions[target], amplitude, duration, frequency));
        }

        private Vector3 BaseScale(Transform target)
        {
            // La escala base se actualiza solo si no hay un golpe en curso y el objeto no está encogido a cero.
            bool punching = _punches.TryGetValue(target, out var running) && running != null;
            bool known = _baseScales.ContainsKey(target);
            bool collapsed = target.localScale.sqrMagnitude < 0.0001f;
            if (!known || (!punching && !collapsed))
                _baseScales[target] = collapsed && known ? _baseScales[target] : target.localScale;
            return _baseScales[target];
        }

        private IEnumerator ValueRoutine(float from, float to, float duration, FeelEase ease, Action<float> setter, Action onDone)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Evaluate(ease, duration <= 0f ? 1f : t / duration);
                setter?.Invoke(Mathf.LerpUnclamped(from, to, k));
                yield return null;
            }
            setter?.Invoke(to);
            onDone?.Invoke();
        }

        private IEnumerator PunchRoutine(Transform target, Vector3 baseScale, float amount, float duration)
        {
            float t = 0f;
            while (t < duration && target != null)
            {
                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / duration);
                // Oscilación amortiguada: crece, se aplasta un poco y se asienta.
                float wave = Mathf.Sin(n * Mathf.PI * 3f) * (1f - n) * (1f - n);
                float stretch = 1f + amount * wave;
                float squash = 1f - amount * 0.5f * wave;
                target.localScale = new Vector3(baseScale.x * squash, baseScale.y * stretch, baseScale.z * squash);
                yield return null;
            }
            if (target != null) target.localScale = baseScale;
            _punches[target] = null;
        }

        private IEnumerator ShakeRoutine(Transform target, Vector3 basePos, float amplitude, float duration, float frequency)
        {
            float t = 0f;
            float seed = UnityEngine.Random.value * 100f;
            while (t < duration && target != null)
            {
                t += Time.deltaTime;
                float decay = 1f - Mathf.Clamp01(t / duration);
                float time = t * frequency;
                var offset = new Vector3(
                    (Mathf.PerlinNoise(seed, time) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(seed + 10f, time) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(seed + 20f, time) - 0.5f) * 2f) * amplitude * decay;
                // La amplitud está en metros del mundo; se convierte al espacio local del padre.
                Vector3 local = target.parent != null ? target.parent.InverseTransformVector(offset) : offset;
                target.localPosition = basePos + local;
                yield return null;
            }
            if (target != null) target.localPosition = basePos;
            _shakes[target] = null;
        }
    }
}
