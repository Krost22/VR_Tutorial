using System.Collections.Generic;
using Autohand;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Ilumina un objetivo (el borde superior del aro) mientras el jugador sostiene el objeto que va ahí (el balón).
    /// Indica "lánzalo aquí" justo cuando importa, sin texto.
    /// </summary>
    public class HoldTargetHighlight : MonoBehaviour
    {
        [Tooltip("El objeto sostenido debe contener este texto en su nombre.")]
        public string heldNameContains = "Basketball";
        public Renderer glow;
        public Color glowColor = new Color(1f, 0.75f, 0.25f, 1f);
        public Light glowLight;
        public float lightIntensity = 2.5f;
        public float fadeSpeed = 6f;
        public AudioClip onSound;
        [Range(0f, 1f)] public float volume = 0.4f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private readonly List<Hand> _hands = new List<Hand>();
        private MaterialPropertyBlock _block;
        private int _holding;
        private float _level;

        private void Start()
        {
            _block = new MaterialPropertyBlock();
            foreach (var hand in FindObjectsByType<Hand>(FindObjectsInactive.Include))
            {
                if (hand.GetComponent<HandProjector>() != null) continue;
                hand.OnGrabbed += HandleGrab;
                hand.OnReleased += HandleRelease;
                _hands.Add(hand);
            }
            Apply(0f);
        }

        private void OnDestroy()
        {
            foreach (var hand in _hands)
            {
                if (hand == null) continue;
                hand.OnGrabbed -= HandleGrab;
                hand.OnReleased -= HandleRelease;
            }
        }

        private bool Matches(Grabbable grab) => grab != null && grab.name.Contains(heldNameContains);

        private void HandleGrab(Hand hand, Grabbable grab)
        {
            if (!Matches(grab)) return;
            _holding++;
            if (_holding == 1 && onSound != null) FeelAudio.Play(onSound, transform.position, volume, 1.2f);
        }

        private void HandleRelease(Hand hand, Grabbable grab)
        {
            if (!Matches(grab)) return;
            _holding = Mathf.Max(0, _holding - 1);
        }

        /// <summary>Para probar o forzar el resaltado desde otros scripts.</summary>
        public void SetHolding(bool holding) => _holding = holding ? 1 : 0;

        private void Update()
        {
            float target = _holding > 0 ? 1f : 0f;
            _level = Mathf.MoveTowards(_level, target, Time.deltaTime * fadeSpeed);
            float pulse = _holding > 0 ? 0.8f + 0.2f * Mathf.Sin(Time.time * 5f) : 1f;
            Apply(_level * pulse);
        }

        private void Apply(float level)
        {
            if (glow != null)
            {
                glow.enabled = level > 0.001f;
                glow.GetPropertyBlock(_block);
                var c = glowColor; c.a *= level;
                _block.SetColor(BaseColorId, c);
                glow.SetPropertyBlock(_block);
            }
            if (glowLight != null)
            {
                glowLight.enabled = level > 0.001f;
                glowLight.intensity = lightIntensity * level;
            }
        }
    }
}
