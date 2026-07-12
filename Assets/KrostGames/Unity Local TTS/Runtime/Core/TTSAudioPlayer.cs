using UnityEngine;

namespace KrostGames.LocalTTS.Runtime.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class TTSAudioPlayer : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip _clip;
        [SerializeField] private bool _playOnStart;

        private AudioSource _audioSource;

        public AudioClip Clip
        {
            get => _clip;
            set => _clip = value;
        }

        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        private void Start()
        {
            if (_playOnStart && _clip != null)
                Play();
        }

        public void Play()
        {
            if (_clip == null)
            {
                Debug.LogWarning("[TTSAudioPlayer] No clip assigned.", this);
                return;
            }
            _audioSource.Stop();
            _audioSource.clip = _clip;
            _audioSource.Play();
        }

        public void Play(AudioClip clip)
        {
            _clip = clip;
            Play();
        }

        public void Stop()
        {
            _audioSource.Stop();
        }
    }
}
