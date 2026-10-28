using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Variantes de los sonidos que más se repiten (golpes, pasos, apariciones, soplidos).
    /// Buena práctica de audio para juegos: un mismo archivo repetido suena "robótico"; se alterna entre
    /// varias tomas parecidas sin repetir la anterior. FeelAudio lo consulta solo.
    ///
    /// El banco vive en Resources/FeelSoundBank.asset y lo genera Tools/Game Feel/Regenerar sonidos.
    /// Para quitarlo: borrar ese asset (cada sonido vuelve a usar su archivo único).
    /// </summary>
    public class FeelSoundBank : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Clip que tienen asignado los componentes (la toma 1).")]
            public AudioClip clip;
            [Tooltip("Otras tomas del mismo sonido.")]
            public AudioClip[] variants = Array.Empty<AudioClip>();
            [Tooltip("Variación aleatoria de volumen (0,1 ≈ ±1 dB).")]
            [Range(0f, 0.3f)] public float volumeJitter = 0.1f;
        }

        public List<Entry> entries = new List<Entry>();

        static FeelSoundBank _instance;
        static bool _loaded;
        Dictionary<AudioClip, Entry> _map;
        readonly Dictionary<Entry, int> _last = new Dictionary<Entry, int>();

        public static FeelSoundBank Instance
        {
            get
            {
                if (!_loaded) { _loaded = true; _instance = Resources.Load<FeelSoundBank>("FeelSoundBank"); }
                return _instance;
            }
        }

        public Entry Find(AudioClip clip)
        {
            if (clip == null) return null;
            if (_map == null)
            {
                _map = new Dictionary<AudioClip, Entry>();
                foreach (var e in entries) if (e != null && e.clip != null) _map[e.clip] = e;
            }
            return _map.TryGetValue(clip, out var entry) ? entry : null;
        }

        /// <summary>Elige una toma al azar sin repetir la última.</summary>
        public AudioClip Pick(Entry entry)
        {
            int count = 1 + (entry.variants?.Length ?? 0);
            if (count == 1) return entry.clip;
            _last.TryGetValue(entry, out int last);
            int index = UnityEngine.Random.Range(0, count - 1);
            if (index >= last) index++;
            _last[entry] = index;
            var pick = index == 0 ? entry.clip : entry.variants[index - 1];
            return pick != null ? pick : entry.clip;
        }
    }
}
