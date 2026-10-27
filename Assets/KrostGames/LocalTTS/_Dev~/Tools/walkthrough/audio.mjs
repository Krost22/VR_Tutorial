// Soundtrack for the walkthrough: the exact voice clips heard in the Editor (logged by the directors) at their film
// times, a soft click per mouse press, and a quiet synthesized pad ducked under the voice. No third-party audio.
// usage: node audio.mjs <cues.json> <out.wav>
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const [cuesFile, out] = process.argv.slice(2);
const FILM = JSON.parse(fs.readFileSync(cuesFile, 'utf8'));
const SR = 48000, N = Math.ceil(FILM.duration * SR);
const TMP = fs.mkdtempSync(path.join(os.tmpdir(), 'tts-walk-audio-'));

function writeWav(file, L, R) {
  const b = Buffer.alloc(44 + L.length * 4);
  b.write('RIFF', 0); b.writeUInt32LE(36 + L.length * 4, 4); b.write('WAVEfmt ', 8); b.writeUInt32LE(16, 16);
  b.writeUInt16LE(1, 20); b.writeUInt16LE(2, 22); b.writeUInt32LE(SR, 24); b.writeUInt32LE(SR * 4, 28); b.writeUInt16LE(4, 32); b.writeUInt16LE(16, 34);
  b.write('data', 36); b.writeUInt32LE(L.length * 4, 40);
  for (let i = 0; i < L.length; i++) {
    b.writeInt16LE(Math.max(-32768, Math.min(32767, Math.round(L[i] * 32767))), 44 + i * 4);
    b.writeInt16LE(Math.max(-32768, Math.min(32767, Math.round(R[i] * 32767))), 46 + i * 4);
  }
  fs.writeFileSync(file, b);
}

// ---------------------------------------------------------------- clicks
{
  const L = new Float32Array(N), R = new Float32Array(N);
  let seed = 7; const rnd = () => ((seed = (seed * 16807) % 2147483647) / 2147483647) * 2 - 1;
  for (const t of FILM.clicks) {
    const s0 = Math.round(t * SR);
    for (let i = 0; i < SR * 0.05 && s0 + i < N; i++) {
      const u = i / SR;
      const v = 0.11 * Math.sin(2 * Math.PI * 2100 * u) * Math.exp(-140 * u) + 0.05 * rnd() * Math.exp(-420 * u) + 0.06 * Math.sin(2 * Math.PI * 520 * u) * Math.exp(-90 * u);
      L[s0 + i] += v; R[s0 + i] += v;
    }
  }
  writeWav(path.join(TMP, 'clicks.wav'), L, R);
}

// ---------------------------------------------------------------- pad: four slow chords, soft sines, low-passed
{
  const L = new Float32Array(N), R = new Float32Array(N);
  const chords = [[48, 55, 59, 64, 71], [45, 52, 55, 60, 67], [41, 48, 52, 57, 64], [43, 50, 55, 59, 62]];
  const len = 8, fadeIn = 2.5, fadeOut = 3.5;
  const hz = m => 440 * Math.pow(2, (m - 69) / 12);
  for (let c = 0; c * len < FILM.duration; c++) {
    const notes = chords[c % chords.length], t0 = c * len - 1.5, t1 = t0 + len + 3;
    notes.forEach((m, k) => {
      const f = hz(m), amp = 0.0058 / Math.sqrt(notes.length) * (k === 0 ? 1.2 : 1);
      for (let i = Math.max(0, Math.floor(t0 * SR)); i < Math.min(N, t1 * SR); i++) {
        const t = i / SR, u = t - t0;
        const env = Math.min(1, u / 2.0) * Math.min(1, (t1 - t) / 3.0);
        const vib = 1 + 0.0015 * Math.sin(2 * Math.PI * (0.13 + k * 0.05) * t + k);
        const ph = 2 * Math.PI * f * vib * t;
        const v = amp * env * (Math.sin(ph) + 0.25 * Math.sin(2 * ph + 0.4));
        L[i] += v * (0.85 + 0.15 * Math.sin(0.7 * t + k));
        R[i] += v * (0.85 + 0.15 * Math.cos(0.6 * t + k));
      }
    });
  }
  let yl = 0, yr = 0; const a = 1 - Math.exp(-2 * Math.PI * 1100 / SR);
  for (let i = 0; i < N; i++) {
    const t = i / SR, g = Math.min(1, t / fadeIn) * Math.min(1, (FILM.duration - t) / fadeOut);
    yl += a * (L[i] - yl); yr += a * (R[i] - yr); L[i] = yl * g; R[i] = yr * g;
  }
  writeWav(path.join(TMP, 'pad.wav'), L, R);
}

// ---------------------------------------------------------------- mix with ffmpeg
const inputs = [], filters = [], labels = [];
FILM.cues.forEach((c, i) => {
  inputs.push('-i', c.file);
  const ms = Math.max(0, Math.round(c.t * 1000));
  filters.push(`[${i}:a]aresample=${SR},aformat=sample_fmts=fltp:channel_layouts=stereo,adelay=${ms}|${ms}[v${i}]`);
  labels.push(`[v${i}]`);
});
const k = FILM.cues.length;
inputs.push('-i', path.join(TMP, 'clicks.wav'), '-i', path.join(TMP, 'pad.wav'));
filters.push(`${labels.join('')}amix=inputs=${k}:normalize=0:duration=longest,apad=whole_dur=${FILM.duration}[voice]`);
filters.push(`[voice]asplit=2[voice1][key]`);
filters.push(`[${k + 1}:a][key]sidechaincompress=threshold=0.02:ratio=6:attack=40:release=600[pad]`);
filters.push(`[voice1][${k}:a][pad]amix=inputs=3:normalize=0:duration=first,atrim=0:${FILM.duration}[out]`);
const raw = path.join(TMP, 'mix.wav');
const r = spawnSync('ffmpeg', ['-v', 'error', '-y', ...inputs, '-filter_complex', filters.join(';'), '-map', '[out]', '-ac', '2', '-ar', String(SR), '-c:a', 'pcm_f32le', raw], { encoding: 'utf8' });
if (r.status) { console.error(r.stderr); process.exit(1); }
// static gain to -16 LUFS (a dynamic loudnorm would lift the quiet pad between the lines), then a peak limiter
const m = spawnSync('ffmpeg', ['-hide_banner', '-i', raw, '-af', 'loudnorm=I=-16:TP=-1.5:print_format=json', '-f', 'null', '-'], { encoding: 'utf8' });
const measured = JSON.parse(m.stderr.slice(m.stderr.lastIndexOf('{'), m.stderr.lastIndexOf('}') + 1));
const gain = -16 - Number(measured.input_i);
const f = spawnSync('ffmpeg', ['-v', 'error', '-y', '-i', raw, '-af', `volume=${gain.toFixed(2)}dB,alimiter=limit=0.84:attack=5:release=80:level=false`, '-c:a', 'pcm_s16le', path.resolve(out)], { encoding: 'utf8' });
fs.rmSync(TMP, { recursive: true, force: true });
if (f.status) { console.error(f.stderr); process.exit(1); }
console.log('audio ->', out, `(${FILM.duration}s, ${k} voice cues, ${FILM.clicks.length} clicks, gain ${gain.toFixed(1)} dB)`);
