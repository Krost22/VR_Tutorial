using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Cierre del tutorial: el golem llega a la meta y consigue la antorcha.
    /// Secuencia: la antorcha se enciende → confeti y fanfarria → la antorcha vuela hasta el golem →
    /// aviso de tutorial completado → fundido a negro → carga la mazmorra (1_GAME).
    /// </summary>
    public class TutorialFinale : MonoBehaviour
    {
        [Header("Recompensa (antorcha)")]
        public Transform reward;
        public FeelIdleMotion rewardIdle;
        public ParticleSystem rewardFlames;
        public Light rewardLight;
        public AudioSource rewardAudio;
        public Color litColor = new Color(1f, 0.62f, 0.28f);
        public float litIntensity = 3f;
        public FeelBurst igniteBurst;

        [Header("El golem la recoge")]
        public Transform carrier;
        [Tooltip("Posición sobre el golem (metros, en el mundo).")]
        public Vector3 carryOffset = new Vector3(0f, 1.9f, 0f);
        public float flyDuration = 0.9f;

        [Header("Celebración")]
        public FeelBurst celebration;
        public GameObject completedPopup;

        [Header("Abrir el paso (la recompensa vuela hasta la pared y la abre)")]
        public Transform deliverTarget;
        [Tooltip("Segundos que el golem sostiene la recompensa antes de que salga volando.")]
        public float carryTime = 1.2f;
        public float deliverDuration = 2.4f;
        public float arcHeight = 2.5f;
        public FeelBurst deliverBurst;
        public FeelDisappear doorToOpen;

        [Header("Siguiente escena")]
        [Tooltip("Si está activo, carga nextScene sola tras delayBeforeLoad. Si no, se entra caminando (ScenePortal o pasillo).")]
        public bool loadAutomatically = false;
        public string nextScene = "1_GAME";
        [Tooltip("Segundos desde que llega a la meta hasta empezar el fundido.")]
        public float delayBeforeLoad = 7f;
        public SceneFader fader;

        public UnityEvent onFinished;

        private bool _played;

        public void Play()
        {
            if (_played) return;
            _played = true;
            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            float start = Time.time;

            // 1) La antorcha se enciende (anticipación → recompensa).
            if (reward != null && igniteBurst != null) igniteBurst.PlayAt(reward.position);
            if (rewardFlames != null) rewardFlames.Play(true);
            if (rewardAudio != null) rewardAudio.Play();
            if (rewardLight != null)
            {
                Color fromColor = rewardLight.color;
                float fromIntensity = rewardLight.intensity;
                FeelTween.Value(0f, 1f, 0.6f, FeelEase.OutCubic, k =>
                {
                    if (rewardLight == null) return;
                    rewardLight.color = Color.Lerp(fromColor, litColor, k);
                    rewardLight.intensity = Mathf.Lerp(fromIntensity, litIntensity, k);
                });
            }
            if (rewardIdle != null) rewardIdle.pulseLight = null;
            yield return new WaitForSeconds(0.35f);

            // 2) Celebración.
            if (celebration != null) celebration.Play();
            if (completedPopup != null) completedPopup.SetActive(true);
            yield return new WaitForSeconds(0.6f);

            // 3) La antorcha vuela hasta el golem y se queda con él.
            if (reward != null && carrier != null)
            {
                if (rewardIdle != null) rewardIdle.enabled = false;
                Vector3 target = carrier.position + carryOffset;
                bool arrived = false;
                FeelTween.MoveTo(reward, target, flyDuration, FeelEase.InOutSine, () => arrived = true);
                while (!arrived) yield return null;
                reward.SetParent(carrier, true);
                FeelTween.Punch(reward, 0.25f, 0.35f);
                FeelHaptics.Pulse(0.3f, 0.1f);
            }

            // 4) La recompensa vuela en arco hasta la pared marcada y la abre.
            if (reward != null && deliverTarget != null)
            {
                yield return new WaitForSeconds(carryTime);
                reward.SetParent(null, true);
                Vector3 from = reward.position;
                Vector3 to = deliverTarget.position;
                Vector3 control = (from + to) * 0.5f + Vector3.up * arcHeight;
                float t = 0f;
                while (t < deliverDuration)
                {
                    t += Time.deltaTime;
                    float k = FeelTween.Evaluate(FeelEase.InOutSine, t / deliverDuration);
                    // Curva de Bézier cuadrática: sube y cae sobre la pared.
                    Vector3 a = Vector3.Lerp(from, control, k);
                    Vector3 b = Vector3.Lerp(control, to, k);
                    reward.position = Vector3.Lerp(a, b, k);
                    yield return null;
                }
                reward.position = to;
                FeelTween.Punch(reward, 0.35f, 0.4f);
                if (deliverBurst != null) deliverBurst.PlayAt(to);
                if (doorToOpen != null) doorToOpen.Hide();
                if (rewardIdle != null) { rewardIdle.enabled = true; rewardIdle.Rebase(); }
            }

            onFinished?.Invoke();
            if (!loadAutomatically || string.IsNullOrEmpty(nextScene)) yield break;

            // 5) Fundido y carga de la siguiente escena (solo si se pidió carga automática).
            float wait = delayBeforeLoad - (Time.time - start);
            if (wait > 0f) yield return new WaitForSeconds(wait);
            var activeFader = fader != null ? fader : SceneFader.Instance;
            if (activeFader != null) activeFader.FadeAndLoad(nextScene);
            else SceneManager.LoadScene(nextScene);
        }
    }
}
