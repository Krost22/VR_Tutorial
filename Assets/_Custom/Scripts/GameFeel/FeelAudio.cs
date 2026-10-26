using System.Collections.Generic;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Reproduce sonidos cortos en 3D usando un pequeño grupo de AudioSources reutilizables.
    /// Técnica de game feel: "sonido en cada acción" con variación de tono para que no suene repetitivo.
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

        public static AudioSource Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 0.9f)
        {
            if (clip == null) return null;
            var source = NextSource();
            source.transform.position = position;
            source.clip = clip;
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
            _next = 0;
            var go = new GameObject("[FeelAudio]");
            _root = go.transform;
            for (int i = 0; i < PoolSize; i++)
            {
                var child = new GameObject("Voice " + i);
                child.transform.SetParent(_root, false);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 1.5f;
                source.maxDistance = 40f;
                source.dopplerLevel = 0f;
                _pool.Add(source);
            }
        }
    }
}
