using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Visual de una torre de proyección y de la gema de su mural (solo lee el estado del pilar, no cambia el puzzle):
    /// - Torre: las franjas de runa y el cristal emisor están casi apagados; se encienden al poner la runa correcta,
    ///   suben con la carga y quedan al máximo con el pilar fijado.
    /// - Gema del mural (donde pega el rayo): sin runa da vueltas despacio, torcida y apagada; mientras el rayo carga
    ///   se va enderezando hasta quedar alineada con el rayo, y al fijarse se enciende con un rebote.
    /// Para quitarlo: borrar el componente (la torre y la gema quedan quietas y con su brillo de reposo).
    /// </summary>
    public class PillarTowerFeel : MonoBehaviour
    {
        public PillarProjector pillar;
        public Color color = Color.white;

        [Header("Torre")]
        [Tooltip("Franjas de runa de la torre (material emisivo).")]
        public Renderer[] glowParts = new Renderer[0];
        [Tooltip("Cristal del que sale el rayo.")]
        public Renderer crystal;

        [Header("Gema del mural")]
        public Transform gem;
        public Renderer gemRenderer;
        [Tooltip("Orientación alineada con el rayo (rotación local del soporte de la gema).")]
        public Quaternion alignedRotation = Quaternion.identity;
        [Tooltip("Grados que está torcida la gema mientras nadie la alinea.")]
        public float misalignDegrees = 55f;

        [Header("Brillo")]
        public float idleEmission = 0.08f;
        public float activeEmission = 0.9f;
        public float lockedEmission = 2.2f;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock _mpb;
        float _level, _align, _spin;
        bool _wasLocked;

        void Update()
        {
            if (pillar == null) return;
            _mpb ??= new MaterialPropertyBlock();
            bool active = pillar.IsActivatedByAltar, locked = pillar.IsLocked();
            float charge = pillar.LockProgress01;

            float target = locked ? 1f : active ? 0.35f + 0.45f * charge : 0f;
            _level = Mathf.MoveTowards(_level, target, Time.deltaTime * 2f);
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * (locked ? 2f : 5f));
            float emission = Mathf.Lerp(idleEmission, locked ? lockedEmission : activeEmission, _level) * pulse;
            foreach (var r in glowParts) Apply(r, emission);
            Apply(crystal, emission * 1.3f);

            if (gem != null)
            {
                // Se endereza con la carga; fijado = alineada del todo.
                float alignTarget = locked ? 1f : active ? charge : 0f;
                _align = Mathf.MoveTowards(_align, alignTarget, Time.deltaTime * (locked ? 3f : 1.5f));
                _spin += Time.deltaTime * Mathf.Lerp(40f, 0f, _align);
                var loose = alignedRotation * Quaternion.Euler(misalignDegrees, _spin, misalignDegrees * 0.4f);
                gem.localRotation = Quaternion.SlerpUnclamped(loose, alignedRotation, FeelTween.Evaluate(FeelEase.OutBack, _align));
                Apply(gemRenderer, Mathf.Lerp(idleEmission, lockedEmission * 1.2f, _align * _align) * pulse);
            }

            if (locked && !_wasLocked && gem != null) FeelTween.Punch(gem, 0.35f, 0.45f);
            _wasLocked = locked;
        }

        void Apply(Renderer r, float emission)
        {
            if (r == null) return;
            r.GetPropertyBlock(_mpb);
            var c = color * emission;
            c.a = 1f;
            _mpb.SetColor(EmissionId, c);
            r.SetPropertyBlock(_mpb);
        }
    }
}
