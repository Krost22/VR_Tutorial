using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Respuesta a choques físicos: golpe sordo, polvo y una sacudida del modelo, proporcional a la fuerza.
    /// Ignora el contacto con el suelo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FeelImpact : MonoBehaviour
    {
        public float minImpactSpeed = 0.25f;
        public float maxImpactSpeed = 2.5f;
        public float cooldown = 0.25f;
        [Tooltip("Se ignoran contactos cuya normal apunta hacia arriba (suelo).")]
        public float floorNormalThreshold = 0.7f;

        public AudioClip sound;
        [Range(0f, 1f)] public float maxVolume = 0.8f;
        public ParticleSystem particlesPrefab;
        public Transform shakeTarget;
        public float shakeAmplitude = 0.04f;
        [Range(0f, 1f)] public float hapticAmplitude = 0.2f;

        private float _lastTime = -10f;

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.time - _lastTime < cooldown || collision.contactCount == 0) return;
            var contact = collision.GetContact(0);
            if (contact.normal.y > floorNormalThreshold) return;

            float speed = collision.relativeVelocity.magnitude;
            if (speed < minImpactSpeed) return;
            _lastTime = Time.time;

            float k = Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, speed);
            if (sound != null) FeelAudio.Play(sound, contact.point, Mathf.Lerp(0.3f, 1f, k) * maxVolume, Random.Range(0.85f, 1.05f));
            if (particlesPrefab != null)
            {
                var ps = Instantiate(particlesPrefab, contact.point, Quaternion.identity);
                ps.Play(true);
                Destroy(ps.gameObject, 3f);
            }
            if (shakeTarget != null) FeelTween.Shake(shakeTarget, shakeAmplitude * Mathf.Lerp(0.4f, 1f, k), 0.25f);
            FeelHaptics.Pulse(hapticAmplitude * Mathf.Lerp(0.4f, 1f, k), 0.06f);
        }
    }
}
