using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Fundido a negro pegado a la cámara del visor. En VR los cortes bruscos y los saltos de cámara marean:
    /// toda transición de escena pasa por un fundido suave (fade out → cargar → fade in).
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        [Tooltip("Material transparente sin luz (Feel_Fade).")]
        public Material fadeMaterial;
        public Color fadeColor = Color.black;
        public bool fadeInOnStart = true;
        public float fadeInDuration = 1.2f;
        public float fadeOutDuration = 1.5f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private Material _material;
        private GameObject _quad;
        private float _alpha;
        private bool _loading;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private IEnumerator Start()
        {
            // Se espera un frame para que el rig de VR haya creado/activado su cámara.
            yield return null;
            EnsureQuad();
            if (fadeInOnStart)
            {
                SetAlpha(1f);
                yield return Fade(1f, 0f, fadeInDuration);
            }
            else SetAlpha(0f);
        }

        public void FadeAndLoad(string sceneName)
        {
            if (_loading || string.IsNullOrEmpty(sceneName)) return;
            _loading = true;
            StartCoroutine(FadeAndLoadRoutine(sceneName));
        }

        private IEnumerator FadeAndLoadRoutine(string sceneName)
        {
            EnsureQuad();
            yield return Fade(_alpha, 1f, fadeOutDuration);
            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[SceneFader] No se pudo cargar la escena '{sceneName}'. ¿Está en la lista de escenas del build?");
                _loading = false;
                yield return Fade(1f, 0f, fadeInDuration);
            }
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                // Se limita el paso por frame: el primer frame tras una carga trae un salto de tiempo
                // enorme y, sin esto, el fundido terminaba de golpe.
                t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                SetAlpha(Mathf.Lerp(from, to, FeelTween.Evaluate(FeelEase.InOutSine, t / duration)));
                yield return null;
            }
            SetAlpha(to);
        }

        private void EnsureQuad()
        {
            if (_quad != null) return;
            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in Camera.allCameras) { if (c.isActiveAndEnabled) { cam = c; break; } }
            }
            if (cam == null || fadeMaterial == null) return;

            _quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _quad.name = "[SceneFade]";
            Destroy(_quad.GetComponent<Collider>());
            _quad.transform.SetParent(cam.transform, false);
            float distance = cam.nearClipPlane + 0.02f;
            _quad.transform.localPosition = new Vector3(0f, 0f, distance);
            _quad.transform.localRotation = Quaternion.identity;
            _quad.transform.localScale = Vector3.one * Mathf.Max(1f, distance * 10f);
            _quad.layer = cam.gameObject.layer;
            var renderer = _quad.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _material = new Material(fadeMaterial);
            renderer.sharedMaterial = _material;
            SetAlpha(_alpha);
        }

        private void SetAlpha(float alpha)
        {
            _alpha = alpha;
            if (_material == null || _quad == null) return;
            var c = fadeColor;
            c.a = alpha;
            _material.SetColor(BaseColorId, c);
            _quad.SetActive(alpha > 0.001f);
        }
    }
}
