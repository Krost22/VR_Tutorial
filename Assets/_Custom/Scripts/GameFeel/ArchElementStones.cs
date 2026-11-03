using System;
using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Piedras de colores incrustadas en el arco de la puerta principal, una por pilar (rojo Fuego, amarillo
    /// Tierra, verde Viento, azul Agua). Muestran el progreso del puzzle donde está la salida:
    /// - al principio: apagadas (piedra oscura con un leve tono de su color);
    /// - mientras su pilar apunta al mural correcto y se está fijando: parpadeo tenue que va creciendo;
    /// - pilar fijado: se encienden con un destello, un rebote y un sonido que sube de tono con cada piedra;
    /// - si se retira la runa del altar y el pilar se suelta: se apagan otra vez;
    /// - con las cuatro encendidas: una ola de luz recorre el arco (la puerta se abre a la vez).
    /// Solo visual y sonido. Para quitarlo: borrar el objeto Piedras_Arco (o este componente).
    /// </summary>
    public class ArchElementStones : MonoBehaviour
    {
        [Serializable]
        public class Stone
        {
            public PillarProjector pillar;
            public Renderer renderer;
            public Color color = Color.white;
            [NonSerialized] public float level, flash;
            [NonSerialized] public bool lit;
        }

        public Stone[] stones = new Stone[0];

        [Header("Aspecto")]
        [Tooltip("Brillo del color base cuando la piedra está apagada.")]
        [Range(0f, 1f)] public float offBrightness = 0.75f;
        [Tooltip("Intensidad de la emisión encendida. Solo brillan las vetas y el símbolo (mapa de emisión del material).")]
        public float litEmission = 2.2f;
        [Tooltip("Emisión máxima mientras el pilar se está fijando.")]
        public float chargeEmission = 1.2f;
        [Tooltip("Brillo residual de las vetas con la piedra apagada (0 = nada).")]
        public float idleEmission = 0.06f;

        [Header("Respuesta (opcional)")]
        [Tooltip("Al encenderse una piedra (las partículas toman el color de la piedra; el tono sube con cada una).")]
        public FeelBurst lightUpFeedback;
        [Tooltip("Al apagarse una piedra (se retiró la runa).")]
        public FeelBurst lightOffFeedback;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock _mpb;
        int _litCount;
        float _waveTime = -1f;
        int _waveDone;

        public int LitCount => _litCount;

        void Start()
        {
            _mpb ??= new MaterialPropertyBlock();
            for (int i = 0; i < stones.Length; i++) Apply(stones[i], i, 0f);
        }

        void Update()
        {
            _mpb ??= new MaterialPropertyBlock();
            int lit = 0;
            for (int i = 0; i < stones.Length; i++)
            {
                var s = stones[i];
                if (s == null || s.renderer == null) continue;
                bool locked = s.pillar != null && s.pillar.IsLocked();
                float charge = s.pillar != null && !locked && s.pillar.IsActivatedByAltar ? s.pillar.LockProgress01 : 0f;

                if (locked && !s.lit)
                {
                    s.lit = true;
                    s.flash = 1f;
                    OnLit(s, CountLit());
                }
                else if (!locked && s.lit)
                {
                    s.lit = false;
                    if (lightOffFeedback != null) lightOffFeedback.PlayAt(s.renderer.bounds.center);
                }
                if (s.lit) lit++;

                s.level = Mathf.MoveTowards(s.level, s.lit ? 1f : 0f, Time.deltaTime * (s.lit ? 2.5f : 1.5f));
                s.flash = Mathf.MoveTowards(s.flash, 0f, Time.deltaTime * 1.6f);
                Apply(s, i, charge);
            }

            // Las cuatro encendidas: ola de luz por el arco (una sola vez cada vez que se completa).
            if (stones.Length > 0 && lit == stones.Length && _litCount < stones.Length) { _waveTime = Time.time + 0.5f; _waveDone = 0; }
            _litCount = lit;
            if (_waveTime > 0f)
            {
                for (int i = 0; i < stones.Length && i < 32; i++)
                {
                    if ((_waveDone & (1 << i)) != 0 || Time.time < _waveTime + i * 0.15f) continue;
                    _waveDone |= 1 << i;
                    if (stones[i] != null) stones[i].flash = 1f;
                }
                if (Time.time > _waveTime + stones.Length * 0.15f) _waveTime = -1f;
            }
        }

        int CountLit()
        {
            int n = 0;
            foreach (var s in stones) if (s != null && s.lit) n++;
            return n;
        }

        void OnLit(Stone s, int order)
        {
            FeelTween.Punch(s.renderer.transform, 0.25f, 0.4f);
            if (lightUpFeedback != null)
            {
                lightUpFeedback.tintParticles = true;
                lightUpFeedback.particleTint = s.color;
                lightUpFeedback.PlayStepAt(s.renderer.bounds.center, (order - 1) * 2);
            }
        }

        void Apply(Stone s, int index, float charge)
        {
            if (s == null || s.renderer == null) return;
            float t = Time.time;
            // Roca con un tinte leve de su elemento (apagada) que se aviva un poco al encenderse; la luz real sale
            // de las vetas y el símbolo grabados (mapa de emisión), no de toda la losa.
            var offBase = Color.Lerp(new Color(0.78f, 0.76f, 0.72f), s.color, 0.22f) * offBrightness;
            var onBase = Color.Lerp(new Color(0.85f, 0.83f, 0.8f), s.color, 0.4f);
            var baseColor = Color.Lerp(offBase, onBase, s.level);
            baseColor.a = 1f;

            float pulse = 0.82f + 0.18f * Mathf.Sin(t * 1.6f + index * 1.4f);
            float chargeFlicker = charge > 0f ? charge * chargeEmission * (0.55f + 0.45f * Mathf.Sin(t * 14f)) : 0f;
            var emission = s.color * (idleEmission + s.level * litEmission * pulse + s.flash * 1.6f + chargeFlicker);
            emission.a = 1f;

            s.renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, baseColor);
            _mpb.SetColor(EmissionId, emission);
            s.renderer.SetPropertyBlock(_mpb);
        }
    }
}
