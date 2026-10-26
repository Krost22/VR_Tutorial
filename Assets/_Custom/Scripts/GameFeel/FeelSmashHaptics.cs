using Autohand.Demo;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Al aplastar algo (tomates, arañas) con el martillo se siente el golpe en el control y el objeto rebota.
    /// Se pone en el grupo que contiene los objetos aplastables.
    /// </summary>
    public class FeelSmashHaptics : MonoBehaviour
    {
        [Range(0f, 1f)] public float hapticAmplitude = 0.6f;
        public float hapticDuration = 0.09f;
        public FeelBurst extraBurst;

        private void Start()
        {
            foreach (var smash in GetComponentsInChildren<Smash>(true))
            {
                var target = smash;
                smash.OnSmash.AddListener(() => HandleSmash(target));
            }
        }

        private void HandleSmash(Smash smash)
        {
            Vector3 at = smash != null ? smash.transform.position : transform.position;
            FeelHaptics.PulseNear(at, hapticAmplitude, hapticDuration);
            if (extraBurst != null) extraBurst.PlayAt(at);
        }
    }
}
