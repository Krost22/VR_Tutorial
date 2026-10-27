// Amplitude envelopes (60 values per second, 0..1) of the voice-over clips, so the waveforms in the film follow the real voice.
// usage: node env.mjs   -> env.json
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
export function readWav(file) {
  const b = fs.readFileSync(file);
  let p = 12, sr = 0, ch = 1, data = null;
  while (p + 8 <= b.length) {
    const id = b.toString('ascii', p, p + 4), size = b.readUInt32LE(p + 4);
    if (id === 'fmt ') { ch = b.readUInt16LE(p + 10); sr = b.readUInt32LE(p + 12); }
    if (id === 'data') data = b.subarray(p + 8, p + 8 + size);
    p += 8 + size + (size & 1);
  }
  const n = data.length / 2 / ch, x = new Float32Array(n);
  for (let i = 0; i < n; i++) x[i] = data.readInt16LE(i * 2 * ch) / 32768;
  return { sr, x };
}

const env = {};
for (const file of fs.readdirSync(path.join(HERE, 'vo')).filter(f => f.endsWith('.wav'))) {
  const { sr, x } = readWav(path.join(HERE, 'vo', file));
  const hop = sr / 60, values = [];
  for (let s = 0; s < x.length; s += hop) {
    let sum = 0, n = 0;
    for (let i = Math.floor(s); i < Math.min(x.length, Math.floor(s + hop)); i++) { sum += x[i] * x[i]; n++; }
    values.push(Math.sqrt(sum / Math.max(1, n)));
  }
  const peak = Math.max(...values) || 1;
  env[path.basename(file, '.wav')] = values.map(v => +Math.min(1, Math.pow(v / peak, 0.7)).toFixed(3));
}
fs.writeFileSync(path.join(HERE, 'env.json'), JSON.stringify(env));
console.log('env.json:', Object.entries(env).map(([k, v]) => `${k}=${(v.length / 60).toFixed(2)}s`).join(' '));
