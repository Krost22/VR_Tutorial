// Pixel-art adventure level used as the "game in development" behind the pets in the trailer.
// Writes PNGs (16 px = 1 world unit) into ./level/. Colors are drawn directly as hex strings.
import zlib from 'node:zlib';
import fs from 'node:fs';

class C {
  constructor(w, h) { this.w = w; this.h = h; this.p = new Array(w * h).fill(null); }
  set(x, y, k) { x = Math.floor(x); y = Math.floor(y); if (x >= 0 && y >= 0 && x < this.w && y < this.h) this.p[y * this.w + x] = k; }
  get(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h ? this.p[y * this.w + x] : null; }
  each(fn) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) fn(x, y); }
  ellipse(cx, cy, rx, ry, k, pred) { this.each((x, y) => { const dx = (x + .5 - cx) / rx, dy = (y + .5 - cy) / ry; if (dx * dx + dy * dy <= 1 && (!pred || pred(x, y))) this.set(x, y, k); }); }
  rect(x, y, w, h, k) { for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) this.set(x + i, y + j, k); }
  poly(pts, k) {
    this.each((x, y) => {
      const px = x + .5, py = y + .5; let inside = false;
      for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) { const [xi, yi] = pts[i], [xj, yj] = pts[j]; if ((yi > py) !== (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) inside = !inside; }
      if (inside) this.set(x, y, k);
    });
  }
  outline(k = '#1b1b2a') {
    const add = [];
    this.each((x, y) => { if (this.get(x, y) === null && [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => this.get(x + dx, y + dy) !== null)) add.push([x, y]); });
    for (const [x, y] of add) this.set(x, y, k);
    return this;
  }
  save(file) {
    const raw = Buffer.alloc((this.w * 4 + 1) * this.h);
    this.each((x, y) => { const k = this.get(x, y); if (!k) return; const o = y * (this.w * 4 + 1) + 1 + x * 4; raw[o] = parseInt(k.slice(1, 3), 16); raw[o + 1] = parseInt(k.slice(3, 5), 16); raw[o + 2] = parseInt(k.slice(5, 7), 16); raw[o + 3] = 255; });
    const chunk = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(zlib.crc32(td)); return Buffer.concat([l, td, c]); };
    const ih = Buffer.alloc(13); ih.writeUInt32BE(this.w, 0); ih.writeUInt32BE(this.h, 4); ih[8] = 8; ih[9] = 6;
    fs.writeFileSync(file, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ih), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]));
  }
}
const out = process.argv[2] || 'level'; fs.mkdirSync(out, { recursive: true });
const lerp = (a, b, t) => '#' + [1, 3, 5].map(i => Math.round(parseInt(a.slice(i, i + 2), 16) * (1 - t) + parseInt(b.slice(i, i + 2), 16) * t).toString(16).padStart(2, '0')).join('');
const rnd = (() => { let s = 7; return () => (s = (s * 16807) % 2147483647) / 2147483647; })();

// ---------- background (whole view: 432 x 240 px = 27 x 15 units) ----------
{
  const c = new C(432, 240);
  for (let y = 0; y < 240; y++) { const col = lerp('#4fb8f5', '#c8f1ff', Math.min(1, y / 170)); for (let x = 0; x < 432; x++) c.set(x, y, col); }
  c.ellipse(372, 38, 20, 20, '#fff6c2'); c.ellipse(372, 38, 14, 14, '#ffe66b');
  const cloud = (x, y, s) => { for (const [dx, dy, r] of [[0, 0, 9], [10, -5, 11], [22, 0, 9], [11, 3, 10]]) c.ellipse(x + dx * s, y + dy * s, r * s, r * s * 0.8, '#ffffff'); c.rect(Math.round(x - 6 * s), Math.round(y + 3 * s), Math.round(34 * s), Math.round(4 * s), '#e2f3ff'); };
  cloud(60, 40, 1); cloud(210, 26, 0.8); cloud(300, 70, 0.6); cloud(20, 95, 0.5);
  // far mountains with snow caps
  const peaks = [[-20, 150], [40, 92], [95, 150], [150, 80], [215, 150], [265, 100], [320, 150], [380, 88], [450, 150]];
  for (let i = 0; i + 2 < peaks.length; i += 2) {
    const [a, b, d] = [peaks[i], peaks[i + 1], peaks[i + 2]];
    c.poly([[a[0], 200], a, b, d, [d[0], 200]], '#8190c9');
    c.poly([[b[0] - 12, b[1] + 16], b, [b[0] + 12, b[1] + 16], [b[0] + 5, b[1] + 13], [b[0], b[1] + 18], [b[0] - 6, b[1] + 13]], '#eef4ff');
  }
  for (let x = 0; x < 432; x++) for (let y = 150; y < 200; y++) if (c.get(x, y) === '#8190c9' && (x + y) % 7 === 0) c.set(x, y, '#7382bb');
  // rolling hills: back layer, castle on its own hill, front layer
  for (let x = 0; x < 432; x++) { const h1 = 182 + Math.sin(x / 40) * 10 + Math.sin(x / 13) * 3; for (let y = Math.floor(h1); y < 240; y++) c.set(x, y, '#5aa83f'); }
  // castle on a hill
  c.ellipse(300, 196, 70, 40, '#5aa83f');
  const stone = '#9d98b8', dark = '#7c7797', roof = '#d9544f';
  c.rect(276, 128, 48, 40, stone);
  for (const tx of [266, 322]) { c.rect(tx, 112, 14, 56, stone); c.poly([[tx - 2, 113], [tx + 7, 94], [tx + 16, 113]], roof); c.rect(tx + 5, 125, 4, 7, '#3a3550'); }
  c.rect(292, 104, 16, 26, stone); c.poly([[289, 105], [300, 82], [311, 105]], roof);
  c.rect(299, 72, 1, 12, '#5a4a3a'); c.rect(300, 72, 7, 4, '#ffd23f');
  for (let x = 276; x < 324; x += 6) c.rect(x, 124, 4, 4, stone);
  c.rect(294, 148, 12, 20, '#4a3528'); c.ellipse(300, 148, 6, 5, '#4a3528');
  for (const [x, y] of [[283, 136], [313, 136], [297, 112]]) c.rect(x, y, 4, 6, '#3a3550');
  for (let y = 130; y < 168; y += 5) for (let x = 277 + (y % 2) * 3; x < 323; x += 7) c.set(x, y, dark);
  for (let x = 0; x < 432; x++) { const h2 = 205 + Math.sin(x / 30 + 2) * 8; for (let y = Math.floor(h2); y < 240; y++) c.set(x, y, '#4a9235'); }
  for (let i = 0; i < 40; i++) { const x = rnd() * 432, y = 212 + rnd() * 26; c.ellipse(x, y, 3 + rnd() * 3, 2, '#3f8230'); }
  c.save(`${out}/background.png`);
}

// ---------- ground strip: grass on dirt with flowers (432 x 16) ----------
{
  const c = new C(432, 16);
  c.rect(0, 0, 432, 16, '#9a6436');
  for (let i = 0; i < 90; i++) c.set(rnd() * 432, 5 + rnd() * 11, rnd() > 0.5 ? '#7e4e28' : '#b07845');
  c.rect(0, 0, 432, 4, '#5fbf3c');
  for (let x = 0; x < 432; x++) { if (x % 3 === 0) c.set(x, 4, '#5fbf3c'); if (x % 5 === 0) c.set(x, 0, '#8fe05a'); }
  c.save(`${out}/ground.png`);
}

// ---------- props (outlined like the pets) ----------
const prop = (name, w, h, draw) => { const c = new C(w, h); draw(c); c.outline(); c.save(`${out}/${name}.png`); };
prop('tree', 44, 60, c => {
  c.rect(19, 34, 7, 25, '#8a5a33'); c.rect(19, 34, 2, 25, '#a8703f'); c.rect(15, 56, 15, 3, '#8a5a33');
  for (const [x, y, r] of [[22, 22, 16], [11, 30, 10], [33, 30, 10], [22, 11, 11]]) c.ellipse(x, y, r, r * 0.85, '#3f9a3a');
  for (const [x, y, r] of [[18, 16, 7], [28, 22, 6], [12, 28, 5]]) c.ellipse(x, y, r, r * 0.8, '#5cc24a');
  for (const [x, y] of [[14, 22], [30, 30], [24, 12]]) c.ellipse(x, y, 1.6, 1.6, '#e0443a');
});
prop('bush', 26, 14, c => { for (const [x, y, r] of [[7, 8, 6], [13, 6, 7], [20, 8, 6]]) c.ellipse(x, y, r, r * 0.9, '#3f9a3a'); c.ellipse(11, 5, 3, 2, '#5cc24a'); c.set(18, 5, '#ff8fb0'); c.set(8, 7, '#ffffff'); });
prop('sign', 18, 20, c => { c.rect(8, 8, 3, 12, '#8a5a33'); c.rect(1, 2, 16, 8, '#b98552'); c.rect(1, 2, 16, 2, '#d9a06a'); c.rect(4, 5, 8, 1, '#6b4226'); c.rect(4, 7, 5, 1, '#6b4226'); c.poly([[13, 4], [16, 6], [13, 8]], '#6b4226'); });
const platform = (name, w) => prop(name, w, 18, c => {
  c.rect(0, 4, w, 12, '#9aa0b0');
  for (let y = 8; y < 16; y += 4) for (let x = (y / 4) % 2 * 4; x < w; x += 8) { c.rect(x, y, 1, 4, '#6f7584'); }
  c.rect(0, 12, w, 1, '#6f7584'); c.rect(0, 8, w, 1, '#6f7584');
  c.rect(0, 1, w, 4, '#5fbf3c'); for (let x = 0; x < w; x += 3) c.set(x, 0, '#8fe05a'); for (let x = 2; x < w; x += 5) c.set(x, 5, '#5fbf3c');
});
platform('platform_a', 72); platform('platform_b', 52);
prop('knight', 18, 26, c => {
  c.rect(5, 18, 3, 7, '#7c8699'); c.rect(10, 18, 3, 7, '#7c8699'); c.rect(4, 24, 4, 2, '#4a4f5c'); c.rect(10, 24, 4, 2, '#4a4f5c');
  c.rect(4, 10, 10, 9, '#c9d3e0'); c.rect(4, 15, 10, 2, '#8a5a33'); c.rect(8, 15, 2, 2, '#ffd23f');
  c.rect(4, 2, 10, 9, '#dfe6ef'); c.rect(6, 5, 7, 2, '#1b1b2a'); c.rect(8, 0, 2, 3, '#e0443a'); c.rect(9, -1, 3, 2, '#e0443a');
  c.rect(14, 5, 2, 13, '#e8eef6'); c.rect(13, 16, 4, 1, '#8a5a33'); c.rect(14, 17, 2, 2, '#6b4226');
  c.rect(1, 11, 4, 7, '#3a6fd8'); c.rect(2, 13, 2, 3, '#ffd23f');
});
prop('slime', 16, 12, c => { c.ellipse(8, 8, 7, 5.5, '#6fd36f', (x, y) => y < 12); c.rect(1, 9, 14, 2, '#6fd36f'); c.ellipse(5.5, 6, 2, 2, '#b8f5b0'); c.rect(9, 6, 1, 2, '#1b1b2a'); c.rect(12, 6, 1, 2, '#1b1b2a'); });
prop('chest', 18, 14, c => { c.rect(1, 5, 16, 9, '#a0632f'); c.rect(1, 1, 16, 5, '#c07a3c'); c.rect(1, 5, 16, 1, '#6b4226'); c.rect(7, 4, 4, 4, '#ffd23f'); c.rect(8, 5, 2, 2, '#8a6a10'); c.rect(1, 1, 1, 13, '#ffd23f'); c.rect(16, 1, 1, 13, '#ffd23f'); });
prop('coin', 10, 10, c => { c.ellipse(5, 5, 4, 4, '#ffd23f'); c.rect(4, 3, 2, 4, '#f0a800'); c.set(3, 3, '#fff5b8'); });
prop('flag', 14, 30, c => { c.rect(1, 1, 2, 29, '#d8d8e0'); c.poly([[3, 2], [13, 6], [3, 11]], '#ff8246'); c.rect(0, 27, 5, 3, '#6f7584'); });
console.log('level art written to', out);
