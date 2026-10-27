using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTutorial.GameFeel;

namespace VRTutorial.Tutorial
{
    /// <summary>
    /// Fase 1 del tutorial, al estilo de Halo: se enciende una luz cada vez y el jugador la mira fijamente
    /// hasta cargarla. Cada luz cargada vuela a la barrera, enciende su gema y apaga un haz. Con todas,
    /// la barrera se retira y se habilita el movimiento.
    ///
    /// Guías: punto de mira (solo en esta fase), anillo que se cierra sobre la luz mientras se carga,
    /// flecha en el borde de la vista cuando la luz queda fuera y zumbido 3D que sale de la luz.
    /// Reemplaza a LookDirectionTutorial (que queda desactivado en la escena).
    /// </summary>
    public class GazeSequenceTutorial : MonoBehaviour
    {
        [Header("Jugador")]
        public Camera playerCamera;
        [Tooltip("Se desactiva hasta terminar la fase (movimiento del jugador).")]
        public MonoBehaviour movementScriptToDisable;

        [Header("Luces, en orden")]
        public List<Transform> targets = new List<Transform>();
        [Tooltip("Margen de la mirada, en grados, además del tamaño de la luz.")]
        public float gazeAngle = 6f;
        [Tooltip("Segundos mirando fijamente para cargar una luz.")]
        public float dwellTime = 0.7f;
        public float maxDistance = 15f;
        [Tooltip("Espera antes de encender la primera luz (deja terminar el fundido de entrada).")]
        public float startDelay = 1.2f;
        [Tooltip("Texto que acompaña solo a la primera luz.")]
        public GameObject firstHint;

        [Header("Barrera")]
        [Tooltip("Haces de arriba hacia abajo: se apaga uno por luz.")]
        public LaserBeamSet beams;
        [Tooltip("Una gema por luz; la luz vuela hasta ella.")]
        public List<Renderer> gems = new List<Renderer>();
        public Material gemOnMaterial;
        [Tooltip("Se retira al terminar (lleva el colisionador que bloquea el paso).")]
        public GameObject barrier;
        public float flyTime = 0.8f;

        [Header("Guías")]
        public Material reticleMaterial;
        public Material ringMaterial;
        public Material arrowMaterial;
        public Color guideColor = new Color(0.35f, 0.9f, 1f, 1f);
        [Tooltip("A partir de este ángulo (grados) aparece la flecha que señala la luz.")]
        public float arrowShowAngle = 35f;

        [Header("Sonido y respuesta")]
        public AudioClip humClip;
        [Range(0f, 1f)] public float humVolume = 0.35f;
        public FeelBurst lookFeedback;
        public FeelBurst arriveFeedback;
        public FeelBurst completeFeedback;

        [Header("Al terminar")]
        public GameObject canvasHUD;
        public GameObject popUp;
        public GameObject popUpInicial;
        public GameObject nextStep;

        public int CurrentIndex => _current;
        public bool IsCharging => !_done && _dwell > 0f;
        public bool Completed => _done;
        public Transform CurrentTarget => _current >= 0 && _current < targets.Count && !_busy ? targets[_current] : null;

        int _current = -1;
        float _dwell;
        bool _busy, _done;
        float _hapticTimer;
        float _activatedAt;
        Vector3[] _baseScale;
        Vector3[] _basePos;
        AudioSource _hum;
        Transform _reticle, _ring, _arrow;
        Renderer _ringRenderer, _arrowRenderer, _reticleRenderer;
        MaterialPropertyBlock _mpb;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Start()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            if (movementScriptToDisable != null) movementScriptToDisable.enabled = false;
            if (canvasHUD != null) canvasHUD.SetActive(false); // la mira vieja se reemplaza por la nueva
            if (firstHint != null) firstHint.SetActive(false);

            _baseScale = new Vector3[targets.Count];
            _basePos = new Vector3[targets.Count];
            for (int i = 0; i < targets.Count; i++)
            {
                _baseScale[i] = targets[i].localScale;
                _basePos[i] = targets[i].position;
                targets[i].gameObject.SetActive(false);
            }

            _mpb = new MaterialPropertyBlock();
            _reticle = MakeQuad("Guia_Mira", reticleMaterial, out _reticleRenderer);
            _ring = MakeQuad("Guia_Anillo", ringMaterial, out _ringRenderer);
            _arrow = MakeArrow("Guia_Flecha", arrowMaterial, out _arrowRenderer);
            _ring.gameObject.SetActive(false);
            _arrow.gameObject.SetActive(false);

            StartCoroutine(BeginRoutine());
        }

        IEnumerator BeginRoutine()
        {
            yield return new WaitForSeconds(startDelay);
            Activate(0);
        }

        void Activate(int index)
        {
            _current = index;
            _dwell = 0f;
            if (index >= targets.Count) return;
            var t = targets[index];
            t.position = _basePos[index];
            t.localScale = _baseScale[index];
            t.gameObject.SetActive(true);
            FeelTween.PopIn(t, 0.4f);
            _activatedAt = Time.time;

            if (humClip != null)
            {
                _hum = t.GetComponent<AudioSource>();
                if (_hum == null) _hum = t.gameObject.AddComponent<AudioSource>();
                _hum.clip = humClip;
                _hum.loop = true;
                _hum.spatialBlend = 1f;
                _hum.rolloffMode = AudioRolloffMode.Linear;
                _hum.minDistance = 1f;
                _hum.maxDistance = 14f;
                _hum.volume = humVolume;
                _hum.pitch = 1f;
                _hum.Play();
            }
            if (index == 0 && firstHint != null) firstHint.SetActive(true);
        }

        void Update()
        {
            if (_done || _busy || _current < 0 || _current >= targets.Count || playerCamera == null) return;

            var t = targets[_current];
            var cam = playerCamera.transform;
            Vector3 to = t.position - cam.position;
            float dist = to.magnitude;
            float radius = 0.5f * Mathf.Max(_baseScale[_current].x, _baseScale[_current].y);
            float allowed = gazeAngle + Mathf.Atan2(radius, Mathf.Max(dist, 0.01f)) * Mathf.Rad2Deg;
            bool looking = dist <= maxDistance && Vector3.Angle(cam.forward, to) <= allowed;

            // Mirar carga; apartar la vista descarga el doble de rápido (perdona mirar de pasada).
            _dwell = looking ? _dwell + Time.deltaTime : Mathf.Max(0f, _dwell - Time.deltaTime * 2f);
            float k = Mathf.Clamp01(_dwell / dwellTime);

            // Carga: la luz crece, el zumbido sube de tono y la vibración se acelera.
            float bob = Mathf.Sin(Time.time * 2.2f) * 0.03f * (1f - k);
            t.position = _basePos[_current] + Vector3.up * bob;
            if (Time.time - _activatedAt > 0.45f) t.localScale = _baseScale[_current] * (1f + 0.3f * k); // deja terminar la aparición
            if (_hum != null) _hum.pitch = Mathf.Lerp(1f, 1.9f, k);
            if (k > 0f)
            {
                _hapticTimer -= Time.deltaTime;
                if (_hapticTimer <= 0f)
                {
                    FeelHaptics.Pulse(Mathf.Lerp(0.08f, 0.35f, k), 0.02f);
                    _hapticTimer = Mathf.Lerp(0.18f, 0.05f, k);
                }
            }

            if (_dwell >= dwellTime) StartCoroutine(CompleteRoutine(_current));
        }

        void LateUpdate()
        {
            if (playerCamera == null) return;
            var cam = playerCamera.transform;
            bool show = !_done;
            if (_reticle.gameObject.activeSelf != show) _reticle.gameObject.SetActive(show);
            if (!show) { _ring.gameObject.SetActive(false); _arrow.gameObject.SetActive(false); return; }

            // Punto de mira a la profundidad de lo que se mira (evita verlo doble), tamaño angular constante.
            float depth = 3f;
            var target = CurrentTarget;
            if (target != null) depth = Vector3.Distance(cam.position, target.position) - 0.5f * Mathf.Max(_baseScale[_current].x, _baseScale[_current].y) * 1.4f - 0.05f;
            else if (Physics.Raycast(cam.position, cam.forward, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore)) depth = hit.distance * 0.97f;
            depth = Mathf.Clamp(depth, 0.6f, 10f);
            _reticle.SetPositionAndRotation(cam.position + cam.forward * depth, cam.rotation);
            _reticle.localScale = Vector3.one * depth * 0.034f;
            float k = target != null ? Mathf.Clamp01(_dwell / dwellTime) : 0f;
            SetColor(_reticleRenderer, Color.Lerp(new Color(1f, 1f, 1f, 0.7f), guideColor, k));

            // Anillo sobre la luz: se cierra mientras se carga.
            if (target != null)
            {
                _ring.gameObject.SetActive(true);
                float size = 2f * 0.5f * Mathf.Max(_baseScale[_current].x, _baseScale[_current].y);
                float pulse = k > 0f ? 0f : (Mathf.Sin(Time.time * 4f) * 0.08f);
                float scale = size * Mathf.Lerp(2.6f + pulse, 1.35f, FeelTween.Evaluate(FeelEase.OutCubic, k));
                var facing = Quaternion.LookRotation(target.position - cam.position, cam.up);
                _ring.SetPositionAndRotation(target.position, facing);
                var glow = target.Find("Brillo");
                if (glow != null) glow.rotation = facing;
                _ring.localScale = Vector3.one * scale;
                var c = guideColor; c.a = Mathf.Lerp(0.35f, 1f, k);
                SetColor(_ringRenderer, c);
            }
            else _ring.gameObject.SetActive(false);

            // Flecha en el borde de la vista cuando la luz está fuera.
            if (target != null)
            {
                Vector3 to = target.position - cam.position;
                float angle = Vector3.Angle(cam.forward, to);
                if (angle > arrowShowAngle)
                {
                    Vector3 local = cam.InverseTransformDirection(to.normalized);
                    var dir = new Vector2(local.x, local.y);
                    if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
                    dir.Normalize();
                    float slide = 0.02f * Mathf.Sin(Time.time * 6f);
                    Vector3 pos = cam.position + cam.forward * 1.2f + (cam.right * dir.x + cam.up * dir.y) * (0.3f + slide);
                    float roll = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                    _arrow.SetPositionAndRotation(pos, cam.rotation * Quaternion.Euler(0f, 0f, roll));
                    _arrow.localScale = Vector3.one * 0.09f;
                    var c = guideColor; c.a = Mathf.Clamp01((angle - arrowShowAngle) / 10f);
                    SetColor(_arrowRenderer, c);
                    _arrow.gameObject.SetActive(true);
                }
                else _arrow.gameObject.SetActive(false);
            }
            else _arrow.gameObject.SetActive(false);
        }

        IEnumerator CompleteRoutine(int index)
        {
            _busy = true;
            var t = targets[index];
            if (_hum != null) _hum.Stop();
            if (index == 0 && firstHint != null) firstHint.SetActive(false);

            if (lookFeedback != null) lookFeedback.PlayStepAt(t.position, index * 2);
            FeelHaptics.Pulse(0.55f, 0.08f);

            // La luz vuela en arco hasta su gema en la barrera.
            Renderer gem = index < gems.Count ? gems[index] : null;
            if (gem != null)
            {
                Vector3 p0 = t.position, p2 = gem.bounds.center;
                Vector3 p1 = (p0 + p2) * 0.5f + Vector3.up * 0.6f;
                Vector3 s0 = t.localScale, s1 = _baseScale[index] * 0.45f;
                float time = 0f;
                while (time < flyTime)
                {
                    time += Time.deltaTime;
                    float u = FeelTween.Evaluate(FeelEase.InOutSine, Mathf.Clamp01(time / flyTime));
                    t.position = (1 - u) * (1 - u) * p0 + 2 * (1 - u) * u * p1 + u * u * p2;
                    t.localScale = Vector3.Lerp(s0, s1, u);
                    yield return null;
                }
                if (gemOnMaterial != null) gem.sharedMaterial = gemOnMaterial;
                FeelTween.Punch(gem.transform, 0.35f, 0.3f);
                if (arriveFeedback != null) arriveFeedback.PlayStepAt(p2, index * 2);
            }
            t.gameObject.SetActive(false);
            if (beams != null) beams.TurnOff(index);

            if (index + 1 < targets.Count)
            {
                yield return new WaitForSeconds(0.45f);
                _busy = false;
                Activate(index + 1);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
                Finish();
            }
        }

        void Finish()
        {
            _done = true;
            _busy = false;
            _current = targets.Count;
            if (popUpInicial != null) popUpInicial.SetActive(false);
            if (popUp != null) popUp.SetActive(true);
            if (canvasHUD != null) canvasHUD.SetActive(false);
            if (nextStep != null) nextStep.SetActive(true);
            if (completeFeedback != null) completeFeedback.Play();
            if (barrier != null) FeelDisappear.HideOrDeactivate(barrier);
            if (movementScriptToDisable != null) movementScriptToDisable.enabled = true;
        }

        void SetColor(Renderer r, Color c)
        {
            if (r == null) return;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            r.SetPropertyBlock(_mpb);
        }

        Transform MakeQuad(string name, Material mat, out Renderer renderer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.transform.SetParent(transform, true);
            return go.transform;
        }

        Transform MakeArrow(string name, Material mat, out Renderer renderer)
        {
            // Punta de flecha plana que apunta a +Y; las UV van al centro de la textura (parte más brillante).
            var mesh = new Mesh { name = "Guia_FlechaMesh" };
            mesh.vertices = new[] { new Vector3(0f, 0.5f, 0f), new Vector3(-0.45f, -0.35f, 0f), new Vector3(0f, -0.1f, 0f), new Vector3(0.45f, -0.35f, 0f) };
            mesh.uv = new[] { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            renderer = mr;
            go.transform.SetParent(transform, true);
            return go.transform;
        }
    }
}
