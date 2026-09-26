// Editor Pets logo + key art: a pixel-art capybara (with its yuzu) peeking diagonally from the bottom-right corner
// over a pastel sky. Shapes are rasterized directly in the rotated frame, so the 45° tilt stays clean pixel art.
// usage: node logo.mjs <outDir>   -> art-resolution PNGs (upscaled later with nearest neighbour)
import zlib from 'node:zlib';
import fs from 'node:fs';

const out = process.argv[2] || 'brand'; fs.mkdirSync(out, { recursive: true });
const hexRgb = h => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];
const lerp = (a, b, t) => { const A = hexRgb(a), B = hexRgb(b); return '#' + A.map((v, i) => Math.round(v + (B[i] - v) * t).toString(16).padStart(2, '0')).join(''); };

class C {
  constructor(w, h) { this.w = w; this.h = h; this.p = new Array(w * h).fill(null); }
  get(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h ? this.p[y * this.w + x] : null; }
  set(x, y, k) { if (x >= 0 && y >= 0 && x < this.w && y < this.h) this.p[y * this.w + x] = k; }
  each(fn) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) fn(x, y); }
  outline(k) { const add = []; this.each((x, y) => { if (this.get(x, y) === null && [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => this.get(x + dx, y + dy) !== null)) add.push([x, y]); }); for (const [x, y] of add) this.set(x, y, k); return this; }
  over(top) { top.each((x, y) => { const k = top.get(x, y); if (k) this.set(x, y, k); }); return this; }
  save(file) {
    const raw = Buffer.alloc((this.w * 3 + 1) * this.h);
    this.each((x, y) => { const [r, g, b] = hexRgb(this.get(x, y) || '#000000'); const o = y * (this.w * 3 + 1) + 1 + x * 3; raw[o] = r; raw[o + 1] = g; raw[o + 2] = b; });
    const chunk = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(zlib.crc32(td)); return Buffer.concat([l, td, c]); };
    const ih = Buffer.alloc(13); ih.writeUInt32BE(this.w, 0); ih.writeUInt32BE(this.h, 4); ih[8] = 8; ih[9] = 2; // 24-bit RGB, no alpha
    fs.writeFileSync(file, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ih), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]));
  }
}

// ----- pastel sky with soft clouds and sparkles -----
function sky(w, h, { clouds = [], sparkles = [], ground = 0 } = {}) {
  const c = new C(w, h);
  c.each((x, y) => c.set(x, y, lerp('#d4f1ff', '#9fd8f4', y / h)));
  for (const [cx, cy, s] of clouds) for (const [dx, dy, r] of [[0, 0, 9], [10, -5, 11], [22, 0, 9], [11, 3, 10]]) {
    const R = r * s;
    c.each((x, y) => { const ex = (x + .5 - (cx + dx * s)) / R, ey = (y + .5 - (cy + dy * s)) / (R * 0.75); if (ex * ex + ey * ey <= 1) c.set(x, y, y + .5 > cy + 4 * s ? '#e6f6ff' : '#ffffff'); });
  }
  for (const [sx, sy, big] of sparkles) {
    const pts = big ? [[0, -2], [0, -1], [0, 0], [0, 1], [0, 2], [-2, 0], [-1, 0], [1, 0], [2, 0]] : [[0, -1], [0, 0], [0, 1], [-1, 0], [1, 0]];
    for (const [dx, dy] of pts) c.set(sx + dx, sy + dy, (dx === 0 && dy === 0) ? '#fffbe0' : '#ffffff');
  }
  if (ground) c.each((x, y) => { const top = h - ground; if (y >= top) c.set(x, y, y < top + 3 ? (y === top && x % 3 === 0 ? '#8fe05a' : '#5fbf3c') : ((x * 7 + y * 13) % 11 === 0 ? '#7e4e28' : '#9a6436')); });
  return c;
}

// ----- the capybara, drawn in its own frame (snout towards -x, body towards +x), rotated by `deg` -----
function capybara(w, h, cx, cy, scale, deg = 45) {
  if (Array.isArray(scale)) [scale, deg] = scale;
  const t = deg * Math.PI / 180, cos = Math.cos(t), sin = Math.sin(t);
  const ell = (x0, y0, rx, ry) => (x, y) => ((x - x0) / rx) ** 2 + ((y - y0) / ry) ** 2 <= 1;
  const cap = (x0, y0, x1, y1, r) => (x, y) => {
    const vx = x1 - x0, vy = y1 - y0, px = x - x0, py = y - y0, u = Math.max(0, Math.min(1, (px * vx + py * vy) / (vx * vx + vy * vy)));
    return (px - u * vx) ** 2 + (py - u * vy) ** 2 <= r * r;
  };
  const B = '#a97a52', D = '#8b603d', N = '#4a3222';
  const layers = [
    [cap(14, 10, 120, 58, 21), B],                   // neck + body, runs off the corner
    [cap(22, -9, 120, 36, 5), D],                    // darker back line
    [ell(0, 0, 21, 17), B],                       // head
    [ell(4, -9, 15, 6), D],                       // top of the head
    [cap(-10, 4, -23, 6, 11), B],                    // big square muzzle
    [ell(-30.5, 1.5, 2.8, 2.4), N],                     // nose
    [ell(-13, 7, 3.6, 2.1), '#f2a3a3'],           // blush
    [ell(-7, -5, 3.3, 3.3), '#1a120a'],           // eye
    [ell(-8.2, -6.6, 1.2, 1.2), '#ffffff'],             // eye shine
    [ell(13, -14, 4.8, 4.2), D],                  // ear
    [ell(13, -13, 2.3, 2.1), N],
    [ell(-1, -20, 8.5, 7.5), '#ffb52e'],          // yuzu
    [ell(-4, -23, 2.4, 1.7), '#ffe08a'],
    [cap(0, -27, 0, -29, 0.7), '#6b4226'],           // stem
    [cap(1, -28.5, 7, -31, 1.6), '#5ab04a'],         // leaf
  ];
  const layer = new C(w, h);
  layer.each((x, y) => {
    const dx = x + .5 - cx, dy = y + .5 - cy;
    const lx = (dx * cos + dy * sin) / scale, ly = (-dx * sin + dy * cos) / scale;
    for (const [shape, col] of layers) if (shape(lx, ly)) layer.set(x, y, col);
  });
  return layer.outline('#3a2414');
}

// Sizes are "art pixels"; each image is upscaled by an integer factor later.
const images = {
  icon_small: { w: 80, h: 80, capy: [45, 50, [0.9, 22]], clouds: [], sparkles: [[10, 12, 1], [70, 70, 0]] },
  logo:   { w: 128, h: 128, capy: [76, 84, 1.42], clouds: [[16, 22, 0.8], [70, 12, 0.55]], sparkles: [[20, 58, 1], [44, 30, 0], [100, 22, 1]] },
  icon:   { w: 80, h: 80, capy: [46, 53, 0.9], clouds: [[8, 14, 0.5]], sparkles: [[14, 38, 1], [60, 10, 0]] },
  card:   { w: 210, h: 140, capy: [162, 100, 1.2], clouds: [[150, 16, 0.6], [196, 44, 0.4]], sparkles: [[126, 30, 1], [196, 18, 0]] },
  cover:  { w: 390, h: 260, ground: 12, capy: [312, 196, 2.45], clouds: [[230, 30, 0.9], [330, 20, 0.7], [370, 70, 0.5]], sparkles: [[210, 70, 1], [270, 50, 0], [360, 110, 1], [200, 120, 0]] },
  social: { w: 240, h: 126, ground: 10, capy: [188, 96, 1.4], clouds: [[20, 20, 0.7], [90, 12, 0.5], [150, 26, 0.45]], sparkles: [[60, 40, 1], [120, 36, 0], [218, 22, 1]] },
};
for (const [name, o] of Object.entries(images)) {
  const bg = sky(o.w, o.h, o);
  bg.over(capybara(o.w, o.h, ...o.capy));
  bg.save(`${out}/${name}_art.png`);
}
console.log('brand art written to', out);
