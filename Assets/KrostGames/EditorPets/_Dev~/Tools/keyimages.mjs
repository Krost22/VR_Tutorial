// Composes the Asset Store key images from the brand art (+ real pet sprites) at their exact sizes, 24-bit PNG.
// usage: node keyimages.mjs <ffmpeg.exe> <outDir>
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
const [FF, OUT] = process.argv.slice(2); fs.mkdirSync(OUT, { recursive: true });
const PETS = 'C:/Users/eduar/OneDrive/Documentos/Github/VR_Tutorial/Assets/KrostGames/EditorPets/Pets';
const BLACK = 'C\\:/Windows/Fonts/seguibl.ttf', SEMI = 'C\\:/Windows/Fonts/segoeuib.ttf';
const NAVY = '0x1d3354';
const ff = (args, label) => { const r = spawnSync(FF, ['-loglevel', 'error', '-y', ...args], { encoding: 'utf8' }); if (r.status) { console.error(label, r.stderr); process.exit(1); } console.log('ok', label); };
const esc = s => s.replace(/:/g, '\\:').replace(/'/g, '\u2019');
const text = (t, font, size, x, y, color = NAVY) => `drawtext=fontfile='${font}':text='${esc(t)}':fontsize=${size}:fontcolor=${color}:x=${x}:y=${y}`;
// first Idle frame of a pet, scaled by an integer so its pixels match the art grid
const pet = (file, px) => ({ file: `${PETS}/${file}`, px });

function compose(name, art, scale, w, h, pets = [], texts = []) {
  const inputs = ['-i', `brand/${art}`, ...pets.flatMap(p => ['-i', p.file])];
  const g = [`[0:v]scale=iw*${scale}:ih*${scale}:flags=neighbor[b0]`];
  let last = 'b0';
  pets.forEach((p, i) => {
    g.push(`[${i + 1}:v]crop=${p.px}:${p.px}:0:0,scale=${p.px * scale}:${p.px * scale}:flags=neighbor${p.flip ? ',hflip' : ''}[p${i}]`);
    g.push(`[${last}][p${i}]overlay=x=${p.x}:y=${h}-${p.ground}-h[b${i + 1}]`); last = `b${i + 1}`;
  });
  g.push(`[${last}]${texts.length ? texts.join(',') + ',' : ''}format=rgb24[v]`);
  ff([...inputs, '-filter_complex', g.join(';'), '-map', '[v]', '-frames:v', '1', `${OUT}/${name}.png`], name);
}

// Icon 160x160: no text
compose('Icon_160x160', 'icon_art.png', 2, 160, 160);
// Package logo 512x512 (also used as the in-editor icon)
compose('Logo_512x512', 'logo_art.png', 4, 512, 512);
// Card 420x280: title + publisher only
compose('Card_420x280', 'card_art.png', 2, 420, 280, [], [
  text('Editor', BLACK, 58, 26, 58), text('Pets', BLACK, 58, 26, 118), text('by KrostGames', SEMI, 20, 30, 196, '0x3a5a80'),
]);
// Cover 1950x1300: title, tagline, publisher + a few pets on the grass
compose('Cover_1950x1300', 'cover_art.png', 5, 1950, 1300, [
  { ...pet('Deer/Deer_Idle.png', 40), x: 90, ground: 60 },
  { ...pet('Tabby Cat/TabbyCat_Idle.png', 32), x: 330, ground: 60 },
  { ...pet('Mishy/Mishy_Idle.png', 32), x: 520, ground: 60 },
  { ...pet('Red Dragon/RedDragon_Idle.png', 40), x: 710, ground: 60 },
  { ...pet('Turtle/Turtle_Idle.png', 32), x: 940, ground: 60 },
], [
  text('EDITOR PETS', BLACK, 190, 100, 150), text('Pixel pets that live in your Scene View', SEMI, 66, 108, 380),
  text('by KrostGames', SEMI, 46, 110, 480, '0x3a5a80'),
]);
// Social 1200x630: no text
compose('Social_1200x630', 'social_art.png', 5, 1200, 630, [
  { ...pet('Mishy/Mishy_Idle.png', 32), x: 120, ground: 50 },
  { ...pet('Red Dragon/RedDragon_Idle.png', 40), x: 330, ground: 50 },
  { ...pet('Crowned Pigeon/CrownedPigeon_Idle.png', 24), x: 590, ground: 50 },
]);
