// Pixel-art sprite generator for the bundled pets and items (dev tool, not shipped: lives in _Dev~).
// Shapes are drawn with palette keys, outlined, then colored. Needs Node 22+. From the EditorPets folder:
//   node "_Dev~/Tools/petgen.mjs" preview <outDir> [Name1,Name2]   8x previews to eyeball
//   node "_Dev~/Tools/petgen.mjs" write Pets [Name1,Name2]          sheets into Pets/<Pet Name>/
//   node "_Dev~/Tools/petgen.mjs" items Items                       Items/Ball.png, Heart.png, Food/*.png
// Then in Unity: select a pet's sheets > + New Pet (new pets), or just let Unity reimport (existing ones).
import zlib from 'node:zlib';
import fs from 'node:fs';
import path from 'node:path';

// ---------- canvas ----------
class Canvas {
  constructor(w, h) { this.w = w; this.h = h; this.p = new Array(w * h).fill(null); }
  set(x, y, k) { x = Math.floor(x); y = Math.floor(y); if (x >= 0 && y >= 0 && x < this.w && y < this.h) this.p[y * this.w + x] = k; }
  get(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h ? this.p[y * this.w + x] : null; }
  each(fn) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) fn(x, y); }
  ellipse(cx, cy, rx, ry, k, pred) {
    this.each((x, y) => {
      const dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
      if (dx * dx + dy * dy <= 1 && (!pred || pred(x, y))) this.set(x, y, k);
    });
  }
  // thick segment: every pixel whose center is within r of the segment
  capsule(x0, y0, x1, y1, r, k, pred) {
    const vx = x1 - x0, vy = y1 - y0, len2 = vx * vx + vy * vy || 1e-9;
    this.each((x, y) => {
      const px = x + 0.5 - x0, py = y + 0.5 - y0;
      const t = Math.max(0, Math.min(1, (px * vx + py * vy) / len2));
      const dx = px - t * vx, dy = py - t * vy;
      if (dx * dx + dy * dy <= r * r && (!pred || pred(x, y))) this.set(x, y, k);
    });
  }
  rect(x, y, w, h, k) { for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) this.set(x + i, y + j, k); }
  px(list, k) { for (const [x, y] of list) this.set(x, y, k); }
  stamp(rows, ox, oy) { rows.forEach((r, y) => [...r].forEach((c, x) => { if (c !== '.') this.set(ox + x, oy + y, c); })); }
  outline(k = 'K') {
    const add = [];
    this.each((x, y) => {
      if (this.get(x, y) !== null) return;
      if ([[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => this.get(x + dx, y + dy) !== null)) add.push([x, y]);
    });
    for (const [x, y] of add) this.set(x, y, k);
    return this;
  }
}

// ---------- png ----------
function png(w, h, rgba) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) rgba.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4);
  const chunk = (type, data) => {
    const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
    const td = Buffer.concat([Buffer.from(type), data]);
    const crc = Buffer.alloc(4); crc.writeUInt32BE(zlib.crc32(td));
    return Buffer.concat([len, td, crc]);
  };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}
const hex = h => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16), 255];

// frames (Canvas[]) -> horizontal strip RGBA
function sheet(frames, pal, scale = 1, bg = null) {
  const fw = frames[0].w, fh = frames[0].h, W = fw * frames.length * scale, H = fh * scale;
  const buf = Buffer.alloc(W * H * 4);
  frames.forEach((f, i) => f.each((x, y) => {
    const k = f.get(x, y);
    let c = k === null ? bg : (pal[k] ? hex(pal[k]) : [255, 0, 255, 255]);
    if (!c) return;
    for (let sy = 0; sy < scale; sy++) for (let sx = 0; sx < scale; sx++) {
      const o = ((y * scale + sy) * W + (i * fw + x) * scale + sx) * 4;
      buf[o] = c[0]; buf[o + 1] = c[1]; buf[o + 2] = c[2]; buf[o + 3] = c[3];
    }
  }));
  return { w: W, h: H, buf };
}

// sleep "z" rising above (x, y)
const Z_SMALL = ['ZZZZ', '..Z.', '.Z..', 'ZZZZ'];
const Z_BIG = ['ZZZZZ', '...Z.', '..Z..', '.Z...', 'ZZZZZ'];
function zzz(c, x, y, i, mini) {
  const [dx, dy, g] = (mini ? [[0, 0, Z_SMALL], [1, -1, Z_SMALL], [1, -2, Z_SMALL], [2, -3, Z_SMALL]] : [[0, 0, Z_SMALL], [1, -1, Z_SMALL], [2, -3, Z_BIG], [3, -4, Z_BIG]])[i % 4];
  c.stamp(g.map(r => r.replace(/Z/g, 'z')), x + dx + 1, y + dy + 1);
  c.stamp(g, x + dx, y + dy);
}
const happyEye = (c, x, y) => c.px([[x - 1, y], [x, y - 1], [x + 1, y]], 'K');

// ---------- pigeon (24x24, faces right) ----------
function pigeon(v, o = {}) {
  const c = new Canvas(24, 24);
  const by = o.sit ? 3 : 0, hx = o.hx || 0, hy = o.hy || 0;
  // tail
  const tail = v.longTail ? [[5.5, 13 + by], [0.5, 16.5 + by], [1.5, 18.5 + by], [6.5, 17.5 + by]] : [[5.5, 13 + by], [1.2, 15 + by], [1.2, 18 + by], [6.5, 17.5 + by]];
  polyFill(c, tail, 'T');
  c.capsule(1.8, 15.5 + by, 1.8, 17.5 + by, 0.7, 't');
  // legs
  if (!o.sit) {
    const legs = { stand: [[9.5, 9.5], [12.5, 12.5]], a: [[9.5, 8], [12.5, 13.8]], b: [[9.5, 11], [12.5, 11]] }[o.legs || 'stand'];
    for (const [top, bot] of legs) { c.capsule(top, 19.5, bot, 21.5, 0.6, 'L'); c.capsule(bot, 22.5, bot + 1, 22.5, 0.55, 'L'); }
  }
  // body, belly shade, wing
  c.ellipse(10.5, 15.2 + by, 7.2, 4.6, 'B');
  c.ellipse(10.5, 15.2 + by, 7.2, 4.6, 'D', (x, y) => y + 0.5 > 17 + by);
  const wy = 14.4 + by - (o.wing ? 2 : 0);
  c.ellipse(9.2, wy, 5.2, o.wing ? 3.4 : 2.6, 'W');
  if (v.bars) { c.capsule(7.5, wy - 1.2, 6.5, wy + 1.4, 0.5, 'X'); c.capsule(9.5, wy - 1.2, 8.5, wy + 1.8, 0.5, 'X'); }
  if (v.spots) c.px([[7, Math.floor(wy)], [10, Math.floor(wy) + 1], [12, Math.floor(wy)]], 'X');
  // neck + head
  const hcx = 16.5 + hx, hcy = 8 + hy;
  c.capsule(15.2, 13 + by, hcx - 0.3, hcy + 1.5, 2.4, 'N');
  c.capsule(15.2, 13 + by, hcx - 0.3, hcy + 1.5, 2.4, 'M', (x, y) => y + 0.5 > (13 + by + hcy + 1.5) / 2 + 0.8);
  if (v.crest) for (const tx of [13.5, 15, 16.5, 18, 19.5]) {
    c.capsule(hcx, hcy - 1, tx, hcy - 6.3, 0.55, 'R');
    c.set(tx, hcy - 6.3, 'r');
  }
  c.ellipse(hcx, hcy, 2.9, 2.7, 'H');
  // beak + cere
  const bx = Math.floor(hcx + 2.6), byk = Math.floor(hcy);
  c.px([[bx, byk], [bx + 1, byk]], 'Y');
  c.set(bx, byk - 1, 'C');
  // eye
  const ex = Math.floor(hcx + 0.6), ey = Math.floor(hcy - 1);
  if (o.eye === 'happy') happyEye(c, ex, ey);
  else if (o.eye === 'closed') c.px([[ex - 1, ey], [ex, ey]], 'K');
  else { c.set(ex, ey, 'P'); if (v.eyeRing) c.set(ex - 1, ey, 'E'); }
  if (o.eye === 'happy') c.set(ex - 1, ey + 2, 'p');
  c.outline();
  if (o.z !== undefined) zzz(c, 16, 3, o.z);
  return c;
}

function polyFill(c, pts, k) {
  c.each((x, y) => {
    const px = x + 0.5, py = y + 0.5; let inside = false;
    for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
      const [xi, yi] = pts[i], [xj, yj] = pts[j];
      if ((yi > py) !== (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) inside = !inside;
    }
    if (inside) c.set(x, y, k);
  });
}

function pigeonSheets(v) {
  return {
    Idle: [{}, { hy: -1 }, { eye: 'closed' }, {}].map(o => pigeon(v, o)),
    Walk: [{ legs: 'a', hx: 1 }, { hx: 0 }, { legs: 'b', hx: 1 }, { hx: -1 }].map(o => pigeon(v, o)),
    Sleep: [0, 1, 2, 3].map(z => pigeon(v, { sit: true, hx: -1, hy: 4, eye: 'closed', z })),
    Eat: [{ hx: 3, hy: 9 }, { hx: 4, hy: 11 }, { hx: 3, hy: 9 }, { hx: 0, hy: 0 }].map(o => pigeon(v, o)),
    Happy: [{ wing: 1, eye: 'happy', hy: -1 }, { eye: 'happy', hy: -1 }].map(o => pigeon(v, o)),
  };
}

const PIGEONS = {
  RockPigeon: { bars: true, eyeRing: true, pal: { B: '#8e98ad', D: '#737c92', W: '#aeb6c8', X: '#353a4a', T: '#737c92', t: '#353a4a', N: '#4fa37a', M: '#9166ad', H: '#7d88a2', Y: '#3a3434', C: '#ece8df', P: '#1c1c22', E: '#f0862a', L: '#e0607a', p: '#f59ab0', K: '#23232e', Z: '#ffffff', z: '#2a3350' } },
  WhiteDove: { pal: { B: '#f7f7f4', D: '#dedfe3', W: '#e9eaee', X: '#c9cbd3', T: '#e2e3e8', t: '#c9cbd3', N: '#f7f7f4', M: '#ececef', H: '#fbfbf9', Y: '#e8a25a', C: '#f4d9b8', P: '#1c1c22', E: '#1c1c22', L: '#f07a92', p: '#ffb3c4', K: '#6a6f80', Z: '#ffffff', z: '#2a3350' } },
  MourningDove: { spots: true, longTail: true, eyeRing: true, pal: { B: '#c9ab8a', D: '#b2926f', W: '#bba07f', X: '#3b2e25', T: '#9c8468', t: '#3b2e25', N: '#d4b89a', M: '#c9a2a0', H: '#c2a585', Y: '#3a3030', C: '#8a7a70', P: '#1c1c22', E: '#7fb6e6', L: '#d98a8a', p: '#f0a0a8', K: '#3d3027', Z: '#ffffff', z: '#2a3350' } },
  CrownedPigeon: { crest: true, eyeRing: true, pal: { B: '#7f9fce', D: '#8c3b52', W: '#9fb8de', X: '#5d7ab0', T: '#6d8cc0', t: '#4a6293', N: '#86a6d4', M: '#7f9fce', H: '#8eaddb', R: '#8eaddb', r: '#ffffff', Y: '#3a3a44', C: '#6a6a78', P: '#1c1c22', E: '#d6283a', L: '#b05070', p: '#f59ab0', K: '#263250', Z: '#ffffff', z: '#2a3350' } },
  NicobarPigeon: { eyeRing: true, pal: { B: '#2f7f6e', D: '#256357', W: '#c08a3a', X: '#2a9c8a', T: '#ffffff', t: '#e8e8e8', N: '#3aa38e', M: '#6fb04a', H: '#3b4450', Y: '#262626', C: '#262626', P: '#1c1c22', E: '#d9d9d9', L: '#b04868', p: '#f59ab0', K: '#141c1f', Z: '#ffffff', z: '#2a3350' } },
};

// ---------- turtle (32x32, faces right) ----------
function turtle(o = {}) {
  const c = new Canvas(32, 32);
  const hx = o.hx || 0, hy = o.hy || 0, lf = o.lf || 0, lb = o.lb || 0, legLen = o.tuck ? 25.5 : 28;
  // legs + tail (behind shell)
  for (const [x, off] of [[9, lb], [21, lf]]) {
    c.capsule(x + 0.5, 24, x + 0.5 + off, legLen - 1, 1.7, 'G');
    c.rect(Math.round(x - 1 + off), legLen - 1, 4, 1, 'g');
  }
  c.capsule(5.5, 24, 3 + (o.tail || 0), 25.5, 0.9, 'G');
  // neck + head
  const hcx = 27.5 + hx, hcy = 20.5 + hy;
  c.capsule(22.5, 23, hcx - 1.5, hcy + 0.5, 1.8, 'G');
  c.ellipse(hcx, hcy, 3.1, 2.5, 'G');
  c.ellipse(hcx, hcy, 3.1, 2.5, 'g', (x, y) => y + 0.5 > hcy + 1.2);
  const ex = Math.floor(hcx + 0.5), ey = Math.floor(hcy - 1);
  if (o.eye === 'happy') { happyEye(c, ex, ey); c.set(ex - 1, ey + 2, 'p'); }
  else if (o.eye === 'closed') c.px([[ex - 1, ey], [ex, ey]], 'K');
  else c.set(ex, ey, 'P');
  if (o.mouth) c.px([[Math.floor(hcx + 2), Math.floor(hcy + 1)]], 'K');
  // shell
  c.ellipse(15.5, 24.5, 11, 8.2, 'S', (x, y) => y + 0.5 <= 24);
  for (const [cx, cy] of [[10.3, 21.8], [15.5, 19.6], [20.7, 21.8], [15.5, 23.2]]) c.ellipse(cx, cy, 2.4, 1.7, 'h');
  c.ellipse(15.5, 24.5, 11, 8.2, 'x', (x, y) => y + 0.5 <= 17.6);
  c.capsule(5, 24, 26, 24, 0.9, 'U');
  c.outline();
  if (o.z !== undefined) zzz(c, 24, 8, o.z);
  return c;
}
const TURTLE_PAL = { G: '#9ccc5c', g: '#6f9a3a', S: '#4f8f45', h: '#7bb865', x: '#3c7236', U: '#d7bf6a', P: '#1a1f14', p: '#f29c9c', K: '#1f3320', Z: '#ffffff', z: '#2a3350' };
const turtleSheets = () => ({
  Idle: [{}, { hy: -1 }, { eye: 'closed' }, {}].map(turtle),
  Walk: [{ lf: 1.5, lb: -1.5, hx: 0.5 }, { hy: -0.5 }, { lf: -1.5, lb: 1.5, hx: 0.5 }, { hy: -0.5 }].map(turtle),
  Sleep: [0, 1, 2, 3].map(z => turtle({ hx: -4, hy: 2, eye: 'closed', tuck: true, z })),
  Eat: [{ hx: 1, hy: 5, mouth: 1 }, { hx: 1, hy: 6 }, { hx: 1, hy: 5, mouth: 1 }, { hx: 0, hy: 1 }].map(turtle),
  Happy: [{ hy: -2, eye: 'happy', tail: 1 }, { hy: -3, eye: 'happy', lf: 1 }].map(turtle),
});

// ---------- crab (24x24, front view) ----------
function crab(o = {}) {
  const c = new Canvas(24, 24);
  const by = o.by || 0, cl = o.cl || [0, 0], eyeY = o.low ? 2 : 0;
  // legs: three per side, alternating lift
  for (const s of [-1, 1]) for (let i = 0; i < 3; i++) {
    const lift = ((i + (s > 0 ? 1 : 0) + (o.step || 0)) % 2) * (o.step === undefined ? 0 : 1);
    const kx = 12 + s * (6.3 + i * 1.1), ky = 16.5 + i * 0.9 + by - lift;
    c.capsule(12 + s * (4 + i * 1.2), 17 + i * 0.6 + by, kx, ky, 0.62, 'R');
    c.capsule(kx, ky, kx + s * 0.9, 21.8 - lift, 0.62, 'R');
  }
  // claws + arms
  for (const s of [-1, 1]) {
    const up = cl[s < 0 ? 0 : 1], ccx = 12 + s * 8.1, ccy = 10.5 + up + by;
    c.capsule(12 + s * 5.5, 15 + by, ccx - s * 0.5, ccy + 1.5, 0.9, 'R');
    c.ellipse(ccx, ccy, 2.5, 2.4, 'R');
    c.set(ccx + s * 0.2, ccy - 2, null); c.set(ccx + s * 0.2, ccy - 1, null);
    c.set(ccx - s * 1.2, ccy - 1, 'h');
  }
  // body
  c.ellipse(12, 15.5 + by, 6.9, 4.3, 'R');
  c.ellipse(12, 17.2 + by, 5, 2, 'r');
  c.px([[9, 13 + by], [10, 12 + by], [14, 13 + by]], 'h');
  // eyes on stalks
  for (const x of [9, 14]) c.capsule(x + 0.5, 12 + by, x + 0.5, 9.5 + eyeY + by, 0.55, 'R');
  for (const x of [8, 14]) {
    if (o.eye === 'happy') { happyEye(c, x + 1, 8 + eyeY + by); continue; }
    if (o.eye === 'closed') { c.px([[x, 8 + eyeY + by], [x + 1, 8 + eyeY + by]], 'K'); continue; }
    c.rect(x, 7 + eyeY + by, 2, 2, 'W');
    c.set(x === 8 ? x + 1 : x, 8 + eyeY + by, 'P');
  }
  if (o.eye === 'happy') c.px([[8, 14 + by], [15, 14 + by]], 'p');
  // mouth
  c.px(o.chew ? [[11, 16 + by], [12, 16 + by]] : [[10, 16 + by], [11, 17 + by], [12, 17 + by], [13, 16 + by]], 'K');
  c.outline();
  if (o.z !== undefined) zzz(c, 16, 4, o.z);
  return c;
}
const CRAB_PAL = { R: '#e8563b', r: '#f3a06a', h: '#ff9a7a', W: '#ffffff', P: '#1a1414', p: '#ffb0b0', K: '#4a1a14', Z: '#ffffff', z: '#2a3350' };
const crabSheets = () => ({
  Idle: [{}, { cl: [-1, -1] }, { eye: 'closed' }, { cl: [0, -1] }].map(crab),
  Walk: [{ step: 0, cl: [0, -1] }, { step: 1, by: -1 }, { step: 0, cl: [-1, 0] }, { step: 1, by: -1 }].map(crab),
  Sleep: [0, 1, 2, 3].map(z => crab({ low: true, eye: 'closed', cl: [3, 3], by: 1, z })),
  Eat: [{ cl: [0, 4], chew: 1 }, { cl: [0, 2] }, { cl: [4, 0], chew: 1 }, { cl: [2, 0] }].map(crab),
  Happy: [{ cl: [-4, -4], eye: 'happy' }, { cl: [-2, -2], eye: 'happy', by: -1 }].map(crab),
});

// ---------- deer (40x40, faces right) ----------
function deer(o = {}) {
  const c = new Canvas(40, 40);
  const hx = o.hx || 0, hy = o.hy || 0, lie = o.lie;
  const d = o.legs || [0, 0, 0, 0]; // hoof x offsets: back-far, back-near, front-far, front-near
  const bodyY = lie ? 31.5 : 22.5;
  if (!lie) {
    // far legs first (darker), then near legs
    [[11.5, 'D', 0], [24.5, 'D', 2], [13.5, 'B', 1], [26.5, 'B', 3]].forEach(([x, k, i]) => {
      c.capsule(x, 25, x + d[i], 35.5, 1.0, k);
      c.capsule(x + d[i], 36, x + d[i], 36.6, 1.0, 'k');
    });
  }
  // tail
  const ty = bodyY - 3 - (o.tailUp ? 2 : 0);
  c.ellipse(7.3, ty, 1.7, 2.2, 'B');
  c.ellipse(6.9, ty + 0.9, 1.1, 1.3, 'W');
  // body
  c.ellipse(17.5, bodyY, 10.8, 5.3, 'B');
  c.ellipse(17.5, bodyY, 10.8, 5.3, 'b', (x, y) => y + 0.5 > bodyY + 2.4);
  c.px([[13, bodyY - 3], [17, bodyY - 4], [21, bodyY - 3], [15, bodyY - 1], [19, bodyY - 1], [11, bodyY - 1]].map(([x, y]) => [x, Math.floor(y)]), 'W');
  if (lie) { c.ellipse(24, 36.2, 3.2, 1.5, 'B'); c.ellipse(11, 36.2, 3.2, 1.5, 'D'); }
  // neck, head
  const hcx = 30.5 + hx, hcy = 10.5 + hy;
  c.capsule(25, lie ? 29 : 20, hcx - 1, hcy + 1.5, 2.6, 'B');
  c.capsule(hcx - 1.5, hcy - 1.5, hcx - 4 + (o.ear || 0), hcy - 3.8, 1.15, 'B');
  c.set(hcx - 3 + (o.ear || 0), hcy - 3, 'p');
  c.ellipse(hcx, hcy, 3.2, 2.7, 'B');
  c.capsule(hcx + 0.5, hcy + 1, hcx + 4.3, hcy + 1.4, 1.45, 'B');
  c.capsule(hcx + 1, hcy + 2, hcx + 4, hcy + 2.2, 0.8, 'b');
  c.set(hcx + 5, hcy + 0.5, 'K');
  // antlers
  const ax = hcx - 0.5, ay = hcy - 2.5;
  c.capsule(ax - 1, ay, ax - 3, ay - 6, 0.55, 'A'); c.capsule(ax - 2, ay - 3, ax - 5, ay - 4, 0.5, 'A');
  c.capsule(ax + 1.5, ay, ax + 2.5, ay - 6.2, 0.55, 'A'); c.capsule(ax + 2, ay - 3.2, ax + 4.5, ay - 4.5, 0.5, 'A');
  const ex = Math.floor(hcx + 0.8), ey = Math.floor(hcy - 0.8);
  if (o.eye === 'happy') { happyEye(c, ex, ey); c.set(ex, ey + 2, 'p'); }
  else if (o.eye === 'closed') c.px([[ex - 1, ey], [ex, ey]], 'K');
  else c.set(ex, ey, 'P');
  c.outline();
  if (o.z !== undefined) zzz(c, 32, 12, o.z);
  return c;
}
const DEER_PAL = { B: '#b5793f', D: '#8e5b2c', b: '#f0dcb8', W: '#fbf4e4', A: '#ead8ac', k: '#5b3a24', P: '#1e120a', p: '#eea0a0', K: '#3a2415', Z: '#ffffff', z: '#2a3350' };
const deerSheets = () => ({
  Idle: [{}, { ear: 1 }, { eye: 'closed' }, { tailUp: 1 }].map(deer),
  Walk: [{ legs: [2, -2, -2, 2] }, { legs: [0, 0, 0, 0], hy: -0.5 }, { legs: [-2, 2, 2, -2] }, { legs: [0, 0, 0, 0], hy: -0.5 }].map(deer),
  Sleep: [0, 1, 2, 3].map(z => deer({ lie: true, hx: 0, hy: 17, eye: 'closed', z })),
  Eat: [{ hx: 4, hy: 22 }, { hx: 4, hy: 23 }, { hx: 4, hy: 22, tailUp: 1 }, { hx: 1, hy: 2 }].map(deer),
  Happy: [{ eye: 'happy', tailUp: 1, hy: -1 }, { eye: 'happy', tailUp: 1, legs: [0, 0, 1, 1] }].map(deer),
});

// ---------- dragon (40x40, faces right) ----------
function dragon(v, o = {}) {
  const c = new Canvas(40, 40);
  const hx = o.hx || 0, hy = o.hy || 0, lie = o.lie, d = o.legs || [0, 0, 0, 0], wing = o.wing || 0;
  const by = lie ? 31 : 25;
  // tail with spade tip
  const tailUp = o.tailUp ? -3 : 0;
  c.capsule(10, by, 5, by - 2 + tailUp, 2.2, 'B');
  c.capsule(5, by - 2 + tailUp, 2.5, by - 6 + tailUp, 1.4, 'B');
  polyFill(c, [[0.5, by - 8 + tailUp], [4.5, by - 7.5 + tailUp], [2.5, by - 4.5 + tailUp]], 'S');
  // far wing (behind the body)
  const wingPts = wing === 2 ? [[15, by - 3], [23, by - 3], [26, by - 18], [19, by - 15], [12, by - 16]]
                : wing === 1 ? [[14, by - 3], [23, by - 3], [24, by - 13], [17, by - 12], [10, by - 11]]
                : [[13, by - 3], [23, by - 3], [20, by - 9], [14, by - 8]];
  polyFill(c, wingPts.map(([x, y]) => [x + 2, y - 1]), 'D');
  // legs
  if (!lie) [[12, 'D', 0], [23, 'D', 2], [14, 'B', 1], [25, 'B', 3]].forEach(([x, k, i]) => {
    c.capsule(x, by + 2, x + d[i], 35.2, 1.6, k);
    c.rect(Math.round(x + d[i] - 1), 36, 3, 1, 'S');
  });
  // body + belly
  c.ellipse(18.5, by, 9.8, 5.6, 'B');
  c.ellipse(18.5, by, 9.8, 5.6, 'b', (x, y) => y + 0.5 > by + 2.2);
  if (lie) { c.ellipse(25, 36.3, 3.4, 1.4, 'B'); c.ellipse(12, 36.3, 3.4, 1.4, 'D'); }
  // back spikes
  for (const sx of [11, 14.5, 18, 21.5]) polyFill(c, [[sx - 1.3, by - 4.3], [sx + 1.3, by - 4.6], [sx - 0.2, by - 7.2]], 'S');
  // neck + head
  const hcx = 30 + hx, hcy = 13 + hy;
  c.capsule(25, by - 2, hcx - 1, hcy + 1.5, 2.8, 'B');
  c.capsule(26.5, by - 1, hcx + 0.5, hcy + 3, 1.2, 'b');
  for (const [dx, len] of v.longHorns ? [[-1.5, 6.5], [0.5, 6]] : [[-1.5, 4.5], [0.5, 4]])
    c.capsule(hcx + dx, hcy - 2, hcx + dx - len * 0.8, hcy - 2 - len * 0.6, 0.75, 'H');
  c.ellipse(hcx, hcy, 3.7, 3.1, 'B');
  c.capsule(hcx + 1, hcy + 1, hcx + 5.5, hcy + 1.5, 2, 'B');
  c.capsule(hcx + 1.5, hcy + 2.6, hcx + 5.3, hcy + 2.8, 0.7, 'b');
  c.set(hcx + 5.5, hcy + 0.2, 'K');
  const ex = Math.floor(hcx + 0.5), ey = Math.floor(hcy - 0.8);
  if (o.eye === 'happy') { happyEye(c, ex, ey); c.set(ex - 1, ey + 2, 'p'); }
  else if (o.eye === 'closed') c.px([[ex - 1, ey], [ex, ey]], 'K');
  else { c.set(ex, ey, 'P'); c.set(ex - 1, ey, 'W'); }
  // near wing: membrane + arm bone
  polyFill(c, wingPts, 'M');
  const [[x0, y0], [x1], [tx, ty]] = wingPts;
  for (const f of [0.2, 0.55]) c.capsule(tx, ty, x0 + (x1 - x0) * f, y0 - 0.5, 0.45, 'D');
  c.capsule(wingPts[1][0], wingPts[1][1], tx, ty, 0.7, 'B');
  c.outline();
  // breath puff (after the outline so it looks soft)
  if (o.breath) {
    const s = o.breath, bx = hcx + 5.5 + s * 0.5, bY = hcy + 1.5;
    c.ellipse(bx, bY, 1.5 + s * 0.3, 1.4 + s * 0.3, 'f');
    c.ellipse(bx - 0.4, bY, 0.9 + s * 0.2, 0.8 + s * 0.2, 'F');
  }
  if (o.z !== undefined) zzz(c, 31, 12, o.z);
  return c;
}
const DRAGONS = {
  RedDragon: { pal: { B: '#d8483a', D: '#a3302a', b: '#f6cf92', S: '#5a1f1a', H: '#f3e6cc', M: '#f59347', W: '#ffffff', P: '#1a0f0f', F: '#ffe066', f: '#ff7a1a', K: '#3b1210', p: '#ff9a9a', Z: '#ffffff', z: '#2a3350' } },
  IceDragon: { longHorns: true, pal: { B: '#6fb8ea', D: '#4787bb', b: '#e8f6ff', S: '#2a5d91', H: '#ffffff', M: '#bfe8ff', W: '#ffffff', P: '#10213a', F: '#ffffff', f: '#9fe3ff', K: '#16304f', p: '#ffb3c8', Z: '#ffffff', z: '#2a3350' } },
};
const dragonSheets = v => ({
  Idle: [{}, { wing: 1 }, { eye: 'closed' }, { tailUp: 1 }].map(o => dragon(v, o)),
  Walk: [{ legs: [2, -2, -2, 2] }, { wing: 1, hy: -0.5 }, { legs: [-2, 2, 2, -2] }, { wing: 1, hy: -0.5 }].map(o => dragon(v, o)),
  Sleep: [0, 1, 2, 3].map(z => dragon(v, { lie: true, hy: 16, hx: -1, eye: 'closed', z })),
  Eat: [{ hx: 3, hy: 18 }, { hx: 3, hy: 19 }, { hx: 3, hy: 18, tailUp: 1 }, { hx: 1, hy: 2 }].map(o => dragon(v, o)),
  Happy: [{ wing: 2, eye: 'happy', tailUp: 1, breath: 1 }, { wing: 1, eye: 'happy', tailUp: 1, breath: 2, hy: -1 }].map(o => dragon(v, o)),
});

// ---------- snake (32x32, faces right) ----------
function snake(o = {}) {
  const c = new Canvas(32, 32);
  const ph = o.phase || 0, hx = o.hx || 0, hy = o.hy || 0;
  const pts = [];
  if (o.coil) { // sleeping: three stacked rings, shaded at the bottom so they read as separate loops
    for (const [cx, cy, rx, ry] of [[14, 27.2, 9, 2.6], [15, 24.2, 7.2, 2.4], [16, 21.4, 5.2, 2.1]]) {
      c.ellipse(cx, cy, rx, ry, 'G');
      c.ellipse(cx, cy, rx, ry, 'D', (x, y) => y + 0.5 > cy + ry - 1.4);
      c.ellipse(cx, cy, rx, ry, 'K', (x, y) => y + 0.5 > cy + ry - 0.5);
      c.px([[Math.floor(cx - rx * 0.5), Math.floor(cy - 1)], [Math.floor(cx + rx * 0.3), Math.floor(cy - 1)]], 'D');
    }
    pts.push([19, 21], [20, 20.5]);
  } else {
    for (let x = 2; x <= 21; x += 0.5) pts.push([x, 27 + Math.sin(x / 3.2 + ph) * 1.6 * Math.min(1, (x - 1) / 6)]);
  }
  // body: tapering capsule chain, darker diamonds on the back, lighter belly stripe
  if (!o.coil) for (let i = 1; i < pts.length; i++) {
    const t = i / (pts.length - 1), r = 0.7 + 1.7 * Math.min(1, t * 1.6);
    c.capsule(pts[i - 1][0], pts[i - 1][1], pts[i][0], pts[i][1], r, 'G');
  }
  if (!o.coil) for (let i = 4; i < pts.length; i += 5) c.ellipse(pts[i][0], pts[i][1] - 0.6, 1.1, 0.9, 'D');
  if (!o.coil) for (let i = 2; i < pts.length; i++) c.capsule(pts[i - 1][0], pts[i - 1][1] + 1.5, pts[i][0], pts[i][1] + 1.5, 0.5, 'y');
  // neck + head
  const [nx, ny] = pts[pts.length - 1];
  const hcx = (o.coil ? 21.5 : 26) + hx, hcy = (o.coil ? 18.6 : 22) + hy;
  c.capsule(nx, ny, hcx - 2, hcy + 1, 2.3, 'G');
  c.ellipse(hcx, hcy, 3.6, 2.6, 'G');
  c.ellipse(hcx, hcy, 3.6, 2.6, 'y', (x, y) => y + 0.5 > hcy + 1.3);
  const ex = Math.floor(hcx + 0.8), ey = Math.floor(hcy - 1);
  if (o.eye === 'happy') { happyEye(c, ex, ey); c.set(ex - 1, ey + 2, 'p'); }
  else if (o.eye === 'closed') c.px([[ex - 1, ey], [ex, ey]], 'K');
  else { c.set(ex, ey, 'P'); c.set(ex, ey - 1, 'W'); }
  c.outline();
  if (o.tongue) { // forked tongue drawn over the outline
    const tx = Math.floor(hcx + 4), ty = Math.floor(hcy + 0.5);
    c.px([[tx, ty], [tx + 1, ty]], 'T');
    if (o.tongue > 1) c.px([[tx + 2, ty - 1], [tx + 2, ty + 1]], 'T');
  }
  if (o.z !== undefined) zzz(c, 23, 9, o.z);
  return c;
}
const SNAKE_PAL = { G: '#72c34d', D: '#3f8a2e', y: '#f0e38a', P: '#141414', W: '#ffffff', T: '#e83a55', K: '#1f3a14', p: '#ff9aa8', Z: '#ffffff', z: '#2a3350' };
const snakeSheets = () => ({
  Idle: [{}, { tongue: 2 }, { tongue: 1 }, { eye: 'closed' }].map(snake),
  Walk: [0, 1, 2, 3].map(i => snake({ phase: i * Math.PI / 2, hy: i % 2 ? -0.5 : 0, tongue: i === 2 ? 2 : 0 })),
  Sleep: [0, 1, 2, 3].map(z => snake({ coil: true, eye: 'closed', z })),
  Eat: [{ hx: 1, hy: 5, tongue: 1 }, { hx: 1.5, hy: 6 }, { hx: 1, hy: 5, tongue: 2 }, {}].map(snake),
  Happy: [{ hy: -4, eye: 'happy', tongue: 2 }, { hy: -5, eye: 'happy', tongue: 1, phase: 1 }].map(snake),
});

// ---------- cat (32x32, faces right) ----------
function cat(o = {}, v = {}) {
  const c = new Canvas(32, 32);
  const hx = o.hx || 0, hy = o.hy || 0, d = o.legs || [0, 0, 0, 0], sleep = o.sleep, fat = v.chubby ? 1 : 0;
  const by = sleep ? 25.5 : 21 + fat;
  // tail
  const tw = o.tail || 0;
  if (sleep) c.capsule(7, 27, 22, 29, 1.2, 'O');
  else {
    c.capsule(7.5, by - 1, 4 + tw, by - 6, 1.2, 'O');
    c.capsule(4 + tw, by - 6, 5.5 + tw * 1.5, by - 10, 1.2, 'O');
    c.set(5.5 + tw * 1.5, by - 10.5, 'S');
  }
  // legs
  if (!sleep) [[10, 'S', 0], [20, 'S', 2], [12, 'O', 1], [22, 'O', 3]].forEach(([x, k, i]) => {
    c.capsule(x, by + 2, x + d[i], 28.4, 1.15, k);
    c.rect(Math.round(x + d[i] - 1), 29, 2, 1, 'C');
  });
  // body with stripes
  const rx = (sleep ? 8.5 : 8.8) + fat * 1.2, ry = (sleep ? 4.6 : 4.1) + fat * 1.1;
  c.ellipse(16, by, rx, ry, 'O');
  c.ellipse(16, by, rx, ry, 'C', (x, y) => y + 0.5 > by + 2.2);
  for (const sx of fat ? [9.5, 12.5, 15.5, 18.5] : [11.5, 14.5, 17.5]) c.capsule(sx, by - 3.8 - fat, sx - 0.5, by - 1.6, 0.55, 'S');
  // head: ears, face, muzzle, eyes
  const hcx = (sleep ? 22 : 23.5) + hx, hcy = (sleep ? 23.5 : 14) + hy;
  polyFill(c, [[hcx - 3.6, hcy - 1], [hcx - 2.6, hcy - 5.2], [hcx - 0.4, hcy - 2.6]], 'O');
  polyFill(c, [[hcx + 0.6, hcy - 2.6], [hcx + 2.8, hcy - 5.2], [hcx + 3.4, hcy - 1]], 'O');
  c.set(hcx - 2.4, hcy - 3.2, 'p'); c.set(hcx + 2.4, hcy - 3.2, 'p');
  c.ellipse(hcx, hcy, 4 + fat * 0.5, 3.4 + fat * 0.3, 'O');
  c.capsule(hcx - 1.8, hcy - 3, hcx - 1.2, hcy - 1.6, 0.5, 'S');
  c.ellipse(hcx + 1.4, hcy + 1.6, 2.2, 1.3, 'C');
  c.set(hcx + 1.4, hcy + 0.8, 'p');
  for (const ex of [Math.floor(hcx - 0.7), Math.floor(hcx + 2.3)]) {
    const ey = Math.floor(hcy - 0.6);
    if (o.eye === 'happy') c.px([[ex - 1, ey], [ex, ey - 1], [ex + 1, ey]], v.lidKey || 'K');
    else if (o.eye === 'closed') c.px([[ex - 1, ey], [ex, ey]], v.lidKey || 'K');
    else { c.set(ex, ey, 'P'); c.set(ex, ey - 1, 'P'); }
  }
  if (o.eye === 'happy') c.px([[Math.floor(hcx - 2), Math.floor(hcy + 1)], [Math.floor(hcx + 3.8), Math.floor(hcy + 1)]], 'p');
  c.outline();
  if (o.z !== undefined) zzz(c, 24, 11, o.z);
  return c;
}
const CAT_PAL = { O: '#f2a24c', S: '#c46f2c', C: '#fbe8cc', p: '#f59ab0', P: '#2a1a10', K: '#4a2a14', Z: '#ffffff', z: '#2a3350' };
const catSheets = (v = {}) => ({
  Idle: [{}, { tail: 1 }, { eye: 'closed' }, { tail: -1 }].map(o => cat(o, v)),
  Walk: [{ legs: [1.5, -1.5, -1.5, 1.5] }, { hy: -0.5, tail: 1 }, { legs: [-1.5, 1.5, 1.5, -1.5] }, { hy: -0.5, tail: -1 }].map(o => cat(o, v)),
  Sleep: [0, 1, 2, 3].map(z => cat({ sleep: true, eye: 'closed', z }, v)),
  Eat: [{ hx: 2, hy: 9 }, { hx: 2, hy: 10 }, { hx: 2, hy: 9, tail: 1 }, {}].map(o => cat(o, v)),
  Happy: [{ eye: 'happy', tail: 1, hy: -1 }, { eye: 'happy', tail: -1 }].map(o => cat(o, v)),
});
// Mishy: a common black tabby, a bit on the chubby side, with yellow-green eyes
const MISHY_PAL = { O: '#3d3d47', S: '#17171d', C: '#6e6e7a', p: '#e88aa0', P: '#c8e04a', K: '#08080c', Z: '#ffffff', z: '#2a3350' };

// ---------- capybara (32x32, faces right; yuzu on its head, obviously) ----------
function capybara(o = {}) {
  const c = new Canvas(32, 32);
  const hx = o.hx || 0, hy = o.hy || 0, d = o.legs || [0, 0, 0, 0], lie = o.lie;
  const by = lie ? 25 : 22;
  if (!lie) [[7, 'D', 0], [18, 'D', 2], [9, 'B', 1], [20, 'B', 3]].forEach(([x, k, i]) => c.capsule(x, by + 3, x + d[i], 28.3, 1.4, k));
  c.ellipse(14, by, 10.5, 5.8, 'B');
  c.ellipse(14, by, 10.5, 5.8, 'D', (x, y) => y + 0.5 < by - 3.3);
  if (lie) { c.ellipse(20, 29, 3, 1.3, 'B'); c.ellipse(8, 29, 3, 1.3, 'D'); }
  const hcx = 23 + hx, hcy = (lie ? 21 : 17) + hy;
  c.ellipse(hcx - 1.5, hcy - 3.2, 1.3, 1.1, 'D');
  c.ellipse(hcx, hcy, 4.4, 3.6, 'B');
  c.capsule(hcx + 1, hcy + 0.8, hcx + 4.8, hcy + 0.8, 2.4, 'B');
  c.capsule(hcx + 4.2, hcy - 0.4, hcx + 5.3, hcy - 0.4, 0.8, 'n');
  c.set(hcx + 3.5, hcy + 2.5, 'n');
  const ex = Math.floor(hcx + 0.5), ey = Math.floor(hcy - 1.2);
  if (o.eye === 'happy') { happyEye(c, ex, ey); c.set(ex, ey + 2, 'p'); }
  else if (o.eye === 'closed') c.px([[ex - 1, ey], [ex, ey]], 'K');
  else c.set(ex, ey, 'P');
  if (!o.noYuzu) {
    c.ellipse(hcx - 0.5, hcy - 5, 2.1, 1.8, 'Y');
    c.set(hcx - 1.2, hcy - 5.6, 'y');
    c.px([[Math.floor(hcx), Math.floor(hcy - 7.2)], [Math.floor(hcx + 1), Math.floor(hcy - 7.6)]], 'L');
  }
  c.outline();
  if (o.z !== undefined) zzz(c, 25, 6, o.z);
  return c;
}
const CAPY_PAL = { B: '#a97a52', D: '#8b603d', n: '#4a3222', Y: '#ffb52e', y: '#ffe08a', L: '#5ab04a', P: '#1a120a', p: '#f0a0a0', K: '#3a2414', Z: '#ffffff', z: '#2a3350' };
const capySheets = () => ({
  Idle: [{}, {}, { eye: 'closed' }, { hy: -0.5 }].map(capybara),
  Walk: [{ legs: [1.5, -1.5, -1.5, 1.5] }, { hy: -0.5 }, { legs: [-1.5, 1.5, 1.5, -1.5] }, { hy: -0.5 }].map(capybara),
  Sleep: [0, 1, 2, 3].map(z => capybara({ lie: true, eye: 'closed', z })),
  Eat: [{ hx: 1, hy: 6, noYuzu: true }, { hx: 1, hy: 7, noYuzu: true }, { hx: 1, hy: 6, noYuzu: true }, {}].map(capybara),
  Happy: [{ eye: 'happy' }, { eye: 'happy', hy: -1 }].map(capybara),
});

// ---------- Clawdito: parody of Claude Code's blocky orange mascot (16x16, flat, no outline) ----------
function clawdito(o = {}) {
  const c = new Canvas(16, 16);
  const by = o.by || 0, squash = o.sleep ? 2 : 0;
  c.rect(3, 7 + by + squash, 10, 7 - squash, 'O');
  if (o.armsUp) { c.rect(1, 6 + by, 2, 2, 'O'); c.rect(13, 6 + by, 2, 2, 'O'); c.set(2, 8 + by, 'O'); c.set(13, 8 + by, 'O'); }
  else if (!o.sleep) { c.rect(1, 10 + by, 2, 2, 'O'); c.rect(13, 10 + by, 2, 2, 'O'); }
  if (!o.sleep) [4, 6, 9, 11].forEach((x, i) => c.rect(x, 14, 1, (o.lift || []).includes(i) ? 1 : 2, 'O'));
  const ey = 8 + by + squash + (o.look || 0);
  for (const ex of [5, 10]) {
    if (o.eye === 'happy') c.px([[ex - 1, ey + 1], [ex, ey], [ex + 1, ey + 1]], 'K');
    else if (o.eye === 'closed') c.px([[ex, ey + 1]], 'K');
    else c.rect(ex, ey, 1, 2, 'K');
  }
  if (o.star) c.stamp(o.star === 1 ? ['A.A.A', '.AAA.', 'AAAAA', '.AAA.', 'A.A.A'] : ['..A..', '..A..', 'AAAAA', '..A..', '..A..'], 6, 0);
  if (o.crumb) c.set(12, 13 + (o.crumb - 1), 'A');
  if (o.z !== undefined) zzz(c, 10, 4, o.z, true);
  return c;
}
const CLAWD_PAL = { O: '#d97757', A: '#f3b393', K: '#141414', Z: '#ffffff', z: '#2a3350' };
const clawdSheets = () => ({
  Idle: [{}, { by: 0 }, { eye: 'closed' }, {}].map(clawdito),
  Walk: [{ lift: [0, 2] }, { by: -1 }, { lift: [1, 3] }, { by: -1 }].map(clawdito),
  Sleep: [0, 1, 2, 3].map(z => clawdito({ sleep: true, eye: 'closed', z })),
  Eat: [{ look: 1, crumb: 1 }, { look: 1, by: 1, crumb: 2 }, { look: 1 }, {}].map(clawdito),
  Happy: [{ armsUp: true, eye: 'happy', star: 1 }, { armsUp: true, eye: 'happy', star: 2, by: -1 }].map(clawdito),
});

// ---------- Kimoon: moon-themed parody inspired by Kimi / Moonshot AI branding (16x16) ----------
function kimoon(o = {}) {
  const c = new Canvas(16, 16);
  const by = o.by || 0;
  const feet = o.feet || [0, 0];
  if (!o.sleep) { c.rect(5, 14 - feet[0], 2, 1, 'B'); c.rect(9, 14 - feet[1], 2, 1, 'B'); }
  const cy = (o.sleep ? 10.5 : 9.2) + by;
  // the body is the moon: dark disc with a lit crescent on its right edge
  const ry = o.sleep ? 4 : 4.7;
  c.ellipse(8, cy, 5.3, ry, 'B');
  c.ellipse(8, cy, 5.3, ry, 'Y', (x, y) => ((x + 0.5 - 6.4) / 5.0) ** 2 + ((y + 0.5 - cy + 0.4) / (ry - 0.2)) ** 2 > 1);
  c.set(4, Math.floor(cy - 3), 'h');
  const ey = Math.floor(cy - 0.5);
  for (const ex of [4, 8]) {
    if (o.eye === 'happy') c.px([[ex, ey + 1], [ex + 1, ey], [ex + 2, ey + 1]], 'W');
    else if (o.eye === 'closed') c.px([[ex, ey + 1], [ex + 1, ey + 1]], 'W');
    else { c.rect(ex, ey, 2, 2, 'W'); c.set(ex + 1, ey + 1, 'P'); }
  }
  if (o.eye === 'happy') c.px([[3, ey + 2], [10, ey + 2]], 'p');
  if (o.munch) c.px([[6, ey + 3], [7, ey + 3]], 'p');
  c.outline();
  if (o.z !== undefined) zzz(c, 9, 3, o.z, true);
  return c;
}
const KIMOON_PAL = { B: '#1c2230', h: '#34405c', Y: '#d6ecff', W: '#ffffff', P: '#2d7cf6', p: '#ff8fb0', K: '#3f8ef0', Z: '#ffffff', z: '#2a3350' };
const kimoonSheets = () => ({
  Idle: [{}, { by: -1 }, { eye: 'closed' }, {}].map(kimoon),
  Walk: [{ feet: [1, 0] }, { by: -1 }, { feet: [0, 1] }, { by: -1 }].map(kimoon),
  Sleep: [0, 1, 2, 3].map(z => kimoon({ sleep: true, eye: 'closed', z })),
  Eat: [{ munch: 1, by: 1 }, { by: 1 }, { munch: 1, by: 1 }, {}].map(kimoon),
  Happy: [{ eye: 'happy', by: -2 }, { eye: 'happy', by: -1 }].map(kimoon),
});

// ---------- items: food library, ball, heart (16x16, drawn at 2x) ----------
function item(draw) { const c = new Canvas(16, 16); draw(c); return c.outline(); }
const ITEMS = {
  // food library (Items/Food): one per species + a few extras to pick from
  Bowl: [c => { // default food: bowl of kibble
    c.ellipse(8, 10.2, 5.2, 1.6, 'k');
    for (const [x, y] of [[5, 9], [7, 8], [9, 8], [11, 9], [6, 10], [8, 9], [10, 10]]) c.set(x, y, 'K2');
    c.rect(3, 11, 10, 3, 'B'); c.rect(4, 14, 8, 1, 'B'); c.rect(3, 11, 10, 1, 'b');
  }, { k: '#c98a4b', K2: '#8a5528', B: '#e0484f', b: '#ff7a80' }],
  Bone: [c => {
    c.capsule(4, 11, 12, 11, 1.1, 'W');
    for (const [x, y] of [[3.3, 9.8], [3.3, 12.2], [12.7, 9.8], [12.7, 12.2]]) c.ellipse(x, y, 1.6, 1.6, 'W');
    c.px([[5, 12], [6, 12], [7, 12], [8, 12], [9, 12], [10, 12], [11, 12], [2, 13], [13, 13]], 'w');
  }, { W: '#f6efdc', w: '#d9ccab' }],
  Fish: [c => {
    polyFill(c, [[1, 8.5], [4, 11], [1, 13.5]], 'T');
    c.ellipse(8.5, 11, 5.5, 2.8, 'F');
    c.ellipse(8.5, 11, 5.5, 2.8, 'f', (x, y) => y + 0.5 > 12);
    c.set(12, 10, 'K2'); c.px([[8, 9], [9, 9]], 'T');
  }, { F: '#7fb3dc', f: '#d6ecfa', T: '#5a8fbf', K2: '#16202a' }],
  Leaf: [c => {
    polyFill(c, [[1.5, 14], [3, 9], [7, 5.5], [14.5, 3.5], [12.5, 9], [8, 13], [4, 14.5]], 'G');
    polyFill(c, [[1.5, 14], [3, 9], [7, 5.5], [14.5, 3.5], [12.5, 9], [8, 13], [4, 14.5]], 'g', (x, y) => x + y < 16);
    c.capsule(2.5, 13.5, 13.5, 4.5, 0.4, 'v'); c.capsule(7, 9.5, 5, 7.5, 0.35, 'v'); c.capsule(9.5, 7.5, 10.5, 10.5, 0.35, 'v');
  }, { G: '#4fa03a', g: '#7cc75a', v: '#d4f0a0' }],
  Apple: [c => {
    c.ellipse(8, 10.5, 4.6, 4.2, 'R');
    c.ellipse(8, 10.5, 4.6, 4.2, 'r', (x, y) => x > 9 && y > 11);
    c.set(6, 8, 'h'); c.px([[8, 5], [8, 6]], 'S'); c.px([[9, 5], [10, 4], [10, 5]], 'L');
  }, { R: '#e0443a', r: '#b8302a', h: '#ffb0a0', S: '#6b4226', L: '#5ab04a' }],
  Shrimp: [c => {
    for (let a = 0; a <= Math.PI * 1.1; a += 0.25) { const x = 8 + Math.cos(a + 0.2) * 4, y = 10 - Math.sin(a + 0.2) * 3.5; c.capsule(x, y, x, y, 1.6 - a * 0.35, 'P'); }
    polyFill(c, [[2, 11.5], [4.5, 12.5], [2, 14]], 'P');
    c.px([[6, 8], [8, 7], [10, 8]], 'p'); c.set(11, 9, 'K2'); c.px([[13, 7], [14, 6]], 'P');
  }, { P: '#ff8a66', p: '#ffc2a8', K2: '#301410' }],
  Egg: [c => {
    c.ellipse(8, 10.2, 3.8, 4.6, 'E');
    c.ellipse(8, 10.2, 3.8, 4.6, 'e', (x, y) => x > 8 && y > 11);
    c.px([[6, 8], [9, 12], [7, 13]], 's'); c.set(6, 7, 'h');
  }, { E: '#f6eedc', e: '#dccfae', s: '#c4a878', h: '#ffffff' }],
  Watermelon: [c => {
    c.ellipse(8, 8, 6.5, 6.5, 'G', (x, y) => y + 0.5 > 8.5);
    c.ellipse(8, 8, 5.6, 5.6, 'w', (x, y) => y + 0.5 > 8.5);
    c.ellipse(8, 8, 4.8, 4.8, 'R', (x, y) => y + 0.5 > 8.5);
    c.px([[6, 10], [8, 11], [10, 10], [7, 12], [9, 12]], 'K2');
  }, { G: '#3f9a3a', w: '#e8f5d0', R: '#ff5a64', K2: '#2a1a1a' }],
  Seeds: [c => {
    c.ellipse(8, 15, 6.5, 5.2, 'S', (x, y) => y + 0.5 < 15);
    for (const [x, y] of [[5, 13], [7, 12], [9, 12], [11, 13], [6, 11], [8, 10], [10, 11], [4, 14], [8, 13], [12, 14], [7, 14], [10, 14]]) c.set(x, y, (x + y) % 2 ? 's' : 'd');
  }, { S: '#e6c98e', s: '#b88a4a', d: '#fff0c8' }],
  Meat: [c => {
    c.capsule(11, 7, 13.5, 4.5, 0.8, 'W'); c.ellipse(13.8, 4, 1.2, 1.2, 'W'); c.ellipse(14.4, 5, 1.1, 1.1, 'W');
    c.ellipse(7, 11, 5, 3.6, 'M');
    c.ellipse(7, 11, 5, 3.6, 'm', (x, y) => y + 0.5 > 12.2);
    c.px([[5, 9], [6, 9]], 'h');
  }, { W: '#f6efdc', M: '#b5552e', m: '#8a3b1e', h: '#e08a5a' }],
  Popsicle: [c => {
    c.rect(7, 12, 2, 3, 'S');
    c.capsule(8, 4.5, 8, 10.5, 2.6, 'I');
    c.capsule(8, 4.5, 8, 10.5, 2.6, 'i', (x, y) => x > 8);
    c.set(10, 4, null); c.set(10, 5, null); c.set(9, 3, null);
    c.set(6, 6, 'h');
  }, { S: '#e0c080', I: '#6fc8ff', i: '#3f9ee8', h: '#e6f7ff' }],
  Cookie: [c => {
    c.ellipse(8, 10.5, 5.2, 4.4, 'C');
    c.ellipse(8, 10.5, 5.2, 4.4, 'c', (x, y) => y + 0.5 > 12.5);
    c.px([[6, 8], [10, 9], [7, 11], [11, 12], [5, 12], [9, 13]], 'K2');
    c.set(12, 7, null); c.set(13, 8, null);
  }, { C: '#d9a05b', c: '#b77c3c', K2: '#4a2a18' }],
  Mooncake: [c => {
    c.ellipse(8, 10.5, 5.6, 4.3, 'M');
    c.ellipse(8, 10.5, 4.2, 3.1, 'm');
    c.ellipse(8, 10.5, 2.4, 1.7, 'M');
    c.px([[8, 7], [8, 14], [3, 10], [13, 10]], 'h');
  }, { M: '#e0a64a', m: '#c9852e', h: '#f7d08a' }],
  Carrot: [c => {
    polyFill(c, [[4, 7], [12, 7], [8.5, 15]], 'O');
    c.px([[6, 9], [9, 11], [7, 12]], 'o');
    c.capsule(7, 6, 5, 3, 0.6, 'L'); c.capsule(8, 6, 8, 2.5, 0.6, 'L'); c.capsule(9, 6, 11, 3, 0.6, 'L');
  }, { O: '#ff8a2a', o: '#d4661a', L: '#5ab04a' }],
  Cheese: [c => {
    polyFill(c, [[2, 14], [14, 14], [14, 7.5]], 'Y');
    c.rect(2, 13, 12, 2, 'y');
    c.px([[11, 11], [8, 12], [12, 9]], 'h');
  }, { Y: '#ffd24a', y: '#f0b82e', h: '#d99a1e' }],
  Strawberry: [c => {
    polyFill(c, [[3, 8], [13, 8], [8.5, 15]], 'R');
    c.ellipse(5.5, 8.8, 2.6, 1.8, 'R'); c.ellipse(10.5, 8.8, 2.6, 1.8, 'R');
    c.px([[6, 9], [9, 10], [11, 9], [7, 12], [9, 13]], 's');
    c.px([[5, 7], [7, 6], [8, 5], [9, 6], [11, 7], [8, 7]], 'L');
  }, { R: '#ee3b4b', s: '#ffe07a', L: '#4ea83e' }],
  // other items (Items/)
  Ball: [c => { // tennis ball
    c.ellipse(8, 8, 6.6, 6.6, 'Y');
    c.ellipse(8, 8, 6.6, 6.6, 'y', (x, y) => x + y > 17);
    for (let t = -1; t <= 1; t += 0.1) { c.set(4.2 + t * t * 1.6, 8 + t * 5, 'W'); c.set(11.8 - t * t * 1.6, 8 + t * 5, 'W'); }
    c.set(5, 5, 'h');
  }, { Y: '#d8f04a', y: '#b6cc2e', W: '#ffffff', h: '#f4ffc0' }],
  Heart: [c => {
    c.ellipse(5.3, 6.3, 3.4, 3.2, 'R'); c.ellipse(10.7, 6.3, 3.4, 3.2, 'R');
    polyFill(c, [[1.9, 7], [14.1, 7], [8, 14.2]], 'R');
    c.px([[4, 5], [5, 4]], 'h'); c.px([[11, 11], [10, 12], [12, 10]], 'r');
  }, { R: '#ff4d6d', r: '#d62a4c', h: '#ffc2cf' }],
};
const ITEM_OUTLINE = '#2a1e1e';

// ---------- output ----------
const spaced = n => n.replace(/([a-z])([A-Z])/g, '$1 $2'); // RockPigeon -> Rock Pigeon (the pet's folder)
const PETS = {
  Turtle: { sheets: turtleSheets(), pal: TURTLE_PAL },
  Crab: { sheets: crabSheets(), pal: CRAB_PAL },
  Deer: { sheets: deerSheets(), pal: DEER_PAL },
  Snake: { sheets: snakeSheets(), pal: SNAKE_PAL },
  TabbyCat: { sheets: catSheets(), pal: CAT_PAL },
  Mishy: { sheets: catSheets({ chubby: true, lidKey: 'P' }), pal: MISHY_PAL },
  Capybara: { sheets: capySheets(), pal: CAPY_PAL },
  Clawdito: { sheets: clawdSheets(), pal: CLAWD_PAL },
  Kimoon: { sheets: kimoonSheets(), pal: KIMOON_PAL },
  ...Object.fromEntries(Object.entries(DRAGONS).map(([n, v]) => [n, { sheets: dragonSheets(v), pal: v.pal }])),
  ...Object.fromEntries(Object.entries(PIGEONS).map(([n, v]) => [n, { sheets: pigeonSheets(v), pal: v.pal }])),
};

const [mode, out, only] = process.argv.slice(2);
for (const [name, pet] of Object.entries(PETS).filter(([n]) => !only || only.split(',').includes(n))) {
  if (mode === 'preview') {
    // one image per pet: rows = states, scaled 8x on a light background
    const rows = Object.values(pet.sheets).map(f => sheet(f, pet.pal, 8, [205, 212, 220, 255]));
    const W = Math.max(...rows.map(r => r.w)) + 8, H = rows.reduce((a, r) => a + r.h + 8, 0);
    const buf = Buffer.alloc(W * H * 4, 255);
    let oy = 0;
    for (const r of rows) { for (let y = 0; y < r.h; y++) r.buf.copy(buf, ((oy + y) * W) * 4, y * r.w * 4, (y + 1) * r.w * 4); oy += r.h + 8; }
    fs.writeFileSync(path.join(out, `preview_${name}.png`), png(W, H, buf));
  } else if (mode === 'write') {
    const dir = path.join(out, spaced(name)); fs.mkdirSync(dir, { recursive: true });
    for (const [state, frames] of Object.entries(pet.sheets)) {
      const s = sheet(frames, pet.pal);
      fs.writeFileSync(path.join(dir, `${name}_${state}.png`), png(s.w, s.h, s.buf));
    }
  }
}
if (mode === 'items-preview' || mode === 'items') {
  for (const [name, [draw, pal]] of Object.entries(ITEMS)) {
    const s = sheet([item(draw)], { ...pal, K: ITEM_OUTLINE }, mode === 'items' ? 1 : 8, mode === 'items' ? null : [205, 212, 220, 255]);
    const food = !['Ball', 'Heart'].includes(name);
    const dir = mode === 'items' ? path.join(out, food ? 'Food' : '') : out;
    fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(path.join(dir, (mode === 'items' ? '' : 'item_') + name + '.png'), png(s.w, s.h, s.buf));
  }
}
console.log('done', mode);
