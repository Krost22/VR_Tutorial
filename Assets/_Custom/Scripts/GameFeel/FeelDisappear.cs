using System.Collections;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Hace que un objeto (muro láser, pared secreta, puerta) desaparezca con anticipación y animación
    /// en vez de apagarse de golpe: tiembla/parpadea, suena, suelta partículas y se encoge o se hunde.
    /// Si se quita este componente, los scripts vuelven a apagar el objeto al instante como antes.
    /// </summary>
    public class FeelDisappear : MonoBehaviour
    {
        public enum Mode { ShrinkVertical, ShrinkAll, SinkDown }

        public Mode mode = Mode.ShrinkVertical;

        [Header("Anticipación")]
        [Tooltip("Tiempo que tiembla/parpadea antes de irse.")]
        public float anticipation = 0.3f;
        public float shakeAmplitude = 0.02f;
        [Tooltip("Parpadeo de los renderers durante la anticipación (bueno para láseres).")]
        public bool flicker = true;

        [Header("Salida")]
        public float duration = 0.45f;
        [Tooltip("Metros que baja en modo SinkDown.")]
        public float sinkDistance = 3f;

        [Header("Respuesta")]
        public AudioClip sound;
        [Range(0f, 1f)] public float volume = 0.8f;
        public ParticleSystem particlesPrefab;
        public Color particleTint = Color.white;
        public bool tintParticles;
        [Range(0f, 1f)] public float hapticAmplitude = 0.25f;
        public float hapticDuration = 0.15f;

        private bool _hiding;

        /// <summary>Apaga el objeto con animación si tiene este componente; si no, lo apaga al instante.</summary>
        public static void HideOrDeactivate(GameObject target)
        {
            if (target == null) return;
            var feel = target.GetComponent<FeelDisappear>();
            if (feel != null && feel.enabled && target.activeInHierarchy) feel.Hide();
            else target.SetActive(false);
        }

        public void Hide()
        {
            if (_hiding || !gameObject.activeInHierarchy) return;
            StartCoroutine(HideRoutine());
        }

        private IEnumerator HideRoutine()
        {
            _hiding = true;
            var t = transform;
            Vector3 baseScale = t.localScale;
            Vector3 basePos = t.localPosition;
            Vector3 center = WorldCenter();
            var renderers = GetComponentsInChildren<Renderer>();
            var wasEnabled = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) wasEnabled[i] = renderers[i].enabled;

            if (sound != null) FeelAudio.PlayVaried(sound, center, volume, new Vector2(0.95f, 1.05f));
            FeelHaptics.PulseNear(center, hapticAmplitude, hapticDuration);

            // 1) Anticipación: temblor + parpadeo.
            float elapsed = 0f;
            while (elapsed < anticipation)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - elapsed / Mathf.Max(0.0001f, anticipation);
                Vector3 jitter = Random.insideUnitSphere * shakeAmplitude * (0.4f + 0.6f * decay);
                t.localPosition = basePos + (t.parent != null ? t.parent.InverseTransformVector(jitter) : jitter);
                if (flicker)
                {
                    bool on = Mathf.Repeat(elapsed * 18f, 1f) > 0.35f;
                    for (int i = 0; i < renderers.Length; i++) if (renderers[i]) renderers[i].enabled = on && wasEnabled[i];
                }
                yield return null;
            }
            t.localPosition = basePos;
            for (int i = 0; i < renderers.Length; i++) if (renderers[i]) renderers[i].enabled = wasEnabled[i];

            if (particlesPrefab != null)
            {
                var ps = Instantiate(particlesPrefab, center, Quaternion.identity);
                if (tintParticles)
                    foreach (var child in ps.GetComponentsInChildren<ParticleSystem>(true)) { var main = child.main; main.startColor = particleTint; }
                ps.Play(true);
                Destroy(ps.gameObject, 4f);
            }

            // 2) Salida con easing (acelera al final, como si se lo tragara el suelo).
            elapsed = 0f;
            Vector3 worldDown = Vector3.down * sinkDistance;
            Vector3 localDown = t.parent != null ? t.parent.InverseTransformVector(worldDown) : worldDown;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = FeelTween.Evaluate(FeelEase.InBack, elapsed / Mathf.Max(0.0001f, duration));
                switch (mode)
                {
                    case Mode.ShrinkVertical:
                        t.localScale = new Vector3(baseScale.x, baseScale.y * Mathf.Max(0f, 1f - k), baseScale.z);
                        break;
                    case Mode.ShrinkAll:
                        t.localScale = baseScale * Mathf.Max(0f, 1f - k);
                        break;
                    case Mode.SinkDown:
                        t.localPosition = basePos + localDown * Mathf.Clamp01(k);
                        if (Mathf.Repeat(elapsed * 25f, 1f) < 0.5f) t.localPosition += t.parent != null ? t.parent.InverseTransformVector(Random.insideUnitSphere * shakeAmplitude * 0.5f) : Random.insideUnitSphere * shakeAmplitude * 0.5f;
                        break;
                }
                yield return null;
            }

            // Se restaura la forma original (en el mismo frame, no se ve) por si alguien vuelve a activar el objeto.
            t.localScale = baseScale;
            t.localPosition = basePos;
            _hiding = false;
            gameObject.SetActive(false);
        }

        private Vector3 WorldCenter()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return transform.position;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }
    }
}
