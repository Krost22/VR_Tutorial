using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Los avisos aparecen con un rebote de escala, un "pop" y un toque de vibración en vez de aparecer de golpe.
    /// </summary>
    public class FeelPopup : MonoBehaviour
    {
        public Transform target;
        public float duration = 0.4f;
        public AudioClip sound;
        [Range(0f, 1f)] public float volume = 0.6f;
        [Range(0f, 1f)] public float hapticAmplitude = 0.15f;

        private Vector3 _baseScale;
        private bool _initialized;

        private void Awake() => Init();

        private void Init()
        {
            if (_initialized) return;
            if (target == null) target = transform;
            _baseScale = target.localScale;
            _initialized = true;
        }

        private void OnEnable()
        {
            Init();
            target.localScale = _baseScale * 0.01f;
            FeelTween.ScaleTo(target, _baseScale, duration, FeelEase.OutBack);
            if (sound != null) FeelAudio.Play(sound, target.position, volume, Random.Range(0.97f, 1.03f), 0.5f);
            FeelHaptics.Pulse(hapticAmplitude, 0.05f);
        }

        private void OnDisable()
        {
            if (_initialized && target != null) target.localScale = _baseScale;
        }
    }
}
