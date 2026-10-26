using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Movimiento secundario en reposo: flota, gira y "respira" con luz. Atrae la mirada hacia lo importante
    /// (punto de interés) sin mover la cámara del jugador. Solo para objetos sin física.
    /// </summary>
    public class FeelIdleMotion : MonoBehaviour
    {
        [Header("Flotar")]
        public float bobHeight = 0.08f;
        public float bobSpeed = 1.6f;

        [Header("Girar")]
        public float spinDegreesPerSecond = 35f;

        [Header("Respirar (escala)")]
        public float breatheAmount = 0.03f;
        public float breatheSpeed = 2.2f;

        [Header("Luz que pulsa")]
        public Light pulseLight;
        public float lightMin = 0.6f;
        public float lightMax = 1.6f;
        public float lightSpeed = 2.5f;

        private Vector3 _basePos;
        private Vector3 _baseScale;
        private float _phase;

        private void OnEnable()
        {
            _basePos = transform.localPosition;
            _baseScale = transform.localScale;
            _phase = Random.value * 10f;
        }

        private void OnDisable()
        {
            transform.localPosition = _basePos;
            transform.localScale = _baseScale;
        }

        /// <summary>Fija la posición actual como nuevo centro del flotado.</summary>
        public void Rebase()
        {
            _basePos = transform.localPosition;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            float time = Time.time + _phase;
            Vector3 worldOffset = Vector3.up * Mathf.Sin(time * bobSpeed) * bobHeight;
            transform.localPosition = _basePos + (transform.parent != null ? transform.parent.InverseTransformVector(worldOffset) : worldOffset);
            transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
            if (breatheAmount > 0f) transform.localScale = _baseScale * (1f + Mathf.Sin(time * breatheSpeed) * breatheAmount);
            if (pulseLight != null) pulseLight.intensity = Mathf.Lerp(lightMin, lightMax, (Mathf.Sin(time * lightSpeed) + 1f) * 0.5f);
        }
    }
}
