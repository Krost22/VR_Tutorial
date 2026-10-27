// Builds the Local TTS manuals (EN + ES) from README.md / LEEME.md, with screenshots and a cover,
// then prints them to PDF with Microsoft Edge (headless) into Documentation/.
// usage: node build.mjs            (writes manual_en.html, manual_es.html here and the PDFs in Documentation/)
//        node build.mjs --html     (HTML only)
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../..');                      // the package folder
const DOCS = path.join(ROOT, 'Documentation');
const VERSION = JSON.parse(fs.readFileSync(path.join(ROOT, 'package.json'), 'utf8')).version;

const dataUri = file => {
  const ext = path.extname(file).slice(1).toLowerCase();
  const mime = ext === 'jpg' || ext === 'jpeg' ? 'image/jpeg' : 'image/png';
  return `data:${mime};base64,${fs.readFileSync(file).toString('base64')}`;
};
const img = name => dataUri(path.join(HERE, 'img', name));
const LOGO = dataUri(path.join(ROOT, '_Dev~', 'Marketing', 'KrostGames_Logo_black.png'));

// ---------------------------------------------------------------- tiny Markdown -> HTML (what the READMEs use)
const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
function inline(s) {
  const codes = [];
  s = s.replace(/`([^`]+)`/g, (_, c) => { codes.push(c); return `\u0000${codes.length - 1}\u0000`; });
  s = esc(s)
    .replace(/\*\*([^*]+)\*\*/g, '<b>$1</b>')
    .replace(/(^|[^*])\*([^*\s][^*]*)\*/g, '$1<i>$2</i>')
    .replace(/\[([^\]]+)\]\((#[^)]*)\)/g, '$1')                      // in-page anchors: plain text in print
    .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2">$1</a>');
  return s.replace(/\u0000(\d+)\u0000/g, (_, i) => `<code>${esc(codes[+i])}</code>`);
}

function markdown(md, figures) {
  const lines = md.replace(/\r/g, '').split('\n');
  const out = [];
  let i = lines.findIndex(l => /^## /.test(l)), skipping = false;   // the cover replaces the title and intro
  while (i < lines.length) {
    const line = lines[i];
    if (/^## /.test(line)) skipping = /^## (Contents|Contenido)\b/.test(line);
    if (skipping && line.trim() === '---') { skipping = false; i++; continue; }
    if (skipping || /^# /.test(line) || line.trim() === '---') { i++; continue; }

    if (line.startsWith('```')) {
      const lang = line.slice(3).trim(); const code = [];
      for (i++; i < lines.length && !lines[i].startsWith('```'); i++) code.push(lines[i]);
      i++;
      out.push(`<pre class="${lang}"><code>${esc(code.join('\n'))}</code></pre>`);
      continue;
    }
    const h = line.match(/^(#{2,3}) (.*)$/);
    if (h) {
      const level = h[1].length, text = h[2];
      out.push(level === 2 ? `<h2>${inline(text)}</h2>` : `<h3>${inline(text)}</h3>`);
      if (figures[text]) out.push(figures[text]);
      i++; continue;
    }
    if (line.startsWith('|')) {
      const rows = [];
      for (; i < lines.length && lines[i].startsWith('|'); i++) rows.push(lines[i]);
      const cells = r => r.replace(/^\||\|$/g, '').split('|').map(c => c.trim());
      const head = cells(rows[0]);
      const body = rows.slice(2).map(cells);
      const thead = head.some(c => c) ? '<thead><tr>' + head.map(c => `<th>${inline(c)}</th>`).join('') + '</tr></thead>' : '';
      out.push('<table>' + thead + '<tbody>' +
        body.map(r => '<tr>' + r.map(c => `<td>${inline(c)}</td>`).join('') + '</tr>').join('') + '</tbody></table>');
      continue;
    }
    if (line.startsWith('> ')) {
      const quote = [];
      for (; i < lines.length && lines[i].startsWith('>'); i++) quote.push(lines[i].replace(/^>\s?/, ''));
      out.push(`<div class="note">${inline(quote.join(' '))}</div>`);
      continue;
    }
    if (/^(-|\d+\.) /.test(line)) {
      const ordered = /^\d+\./.test(line); const items = [];
      for (; i < lines.length && /^(-|\d+\.) /.test(lines[i]); i++) items.push(lines[i].replace(/^(-|\d+\.) /, ''));
      out.push(`<${ordered ? 'ol' : 'ul'}>` + items.map(t => `<li>${inline(t)}</li>`).join('') + `</${ordered ? 'ol' : 'ul'}>`);
      continue;
    }
    if (line.trim() === '') { i++; continue; }
    const para = [];
    for (; i < lines.length && lines[i].trim() !== '' && !/^(#|```|\||> |- |\d+\. |---)/.test(lines[i]); i++) para.push(lines[i]);
    out.push(`<p>${inline(para.join(' '))}</p>`);
  }
  return out.join('\n');
}

const fig = (src, caption, cls = '') => `<figure class="${cls}"><img src="${src}"><figcaption>${caption}</figcaption></figure>`;
const pair = (a, b, caption) => `<figure class="pair"><div><img src="${a}"><img src="${b}"></div><figcaption>${caption}</figcaption></figure>`;

const BOOKS = {
  en: {
    file: 'README.md', lang: 'en', pdf: 'LocalTTS_Manual_EN.pdf',
    title: 'Local TTS — User Manual',
    tagline: 'Offline text-to-speech for Unity with the voices installed in Windows',
    sub: 'Speak in real time, or bake lines into .wav clips that play on every platform. No cloud, no API keys, no tokens.',
    meta: `Version ${VERSION} · Unity 2022.3 LTS or newer · KrostGames`,
    toc: 'Contents',
    figures: {
      'The Generator window': pair(img('generator_en.png'), img('generator_en_bottom.png'), 'Tools > Local TTS > Generator: text, voice and output (left); last clip, batch, history and tools (right).'),
      'TTS Speaker component': fig(img('speaker_en.png'), 'A TTS Speaker using a Voice Profile, with three baked lines.', 'half'),
      'Line Library': fig(img('library_en.png'), 'The Line Library: lines recorded from code, one baked and two pending.', 'half'),
      'Voice Profiles': fig(img('profile_en.png'), 'A Voice Profile asset with the installed-voice picker and ▶ test.', 'half'),
    },
  },
  es: {
    file: 'LEEME.md', lang: 'es', pdf: 'LocalTTS_Manual_ES.pdf',
    title: 'Local TTS — Manual de usuario',
    tagline: 'Texto a voz offline para Unity con las voces que ya trae Windows',
    sub: 'Habla en tiempo real u hornea líneas en clips .wav que suenan en cualquier plataforma. Sin nube, sin claves de API, sin tokens.',
    meta: `Versión ${VERSION} · Unity 2022.3 LTS o superior · KrostGames`,
    toc: 'Contenido',
    figures: {
      'La ventana Generator': pair(img('generator_es.png'), img('generator_es_bottom.png'), 'Tools > Local TTS > Generator: texto, voz y salida (izquierda); último clip, lotes, historial y herramientas (derecha).'),
      'Componente TTS Speaker': fig(img('speaker_es.png'), 'Un TTS Speaker con un perfil de voz y tres líneas horneadas.', 'half'),
      'Line Library': fig(img('library_es.png'), 'La Line Library: líneas registradas desde código, una horneada y dos pendientes.', 'half'),
      'Perfiles de voz': fig(img('profile_es.png'), 'Un asset Voice Profile con el selector de voces instaladas y ▶ para probar.', 'half'),
    },
  },
};

const css = `
@page { size: A4; margin: 15mm 15mm 16mm 15mm; }
* { box-sizing: border-box; }
body { font-family: 'Segoe UI', Arial, sans-serif; color: #1c2522; font-size: 10.2pt; line-height: 1.5; margin: 0; }
h1 { font-family: 'Segoe UI Black', 'Segoe UI', sans-serif; font-size: 36pt; letter-spacing: -0.5pt; margin: 0; color: #0f1513; }
h2 { font-family: 'Segoe UI Black', 'Segoe UI', sans-serif; font-size: 17pt; color: #0f1513; border-bottom: 3px solid #1DB992; padding-bottom: 4px; margin: 18px 0 10px; break-after: avoid; }
h2.newpage { break-before: page; margin-top: 0; }
h3 { font-size: 12pt; color: #0d7a5f; margin: 14px 0 4px; break-after: avoid; }
p { margin: 6px 0; }
a { color: #0d7a5f; text-decoration: none; }
code { font-family: Consolas, monospace; background: #edf6f2; padding: 1px 4px; border-radius: 3px; font-size: 9pt; }
pre { background: #0f1513; color: #e8f2ee; padding: 10px 12px; border-radius: 6px; font-size: 8.4pt; line-height: 1.45; white-space: pre-wrap; break-inside: avoid; }
pre code { background: none; color: inherit; padding: 0; font-size: inherit; }
table { border-collapse: collapse; width: 100%; margin: 8px 0 12px; font-size: 9.2pt; break-inside: avoid; }
th { background: #0f1513; color: #fff; text-align: left; padding: 5px 8px; }
td { border-bottom: 1px solid #dfe9e5; padding: 5px 8px; vertical-align: top; }
tr:nth-child(even) td { background: #f5faf8; }
ul, ol { margin: 4px 0 8px; padding-left: 20px; } li { margin: 2px 0; }
.note { background: #eafaf4; border-left: 4px solid #1DB992; padding: 7px 12px; border-radius: 4px; margin: 8px 0; }
figure { margin: 10px 0 14px; text-align: center; break-inside: avoid; }
figure img { max-width: 100%; border-radius: 6px; border: 1px solid #cfdcd7; }
figure.half img { max-width: 62%; max-height: 165mm; }
figure.wide img { width: 100%; }
figure.pair > div { display: flex; gap: 10px; justify-content: center; }
figure.pair img { width: 48%; }
figcaption { font-size: 8.6pt; color: #5d6d67; margin-top: 4px; }
.cover { height: 262mm; display: flex; flex-direction: column; justify-content: space-between; break-after: page; }
.cover .brand { display: flex; align-items: center; justify-content: space-between; }
.cover .brand img { height: 92px; }
.cover .bar { height: 8px; width: 120px; background: #1DB992; border-radius: 4px; margin: 18px 0 14px; }
.cover .tagline { font-size: 16pt; color: #24332e; margin: 0 0 6px; font-weight: 600; }
.cover .sub { font-size: 11pt; color: #4b5b55; margin: 0; max-width: 150mm; }
.cover .meta { color: #5d6d67; font-size: 9.5pt; margin-top: 10px; }
.cover .shot { width: 100%; border-radius: 8px; border: 1px solid #cfdcd7; }
.cover .toc { columns: 2; font-size: 10pt; color: #24332e; } .cover .toc div { margin: 2px 0; }
.cover .toc-title { font-weight: 700; margin-bottom: 4px; }
`;

for (const [key, book] of Object.entries(BOOKS)) {
  const md = fs.readFileSync(path.join(ROOT, book.file), 'utf8');
  const sections = [...md.matchAll(/^## (.+)$/gm)].map(m => m[1]).filter(t => !/^(Contents|Contenido)$/.test(t));
  let body = markdown(md, book.figures);
  // Start a few big chapters on a new page.
  for (const h of [sections[5], sections[9]].filter(Boolean)) body = body.replace(`<h2>${inline(h)}</h2>`, `<h2 class="newpage">${inline(h)}</h2>`);
  const html = `<!doctype html><html lang="${book.lang}"><head><meta charset="utf-8"><title>${book.title}</title><style>${css}</style></head><body>
<section class="cover">
  <div>
    <div class="brand"><div><h1>Local TTS</h1></div><img src="${LOGO}" alt="KrostGames"></div>
    <div class="bar"></div>
    <p class="tagline">${book.tagline}</p>
    <p class="sub">${book.sub}</p>
    <div class="meta">${book.meta}</div>
  </div>
  <img class="shot" src="${img('demo_speaking.png')}">
  <div><div class="toc-title">${book.toc}</div><div class="toc">${sections.map((s, i) => `<div>${i + 1}. ${inline(s)}</div>`).join('')}</div></div>
</section>
${body}
</body></html>`;
  const htmlPath = path.join(HERE, `manual_${key}.html`);
  fs.writeFileSync(htmlPath, html);
  console.log('html', htmlPath);

  if (process.argv.includes('--html')) continue;
  fs.mkdirSync(DOCS, { recursive: true });
  const edge = ['C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Microsoft/Edge/Application/msedge.exe'].find(p => fs.existsSync(p));
  if (!edge) { console.error('Microsoft Edge not found: open the HTML and print it to PDF.'); continue; }
  const pdf = path.join(DOCS, book.pdf);
  const r = spawnSync(edge, ['--headless=new', '--disable-gpu', '--no-pdf-header-footer', '--run-all-compositor-stages-before-draw',
    `--print-to-pdf=${pdf}`, 'file:///' + htmlPath.replace(/\\/g, '/')], { encoding: 'utf8' });
  console.log(r.status === 0 && fs.existsSync(pdf) ? `pdf  ${pdf} (${(fs.statSync(pdf).size / 1024).toFixed(0)} KB)` : `pdf failed: ${r.stderr}`);
}
