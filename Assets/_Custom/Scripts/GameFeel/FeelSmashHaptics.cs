using Autohand.Demo;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Al aplastar algo (tomates, arañas) con el martillo se siente el golpe en el control.
    /// Las arañas tienen respuesta propia: salpicadura verde, sonido extra y un contador flotante "Arañas 3/13".
    /// Se pone en el grupo que contiene los objetos aplastables.
    /// </summary>
    public class FeelSmashHaptics : MonoBehaviour
    {
        [Range(0f, 1f)] public float hapticAmplitude = 0.6f;
        public float hapticDuration = 0.09f;
        public FeelBurst extraBurst;

        [Header("Arañas")]
        [Tooltip("Objetos cuyo nombre contiene este texto reciben la respuesta especial.")]
        public string specialNameContains = "Araña";
        public FeelBurst specialBurst;
        public bool showCounter = true;
        public string counterLabel = "Arañas";
        public Color counterColor = new Color(0.6f, 1f, 0.35f);

        private int _specialTotal;
        private int _specialSmashed;

        private void Start()
        {
            foreach (var smash in GetComponentsInChildren<Smash>(true))
            {
                var target = smash;
                if (IsSpecial(target)) _specialTotal++;
                smash.OnSmash.AddListener(() => HandleSmash(target));
            }
        }

        private bool IsSpecial(Smash smash) => !string.IsNullOrEmpty(specialNameContains) && smash != null && smash.name.Contains(specialNameContains);

        private void HandleSmash(Smash smash)
        {
            Vector3 at = smash != null ? smash.transform.position : transform.position;
            FeelHaptics.PulseNear(at, hapticAmplitude, hapticDuration);
            if (IsSpecial(smash))
            {
                _specialSmashed++;
                if (specialBurst != null) specialBurst.PlayStepAt(at, Mathf.Min(_specialSmashed - 1, 7));
                if (showCounter)
                {
                    string text = _specialSmashed >= _specialTotal
                        ? "¡Todas las " + counterLabel.ToLower() + "!"
                        : counterLabel + " " + _specialSmashed + "/" + _specialTotal;
                    FeelFloatingText.Show(text, at + Vector3.up * 0.3f, counterColor, 1.1f);
                }
            }
            else if (extraBurst != null) extraBurst.PlayAt(at);
        }
    }
}
