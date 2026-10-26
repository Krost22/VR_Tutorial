using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Movimiento secundario al desplazarse: el golem "da pasos" (sube/baja y se inclina) y cada paso suena.
    /// Va en el modelo visual (hijo) y lee la velocidad del cuerpo físico (padre).
    /// </summary>
    public class FeelMotionBob : MonoBehaviour
    {
        public Rigidbody body;
        [Tooltip("Velocidad (m/s) a partir de la cual se considera que camina.")]
        public float minSpeed = 0.08f;
        public float stepHeight = 0.06f;
        public float stepsPerMeter = 1.6f;
        public float leanDegrees = 6f;

        [Header("Pisadas")]
        public AudioClip stepSound;
        [Range(0f, 1f)] public float stepVolume = 0.35f;

        private Vector3 _baseLocalPos;
        private Quaternion _baseLocalRot;
        private Vector3 _lastBodyPos;
        private float _cycle;
        private float _blend;
        private bool _stepDownPlayed;

        private void Start()
        {
            if (body == null) body = GetComponentInParent<Rigidbody>();
            _baseLocalPos = transform.localPosition;
            _baseLocalRot = transform.localRotation;
            if (body != null) _lastBodyPos = body.position;
        }

        private void LateUpdate()
        {
            if (body == null || Time.deltaTime <= 0f) return;
            Vector3 delta = body.position - _lastBodyPos;
            _lastBodyPos = body.position;
            delta.y = 0f;
            float speed = delta.magnitude / Time.deltaTime;

            bool walking = speed > minSpeed;
            _blend = Mathf.MoveTowards(_blend, walking ? 1f : 0f, Time.deltaTime * 4f);
            if (walking) _cycle += delta.magnitude * stepsPerMeter * Mathf.PI * 2f;

            float bob = Mathf.Abs(Mathf.Sin(_cycle * 0.5f)) * stepHeight * _blend;
            Vector3 worldOffset = Vector3.up * bob;
            transform.localPosition = _baseLocalPos + (transform.parent != null ? transform.parent.InverseTransformVector(worldOffset) : worldOffset);

            // Inclinación hacia donde avanza (follow-through).
            Vector3 localDir = body.transform.InverseTransformDirection(delta.normalized);
            float lean = leanDegrees * _blend * Mathf.Clamp(localDir.y != 0 ? Mathf.Sign(localDir.y) : 0f, -1f, 1f);
            transform.localRotation = _baseLocalRot * Quaternion.Euler(lean, 0f, Mathf.Sin(_cycle * 0.5f) * 2f * _blend);

            // Pisada: cuando el ciclo toca el suelo.
            bool down = Mathf.Abs(Mathf.Sin(_cycle * 0.5f)) < 0.15f;
            if (down && !_stepDownPlayed && _blend > 0.5f && stepSound != null)
            {
                FeelAudio.PlayVaried(stepSound, transform.position, stepVolume, new Vector2(0.85f, 1.05f));
                _stepDownPlayed = true;
            }
            else if (!down) _stepDownPlayed = false;
        }
    }
}
