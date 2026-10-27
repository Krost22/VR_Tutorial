// Renders walkthrough.html (a page whose frames are a pure function of time: await window.seek(t)) with headless Chrome over the
// DevTools protocol, no npm packages needed. Motion blur = several sub-frames per frame averaged by ffmpeg (tmix).
//
//   node render.mjs stills  <out-dir> t1 t2 ...       PNG per time + contact sheet
//   node render.mjs draft   <out.mp4>                 30 fps, half size, no blur (timing check)
//   node render.mjs master  <out.mp4> [--workers 3] [--sub 4] [--from s --to s]
//   node render.mjs mux     <video.mp4> <audio.wav> <out.mp4>
import { spawn, spawnSync } from 'node:child_process';
import fs from 'node:fs';
import http from 'node:http';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const CHROME = ['C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe']
  .find(p => fs.existsSync(p));
const FFMPEG = process.env.FFMPEG || 'ffmpeg';
const args = process.argv.slice(2);
const opt = (name, def) => { const i = args.indexOf('--' + name); return i >= 0 ? args[i + 1] : def; };

// ---------------------------------------------------------------- static server (+ Windows fonts, never copied into the repo)
const MIME = { '.html': 'text/html', '.js': 'text/javascript', '.json': 'application/json', '.png': 'image/png', '.jpg': 'image/jpeg',
  '.ttf': 'font/ttf', '.otf': 'font/otf', '.wav': 'audio/wav', '.m4a': 'audio/mp4', '.svg': 'image/svg+xml' };
function serve() {
  return new Promise(resolve => {
    const server = http.createServer((req, res) => {
      const url = decodeURIComponent(req.url.split('?')[0]);
      // /frames/A/00001.jpg -> $FRAMES/framesA/00001.jpg (the recorded takes live outside the repo)
      const fr = url.match(/^\/frames\/(\w+)\/(\d+\.jpg)$/);
      const file = fr ? path.join(process.env.FRAMES || '', 'frames' + fr[1], fr[2])
        : url.startsWith('/winfonts/') ? path.join('C:/Windows/Fonts', path.basename(url)) : path.join(HERE, url);
      fs.readFile(file, (err, data) => {
        if (err) { res.writeHead(404); res.end(); return; }
        res.writeHead(200, { 'Content-Type': MIME[path.extname(file).toLowerCase()] || 'application/octet-stream', 'Cache-Control': 'no-store' });
        res.end(data);
      });
    }).listen(0, '127.0.0.1', () => resolve(server));
  });
}

// ---------------------------------------------------------------- minimal DevTools client
async function openBrowser(port, width, height) {
  const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'tts-walkthrough-'));
  const chrome = spawn(CHROME, ['--headless=new', '--remote-debugging-port=0', `--user-data-dir=${profile}`, '--hide-scrollbars',
    '--mute-audio', '--force-color-profile=srgb', '--disable-lcd-text', '--font-render-hinting=none', '--force-device-scale-factor=1',
    `--window-size=${width},${height}`, 'about:blank'], { stdio: ['ignore', 'ignore', 'pipe'] });
  const wsUrl = await new Promise((resolve, reject) => {
    let buf = '';
    chrome.stderr.on('data', d => { buf += d; const m = buf.match(/DevTools listening on (ws:\/\/\S+)/); if (m) resolve(m[1]); });
    chrome.on('exit', c => reject(new Error('chrome exited ' + c)));
  });
  const httpBase = wsUrl.replace(/^ws/, 'http').replace(/\/devtools\/browser\/.*$/, '');
  const target = await (await fetch(`${httpBase}/json/new?about:blank`, { method: 'PUT' })).json();
  const ws = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise(r => ws.addEventListener('open', r, { once: true }));
  let id = 0; const pending = new Map(); const logs = [];
  ws.addEventListener('message', e => {
    const msg = JSON.parse(e.data);
    if (msg.id && pending.has(msg.id)) { const { resolve, reject } = pending.get(msg.id); pending.delete(msg.id); msg.error ? reject(new Error(JSON.stringify(msg.error))) : resolve(msg.result); }
    else if (msg.method === 'Runtime.consoleAPICalled' && ['error', 'warning'].includes(msg.params.type)) logs.push(msg.params.args.map(a => a.value ?? a.description).join(' '));
    else if (msg.method === 'Runtime.exceptionThrown') logs.push('EXCEPTION ' + JSON.stringify(msg.params.exceptionDetails).slice(0, 400));
  });
  const send = (method, params = {}) => new Promise((resolve, reject) => { const i = ++id; pending.set(i, { resolve, reject }); ws.send(JSON.stringify({ id: i, method, params })); });
  await send('Runtime.enable');
  await send('Page.enable');
  await send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: false });
  await send('Page.navigate', { url: `http://127.0.0.1:${port}/walkthrough.html?render=1` });
  for (let i = 0; i < 300; i++) {
    const r = await send('Runtime.evaluate', { expression: 'window.ready === true || (window.bootError ? "ERR " + window.bootError : false)', returnByValue: true });
    if (r.result.value === true) break;
    if (typeof r.result.value === 'string') throw new Error(r.result.value);
    await new Promise(res => setTimeout(res, 100));
  }
  const meta = (await send('Runtime.evaluate', { expression: 'JSON.stringify(window.FILM)', returnByValue: true })).result.value;
  return {
    film: JSON.parse(meta), logs,
    async frame(t, format = 'jpeg') {
      await send('Runtime.evaluate', { expression: `Promise.resolve(window.seek(${t})).then(() => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r))))`, awaitPromise: true });
      const shot = await send('Page.captureScreenshot', format === 'png' ? { format: 'png', optimizeForSpeed: true } : { format: 'jpeg', quality: 93, optimizeForSpeed: true });
      return Buffer.from(shot.data, 'base64');
    },
    async evaluate(expression) {
      const r = await send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
      return r.exceptionDetails ? 'EXCEPTION ' + JSON.stringify(r.exceptionDetails).slice(0, 500) : JSON.stringify(r.result.value, null, 1);
    },
    close() { try { ws.close(); } catch { } chrome.kill(); setTimeout(() => fs.rmSync(profile, { recursive: true, force: true }), 1500); },
  };
}

// ---------------------------------------------------------------- commands
const cmd = args[0];
const server = await serve();
const port = server.address().port;

async function renderRange(out, fps, sub, from, to, width, height) {
  const page = await openBrowser(port, 1920, 1080);
  const frames = Math.round((to - from) * fps);
  const vf = [];
  if (sub > 1) vf.push(`tmix=frames=${sub}`, `select='eq(mod(n\\,${sub})\\,${sub - 1})'`, `setpts=N/${fps}/TB`);
  if (width !== 1920) vf.push(`scale=${width}:${height}:flags=lanczos`);
  vf.push('format=yuv420p');
  const ff = spawn(FFMPEG, ['-v', 'error', '-y', '-f', 'image2pipe', '-c:v', 'mjpeg', '-framerate', String(fps * sub), '-i', '-',
    '-vf', vf.join(','), '-r', String(fps), '-c:v', 'libx264', '-preset', sub > 1 ? 'slow' : 'veryfast', '-crf', sub > 1 ? '16' : '22',
    '-colorspace', 'bt709', '-color_primaries', 'bt709', '-color_trc', 'bt709', '-movflags', '+faststart', out], { stdio: ['pipe', 'inherit', 'inherit'] });
  const started = Date.now();
  for (let i = 0; i < frames; i++) {
    const t0 = from + i / fps;
    for (let j = 0; j < sub; j++) {
      const offset = sub > 1 ? ((j + 0.5) / sub - 0.5) * 0.5 / fps : 0;   // 180-degree shutter
      const t = Math.min(page.film.duration - 1e-4, Math.max(0, t0 + offset));
      const jpg = await page.frame(t);
      if (!ff.stdin.write(jpg)) await new Promise(r => ff.stdin.once('drain', r));
    }
    if (i % 60 === 0) { const el = (Date.now() - started) / 1000; process.stdout.write(`\r[${from.toFixed(1)}-${to.toFixed(1)}] ${i}/${frames}  ${(i / Math.max(el, 0.001)).toFixed(1)} f/s  `); }
  }
  ff.stdin.end();
  await new Promise(r => ff.on('close', r));
  if (page.logs.length) console.log('\npage warnings:\n  ' + [...new Set(page.logs)].slice(0, 10).join('\n  '));
  page.close();
}

try {
  if (cmd === 'stills') {
    const dir = path.resolve(args[1]); fs.mkdirSync(dir, { recursive: true });
    const times = args.slice(2).map(Number);
    const page = await openBrowser(port, 1920, 1080);
    const files = [];
    for (const t of times) { const f = path.join(dir, `t${t.toFixed(2).padStart(6, '0')}.png`); fs.writeFileSync(f, await page.frame(t, 'png')); files.push(f); }
    if (page.logs.length) console.log('page warnings:\n  ' + [...new Set(page.logs)].slice(0, 10).join('\n  '));
    page.close();
    const cols = Math.min(4, files.length), rows = Math.ceil(files.length / cols);
    const inputs = files.flatMap(f => ['-i', f]);
    const labels = files.map((f, i) => `[${i}:v]scale=480:-1,drawtext=fontfile='C\\:/Windows/Fonts/segoeuib.ttf':text='${times[i].toFixed(2)}s':x=8:y=6:fontsize=18:fontcolor=white:box=1:boxcolor=black@0.7[v${i}]`);
    const pads = [];
    for (let i = files.length; i < cols * rows; i++) { labels.push(`color=c=white:s=480x270:d=1[v${i}]`); pads.push(i); }
    const layout = Array.from({ length: cols * rows }, (_, i) => `${(i % cols) * 480}_${Math.floor(i / cols) * 270}`).join('|');
    spawnSync(FFMPEG, ['-v', 'error', '-y', ...inputs, '-filter_complex', labels.join(';') + ';' + Array.from({ length: cols * rows }, (_, i) => `[v${i}]`).join('') +
      `xstack=inputs=${cols * rows}:layout=${layout}`, '-frames:v', '1', path.join(dir, 'sheet.png')], { stdio: 'inherit' });
    console.log('stills ->', dir);
  } else if (cmd === 'draft') {
    await renderRange(path.resolve(args[1]), 30, 1, 0, Number(opt('to', 0)) || (await (async () => { const p = await openBrowser(port, 1920, 1080); const d = p.film.duration; p.close(); return d; })()), 960, 540);
    console.log('\ndraft ->', args[1]);
  } else if (cmd === 'master') {
    const out = path.resolve(args[1]);
    const probe = await openBrowser(port, 1920, 1080); const film = probe.film; probe.close();
    const fps = film.fps, sub = Number(opt('sub', 4)), workers = Number(opt('workers', 3));
    const from = Number(opt('from', 0)), to = Number(opt('to', film.duration));
    const total = Math.round((to - from) * fps);
    const bounds = Array.from({ length: workers + 1 }, (_, k) => from + Math.round(total * k / workers) / fps);
    const parts = bounds.slice(0, -1).map((b, k) => out.replace(/\.mp4$/, `_part${k}.mp4`));
    await Promise.all(parts.map((p, k) => renderRange(p, fps, sub, bounds[k], bounds[k + 1], 1920, 1080)));
    const list = out.replace(/\.mp4$/, '_parts.txt');
    fs.writeFileSync(list, parts.map(p => `file '${p.replace(/\\/g, '/')}'`).join('\n'));
    spawnSync(FFMPEG, ['-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', list, '-c', 'copy', '-movflags', '+faststart', out], { stdio: 'inherit' });
    parts.forEach(p => fs.rmSync(p, { force: true })); fs.rmSync(list, { force: true });
    console.log('\nmaster ->', out);
  } else if (cmd === 'eval') {
    // node render.mjs eval <t> "<expression>"  -> seeks and prints the expression (debugging)
    const page = await openBrowser(port, 1920, 1080);
    await page.frame(Number(args[1]), 'png');
    const ws = null;
    console.log(await page.evaluate(args[2]));
    if (page.logs.length) console.log('page warnings:\n  ' + [...new Set(page.logs)].slice(0, 10).join('\n  '));
    page.close();
  } else if (cmd === 'cues') {
    // node render.mjs cues <out.json>  -> the film's timeline (duration, voice cues, clicks) for the soundtrack
    const page = await openBrowser(port, 1920, 1080);
    fs.writeFileSync(path.resolve(args[1]), JSON.stringify(page.film, null, 1));
    page.close();
    console.log('cues ->', args[1]);
  } else if (cmd === 'mux') {
    const [video, audio, out] = args.slice(1).map(a => path.resolve(a));
    spawnSync(FFMPEG, ['-v', 'error', '-y', '-i', video, '-i', audio, '-map', '0:v', '-map', '1:a', '-c:v', 'copy', '-c:a', 'aac', '-b:a', '256k',
      '-shortest', '-movflags', '+faststart', out], { stdio: 'inherit' });
    console.log('mux ->', out);
  } else {
    console.log('commands: stills <dir> t..., draft <out>, master <out> [--workers N --sub N --from s --to s], mux <video> <audio> <out>');
  }
} finally {
  server.close();
}
