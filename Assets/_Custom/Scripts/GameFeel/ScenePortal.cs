using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Zona de paso: cuando el jugador entra, fundido a negro y carga la escena indicada.
    /// Se usa en la escena del tutorial suelta; en la escena combinada no existe (el pasillo sigue a la mazmorra).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ScenePortal : MonoBehaviour
    {
        public string sceneName = "1_GAME";
        public SceneFader fader;
        private bool _used;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (_used) return;
            bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<Autohand.AutoHandPlayer>() != null;
            if (!isPlayer) return;
            _used = true;
            FeelHaptics.Pulse(0.3f, 0.15f);
            var f = fader != null ? fader : SceneFader.Instance;
            if (f != null) f.FadeAndLoad(sceneName);
            else SceneManager.LoadScene(sceneName);
        }
    }
}
