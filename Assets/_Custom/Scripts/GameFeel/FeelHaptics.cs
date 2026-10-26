using System.Collections;
using System.Collections.Generic;
using Autohand;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Vibración de los controles (AutoHand) sincronizada con el sonido y lo visual.
    /// Técnica de game feel: "háptica" — confirma con el tacto lo que el jugador ve y oye.
    /// </summary>
    public static class FeelHaptics
    {
        /// <summary>Interruptor global por si se quiere desactivar toda la vibración añadida.</summary>
        public static bool Enabled = true;

        private static readonly List<Hand> _hands = new List<Hand>();
        private static int _cachedFrame = -1;

        /// <summary>Vibra ambas manos activas.</summary>
        public static void Pulse(float amplitude, float duration)
        {
            if (!Enabled || amplitude <= 0f || duration <= 0f) return;
            foreach (var hand in ActiveHands())
                hand.PlayHapticVibration(duration, Mathf.Clamp01(amplitude));
        }

        /// <summary>Vibra más fuerte la mano más cercana a un punto y más suave la otra.</summary>
        public static void PulseNear(Vector3 point, float amplitude, float duration)
        {
            if (!Enabled || amplitude <= 0f || duration <= 0f) return;
            Hand closest = null;
            float best = float.MaxValue;
            var hands = ActiveHands();
            foreach (var hand in hands)
            {
                float d = (hand.transform.position - point).sqrMagnitude;
                if (d < best) { best = d; closest = hand; }
            }
            foreach (var hand in hands)
                hand.PlayHapticVibration(duration, Mathf.Clamp01(hand == closest ? amplitude : amplitude * 0.4f));
        }

        /// <summary>Patrón "ta-tán" para logros: dos pulsos, el segundo más fuerte.</summary>
        public static void SuccessPattern(MonoBehaviour host, float amplitude = 0.5f)
        {
            if (!Enabled || host == null || !host.isActiveAndEnabled) return;
            host.StartCoroutine(Pattern(new[] { amplitude * 0.6f, amplitude }, new[] { 0.06f, 0.14f }, 0.09f));
        }

        /// <summary>Patrón de error: dos pulsos cortos y secos (guía de Meta: errores con un zumbido distinto).</summary>
        public static void ErrorPattern(MonoBehaviour host, float amplitude = 0.45f)
        {
            if (!Enabled || host == null || !host.isActiveAndEnabled) return;
            host.StartCoroutine(Pattern(new[] { amplitude, amplitude }, new[] { 0.05f, 0.05f }, 0.07f));
        }

        private static IEnumerator Pattern(float[] amps, float[] durations, float gap)
        {
            for (int i = 0; i < amps.Length; i++)
            {
                Pulse(amps[i], durations[i]);
                yield return new WaitForSeconds(durations[i] + gap);
            }
        }

        private static List<Hand> ActiveHands()
        {
            if (_cachedFrame == Time.frameCount) return _hands;
            _cachedFrame = Time.frameCount;
            _hands.Clear();
            foreach (var hand in Object.FindObjectsByType<Hand>())
            {
                // Solo manos reales activas (se excluyen proyecciones de agarre/resaltado).
                if (hand.isActiveAndEnabled && hand.GetComponent<HandProjector>() == null)
                    _hands.Add(hand);
            }
            return _hands;
        }
    }
}
