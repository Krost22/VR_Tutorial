using UnityEngine;

namespace KrostGames.TTS
{
    /// <summary>16-bit PCM produced by the speech engine. Cheap to keep in memory and to turn into an AudioClip.</summary>
    public sealed class TTSAudioData
    {
        public TTSAudioData(short[] samples, int channels, int sampleRate)
        {
            Samples = samples;
            Channels = Mathf.Max(1, channels);
            SampleRate = Mathf.Max(1, sampleRate);
        }

        /// <summary>Interleaved 16-bit samples.</summary>
        public short[] Samples { get; }
        public int Channels { get; }
        public int SampleRate { get; }

        public int SampleFrames => Samples.Length / Channels;

        /// <summary>Duration in seconds.</summary>
        public float Length => (float)SampleFrames / SampleRate;

        /// <summary>Text that produced this audio.</summary>
        public string Text { get; internal set; }

        /// <summary>Voice that spoke it.</summary>
        public TTSVoice Voice { get; internal set; }

        /// <summary>Creates a new AudioClip (main thread only). The caller owns it: Destroy it when no longer needed.</summary>
        public AudioClip ToAudioClip(string name = null)
        {
            var clip = AudioClip.Create(string.IsNullOrEmpty(name) ? "TTS" : name, Mathf.Max(1, SampleFrames), Channels, SampleRate, false);
            if (Samples.Length > 0) clip.SetData(WavUtility.ToFloat(Samples), 0);
            return clip;
        }

        /// <summary>The audio as a 16-bit PCM .wav file.</summary>
        public byte[] ToWav() => WavUtility.Encode(Samples, Channels, SampleRate);

        /// <summary>Copy without the silence Windows voices add before and after speech.</summary>
        public TTSAudioData Trimmed()
        {
            short[] trimmed = WavUtility.TrimSilence(Samples, Channels, SampleRate);
            if (ReferenceEquals(trimmed, Samples)) return this;
            return new TTSAudioData(trimmed, Channels, SampleRate) { Text = Text, Voice = Voice };
        }
    }
}
