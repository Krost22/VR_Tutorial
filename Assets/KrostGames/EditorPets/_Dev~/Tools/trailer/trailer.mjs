// Builds the Editor Pets trailer (1920x1080, 30 fps) from recorded Unity frames + the real sprite sheets.
// usage: node trailer.mjs <ffmpeg.exe> <out.mp4>
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const [FF, OUT] = process.argv.slice(2);
const HERE = path.dirname(new URL(import.meta.url).pathname).replace(/^\/([A-Za-z]:)/, '$1');
const FR = path.join(HERE, 'frames');
const SEG = path.join(HERE, 'segments'); fs.mkdirSync(SEG, { recursive: true });
const PETS = 'C:/Users/eduar/OneDrive/Documentos/Github/VR_Tutorial/Assets/KrostGames/EditorPets/Pets';
const BOLD = 'C\\:/Windows/Fonts/seguibl.ttf', SEMI = 'C\\:/Windows/Fonts/segoeuib.ttf', REG = 'C\\:/Windows/Fonts/segoeui.ttf';
const BG = '0x1b1c22', ORANGE = '0xff8246';

function ff(args, label) {
  const r = spawnSync(FF, ['-loglevel', 'error', '-y', ...args], { encoding: 'utf8', maxBuffer: 1 << 26 });
  if (r.status !== 0) { console.error(label, r.stderr); process.exit(1); }
  console.log('ok', label);
}
const esc = s => s.replace(/\\/g, '\\\\').replace(/:/g, '\\:').replace(/'/g, "\u2019").replace(/%/g, '\\%');
const text = (t, { font = SEMI, size = 48, color = 'white', x = '(w-text_w)/2', y = 100, alpha = '1', enable = null, box = null } = {}) =>
  `drawtext=fontfile='${font}':text='${esc(t)}':fontsize=${size}:fontcolor=${color}:x='${x}':y='${y}':alpha='${alpha}'` +
  (box ? `:box=1:boxcolor=${box}:boxborderw=22` : '') + (enable ? `:enable='${enable}'` : '');
const fadeIn = (d, len) => `fade=t=in:st=0:d=0.3,fade=t=out:st=${(len - 0.3).toFixed(2)}:d=0.3`;
// caption: orange accent bar + big line + small line, top-left, sliding in
const caption = (title, sub) => [
  `drawbox=x=90:y=70:w=10:h=${sub ? 118 : 70}:color=${ORANGE}:t=fill:enable='gte(t,0.15)'`,
  text(title, { font: BOLD, size: 62, x: '120+max(0,40-t*160)', y: 62, alpha: 'min(1,max(0,(t-0.1)*4))' }),
  ...(sub ? [text(sub, { font: REG, size: 34, color: '0xd8dae3', x: '122+max(0,40-(t-0.15)*160)', y: 142, alpha: 'min(1,max(0,(t-0.25)*4))' })] : []),
].join(',');
const GRAD = `${SEG}/caption_gradient.png`;
const enc = ['-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-r', '30', '-preset', 'medium', '-crf', '18'];

// sprite sheets: [file, frames, frame px]
const sheet = (pet, file, frames, px) => ({ file: `${PETS}/${pet}/${file}`, frames, px });
const CAST = [
  sheet('Corgi', 'Corgi Walking_pet 1.png', 17, 64), sheet('Mishy', 'Mishy_Walk.png', 4, 32), sheet('Red Dragon', 'RedDragon_Walk.png', 4, 40),
  sheet('Turtle', 'Turtle_Walk.png', 4, 32), sheet('Capybara', 'Capybara_Walk.png', 4, 32), sheet('Deer', 'Deer_Walk.png', 4, 40),
  sheet('Crowned Pigeon', 'CrownedPigeon_Walk.png', 4, 24), sheet('Tabby Cat', 'TabbyCat_Walk.png', 4, 32), sheet('Crab', 'Crab_Walk.png', 4, 24),
  sheet('Ice Dragon', 'IceDragon_Walk.png', 4, 40), sheet('Snake', 'Snake_Walk.png', 4, 32), sheet('Noah', 'Noah.png', 4, 64),
  sheet('Rock Pigeon', 'RockPigeon_Walk.png', 4, 24), sheet('White Dove', 'WhiteDove_Walk.png', 4, 24), sheet('Mourning Dove', 'MourningDove_Walk.png', 4, 24),
  sheet('Nicobar Pigeon', 'NicobarPigeon_Walk.png', 4, 24), sheet('Pixel Dog', 'Dog_Walk.png', 2, 64),
];
// animated, pixel-crisp sprite chain for input #i, displayed about `h` px tall
function sprite(i, s, h, fps = 8) {
  const k = Math.max(1, Math.round(h / s.px));
  return `[${i}:v]crop=${s.px}:${s.px}:'${s.px}*mod(floor(t*${fps}),${s.frames})':0,scale=${s.px * k}:${s.px * k}:flags=neighbor`;
}
const spriteInputs = (list, dur) => list.flatMap(s => ['-loop', '1', '-framerate', '30', '-t', String(dur), '-i', s.file]);

// ---------- recorded clips ----------
ff(['-f', 'lavfi', '-i', 'color=c=black:s=1920x320,format=rgba,geq=r=0:g=0:b=0:a=\'175*pow(1-Y/320\,1.7)\'', '-frames:v', '1', GRAD], 'gradient');
function clip(name, dir, from, to, layout, cap, sub) {
  const len = (to - from) / 20;
  const input = ['-framerate', '20', '-start_number', String(from), '-i', `${FR}/${dir}/f_%04d.jpg`, ...(layout === 'scene' ? ['-i', GRAD] : []), '-frames:v', String(Math.round(len * 30))];
  let vf;
  if (layout === 'scene') vf = `[0:v]fps=30,crop=1120:630:80:90,scale=1920:1080:flags=lanczos[s];[s][1:v]overlay=0:0,${caption(cap, sub)},${fadeIn(0, len)}`;
  else {
    const { w, x, y, crop } = layout;
    vf = `color=c=${BG}:s=1920x1080:r=30[bg];[0:v]fps=30,${crop ? `crop=${crop},` : ''}scale=${w}:-2:flags=lanczos,pad=iw+8:ih+8:4:4:color=0x3a3c46[win];` +
         `[bg][win]overlay=x=${x}:y='${y}+max(0,30-t*120)':shortest=1,${caption(cap, sub)},${fadeIn(0, len)}`;
  }
  ff([...input, '-filter_complex', vf, ...enc, `${SEG}/${name}.mp4`], name);
  return len;
}

function closeup(name, dir, from, to, crop, cap, sub, labels) {
  const len = (to - from) / 20;
  const tags = labels.map(([a, b, l]) => text(l, { font: BOLD, size: 72, color: ORANGE, y: 330, box: '0x1b1c22@0.8', enable: `between(t,${a},${b})` })).join(',');
  const vf = `[0:v]fps=30,crop=${crop},scale=1920:1080:flags=neighbor[s];[s][1:v]overlay=0:0,${caption(cap, sub)},${tags},${fadeIn(0, len)}`;
  ff(['-framerate', '20', '-start_number', String(from), '-i', `${FR}/${dir}/f_%04d.jpg`, '-i', GRAD, '-frames:v', String(Math.round(len * 30)), '-filter_complex', vf, ...enc, `${SEG}/${name}.mp4`], name);
  return len;
}

// ---------- designed cards ----------
function card(name, len, list, spritesGraph, texts) {
  const inputs = ['-f', 'lavfi', '-t', String(len), '-i', `color=c=${BG}:s=1920x1080:r=30`, ...spriteInputs(list, len)];
  const g = [`[0:v]drawbox=x=0:y=1000:w=1920:h=80:color=0x16171c:t=fill[b0]`];
  let last = 'b0';
  spritesGraph.forEach((sg, i) => { g.push(`${sg.chain}[s${i}]`); g.push(`[${last}][s${i}]overlay=x='${sg.x}':y='${sg.y}'${sg.enable ? `:enable='${sg.enable}'` : ''}[b${i + 1}]`); last = `b${i + 1}`; });
  g.push(`[${last}]${texts},${fadeIn(0, len)}[v]`);
  ff([...inputs, '-filter_complex', g.join(';'), '-map', '[v]', '-t', String(len), ...enc, `${SEG}/${name}.mp4`], name);
  return len;
}

const segs = [];
// 1. Intro: parade across the floor + title
{
  const len = 3.8, parade = CAST.slice(0, 7);
  const graph = parade.map((s, i) => ({ chain: sprite(i + 1, s, 120), x: `-260+t*420-${i * 150}+${6 * 150}`, y: `1000-h` }));
  segs.push(['intro', card('intro', len, parade, graph, [
    text('EDITOR PETS', { font: BOLD, size: 150, y: 300, alpha: 'min(1,t*2)' }),
    `drawbox=x=(1920-220)/2:y=480:w=220:h=8:color=${ORANGE}:t=fill:enable='gte(t,0.4)'`,
    text('Pixel pets that live in your Scene View', { font: REG, size: 48, color: '0xd8dae3', y: 520, alpha: 'min(1,max(0,(t-0.5)*2))' }),
  ].join(','))]);
}
// 2-6. Scene View footage over the pixel-art adventure level (2 -> 1 -> 3 -> 5 -> 6 pets)
segs.push(['a', clip('a', 'A_wander', 5, 95, 'scene', 'They live in your Scene View', 'Keeping you company while you build your game')]);
segs.push(['z', closeup('z', 'Z_closeup', 5, 145, '320:180:488:525', 'Hand-made pixel animations', 'Idle, walk, sleep, eat and happy for every pet',
  [[0, 1.35, 'IDLE'], [1.35, 2.85, 'WALK'], [2.85, 4.35, 'SLEEP'], [4.35, 5.85, 'EAT'], [5.85, 7, 'HAPPY']])]);
segs.push(['b', clip('b', 'B_pet', 0, 80, 'scene', 'Click to pet them', 'Hearts guaranteed')]);
segs.push(['c', clip('c', 'C_ball', 8, 118, 'scene', 'Play fetch', 'Throw the ball and watch them chase it')]);
segs.push(['d', clip('d', 'D_feed', 0, 90, 'scene', 'Feed All', 'Every species eats its own favourite food')]);
// 7-9. Editor windows
segs.push(['g', clip('g', 'G_welcome', 35, 165, { w: 900, x: 510, y: 190 }, 'Choose your first pet', 'A starter pick on first launch. Every pet is unlocked')]);
segs.push(['e', clip('e', 'E_window', 0, 130, { w: 1240, x: 336, y: 212 }, 'Manage your whole crew', 'Search, filter, show / hide, solo and duplicate')]);
segs.push(['f', clip('f', 'F_food', 16, 96, { w: 1600, x: 156, y: 330, crop: '1000:330:0:390' }, 'Pick the food in one click', '16 pixel-art foods, or drop in your own')]);
// 9. Make your own pet: sprite sheet -> walking pet
{
  const len = 4.5, mishy = { file: `${PETS}/Mishy/Mishy_Walk.png`, frames: 4, px: 32 };
  segs.push(['make', card('make', len, [mishy, mishy], [
    { chain: `[1:v]scale=128*6:32*6:flags=neighbor`, x: 150, y: 420 },
    { chain: sprite(2, mishy, 256), x: 1420, y: 400, enable: 'gte(t,1.2)' },
  ], [
    text('Make your own pet', { font: BOLD, size: 84, y: 120 }),
    text('Draw a sprite sheet, select it, click + New Pet. Frames are detected automatically.', { font: REG, size: 38, color: '0xd8dae3', y: 240 }),
    text('>', { font: BOLD, size: 140, color: ORANGE, x: 1080, y: 430, alpha: 'min(1,max(0,(t-0.6)*3))' }),
    text('sprite sheet', { font: REG, size: 30, color: '0x9a9caa', x: 150, y: 640 }),
    text('your pet', { font: REG, size: 30, color: '0x9a9caa', x: 1480, y: 690, alpha: 'min(1,max(0,(t-1.2)*3))' }),
  ].join(','))]);
}
// 10. The cast
{
  const len = 4.5, cols = 9;
  const graph = CAST.map((s, i) => {
    const row = Math.floor(i / cols), col = i % cols, n = row === 0 ? cols : CAST.length - cols;
    const x0 = (1920 - n * 200) / 2 + col * 200 + 100, yb = row === 0 ? 560 : 830;
    return { chain: sprite(i + 1, s, 140), x: `${x0}-w/2`, y: `${yb}-h`, enable: `gte(t,${(0.15 + i * 0.06).toFixed(2)})` };
  });
  segs.push(['cast', card('cast', len, CAST, graph, [
    text('17 pets included', { font: BOLD, size: 84, y: 110 }),
    text('Each with Idle, Walk, Sleep, Eat and Happy animations', { font: REG, size: 38, color: '0xd8dae3', y: 225 }),
  ].join(','))]);
}
// 11. Outro
{
  const len = 4, trio = [CAST[2], CAST[3], CAST[10]];
  segs.push(['outro', card('outro', len, trio, trio.map((s, i) => ({ chain: sprite(i + 1, s, 150), x: `${760 + i * 200}-w/2+100`, y: '1000-h' })), [
    text('EDITOR PETS', { font: BOLD, size: 140, y: 250 }),
    `drawbox=x=(1920-220)/2:y=420:w=220:h=8:color=${ORANGE}:t=fill`,
    text('Unity 2022.3+  ·  Editor-only  ·  Light & dark themes', { font: REG, size: 40, color: '0xd8dae3', y: 460 }),
    text('Available on the Unity Asset Store', { font: SEMI, size: 52, color: ORANGE, y: 560, alpha: 'min(1,max(0,(t-0.5)*2))' }),
  ].join(','))]);
}

// ---------- music: cozy chiptune (I-vi-IV-V), generated here, no third-party audio ----------
const total = segs.reduce((a, [, l]) => a + l, 0);
{
  const sr = 44100, n = Math.ceil((total + 0.5) * sr), bpm = 112, beat = 60 / bpm;
  const midi = m => 440 * 2 ** ((m - 69) / 12);
  const chords = [[60, 64, 67], [57, 60, 64], [53, 57, 60], [55, 59, 62]];
  const melody = [76, 79, 81, 79, 76, 74, 72, 74, 72, 76, 77, 76, 74, 71, 67, 71];
  const mix = new Float32Array(n);
  let pBass = 0, pArp = 0, pLead = 0, pKick = 0, lpArp = 0, lpLead = 0, lpNoise = 0, lastBeat = -1, seed = 1;
  const noise = () => ((seed = (seed * 16807) % 2147483647) / 2147483647) * 2 - 1;
  const lp = (y, x, fc) => y + (1 - Math.exp(-2 * Math.PI * fc / sr)) * (x - y);
  for (let i = 0; i < n; i++) {
    const t = i / sr, b = t / beat, beatN = Math.floor(b), tb = (b - beatN) * beat; // tb: seconds since this beat
    const ch = chords[Math.floor(b / 4) % 4];
    // bass: triangle, one note per half bar
    const tHalf = (b % 2) * beat; pBass = (pBass + midi(ch[0] - 24) / sr) % 1;
    let v = 0.20 * (1 - 4 * Math.abs(pBass - 0.5)) * Math.exp(-tHalf * 1.8);
    // arpeggio: 25% pulse, 8th notes, low-passed
    const e8 = Math.floor(b * 2), t8 = (b * 2 - e8) * beat / 2;
    pArp = (pArp + midi(ch[e8 % 3] + 12) / sr) % 1;
    lpArp = lp(lpArp, (pArp < 0.25 ? 1 : -1) * Math.exp(-t8 * 7), 2200); v += 0.07 * lpArp;
    // lead: triangle melody from bar 3
    if (b >= 8) { pLead = (pLead + midi(melody[e8 % 16]) / sr) % 1; lpLead = lp(lpLead, (1 - 4 * Math.abs(pLead - 0.5)) * Math.exp(-t8 * 3), 3000); v += 0.10 * lpLead; }
    // kick: pitch drops 160 -> 50 Hz within each beat (phase reset per beat)
    if (beatN !== lastBeat) { pKick = 0; lastBeat = beatN; }
    const fk = 50 + 110 * Math.exp(-tb * 30); pKick += fk / sr;
    v += 0.45 * Math.sin(2 * Math.PI * pKick) * Math.exp(-tb * 9);
    // soft hat on the off-beats: high-passed noise, very short
    const nz = noise(); lpNoise = lp(lpNoise, nz, 4000);
    const off = ((b + 0.5) % 1) * beat; v += 0.025 * (nz - lpNoise) * Math.exp(-off * 60);
    const fade = Math.max(0, Math.min(1, t / 1.0, (total - t) / 2.5));
    mix[i] = Math.tanh(v * 1.4) / Math.tanh(1.4) * fade;
  }
  let peak = 0; for (const x of mix) peak = Math.max(peak, Math.abs(x));
  const buf = Buffer.alloc(44 + n * 2);
  for (let i = 0; i < n; i++) buf.writeInt16LE(Math.round(mix[i] / peak * 0.7 * 32767), 44 + i * 2);
  buf.write('RIFF', 0); buf.writeUInt32LE(36 + n * 2, 4); buf.write('WAVEfmt ', 8); buf.writeUInt32LE(16, 16); buf.writeUInt16LE(1, 20); buf.writeUInt16LE(1, 22);
  buf.writeUInt32LE(sr, 24); buf.writeUInt32LE(sr * 2, 28); buf.writeUInt16LE(2, 32); buf.writeUInt16LE(16, 34); buf.write('data', 36); buf.writeUInt32LE(n * 2, 40);
  fs.writeFileSync(`${SEG}/music.wav`, buf);
}

// ---------- concat + music ----------
fs.writeFileSync(`${SEG}/list.txt`, segs.map(([n]) => `file '${SEG.replace(/\\/g, '/')}/${n}.mp4'`).join('\n'));
ff(['-f', 'concat', '-safe', '0', '-i', `${SEG}/list.txt`, '-i', `${SEG}/music.wav`, '-map', '0:v', '-map', '1:a', '-c:v', 'copy', '-af', 'loudnorm=I=-16:TP=-1.5:LRA=11', '-ar', '48000', '-c:a', 'aac', '-b:a', '192k', '-shortest', '-movflags', '+faststart', OUT], 'final');
console.log('total', total.toFixed(1), 's ->', OUT);
