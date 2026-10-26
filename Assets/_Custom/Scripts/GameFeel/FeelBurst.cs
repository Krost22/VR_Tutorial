using System.Collections;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// "Paquete" de respuesta para un evento: sonido + partículas + destello de luz + vibración + rebote/sacudida.
    /// Se llama desde scripts o desde eventos del Inspector con Play(), PlayStep(n) o PlayAt(posición).
    /// Para quitar un efecto basta con dejar vacío su campo o poner su intensidad en 0.
    /// </summary>
    public class FeelBurst : MonoBehaviour
    {
        [Tooltip("Punto donde ocurre el efecto. Si está vacío se usa este objeto.")]
        public Transform origin;

        [Header("Sonido")]
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.8f;
        [Tooltip("Tono aleatorio para que el sonido no se repita igual.")]
        public Vector2 pitchRange = new Vector2(0.96f, 1.04f);
        [Tooltip("Con PlayStep(n) el tono sube por la escala mayor: da sensación de progreso.")]
        public bool pitchRisesWithStep = true;
        [Range(0f, 1f)] public float spatialBlend = 0.85f;
        [Tooltip("Segunda capa de sonido opcional (ej. red + campanita al encestar).")]
        public AudioClip layerClip;
        [Range(0f, 1f)] public float layerVolume = 0.6f;

        [Header("Partículas")]
        public ParticleSystem particlesPrefab;
        public Vector3 particleOffset;
        public bool tintParticles;
        public Color particleTint = Color.white;
        public float particleScale = 1f;

        [Header("Destello de luz")]
        [Tooltip("0 = sin destello.")]
        public float flashIntensity = 0f;
        public Color flashColor = new Color(1f, 0.85f, 0.6f);
        public float flashRange = 4f;
        public float flashDuration = 0.35f;

        [Header("Vibración (háptica)")]
        [Range(0f, 1f)] public float hapticAmplitude = 0.3f;
        public float hapticDuration = 0.08f;
        [Tooltip("Usa el patrón 'ta-tán' de logro en vez de un pulso simple.")]
        public bool successPattern;

        [Header("Rebote (squash & stretch)")]
        public Transform punchTarget;
        public float punchAmount = 0.15f;
        public float punchDuration = 0.3f;

        [Header("Sacudida del objeto")]
        public Transform shakeTarget;
        public float shakeAmplitude = 0.02f;
        public float shakeDuration = 0.25f;

        public void Play() => PlayInternal(OriginPosition, -1);

        /// <summary>Reproduce con el tono de la nota número <paramref name="step"/> (0, 1, 2...).</summary>
        public void PlayStep(int step) => PlayInternal(OriginPosition, step);

        public void PlayAt(Vector3 position) => PlayInternal(position, -1);

        public void PlayStepAt(Vector3 position, int step) => PlayInternal(position, step);

        /// <summary>Para eventos del Inspector: reproduce en la posición de otro objeto.</summary>
        public void PlayAtTransform(Transform target) => PlayInternal(target != null ? target.position : OriginPosition, -1);

        private Vector3 OriginPosition => (origin != null ? origin : transform).position;

        private void PlayInternal(Vector3 position, int step)
        {
            if (clip != null)
            {
                float pitch = Random.Range(pitchRange.x, pitchRange.y);
                if (step >= 0 && pitchRisesWithStep) pitch *= FeelAudio.StepToPitch(step);
                FeelAudio.Play(clip, position, volume, pitch, spatialBlend);
            }
            if (layerClip != null) FeelAudio.PlayVaried(layerClip, position, layerVolume, pitchRange, spatialBlend);

            if (particlesPrefab != null) SpawnParticles(position);

            if (flashIntensity > 0f) FeelTween.Instance.StartCoroutine(Flash(position));

            if (hapticAmplitude > 0f)
            {
                if (successPattern && isActiveAndEnabled) FeelHaptics.SuccessPattern(this, hapticAmplitude);
                else FeelHaptics.PulseNear(position, hapticAmplitude, hapticDuration);
            }

            if (punchTarget != null) FeelTween.Punch(punchTarget, punchAmount, punchDuration);
            if (shakeTarget != null) FeelTween.Shake(shakeTarget, shakeAmplitude, shakeDuration);
        }

        private void SpawnParticles(Vector3 position)
        {
            var ps = Instantiate(particlesPrefab, position + particleOffset, Quaternion.identity);
            if (!Mathf.Approximately(particleScale, 1f)) ps.transform.localScale *= particleScale;
            float maxLifetime = 0f;
            foreach (var child in ps.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = child.main;
                if (tintParticles) main.startColor = particleTint;
                if (!Mathf.Approximately(particleScale, 1f)) main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                maxLifetime = Mathf.Max(maxLifetime, main.duration + main.startLifetime.constantMax);
            }
            ps.Play(true);
            Destroy(ps.gameObject, Mathf.Max(1f, maxLifetime + 0.5f));
        }

        private IEnumerator Flash(Vector3 position)
        {
            var go = new GameObject("[FeelFlash]");
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = flashColor;
            light.range = flashRange;
            light.shadows = LightShadows.None;
            light.intensity = flashIntensity;
            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                light.intensity = flashIntensity * (1f - FeelTween.Evaluate(FeelEase.OutCubic, t / flashDuration));
                yield return null;
            }
            Destroy(go);
        }
    }
}
