// Records the Editor region (screen 0,43 1920x989) with wall-clock timestamps while a director script runs.
// usage: node record.mjs <director.cs> <events.json> <out.nut> [maxSeconds]
import { spawn } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { evalFile } from './unity_eval.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const [director, eventsFile, out, maxSeconds = '90'] = process.argv.slice(2);
const events = path.resolve(HERE, eventsFile), error = events.replace(/\.json$/, '.error.txt');
for (const f of [events, error]) fs.rmSync(f, { force: true });

const ff = spawn('ffmpeg', ['-hide_banner', '-loglevel', 'error', '-y', '-use_wallclock_as_timestamps', '1', '-f', 'gdigrab', '-framerate', '30',
  '-draw_mouse', '0', '-offset_x', '0', '-offset_y', '43', '-video_size', '1920x989', '-i', 'desktop',
  '-copyts', '-c:v', 'libx264', '-preset', 'ultrafast', '-crf', '12', '-pix_fmt', 'yuv444p', path.resolve(HERE, out)], { stdio: ['pipe', 'inherit', 'inherit'] });
await new Promise(r => setTimeout(r, 1500));
console.log('director:', evalFile(path.resolve(HERE, director)));
const t0 = Date.now();
while (!fs.existsSync(events) && !fs.existsSync(error) && Date.now() - t0 < Number(maxSeconds) * 1000) await new Promise(r => setTimeout(r, 250));
await new Promise(r => setTimeout(r, 1200));
ff.stdin.write('q');
await new Promise(r => ff.on('exit', r));
console.log(fs.existsSync(error) ? 'ERROR: ' + fs.readFileSync(error, 'utf8').slice(0, 1500) : 'events: ' + events);
