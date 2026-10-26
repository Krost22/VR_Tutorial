using System.Collections;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Brillo (emisión) controlable: sube mientras se carga una acción, destella al completarla
    /// y se queda encendido como indicador permanente de "esto ya está resuelto".
    /// </summary>
    public class FeelGlow : MonoBehaviour
    {
        public Renderer targetRenderer;
        [ColorUsage(false, true)] public Color glowColor = Color.white;
        public float maxIntensity = 2.5f;
        [Tooltip("Nivel que se queda encendido después de MarkSolved().")]
        [Range(0f, 1f)] public float solvedLevel = 0.55f;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private Material _material;
        private float _persistent;
        private Coroutine _flash;

        private void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer != null)
            {
                // Copia del material solo en tiempo de juego: el material del proyecto no se toca.
                _material = targetRenderer.material;
                _material.EnableKeyword("_EMISSION");
                _material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            Apply(0f);
        }

        /// <summary>Nivel de brillo 0..1 (no baja del nivel permanente).</summary>
        public void SetLevel(float level)
        {
            if (_flash != null) return;
            Apply(Mathf.Max(level, _persistent));
        }

        public void Flash(float duration = 0.6f)
        {
            if (!isActiveAndEnabled) return;
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashRoutine(duration));
        }

        public void MarkSolved()
        {
            _persistent = solvedLevel;
            Flash();
        }

        public void ResetGlow()
        {
            _persistent = 0f;
            Apply(0f);
        }

        private IEnumerator FlashRoutine(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = FeelTween.Evaluate(FeelEase.OutCubic, t / duration);
                Apply(Mathf.Lerp(1.6f, _persistent, k));
                yield return null;
            }
            Apply(_persistent);
            _flash = null;
        }

        private void Apply(float level)
        {
            if (_material != null) _material.SetColor(EmissionId, glowColor * (maxIntensity * level));
        }
    }
}
