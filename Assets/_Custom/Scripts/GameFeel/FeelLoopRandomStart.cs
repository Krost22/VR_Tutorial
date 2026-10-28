using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Arranca un bucle de audio en un punto al azar y con un tono ligeramente distinto.
    /// Así varias antorchas con el mismo crepitar no suenan sincronizadas (un "coro" robótico).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FeelLoopRandomStart : MonoBehaviour
    {
        public Vector2 pitchRange = new Vector2(0.93f, 1.07f);

        void Start()
        {
            var a = GetComponent<AudioSource>();
            if (a.clip == null) return;
            a.loop = true;
            a.pitch = Random.Range(pitchRange.x, pitchRange.y);
            a.time = Random.Range(0f, a.clip.length * 0.95f);
            a.Play();
        }
    }
}
