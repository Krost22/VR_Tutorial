using System;

namespace KrostGames.TTS
{
    /// <summary>Minimal RIFF/WAVE reader and writer (PCM 8/16/24/32-bit and 32-bit float in, 16-bit PCM out).</summary>
    public static class WavUtility
    {
        /// <summary>Reads a .wav file into 16-bit PCM. Throws FormatException for anything that is not PCM or float WAVE.</summary>
        public static TTSAudioData Decode(byte[] wav)
        {
            if (wav == null || wav.Length < 12 || !HasTag(wav, 0, "RIFF") || !HasTag(wav, 8, "WAVE"))
                throw new FormatException("Not a RIFF/WAVE file.");

            int format = 0, channels = 0, sampleRate = 0, bits = 0, dataOffset = -1, dataLength = 0;
            int pos = 12;
            while (pos + 8 <= wav.Length)
            {
                int size = BitConverter.ToInt32(wav, pos + 4);
                int body = pos + 8;
                if (size < 0 || body + size > wav.Length) size = wav.Length - body; // streamed or truncated chunk

                if (HasTag(wav, pos, "fmt ") && size >= 16)
                {
                    format = BitConverter.ToUInt16(wav, body);
                    channels = BitConverter.ToUInt16(wav, body + 2);
                    sampleRate = BitConverter.ToInt32(wav, body + 4);
                    bits = BitConverter.ToUInt16(wav, body + 14);
                    if (format == 0xFFFE && size >= 26) format = BitConverter.ToUInt16(wav, body + 24); // WAVE_FORMAT_EXTENSIBLE
                }
                else if (HasTag(wav, pos, "data") && dataOffset < 0)
                {
                    dataOffset = body;
                    dataLength = size;
                }
                pos = body + size + (size & 1);
            }

            if (dataOffset < 0) throw new FormatException("The WAVE file has no data chunk.");
            if (channels <= 0 || sampleRate <= 0) throw new FormatException("The WAVE file has no valid format chunk.");
            if (format != 1 && format != 3) throw new FormatException($"Unsupported WAVE format {format}.");

            int bytesPerSample = bits / 8;
            if (bytesPerSample < 1 || bytesPerSample > 4 || (format == 3 && bits != 32))
                throw new FormatException($"Unsupported sample size: {bits} bits.");

            int count = dataLength / bytesPerSample;
            count -= count % channels;
            var samples = new short[count];

            if (format == 1 && bits == 16)
            {
                Buffer.BlockCopy(wav, dataOffset, samples, 0, count * 2); // Unity platforms are little-endian, like WAVE
            }
            else
            {
                for (int i = 0, p = dataOffset; i < count; i++, p += bytesPerSample)
                {
                    int v;
                    if (format == 3) v = (int)Math.Round(Math.Max(-1f, Math.Min(1f, BitConverter.ToSingle(wav, p))) * 32767f);
                    else if (bits == 8) v = (wav[p] - 128) << 8;
                    else if (bits == 24) v = (short)(wav[p + 1] | (wav[p + 2] << 8));
                    else v = BitConverter.ToInt32(wav, p) >> 16;
                    samples[i] = (short)v;
                }
            }
            return new TTSAudioData(samples, channels, sampleRate);
        }

        /// <summary>Writes 16-bit PCM as a standard .wav file.</summary>
        public static byte[] Encode(short[] samples, int channels, int sampleRate)
        {
            int dataBytes = samples.Length * 2;
            var b = new byte[44 + dataBytes];
            WriteTag(b, 0, "RIFF");
            WriteInt(b, 4, 36 + dataBytes);
            WriteTag(b, 8, "WAVE");
            WriteTag(b, 12, "fmt ");
            WriteInt(b, 16, 16);
            WriteShort(b, 20, 1);
            WriteShort(b, 22, channels);
            WriteInt(b, 24, sampleRate);
            WriteInt(b, 28, sampleRate * channels * 2);
            WriteShort(b, 32, channels * 2);
            WriteShort(b, 34, 16);
            WriteTag(b, 36, "data");
            WriteInt(b, 40, dataBytes);
            Buffer.BlockCopy(samples, 0, b, 44, dataBytes);
            return b;
        }

        /// <summary>
        /// Cuts the silence before and after speech (Windows voices add ~0.1 s before and ~0.7 s after),
        /// keeping a little margin and a 5 ms fade so the cut never clicks. Returns the same array when nothing changes.
        /// </summary>
        public static short[] TrimSilence(short[] samples, int channels, int sampleRate,
            float threshold = 0.01f, float keepBefore = 0.04f, float keepAfter = 0.12f)
        {
            if (samples == null || channels <= 0 || samples.Length < channels) return samples;
            int frames = samples.Length / channels;
            int limit = (int)(threshold * 32768f);

            int first = -1;
            for (int f = 0; f < frames && first < 0; f++)
                for (int c = 0; c < channels; c++)
                    if (Math.Abs((int)samples[f * channels + c]) > limit) { first = f; break; }
            if (first < 0) return samples; // silence only: leave it alone

            int last = first;
            for (int f = frames - 1; f > first; f--)
            {
                bool loud = false;
                for (int c = 0; c < channels; c++)
                    if (Math.Abs((int)samples[f * channels + c]) > limit) { loud = true; break; }
                if (loud) { last = f; break; }
            }

            int start = Math.Max(0, first - (int)(keepBefore * sampleRate));
            int end = Math.Min(frames, last + 1 + (int)(keepAfter * sampleRate));
            if (start == 0 && end == frames) return samples;

            var result = new short[(end - start) * channels];
            Array.Copy(samples, start * channels, result, 0, result.Length);

            int length = end - start, fade = Math.Min(length, sampleRate / 200);
            for (int i = 0; i < fade; i++)
            {
                float gain = (float)i / fade;
                int f = length - 1 - i;
                for (int c = 0; c < channels; c++)
                    result[f * channels + c] = (short)(result[f * channels + c] * gain);
            }
            return result;
        }

        /// <summary>16-bit PCM to the float range AudioClip.SetData expects.</summary>
        public static float[] ToFloat(short[] samples)
        {
            var data = new float[samples.Length];
            for (int i = 0; i < samples.Length; i++) data[i] = samples[i] / 32768f;
            return data;
        }

        static bool HasTag(byte[] b, int at, string tag)
        {
            if (at + 4 > b.Length) return false;
            for (int i = 0; i < 4; i++) if (b[at + i] != tag[i]) return false;
            return true;
        }

        static void WriteTag(byte[] b, int at, string tag) { for (int i = 0; i < 4; i++) b[at + i] = (byte)tag[i]; }

        static void WriteInt(byte[] b, int at, int v)
        {
            b[at] = (byte)v; b[at + 1] = (byte)(v >> 8); b[at + 2] = (byte)(v >> 16); b[at + 3] = (byte)(v >> 24);
        }

        static void WriteShort(byte[] b, int at, int v) { b[at] = (byte)v; b[at + 1] = (byte)(v >> 8); }
    }
}
