// Text-free key art for Local TTS: the product mark as a speaking character, with speech bubbles whose waveforms are
// real voice envelopes (../trailer/env.json). Pure SVG/HTML, rendered with headless Chrome, flattened to 24-bit PNG.
//   node keyart.mjs            -> ../../Marketing/AssetStore/KeyImages (Icon, Card, Cover, Social, Logo)
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const DEV = path.resolve(HERE, '../..');
const OUT = path.join(DEV, 'Marketing/AssetStore/KeyImages');
const CHROME = ['C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(p => fs.existsSync(p));
const ENV = JSON.parse(fs.readFileSync(path.join(DEV, 'Tools/trailer/env.json'), 'utf8'));
const TMP = fs.mkdtempSync(path.join(os.tmpdir(), 'localtts-keyart-'));
let uid = 0;

// ---------------------------------------------------------------- pieces
// The mark as a face: mint tile, two eyes, a mouth made of the logo's waveform bars.
function face(s, { mouth = [0.45, 0.85, 1, 0.7, 0.4], look = 0 } = {}) {
  const id = 'f' + (uid++), bw = s * 0.075, gap = s * 0.03, total = 5 * bw + 4 * gap, x0 = (s - total) / 2, my = s * 0.66;
  const eye = (cx) => `<rect x="${cx - s * 0.065 + look}" y="${s * 0.25}" width="${s * 0.13}" height="${s * 0.19}" rx="${s * 0.065}" fill="#052a20"/>
    <circle cx="${cx + s * 0.022 + look}" cy="${s * 0.295}" r="${s * 0.022}" fill="#e9fff8"/>`;
  return `<svg width="${s}" height="${s}" viewBox="0 0 ${s} ${s}" style="overflow:visible;display:block">
    <defs>
      <linearGradient id="${id}g" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#4ce6bd"/><stop offset=".55" stop-color="#1fbf93"/><stop offset="1" stop-color="#119a74"/></linearGradient>
      <radialGradient id="${id}h" cx=".3" cy=".18" r=".7"><stop offset="0" stop-color="#fff" stop-opacity=".35"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>
      <filter id="${id}s" x="-50%" y="-50%" width="200%" height="200%"><feDropShadow dx="0" dy="${s * 0.05}" stdDeviation="${s * 0.07}" flood-color="#000" flood-opacity=".55"/><feDropShadow dx="0" dy="0" stdDeviation="${s * 0.12}" flood-color="#1db992" flood-opacity=".45"/></filter>
    </defs>
    <g filter="url(#${id}s)"><rect width="${s}" height="${s}" rx="${s * 0.24}" fill="url(#${id}g)"/></g>
    <rect width="${s}" height="${s}" rx="${s * 0.24}" fill="url(#${id}h)"/>
    <rect x="${s * 0.015}" y="${s * 0.015}" width="${s * 0.97}" height="${s * 0.97}" rx="${s * 0.23}" fill="none" stroke="#fff" stroke-opacity=".22" stroke-width="${s * 0.012}"/>
    ${eye(s * 0.37)}${eye(s * 0.63)}
    <ellipse cx="${s * 0.2}" cy="${s * 0.58}" rx="${s * 0.07}" ry="${s * 0.04}" fill="#fff" opacity=".16"/>
    <ellipse cx="${s * 0.8}" cy="${s * 0.58}" rx="${s * 0.07}" ry="${s * 0.04}" fill="#fff" opacity=".16"/>
    ${mouth.map((h, k) => { const bh = s * 0.22 * h; return `<rect x="${x0 + k * (bw + gap)}" y="${my - bh / 2}" width="${bw}" height="${bh}" rx="${bw / 2}" fill="#052a20"/>`; }).join('')}
  </svg>`;
}

// A speech bubble holding a waveform from a real voice line.
function bubble(w, h, { color, key, n = 14, tail = 'left', seed = 1 }) {
  const id = 'b' + (uid++), r = h * 0.42, data = ENV[key].filter(v => v > 0.04);
  const pad = h * 0.3, step = (w - 2 * pad) / n, bw = step * 0.5;
  let bars = '';
  for (let i = 0; i < n; i++) {
    const a = data[Math.floor((i + 0.5) / n * (data.length - 1))];
    const bh = Math.max(bw, (h - 2 * pad * 0.8) * (0.25 + 0.75 * a));
    bars += `<rect x="${pad + i * step + (step - bw) / 2}" y="${(h - bh) / 2}" width="${bw}" height="${bh}" rx="${bw / 2}" fill="${color}"/>`;
  }
  const tx = tail === 'left' ? h * 0.35 : w - h * 0.35;
  const tailPath = tail === 'left'
    ? `M${tx + h * 0.05} ${h - 2} L${tx - h * 0.28} ${h + h * 0.34} L${tx + h * 0.38} ${h - 2} Z`
    : `M${tx - h * 0.05} ${h - 2} L${tx + h * 0.28} ${h + h * 0.34} L${tx - h * 0.38} ${h - 2} Z`;
  return `<svg width="${w}" height="${h * 1.4}" viewBox="0 0 ${w} ${h * 1.4}" style="overflow:visible;display:block">
    <defs><linearGradient id="${id}g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1b2723"/><stop offset="1" stop-color="#121a17"/></linearGradient>
      <filter id="${id}s" x="-30%" y="-30%" width="160%" height="180%"><feDropShadow dx="0" dy="${h * 0.12}" stdDeviation="${h * 0.16}" flood-color="#000" flood-opacity=".5"/></filter></defs>
    <g filter="url(#${id}s)">
      <path d="${tailPath}" fill="#121a17" stroke="${color}" stroke-opacity=".55" stroke-width="${Math.max(1.5, h * 0.025)}"/>
      <rect x="0" y="0" width="${w}" height="${h}" rx="${r}" fill="url(#${id}g)" stroke="${color}" stroke-opacity=".55" stroke-width="${Math.max(1.5, h * 0.025)}"/>
      <rect x="${h * 0.12}" y="${h * 0.32}" width="${w - h * 0.24}" height="${h * 0.6}" rx="${r * 0.8}" fill="#1b2723"/>
    </g>
    ${bars}
  </svg>`;
}

// Concentric sound arcs.
function rings(r0, count, gap, color, cx, cy, from = -40, to = 40) {
  const rad = d => d * Math.PI / 180;
  let s = '';
  for (let i = 0; i < count; i++) {
    const r = r0 + i * gap, a0 = rad(from), a1 = rad(to);
    s += `<path d="M${cx + r * Math.cos(a0)} ${cy + r * Math.sin(a0)} A${r} ${r} 0 0 1 ${cx + r * Math.cos(a1)} ${cy + r * Math.sin(a1)}" fill="none" stroke="${color}" stroke-width="${gap * 0.22}" stroke-linecap="round" opacity="${(0.55 - i * 0.13).toFixed(2)}"/>`;
  }
  return s;
}

const page = (w, h, body, glow = '50% 45%') => `<!doctype html><html><head><meta charset="utf-8"><style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{width:${w}px;height:${h}px;overflow:hidden;background:#0b0f0e}
.abs{position:absolute}
.bg{position:absolute;inset:0;background:
  radial-gradient(circle at ${glow}, rgba(29,185,146,.30), transparent 42%),
  radial-gradient(ellipse 50% 60% at 88% 18%, rgba(124,200,255,.10), transparent 60%),
  radial-gradient(ellipse 45% 55% at 85% 92%, rgba(233,216,166,.08), transparent 60%),
  linear-gradient(155deg,#10201b 0%,#0b0f0e 55%,#080b0a 100%)}
.dots{position:absolute;inset:0;background-image:radial-gradient(rgba(255,255,255,.06) 1.2px, transparent 1.4px);background-size:${Math.round(w / 48)}px ${Math.round(w / 48)}px;
  -webkit-mask-image:radial-gradient(ellipse 70% 70% at ${glow}, #000 10%, transparent 75%)}
</style></head><body><div class="bg"></div><div class="dots"></div>${body}</body></html>`;

function render(name, w, h, html) {
  const file = path.join(TMP, name + '.html'), raw = path.join(TMP, name + '.png'), out = path.join(OUT, name + '.png');
  fs.writeFileSync(file, html);
  spawnSync(CHROME, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1', '--force-color-profile=srgb',
    `--window-size=${w},${h}`, `--screenshot=${raw}`, '--virtual-time-budget=2000', pathToFileURL(file).href]);
  const f = spawnSync('ffmpeg', ['-loglevel', 'error', '-y', '-i', raw, '-vf', `crop=${w}:${h}:0:0,format=rgb24`, out], { encoding: 'utf8' });
  if (f.status) { console.error(name, f.stderr); process.exit(1); }
  console.log('ok', path.relative(DEV, out));
}

const MINT = '#5ef2c5', SKY = '#7cc8ff', SAND = '#ecd9a2';
const floor = (cx, cy, rx) => `<div class="abs" style="left:${cx - rx}px;top:${cy - rx * 0.12}px;width:${rx * 2}px;height:${rx * 0.24}px;border-radius:50%;background:radial-gradient(ellipse, rgba(29,185,146,.35), rgba(29,185,146,0) 70%)"></div>`;

// ---------------------------------------------------------------- images
fs.mkdirSync(OUT, { recursive: true });

// Cover 1950x1300: the character speaking three languages
{
  const fs_ = 600, fx = 260, fy = 400;
  render('Cover_1950x1300', 1950, 1300, page(1950, 1300, `
    ${floor(fx + fs_ / 2, fy + fs_ + 70, 420)}
    <svg class="abs" width="1950" height="1300" style="left:0;top:0">${rings(390, 4, 46, MINT, fx + fs_ / 2, fy + fs_ / 2, -32, 32)}</svg>
    <div class="abs" style="left:${fx}px;top:${fy}px">${face(fs_, { look: 8 })}</div>
    <div class="abs" style="left:1090px;top:230px">${bubble(620, 150, { color: MINT, key: 'welcome', n: 18 })}</div>
    <div class="abs" style="left:1250px;top:560px">${bubble(540, 140, { color: SKY, key: 'es', n: 16 })}</div>
    <div class="abs" style="left:1120px;top:880px">${bubble(470, 130, { color: SAND, key: 'pt', n: 14 })}</div>`, '28% 52%'));
}

// Social 1200x630
{
  const fs_ = 330, fx = 170, fy = 140;
  render('Social_1200x630', 1200, 630, page(1200, 630, `
    ${floor(fx + fs_ / 2, fy + fs_ + 40, 240)}
    <svg class="abs" width="1200" height="630" style="left:0;top:0">${rings(215, 3, 28, MINT, fx + fs_ / 2, fy + fs_ / 2, -30, 30)}</svg>
    <div class="abs" style="left:${fx}px;top:${fy}px">${face(fs_, { look: 5 })}</div>
    <div class="abs" style="left:660px;top:95px">${bubble(380, 92, { color: MINT, key: 'welcome', n: 14 })}</div>
    <div class="abs" style="left:760px;top:285px">${bubble(330, 86, { color: SKY, key: 'es', n: 12 })}</div>
    <div class="abs" style="left:675px;top:460px">${bubble(290, 80, { color: SAND, key: 'pt', n: 11 })}</div>`, '30% 50%'));
}

// Card 420x280
{
  const fs_ = 150, fx = 42, fy = 62;
  render('Card_420x280', 420, 280, page(420, 280, `
    ${floor(fx + fs_ / 2, fy + fs_ + 22, 110)}
    <div class="abs" style="left:${fx}px;top:${fy}px">${face(fs_, { look: 3 })}</div>
    <div class="abs" style="left:222px;top:52px">${bubble(160, 48, { color: MINT, key: 'welcome', n: 8 })}</div>
    <div class="abs" style="left:250px;top:150px">${bubble(132, 44, { color: SKY, key: 'es', n: 7 })}</div>`, '30% 50%'));
}

// Icon 160x160, Logo 512x512 / 120x120: the character alone
for (const [name, s] of [['Icon_160x160', 160], ['Logo_512x512', 512], ['Logo_120x120', 120]]) {
  const fs_ = Math.round(s * 0.74);
  render(name, s, s, page(s, s, `<div class="abs" style="left:${(s - fs_) / 2}px;top:${(s - fs_) / 2 - s * 0.01}px">${face(fs_)}</div>`, '50% 48%'));
}

fs.rmSync(TMP, { recursive: true, force: true });
