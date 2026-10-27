// Asset Store images for Local TTS: key images (icon, card, cover, social, logo) and 1920x1080 screenshots.
// Everything is drawn here with HTML/SVG (brand mark = the Generator window's logo, waveforms = envelopes of real
// voice lines from ../trailer/env.json) or comes from real Editor captures in ./shots. No generated imagery.
// Rendered with headless Chrome at the exact size, then flattened to 24-bit PNG (no alpha) with ffmpeg.
//   node keyimages.mjs            -> ../../Marketing/AssetStore/KeyImages + Screenshots
//   node keyimages.mjs --only cover,card
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const DEV = path.resolve(HERE, '../..');
const OUT_KEYS = path.join(DEV, 'Marketing/AssetStore/KeyImages');
const OUT_SHOTS = path.join(DEV, 'Marketing/AssetStore/Screenshots');
const SHOTS = path.join(HERE, 'shots');
const CHROME = ['C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(p => fs.existsSync(p));
const FFMPEG = process.env.FFMPEG || 'ffmpeg';
const ENV = JSON.parse(fs.readFileSync(path.join(DEV, 'Tools/trailer/env.json'), 'utf8'));
const LOGO = pathToFileURL(path.join(DEV, 'Marketing/KrostGames_Logo_white.png')).href;
const only = (() => { const i = process.argv.indexOf('--only'); return i > 0 ? process.argv[i + 1].split(',') : null; })();

// ---------------------------------------------------------------- brand
const C = { bg: '#0b0f0e', bg2: '#0f1a17', ink: '#f1f5f3', sub: '#cfdad5', mute: '#8fa39b', mint: '#1db992', mint2: '#5ef2c5', dark: '#052a20', card: '#121917' };
const REST = [0.35, 0.7, 1, 0.6, 0.3];                      // bar heights of the logo in the Generator window

// The product mark: mint rounded square with five dark bars (same proportions as the window logo and the trailer).
function mark(size, { glow = true, id = 'm' } = {}) {
  const r = size * 0.22, bw = size * 0.1, gap = size * 0.035, total = 5 * bw + 4 * gap, x0 = (size - total) / 2;
  const bars = REST.map((h, k) => {
    const bh = size * 0.62 * h;
    return `<rect x="${x0 + k * (bw + gap)}" y="${(size - bh) / 2}" width="${bw}" height="${bh}" rx="${bw / 2}" fill="${C.dark}"/>`;
  }).join('');
  return `<svg width="${size}" height="${size}" viewBox="0 0 ${size} ${size}" style="display:block;overflow:visible${glow ? `;filter:drop-shadow(0 ${size * 0.06}px ${size * 0.18}px rgba(29,185,146,.38))` : ''}">
    <defs><linearGradient id="${id}g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#2bd0a6"/><stop offset="1" stop-color="#17a47e"/></linearGradient></defs>
    <rect width="${size}" height="${size}" rx="${r}" fill="url(#${id}g)"/>
    ${bars}</svg>`;
}

// A row of rounded bars mirrored around the centre line, driven by a real voice envelope.
function ribbon(width, height, { n = 90, bw = 0.5, hole = null, curve = 1.25, keys = ['welcome', 'es', 'pt'], fadeL = 0.18, fadeR = 0.18, min = 0.05, seed = 1, colorA = C.mint, colorB = C.mint2, opacity = 1 } = {}) {
  const data = keys.flatMap(k => ENV[k].filter((v, i, a) => v > 0.02 || (i > 0 && a[i - 1] > 0.02)));
  const step = width / n, w = step * bw, cy = height / 2;
  let s = seed; const rnd = () => ((s = (s * 16807) % 2147483647) / 2147483647);
  let bars = '';
  for (let i = 0; i < n; i++) {
    const u = i / (n - 1);
    const x = i * step + step / 2;
    if (hole && x > hole[0] && x < hole[1]) continue;       // leave room for something drawn on top
    const a = Math.pow(data[Math.floor(u * (data.length - 1))], curve);
    const fade = Math.min(1, u / fadeL, (1 - u) / fadeR);
    const ease = fade * fade * (3 - 2 * fade);
    const h = Math.max(w, height * (min + (1 - min) * a * (0.82 + 0.18 * rnd())) * (0.35 + 0.65 * ease));
    bars += `<rect x="${(i * step + (step - w) / 2).toFixed(2)}" y="${(cy - h / 2).toFixed(2)}" width="${w.toFixed(2)}" height="${h.toFixed(2)}" rx="${(w / 2).toFixed(2)}" fill="url(#rg${seed})" opacity="${(0.25 + 0.75 * ease).toFixed(3)}"/>`;
  }
  return `<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" style="display:block;opacity:${opacity}">
    <defs><linearGradient id="rg${seed}" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="${colorA}"/><stop offset="1" stop-color="${colorB}"/></linearGradient></defs>${bars}</svg>`;
}

const FONTS = `
@font-face{font-family:Display;src:local('Segoe UI Black'),local('SegoeUI-Black');font-weight:900}
@font-face{font-family:UI;src:local('Segoe UI Semibold'),local('SegoeUI-Semibold');font-weight:600}
@font-face{font-family:UI;src:local('Segoe UI Bold'),local('SegoeUI-Bold');font-weight:700}
@font-face{font-family:UI;src:local('Segoe UI'),local('SegoeUI');font-weight:400}
@font-face{font-family:Code;src:local('Cascadia Code'),local('CascadiaCode-Regular'),local('Consolas')}`;

const page = (w, h, body, css = '') => `<!doctype html><html><head><meta charset="utf-8"><style>${FONTS}
*{box-sizing:border-box;margin:0;padding:0}
html,body{width:${w}px;height:${h}px;overflow:hidden;background:${C.bg}}
body{position:relative;color:${C.ink};font-family:UI,sans-serif;-webkit-font-smoothing:antialiased}
.abs{position:absolute}
.bg{position:absolute;inset:0;background:
  radial-gradient(ellipse 70% 80% at 78% 30%, rgba(29,185,146,.20), transparent 62%),
  radial-gradient(ellipse 60% 70% at 10% 100%, rgba(94,242,197,.08), transparent 60%),
  linear-gradient(160deg, ${C.bg2} 0%, ${C.bg} 55%, #090c0b 100%)}
.display{font-family:Display;font-weight:900;letter-spacing:-.035em;line-height:.95;white-space:nowrap}
${css}</style></head><body><div class="bg"></div>${body}</body></html>`;

// ---------------------------------------------------------------- render
const TMP = fs.mkdtempSync(path.join(os.tmpdir(), 'localtts-keyimages-'));
function render(name, w, h, html, outDir) {
  if (only && !only.includes(name.split('_')[0].toLowerCase()) && !only.includes(name)) return;
  fs.mkdirSync(outDir, { recursive: true });
  const file = path.join(TMP, name + '.html'), raw = path.join(TMP, name + '.png'), out = path.join(outDir, name + '.png');
  fs.writeFileSync(file, html);
  const r = spawnSync(CHROME, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1', '--force-color-profile=srgb',
    '--allow-file-access-from-files', '--default-background-color=00000000', `--window-size=${w},${h}`, `--screenshot=${raw}`,
    '--virtual-time-budget=2000', pathToFileURL(file).href], { encoding: 'utf8' });
  if (!fs.existsSync(raw)) { console.error(name, r.stderr); process.exit(1); }
  const f = spawnSync(FFMPEG, ['-loglevel', 'error', '-y', '-i', raw, '-vf', `crop=${w}:${h}:0:0,format=rgb24`, out], { encoding: 'utf8' });
  if (f.status) { console.error(name, f.stderr); process.exit(1); }
  console.log('ok', path.relative(DEV, out));
}

// ---------------------------------------------------------------- key images
// Icon 160x160 and logo 512x512: the mark only, no text.
for (const [name, s] of [['Icon_160x160', 160], ['Logo_512x512', 512], ['Logo_120x120', 120]]) {
  render(name, s, s, page(s, s, `<div class="abs" style="left:${s * 0.14}px;top:${s * 0.14}px">${mark(s * 0.72, { glow: true })}</div>`,
    `.bg{background:radial-gradient(circle at 50% 42%, rgba(29,185,146,.30), transparent 70%), linear-gradient(160deg, ${C.bg2}, ${C.bg})}`), OUT_KEYS);
}

// Card 420x280: title and publisher only.
render('Card_420x280', 420, 280, page(420, 280, `
  <div class="abs" style="left:30px;top:30px">${mark(58)}</div>
  <div class="abs display" style="left:28px;top:112px;font-size:78px">Local TTS</div>
  <div class="abs" style="left:31px;top:214px;font:600 21px UI;color:${C.mute};letter-spacing:.01em">by KrostGames</div>
  <div class="abs" style="left:236px;top:14px">${ribbon(170, 96, { n: 22, keys: ['welcome'], fadeL: 0.5, fadeR: 0.25, seed: 3, opacity: 0.55 })}</div>`), OUT_KEYS);

// Cover 1950x1300: title, tagline and publisher; the voice ribbon is a real Windows voice line.
render('Cover_1950x1300', 1950, 1300, page(1950, 1300, `
  <div class="abs" style="left:150px;top:170px">${mark(170, { id: 'cm' })}</div>
  <div class="abs display" style="left:140px;top:395px;font-size:300px">Local TTS</div>
  <div class="abs" style="left:152px;top:720px;font:600 70px UI;color:${C.sub};letter-spacing:-.01em;white-space:nowrap">Offline text-to-speech with <span style="color:${C.mint2}">Windows voices</span></div>
  <img class="abs" src="${LOGO}" style="right:130px;top:140px;width:210px;height:210px;opacity:.95">
  <div class="abs" style="left:0;top:880px">${ribbon(1950, 330, { n: 120, keys: ['welcome', 'es', 'pt', 'checkpoint'], fadeL: 0.22, fadeR: 0.22, seed: 7 })}</div>`), OUT_KEYS);

// Social 1200x630: no text. The mark sits in the middle of a voice line.
render('Social_1200x630', 1200, 630, page(1200, 630, `
  <div class="abs" style="left:0;top:175px">${ribbon(1200, 280, { n: 72, keys: ['hello', 'welcome', 'es'], fadeL: 0.3, fadeR: 0.3, seed: 11, opacity: 0.9, hole: [405, 795] })}</div>
  <div class="abs" style="left:${(1200 - 300) / 2}px;top:${(630 - 300) / 2}px">${mark(300, { id: 'sm' })}</div>`,
  `.bg{background:radial-gradient(ellipse 60% 75% at 50% 50%, rgba(29,185,146,.22), transparent 70%), linear-gradient(160deg, ${C.bg2}, ${C.bg} 60%, #090c0b)}`), OUT_KEYS);

// ---------------------------------------------------------------- screenshots (1920x1080, captions allowed)
const shot = f => pathToFileURL(path.join(SHOTS, f)).href;
const SHOT_CSS = `
.cap{position:absolute;left:120px;top:0;bottom:0;width:640px;display:flex;flex-direction:column;justify-content:center}
.cap .k{font:700 24px UI;letter-spacing:.14em;text-transform:uppercase;color:${C.mint};margin-bottom:22px}
.cap h1{font-family:Display;font-weight:900;font-size:78px;line-height:1.02;letter-spacing:-.03em;color:${C.ink}}
.cap p{font:400 32px UI;line-height:1.42;color:${C.sub};margin-top:30px}
.cap p b{font-weight:600;color:${C.ink}}
.win{position:absolute;border-radius:12px;overflow:hidden;box-shadow:0 30px 80px rgba(0,0,0,.55),0 0 0 1px rgba(255,255,255,.08)}
.win img{display:block}
.kw{color:${C.mint2}} .str{color:#e9d8a6} .cls{color:#9fd9ff} .dim{color:#5f736c} .num{color:#f0b38a}
.brand{position:absolute;left:120px;bottom:64px;display:flex;align-items:center;gap:16px;font:700 26px UI;color:${C.mute}}`;
const brand = `<div class="brand">${mark(40, { glow: false, id: 'b' })}<span>Local TTS</span></div>`;
const caption = (kicker, title, text) => `<div class="cap"><div class="k">${kicker}</div><h1>${title}</h1><p>${text}</p></div>`;
const S = (name, body) => render(name, 1920, 1080, page(1920, 1080, body + brand, SHOT_CSS), OUT_SHOTS);

// 01: the demo scene, full frame, as captured in Play Mode.
render('01_Demo_RealTime', 1920, 1080, page(1920, 1080, `<img class="abs" src="${shot('demo_speaking_1920.png')}" style="left:0;top:0;width:1920px;height:1080px">`), OUT_SHOTS);

S('02_Generator', caption('Generator window', 'Type it.<br>Hear it.<br>Save the .wav.',
  'Preview from memory, then save an AudioClip in one click. <b>Every Windows voice</b> with rate, pitch and volume, naming templates and batch generation from .txt or .csv.') + `
  <div class="win" style="left:840px;top:44px"><img src="${shot('generator_en.png')}" width="470" height="993"></div>
  <div class="win" style="left:1340px;top:44px"><img src="${shot('generator_en_bottom.png')}" width="470" height="993"></div>`);

S('03_Speaker', caption('TTS Speaker component', 'Lines in the Inspector.',
  'Write the lines, press ▶ to listen and <b>bake them with one click</b>. Call Speak() from UI buttons, XR interactions, Timeline or code. Speech goes through the AudioSource, so it can be 3D.') + `
  <div class="win" style="left:1000px;top:${(1080 - 959) / 2}px"><img src="${shot('speaker_en.png')}" width="676" height="959"></div>`);

const code = `<span class="kw">using</span> KrostGames.TTS;

<span class="dim">// Speak in real time: no component, no setup.</span>
<span class="cls">LocalTTS</span>.Speak(<span class="str">"Welcome, traveler!"</span>);

<span class="dim">// Any installed voice, with rate and pitch.</span>
<span class="cls">LocalTTS</span>.DefaultVoice = <span class="kw">new</span> <span class="cls">TTSVoiceOptions</span>(
    <span class="str">"Raul"</span>, <span class="cls">TTSLanguage</span>.Spanish, rate: <span class="num">1</span>, pitch: <span class="num">-1</span>);

<span class="dim">// Or get a clip you own.</span>
<span class="kw">var</span> clip = <span class="kw">await</span> <span class="cls">LocalTTS</span>.SynthesizeAsync(<span class="str">"Score: 120"</span>);`;
S('04_Code', caption('Scripting', 'One line<br>of code.',
  '<b>LocalTTS.Speak(text)</b> synthesizes and plays a line on the spot in the Windows Editor and Windows builds. Repeated lines play instantly from a cache.') + `
  <div class="win" style="left:830px;top:255px;width:990px;background:${C.card};padding:0">
    <div style="height:52px;background:#171f1c;display:flex;align-items:center;gap:10px;padding:0 22px">
      <i style="width:13px;height:13px;border-radius:50%;background:#3a4440"></i><i style="width:13px;height:13px;border-radius:50%;background:#3a4440"></i><i style="width:13px;height:13px;border-radius:50%;background:#3a4440"></i>
      <span style="margin-left:14px;font:600 20px UI;color:${C.mute}">Narrator.cs</span></div>
    <pre style="font:400 26px/1.62 Code;color:#e3ebe7;padding:30px 38px 38px;white-space:pre">${code}</pre></div>`,
);

const yes = `<span style="color:${C.mint2};font:700 34px UI">✓</span>`, no = `<span style="color:#5f736c;font:400 30px UI">—</span>`;
const row = (label, cells, last) => `<tr style="${last ? '' : 'border-bottom:1px solid rgba(255,255,255,.07)'}"><td style="padding:26px 30px;font:600 27px UI;color:${C.ink};text-align:left">${label}</td>${cells.map(c => `<td style="padding:26px 10px;text-align:center">${c}</td>`).join('')}</tr>`;
S('05_Platforms', caption('Every platform', 'Real time on Windows.<br>Baked clips everywhere.',
  'A TTS Speaker in <b>Auto</b> mode plays the baked clip when it matches the line, speaks in real time on Windows when it does not, and falls back to the baked clip on every other platform.') + `
  <div class="win" style="left:880px;top:300px;width:920px;background:${C.card}">
    <table style="width:100%;border-collapse:collapse">
      <tr style="background:#171f1c"><th></th>${['Windows<br>Editor', 'Windows<br>build', 'Android, iOS, WebGL,<br>consoles, macOS, Linux'].map(t => `<th style="padding:22px 10px;font:600 22px/1.25 UI;color:${C.mute}">${t}</th>`).join('')}</tr>
      ${row('Play baked clips', [yes, yes, yes])}
      ${row('Real-time speech', [yes, yes, no])}
      ${row('Generate and bake .wav', [yes, no, no], true)}
    </table></div>
  <div class="abs" style="left:880px;top:760px;width:920px">${ribbon(920, 120, { n: 64, keys: ['checkpoint', 'welcome'], seed: 5, opacity: 0.6 })}</div>`);

S('06_VoiceProfiles', caption('Voice Profiles', 'One voice,<br>many speakers.',
  'A profile stores voice, language, rate, pitch and volume. Share it between speakers and edit it once to change them all. <b>SAPI 5 and OneCore</b> voices, in any language you install.') + `
  <div class="win" style="left:1000px;top:150px"><img src="${shot('profile_en.png')}" width="728" height="473"></div>
  <div class="abs" style="left:1000px;top:690px;width:740px;display:flex;flex-wrap:wrap;gap:14px">
    ${['Zira · en-US', 'Sabina · es-MX', 'Raul · es-MX', 'Maria · pt-BR', 'Daniel · pt-BR'].map(v => `<span style="padding:12px 20px;border-radius:999px;background:rgba(29,185,146,.14);border:1px solid rgba(29,185,146,.35);font:600 24px UI;color:${C.mint2}">${v}</span>`).join('')}
  </div>
  <div class="abs" style="left:1002px;top:830px;font:400 22px UI;color:${C.mute}">Voices installed on the PC used for these screenshots.</div>`);

S('07_LineLibrary', caption('Line Library', 'The same code<br>speaks everywhere.',
  'Lines your code speaks in Play Mode are recorded automatically. <b>Bake pending</b> once and they play on Android, iOS, WebGL and consoles, loaded from Resources with no setup.') + `
  <div class="win" style="left:1040px;top:${(1080 - 783) / 2}px"><img src="${shot('library_en.png')}" width="630" height="783"></div>`);

S('08_Offline', `<div class="cap" style="width:900px">
    <div class="k">100% offline</div>
    <h1 style="font-size:96px">No cloud.<br>No API keys.<br>No tokens.</h1>
    <p>Speech is generated on your own PC with the voices already installed in Windows. <b>No internet connection and no cost per line.</b></p></div>
  <div class="abs" style="left:1220px;top:${(1080 - 420) / 2}px">${mark(420, { id: 'om' })}</div>`);

fs.rmSync(TMP, { recursive: true, force: true });
