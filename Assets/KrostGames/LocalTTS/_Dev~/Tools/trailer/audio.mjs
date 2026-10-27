// Builds the trailer soundtrack from timeline.json: the voice-over clips (generated with Local TTS),
// synthesized sound effects placed at their peak, and a procedural 120 BPM track ducked under the voice.
// No third-party audio. usage: node audio.mjs <out.wav>   (writes a loudness-normalized 48 kHz stereo WAV)
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { readWav } from './env.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.resolve(process.argv[2] || path.join(HERE, 'audio.wav'));
const TL = JSON.parse(fs.readFileSync(path.join(HERE, 'timeline.json'), 'utf8'));
const SR = 48000, N = Math.ceil(TL.duration * SR);
const L = new Float32Array(N), R = new Float32Array(N);

// ---------------------------------------------------------------- DSP helpers
let seed = 12345;
const rnd = () => ((seed = (seed * 16807) % 2147483647) / 2147483647);
const noise = () => rnd() * 2 - 1;
function biquad(type, f, q = 0.707) {             // RBJ cookbook
  const w = 2 * Math.PI * f / SR, c = Math.cos(w), s = Math.sin(w), a = s / (2 * q);
  let b0, b1, b2, a0 = 1 + a, a1 = -2 * c, a2 = 1 - a;
  if (type === 'lp') { b0 = (1 - c) / 2; b1 = 1 - c; b2 = (1 - c) / 2; }
  else if (type === 'hp') { b0 = (1 + c) / 2; b1 = -(1 + c); b2 = (1 + c) / 2; }
  else { b0 = a; b1 = 0; b2 = -a; }               // band-pass (0 dB peak)
  let x1 = 0, x2 = 0, y1 = 0, y2 = 0;
  return x => { const y = (b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0; x2 = x1; x1 = x; y2 = y1; y1 = y; return y; };
}
const bandNoise = (n, lo, hi) => { const hp = biquad('hp', lo), lp = biquad('lp', hi), out = new Float32Array(n); for (let i = 0; i < n; i++) out[i] = lp(hp(noise())); return out; };
function sweep(n, f0, f1, k = 1) { const out = new Float32Array(n); let ph = 0; for (let i = 0; i < n; i++) { const u = i / n; const f = f0 * Math.pow(f1 / f0, Math.pow(u, k)); ph += 2 * Math.PI * f / SR; out[i] = Math.sin(ph); } return out; }
const normalize = x => { let m = 0; for (const v of x) m = Math.max(m, Math.abs(v)); if (m > 0) for (let i = 0; i < x.length; i++) x[i] /= m; return x; };

// ---------------------------------------------------------------- sound effects (shapes ported from the motion-design kit)
const DUR = { tick: .05, click: .12, pop: .16, key: .07, thud: .35, whoosh: .55, swipe: .28, riser: 1.4, impact: 1.6, chime: 1.4 };
const GAIN = { tick: .25, click: .35, pop: .32, key: .16, thud: .45, whoosh: .3, swipe: .22, riser: .28, impact: .6, chime: .26 };
function sfx(kind, p = 1) {
  const n = Math.floor(SR * DUR[kind]), t = i => i / SR, x = new Float32Array(n);
  if (kind === 'tick') { const z = bandNoise(n, 3000, 9000); for (let i = 0; i < n; i++) x[i] = Math.sin(2 * Math.PI * 2600 * p * t(i)) * Math.exp(-t(i) / .004) + .3 * z[i] * Math.exp(-t(i) / .002); }
  else if (kind === 'click') { const z = bandNoise(n, 1500, 8000), z2 = bandNoise(n, 2000, 9000), k = Math.floor(.05 * SR); for (let i = 0; i < n; i++) { x[i] = z[i] * Math.exp(-t(i) / .003) + .6 * Math.sin(2 * Math.PI * 1700 * p * t(i)) * Math.exp(-t(i) / .01); if (i >= k) x[i] += .35 * z2[i - k] * Math.exp(-t(i - k) / .002); } }
  else if (kind === 'pop') { const s = sweep(n, 380 * p, 1150 * p, .5); for (let i = 0; i < n; i++) x[i] = s[i] * Math.min(1, t(i) / .002) * Math.exp(-t(i) / .045); }
  else if (kind === 'key') { const z = bandNoise(n, 1800, 6000), f = (180 + 40 * rnd()) * p; for (let i = 0; i < n; i++) x[i] = z[i] * Math.exp(-t(i) / .005) + .5 * Math.sin(2 * Math.PI * f * t(i)) * Math.exp(-t(i) / .012); }
  else if (kind === 'thud') { const s = sweep(n, 110 * p, 50 * p), z = bandNoise(n, 200, 2000); for (let i = 0; i < n; i++) x[i] = s[i] * Math.exp(-t(i) / .07) + .25 * z[i] * Math.exp(-t(i) / .006); }
  else if (kind === 'whoosh' || kind === 'swipe') {
    const [lo, hi] = kind === 'whoosh' ? [250, 2500] : [900, 7000];
    const dark = bandNoise(n, lo * .4, lo * 2), bright = bandNoise(n, lo, hi);
    for (let i = 0; i < n; i++) { const u = i / n, e = Math.pow(Math.sin(Math.PI * Math.pow(u, .8)), 2); x[i] = (dark[i] * (1 - e) + bright[i] * e) * e; }
  }
  else if (kind === 'riser') { const z = bandNoise(n, 600, 9000), s = sweep(n, 180 * p, 1400 * p, 1.6); for (let i = 0; i < n; i++) { const u = i / n; x[i] = (z[i] * Math.pow(u, 2.5) + .35 * s[i]) * Math.min(1, u * 6) * Math.pow(u, 1.5); } }
  else if (kind === 'impact') { const s = sweep(n, 150 * p, 38 * p, .6), z = bandNoise(n, 60, 1600), h = bandNoise(n, 2000, 9000); for (let i = 0; i < n; i++) x[i] = s[i] * Math.exp(-t(i) / .38) + .7 * z[i] * Math.exp(-t(i) / .045) + .25 * h[i] * Math.exp(-t(i) / .012); }
  else if (kind === 'chime') { [[1318.5, 0], [1975.5, .05], [2637.0, .1], [3951.1, .15]].forEach(([f, d0], k) => { for (let i = Math.floor(d0 * SR); i < n; i++) { const tt = t(i) - d0; x[i] += Math.pow(.9, k) * Math.sin(2 * Math.PI * f * p * tt + 1.3 * Math.sin(2 * Math.PI * 3 * tt)) * Math.exp(-tt / .45) * Math.min(1, tt / .003); } }); }
  for (let i = 0; i < n; i++) x[i] *= Math.min(1, (n - i) / (.004 * SR));   // no click at the end
  return normalize(x);
}
function place(x, peakTime, gain, pan = 0) {
  let peak = 0; for (let i = 1; i < x.length; i++) if (Math.abs(x[i]) > Math.abs(x[peak])) peak = i;
  const start = Math.round(peakTime * SR) - peak, gl = gain * Math.sqrt((1 - pan) / 2) * 1.414, gr = gain * Math.sqrt((1 + pan) / 2) * 1.414;
  for (let i = 0; i < x.length; i++) { const j = start + i; if (j < 0 || j >= N) continue; L[j] += x[i] * gl; R[j] += x[i] * gr; }
}

// ---------------------------------------------------------------- voice-over (resampled to 48 kHz, placed at its start)
const voEnv = new Float32Array(N);
for (const v of TL.vo) {
  const { sr, x } = readWav(path.join(HERE, 'vo', v.id + '.wav'));
  const len = Math.floor(x.length * SR / sr), start = Math.round(v.t * SR);
  for (let i = 0; i < len; i++) {
    const src = i * sr / SR, k = Math.floor(src), f = src - k;
    const y0 = x[Math.max(0, k - 1)], y1 = x[k], y2 = x[Math.min(x.length - 1, k + 1)], y3 = x[Math.min(x.length - 1, k + 2)];
    const s = y1 + 0.5 * f * (y2 - y0 + f * (2 * y0 - 5 * y1 + 4 * y2 - y3 + f * (3 * (y1 - y2) + y3 - y0)));   // Catmull-Rom
    const j = start + i; if (j >= N) break;
    L[j] += s * 0.95; R[j] += s * 0.95; voEnv[j] = Math.max(voEnv[j], Math.abs(s));
  }
}
// smoothed voice presence for ducking (fast attack, slow release)
const duck = new Float32Array(N); { let e = 0; const att = Math.exp(-1 / (0.02 * SR)), rel = Math.exp(-1 / (0.35 * SR)); for (let i = 0; i < N; i++) { const v = Math.min(1, voEnv[i] * 4); e = v > e ? att * e + (1 - att) * v : rel * e + (1 - rel) * v; duck[i] = e; } }

// ---------------------------------------------------------------- sound effects from the timeline
TL.cues.forEach((c, i) => { seed = 1000 + i * 7; const [t, kind, g = 1, o = {}] = c; place(sfx(kind, o.pitch || 1), t, GAIN[kind] * g, o.pan || 0); });
TL.typing.forEach((ty, k) => { const every = ty.every || 1; for (let i = 0; i < ty.n; i += every) { seed = 77 + k * 1000 + i; place(sfx('key', 0.9 + 0.2 * rnd()), ty.t + i / ty.cps, GAIN.key * 0.9, (rnd() - 0.5) * 0.3); } });

// ---------------------------------------------------------------- procedural music, 120 BPM, A minor (Am7 - Fmaj7 - Cmaj7 - G6)
const beat = 60 / TL.bpm, bar = beat * 4, drop = TL.music.drop, fadeOut = TL.music.fadeOut;
const midi = m => 440 * Math.pow(2, (m - 69) / 12);
const CHORDS = [[45, 57, 60, 64, 67], [41, 53, 57, 60, 64], [48, 60, 64, 67, 71], [43, 55, 59, 62, 64]];
const ML = new Float32Array(N), MR = new Float32Array(N);
const lpPadL = [], lpPadR = []; let padState = new Float32Array(16), phases = new Float32Array(32);
let kickPh = 0, lastBeat = -1, bassPh = 0, hpHat = biquad('hp', 7000), lpBass = biquad('lp', 400);
let padLP = 0, padLP2 = 0;
for (let i = 0; i < N; i++) {
  const t = i / SR, b = t / beat, beatN = Math.floor(b), tb = (b - beatN) * beat, chord = CHORDS[Math.floor(t / bar) % 4];
  // sections: intro (pad only), groove from 6 s, breakdown 36.4-37.5 (no kick, filter closes), drop at 37.5, outro
  const groove = t >= 6.0 && !(t >= 36.4 && t < drop);
  const intensity = t < 6 ? 0.55 : t < drop ? 0.85 : 1;
  // kick (pitch drop) on every beat in the groove
  if (beatN !== lastBeat) { kickPh = 0; lastBeat = beatN; }
  let kick = 0;
  if (groove || (t >= drop && t < drop + 0.01)) { const fk = 45 + 105 * Math.exp(-tb * 28); kickPh += 2 * Math.PI * fk / SR; kick = Math.sin(kickPh) * Math.exp(-tb * 7.5) * 0.55; }
  const pump = groove ? 1 - 0.55 * Math.exp(-tb * 9) : 1;          // sidechain feel
  // pad: detuned saws through a soft low-pass that opens towards the drop
  let pad = 0;
  chord.slice(1).forEach((m, k) => { for (const det of [-0.004, 0.004]) { const idx = k * 2 + (det > 0 ? 1 : 0); phases[idx] = (phases[idx] + midi(m) * (1 + det) / SR) % 1; pad += (phases[idx] * 2 - 1); } });
  const cutoff = t < drop ? 500 + 1400 * Math.min(1, t / 36) * (t >= 36.4 ? Math.max(0.15, 1 - (t - 36.4) / 1.1) : 1) : 2400;
  const a = 1 - Math.exp(-2 * Math.PI * cutoff / SR);
  padLP += a * (pad - padLP); padLP2 += a * (padLP - padLP2);
  const barT = (t % bar) / bar;
  const padEnv = Math.min(1, t / 3) * (0.75 + 0.25 * Math.sin(Math.PI * barT));
  const padOut = padLP2 * 0.028 * padEnv * pump * intensity;
  // bass on every beat in the groove
  let bass = 0;
  if (groove || t >= drop) { bassPh = (bassPh + midi(chord[0]) / SR) % 1; const env = Math.min(1, tb / 0.005) * Math.exp(-tb * 3.2); bass = lpBass(Math.sin(2 * Math.PI * bassPh) + 0.3 * (bassPh * 2 - 1)) * env * 0.16 * pump; }
  // hats on the off-beats
  const off = ((b + 0.5) % 1) * beat; const hat = groove ? hpHat(noise()) * Math.exp(-off * 70) * 0.05 : 0;
  // drop hit: a full chord swell
  let swell = 0; if (t >= drop && t < drop + 3) { const u = t - drop; chord.forEach(m => { swell += Math.sin(2 * Math.PI * midi(m + 12) * u) * Math.exp(-u * 1.1); }); swell *= 0.03; }
  const fade = Math.min(1, Math.max(0, (TL.duration - 0.2 - t) / (TL.duration - 0.2 - fadeOut)));
  const voDuck = 1 - 0.5 * duck[i];
  const m = (kick + bass + hat + swell) * voDuck * fade;
  const st = padOut * voDuck * fade;
  ML[i] = m + st * 1.1; MR[i] = m + st * 0.9 + padLP * 0.004 * voDuck * fade;
}
for (let i = 0; i < N; i++) { L[i] += ML[i] * 0.9; R[i] += MR[i] * 0.9; }

// ---------------------------------------------------------------- master: gentle saturation, write, loudness-normalize with ffmpeg
const raw = Buffer.alloc(44 + N * 8);
let peak = 0; for (let i = 0; i < N; i++) peak = Math.max(peak, Math.abs(L[i]), Math.abs(R[i]));
const g = 0.9 / Math.max(peak, 1e-6);
raw.write('RIFF', 0); raw.writeUInt32LE(36 + N * 8, 4); raw.write('WAVEfmt ', 8); raw.writeUInt32LE(16, 16); raw.writeUInt16LE(3, 20); raw.writeUInt16LE(2, 22);
raw.writeUInt32LE(SR, 24); raw.writeUInt32LE(SR * 8, 28); raw.writeUInt16LE(8, 32); raw.writeUInt16LE(32, 34); raw.write('data', 36); raw.writeUInt32LE(N * 8, 40);
for (let i = 0; i < N; i++) { raw.writeFloatLE(Math.tanh(L[i] * g * 1.2) / Math.tanh(1.2), 44 + i * 8); raw.writeFloatLE(Math.tanh(R[i] * g * 1.2) / Math.tanh(1.2), 48 + i * 8); }
const tmp = OUT.replace(/\.wav$/, '_raw.wav');
fs.writeFileSync(tmp, raw);
const probe = spawnSync('ffmpeg', ['-hide_banner', '-i', tmp, '-af', 'loudnorm=I=-15:TP=-1.5:LRA=11:print_format=json', '-f', 'null', '-'], { encoding: 'utf8' }).stderr;
const j = JSON.parse(probe.slice(probe.lastIndexOf('{'), probe.lastIndexOf('}') + 1));
spawnSync('ffmpeg', ['-v', 'error', '-y', '-i', tmp, '-af', `loudnorm=I=-15:TP=-1.5:LRA=11:measured_I=${j.input_i}:measured_TP=${j.input_tp}:measured_LRA=${j.input_lra}:measured_thresh=${j.input_thresh}:offset=${j.target_offset}:linear=true`,
  '-ar', String(SR), '-c:a', 'pcm_s16le', OUT], { stdio: 'inherit' });
fs.rmSync(tmp, { force: true });
console.log(`audio -> ${OUT}  (${TL.duration}s, input ${j.input_i} LUFS -> -15 LUFS)`);
