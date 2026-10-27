using UnityEngine;

namespace KrostGames.TTS
{
    /// <summary>
    /// Plays one clip (for example a .wav made with the TTS Generator) through the AudioSource on this GameObject.
    /// For lines, voices and real-time speech, use <see cref="TTSSpeaker"/>.
    /// </summary>
    [AddComponentMenu("KrostGames/Local TTS/TTS Audio Player")]
    [RequireComponent(typeof(AudioSource))]
    public class TTSAudioPlayer : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] AudioClip _clip;
        [SerializeField] bool _playOnStart;

        AudioSource _audioSource;

        public AudioClip Clip
        {
            get => _clip;
            set => _clip = value;
        }

        public AudioSource AudioSource => _audioSource != null ? _audioSource : (_audioSource = GetComponent<AudioSource>());

        public bool IsPlaying => AudioSource != null && AudioSource.isPlaying;

        void Reset()
        {
            var source = GetComponent<AudioSource>();
            if (source != null) source.playOnAwake = false;
        }

        void Start()
        {
            if (_playOnStart && _clip != null) Play();
        }

        public void Play()
        {
            if (_clip == null)
            {
                Debug.LogWarning("[LocalTTS] TTSAudioPlayer has no clip assigned.", this);
                return;
            }
            AudioSource source = AudioSource;
            source.Stop();
            source.clip = _clip;
            source.Play();
        }

        public void Play(AudioClip clip)
        {
            _clip = clip;
            Play();
        }

        public void Stop()
        {
            if (AudioSource != null) AudioSource.Stop();
        }
    }
}
