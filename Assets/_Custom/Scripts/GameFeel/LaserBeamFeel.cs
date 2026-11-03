using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Rayo de energía de un pilar (va en el objeto del LineRenderer; no cambia el raycast ni el puzzle):
    /// - La textura de energía (núcleo, halo y hebras) fluye del cristal hacia la gema.
    /// - Un núcleo fino casi blanco (segundo LineRenderer hijo) copia la posición del rayo: se lee como luz, no
    ///   como una barra de color.
    /// - Pulso suave; mientras carga parpadea más rápido; fijado queda estable y algo más intenso.
    /// Para quitarlo: borrar el componente y su hijo "Nucleo" (el rayo queda con la textura quieta).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserBeamFeel : MonoBehaviour
    {
        public PillarProjector pillar;
        public LineRenderer core;
        public Color color = Color.white;
        [Tooltip("Velocidad del flujo de energía (repeticiones de textura por segundo).")]
        public float flowSpeed = 1.6f;
        public float coreFlowSpeed = 2.6f;

        static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");
        LineRenderer _beam;
        MaterialPropertyBlock _mpb;
        float _baseWidth, _coreWidth, _offset, _coreOffset;

        void Awake()
        {
            _beam = GetComponent<LineRenderer>();
            _baseWidth = _beam.widthMultiplier;
            if (core != null) _coreWidth = core.widthMultiplier;
            _mpb = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            bool on = _beam.enabled;
            if (core != null)
            {
                if (core.enabled != on) core.enabled = on;
                if (on)
                {
                    core.positionCount = _beam.positionCount;
                    for (int i = 0; i < _beam.positionCount; i++) core.SetPosition(i, _beam.GetPosition(i));
                }
            }
            if (!on) return;

            bool locked = pillar != null && pillar.IsLocked();
            bool charging = pillar != null && !locked && pillar.IsMatched();
            float t = Time.time;
            float pulse = locked ? 1.05f + 0.05f * Mathf.Sin(t * 2.2f)
                        : charging ? 0.9f + 0.25f * Mathf.Abs(Mathf.Sin(t * 9f))
                        : 0.85f + 0.12f * Mathf.Sin(t * 3.1f);
            _beam.widthMultiplier = _baseWidth * pulse;
            if (core != null) core.widthMultiplier = _coreWidth * (0.8f + 0.4f * pulse);

            _offset -= Time.deltaTime * flowSpeed * (locked ? 1.4f : 1f);
            _coreOffset -= Time.deltaTime * coreFlowSpeed * (locked ? 1.4f : 1f);
            Scroll(_beam, _offset);
            if (core != null) Scroll(core, _coreOffset);
        }

        void Scroll(LineRenderer lr, float offset)
        {
            lr.GetPropertyBlock(_mpb);
            _mpb.SetVector(BaseMapSt, new Vector4(1f, 1f, Mathf.Repeat(offset, 1f), 0f));
            lr.SetPropertyBlock(_mpb);
        }
    }
}
