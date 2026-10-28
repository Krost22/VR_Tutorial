using System.Collections.Generic;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Reproduce sonidos cortos en 3D usando un pequeño grupo de AudioSources reutilizables.
    /// Técnica de game feel: "sonido en cada acción" con variación de tono para que no suene repetitivo.
    /// Además: alterna tomas del banco de variantes (FeelSoundBank) y no deja que el mismo sonido se apile
    /// (máximo 3 a la vez y nunca dos en menos de 40 ms).
    /// </summary>
    public static class FeelAudio
    {
        private const int PoolSize = 12;
        private static readonly List<AudioSource> _pool = new List<AudioSource>();
        private static Transform _root;
        private static int _next;

        /// <summary>Semitonos de una escala mayor, usados para subir el tono paso a paso (progresión musical).</summary>
        public static readonly int[] MajorScale = { 0, 2, 4, 5, 7, 9, 11 };

        public static float SemitonesToPitch(float semitones) => Mathf.Pow(2f, semitones / 12f);

        /// <summary>Tono de la nota número <paramref name="step"/> dentro de la escala mayor.</summary>
        public static float StepToPitch(int step)
        {
            if (step < 0) step = 0;
            int octave = step / MajorScale.Length;
            int index = step % MajorScale.Length;
            return SemitonesToPitch(MajorScale[index] + octave * 12);
        }

        // Anti-apilado: el mismo sonido disparado varias veces en el mismo instante solo suma volumen y satura.
        private const float StackWindow = 0.04f;
        private const int MaxSameClip = 3;
        private static readonly Dictionary<AudioClip, float> _lastPlayed = new Dictionary<AudioClip, float>();

        public static AudioSource Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 0.9f)
        {
            if (clip == null) return null;
            EnsurePool(); // antes del registro de apilado: crear el grupo lo reinicia

            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(clip, out float last) && now - last < StackWindow) return null;
            if (CountPlaying(clip) >= MaxSameClip) return null;
            _lastPlayed[clip] = now;

            // Variantes (si hay banco): otra toma del mismo sonido y un poco de variación de volumen.
            var bank = FeelSoundBank.Instance;
            var entry = bank != null ? bank.Find(clip) : null;
            var playClip = clip;
            if (entry != null)
            {
                playClip = bank.Pick(entry);
                volume *= 1f - Random.value * entry.volumeJitter;
            }

            var source = NextSource();
            source.transform.position = position;
            source.clip = playClip;
            _playingOriginal[source] = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = spatialBlend;
            source.loop = false;
            source.Play();
            return source;
        }

        /// <summary>Igual que <see cref="Play"/> pero con un tono aleatorio dentro de un rango.</summary>
        public static AudioSource PlayVaried(AudioClip clip, Vector3 position, float volume, Vector2 pitchRange, float spatialBlend = 0.9f)
        {
            return Play(clip, position, volume, Random.Range(pitchRange.x, pitchRange.y), spatialBlend);
        }

        private static readonly Dictionary<AudioSource, AudioClip> _playingOriginal = new Dictionary<AudioSource, AudioClip>();

        private static int CountPlaying(AudioClip original)
        {
            int n = 0;
            foreach (var s in _pool)
                if (s != null && s.isPlaying && _playingOriginal.TryGetValue(s, out var c) && c == original) n++;
            return n;
        }

        private static AudioSource NextSource()
        {
            EnsurePool();
            // Prioriza una fuente libre; si todas suenan, recicla en orden.
            for (int i = 0; i < _pool.Count; i++)
            {
                var candidate = _pool[(_next + i) % _pool.Count];
                if (!candidate.isPlaying)
                {
                    _next = (_next + i + 1) % _pool.Count;
                    return candidate;
                }
            }
            var recycled = _pool[_next];
            _next = (_next + 1) % _pool.Count;
            return recycled;
        }

        private static void EnsurePool()
        {
            if (_root != null) return;

            // El grupo vive en la escena actual; si se cambia de escena se vuelve a crear.
            _pool.Clear();
            _playingOriginal.Clear();
            _lastPlayed.Clear();
            _next = 0;
            var go = new GameObject("[FeelAudio]");
            _root = go.transform;
            for (int i = 0; i < PoolSize; i++)
            {
                var child = new GameObject("Voice " + i);
                child.transform.SetParent(_root, false);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                // Atenuación con la distancia como pista espacial (guía de audio de Meta). Distancias de sala:
                // a más de 25 m un efecto ya no debe oírse.
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 1.2f;
                source.maxDistance = 25f;
                source.dopplerLevel = 0f;
                source.spread = 0f;
                source.priority = 64; // los efectos de juego van antes que el ambiente (128 por defecto)
                _pool.Add(source);
            }
        }
    }
}
