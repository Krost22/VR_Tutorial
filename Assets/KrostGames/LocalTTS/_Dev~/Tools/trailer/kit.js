/* kit.js — Motion Kit para películas HTML deterministas: todo es función pura de t (segundos).
   Contrato con el renderizador: window.FILM (meta), window.seek(t), window.ready = true.
   Prohibido en seek(): timers, Date, Math.random, transiciones/animaciones CSS que corran solas. */
(() => {
  'use strict';
  const clamp = (x, a = 0, b = 1) => (x < a ? a : x > b ? b : x);
  const lerp = (a, b, u) => a + (b - a) * u;
  const inv = (a, b, x) => clamp((x - a) / (b - a));

  // ---------- Easings: u 0..1 -> 0..1, exactos en 0 y 1
  function bezier(x1, y1, x2, y2) {
    const cx = 3 * x1, bx = 3 * (x2 - x1) - cx, ax = 1 - cx - bx, cy = 3 * y1, by = 3 * (y2 - y1) - cy, ay = 1 - cy - by;
    const X = s => ((ax * s + bx) * s + cx) * s, Y = s => ((ay * s + by) * s + cy) * s, D = s => (3 * ax * s + 2 * bx) * s + cx;
    return u => {
      if (u <= 0) return 0; if (u >= 1) return 1;
      let s = u;
      for (let i = 0; i < 8; i++) { const e = X(s) - u, d = D(s); if (Math.abs(e) < 1e-7) return Y(s); if (Math.abs(d) < 1e-6) break; s -= e / d; }
      let lo = 0, hi = 1; s = u;
      for (let i = 0; i < 40; i++) { const x = X(s); if (Math.abs(x - u) < 1e-7) break; if (x < u) lo = s; else hi = s; s = (lo + hi) / 2; }
      return Y(s);
    };
  }
  const pw = p => [u => u ** p, u => 1 - (1 - u) ** p, u => (u < 0.5 ? 2 ** (p - 1) * u ** p : 1 - (-2 * u + 2) ** p / 2)];
  const [inQuad, outQuad, inOutQuad] = pw(2), [inCubic, outCubic, inOutCubic] = pw(3), [inQuart, outQuart, inOutQuart] = pw(4), [inQuint, outQuint, inOutQuint] = pw(5);
  const E = {
    linear: u => u, inQuad, outQuad, inOutQuad, inCubic, outCubic, inOutCubic, inQuart, outQuart, inOutQuart, inQuint, outQuint, inOutQuint,
    inSine: u => 1 - Math.cos((u * Math.PI) / 2), outSine: u => Math.sin((u * Math.PI) / 2), inOutSine: u => -(Math.cos(Math.PI * u) - 1) / 2,
    inExpo: u => (u <= 0 ? 0 : 2 ** (10 * u - 10)), outExpo: u => (u >= 1 ? 1 : 1 - 2 ** (-10 * u)),
    inOutExpo: u => (u <= 0 ? 0 : u >= 1 ? 1 : u < 0.5 ? 2 ** (20 * u - 10) / 2 : (2 - 2 ** (-20 * u + 10)) / 2),
    outCirc: u => Math.sqrt(1 - (u - 1) ** 2), inCirc: u => 1 - Math.sqrt(1 - u * u),
    outBack: (s = 1.70158) => u => 1 + (s + 1) * (u - 1) ** 3 + s * (u - 1) ** 2,
    bezier,
    // Curvas de autor (elige estas antes que las genéricas)
    smooth: bezier(0.65, 0, 0.35, 1),   // ir de A a B en pantalla
    enter: bezier(0.16, 1, 0.3, 1),     // entradas: llega rápido y frena largo
    exit: bezier(0.5, 0, 0.75, 0),      // salidas: arranca suave y se va acelerando
    emph: bezier(0.2, 0, 0, 1),         // movimientos grandes / cámara con peso
    snap: bezier(0.2, 0.8, 0.2, 1),     // UI ágil
    anticip: bezier(0.6, -0.28, 0.735, 0.045), // retrocede antes de salir (usar poco)
  };

  // ---------- Resortes en forma cerrada (respuesta al escalón 0 -> 1). f = frecuencia (Hz), z = amortiguamiento
  function spring(t, f = 2.2, z = 0.78) {
    if (t <= 0) return 0;
    const w = 2 * Math.PI * f;
    if (z < 1) { const wd = w * Math.sqrt(1 - z * z); return 1 - Math.exp(-z * w * t) * (Math.cos(wd * t) + ((z * w) / wd) * Math.sin(wd * t)); }
    if (z === 1) return 1 - Math.exp(-w * t) * (1 + w * t);
    const s = w * Math.sqrt(z * z - 1), r1 = -z * w + s, r2 = -z * w - s;
    return 1 + (r2 * Math.exp(r1 * t) - r1 * Math.exp(r2 * t)) / (r1 - r2);
  }
  const SPRING = { snappy: [3.2, 0.85], ui: [2.6, 0.8], soft: [1.8, 0.82], heavy: [1.3, 0.9], bouncy: [2.6, 0.55] };
  const sp = (t, t0, preset = 'ui') => { const [f, z] = Array.isArray(preset) ? preset : SPRING[preset]; return spring(t - t0, f, z); };

  // ---------- Valores: número, array o color hex (mezcla en OKLab: sin grises sucios)
  const hex2rgb = h => { h = h.replace('#', ''); if (h.length === 3) h = [...h].map(c => c + c).join(''); const n = parseInt(h, 16); return [(n >> 16 & 255) / 255, (n >> 8 & 255) / 255, (n & 255) / 255]; };
  const lin = c => (c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4), gam = c => (c <= 0.0031308 ? 12.92 * c : 1.055 * c ** (1 / 2.4) - 0.055);
  const toLab = rgb => { const [r, g, b] = rgb.map(lin); const l = Math.cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b), m = Math.cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b), s = Math.cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b); return [0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s, 1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s, 0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s]; };
  const fromLab = ([L, a, b]) => { const l = (L + 0.3963377774 * a + 0.2158037573 * b) ** 3, m = (L - 0.1055613458 * a - 0.0638541728 * b) ** 3, s = (L - 0.0894841775 * a - 1.291485548 * b) ** 3; return [4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s, -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s, -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s].map(c => Math.round(clamp(gam(clamp(c))) * 255)); };
  const mixColor = (a, b, u) => { const A = toLab(hex2rgb(a)), B = toLab(hex2rgb(b)); const [r, g, bb] = fromLab(A.map((x, i) => x + (B[i] - x) * u)); return `rgb(${r},${g},${bb})`; };
  const mix = (a, b, u) => (typeof a === 'number' ? a + (b - a) * u : Array.isArray(a) ? a.map((x, i) => x + (b[i] - x) * u) : mixColor(a, b, u));

  // ---------- Tiempo
  const seg = (t, t0, d, ease = E.smooth) => ease(clamp((t - t0) / d));          // progreso 0..1 con easing
  const on = (t, a, b) => t >= a && t < b;
  // kf(t, [[t0, v0], [t1, v1, ease], ...]): la ease de una clave describe cómo se LLEGA a ella
  function kf(t, keys, ease = E.smooth) {
    if (t <= keys[0][0]) return keys[0][1];
    for (let i = 1; i < keys.length; i++) { const [t1, v1, e] = keys[i]; if (t < t1) { const [t0, v0] = keys[i - 1]; return mix(v0, v1, (e || ease)((t - t0) / (t1 - t0))); } }
    return keys[keys.length - 1][1];
  }
  // springKf: un resorte por cambio de objetivo; la velocidad nunca salta al re-apuntar
  function springKf(t, keys, preset = 'ui') {
    let v = keys[0][1];
    for (let i = 1; i < keys.length && t > keys[i][0]; i++) { const k = sp(t, keys[i][0], preset), a = keys[i - 1][1], b = keys[i][1]; v = typeof v === 'number' ? v + (b - a) * k : v.map((x, j) => x + (b[j] - a[j]) * k); }
    return v;
  }
  const stagger = (i, n, each, from = 'start') => { const c = (n - 1) / 2; const k = from === 'center' ? Math.abs(i - c) : from === 'edges' ? c - Math.abs(i - c) : from === 'end' ? n - 1 - i : from === 'random' ? hash(i, 7) * (n - 1) : i; return k * each; };
  const beats = (bpm, n, offset = 0) => Array.from({ length: n }, (_, i) => offset + (i * 60) / bpm);
  const pulse = (t, times, amp = 0.02, decay = 6) => times.reduce((s, ti) => (t < ti ? s : s + amp * Math.exp(-(t - ti) * decay) * Math.min(1, (t - ti) * 40)), 0);
  const vel = (fn, t, h = 1 / 240) => (fn(t + h) - fn(t - h)) / (2 * h);

  // ---------- Azar determinista
  const hash = (n, s = 0) => { const x = Math.sin(n * 127.1 + s * 311.7) * 43758.5453; return x - Math.floor(x); };
  const noise = (x, s = 0) => { const i = Math.floor(x), f = x - i, u = f * f * (3 - 2 * f); return lerp(hash(i, s), hash(i + 1, s), u) * 2 - 1; };
  const rng = seed => () => { seed |= 0; seed = (seed + 0x6d2b79f5) | 0; let r = Math.imul(seed ^ (seed >>> 15), 1 | seed); r = (r + Math.imul(r ^ (r >>> 7), 61 | r)) ^ r; return ((r ^ (r >>> 14)) >>> 0) / 4294967296; };
  const drift = (t, amp = 6, speed = 0.25, s = 0) => [noise(t * speed, s) * amp, noise(t * speed, s + 9) * amp];

  // ---------- DOM
  const $ = (q, r = document) => r.querySelector(q), $$ = (q, r = document) => [...r.querySelectorAll(q)];
  // set(el, {x, y, s, sx, sy, r, o, blur, w, h, rad, clip, pre, css}) — un solo transform por elemento
  function set(el, p) {
    const s = el.style, tr = [];
    if (p.pre) tr.push(p.pre);
    if (p.x != null || p.y != null) tr.push(`translate(${(p.x || 0).toFixed(2)}px,${(p.y || 0).toFixed(2)}px)`);
    if (p.r != null) tr.push(`rotate(${p.r.toFixed(3)}deg)`);
    if (p.s != null) tr.push(`scale(${p.s.toFixed(4)})`);
    if (p.sx != null || p.sy != null) tr.push(`scale(${(p.sx ?? 1).toFixed(4)},${(p.sy ?? 1).toFixed(4)})`);
    if (tr.length) s.transform = tr.join(' ');
    if (p.o != null) { s.opacity = clamp(p.o).toFixed(4); s.visibility = p.o <= 0.001 ? 'hidden' : 'inherit'; }
    if (p.blur != null) s.filter = p.blur > 0.05 ? `blur(${p.blur.toFixed(2)}px)` : 'none';
    if (p.w != null) s.width = p.w + 'px';
    if (p.h != null) s.height = p.h + 'px';
    if (p.rad != null) s.borderRadius = p.rad + 'px';
    if (p.clip != null) s.clipPath = p.clip;
    if (p.css) Object.assign(s, p.css);
  }
  const show = (el, v) => { el.style.visibility = v ? 'inherit' : 'hidden'; };
  // split(el, 'words'|'chars', mask): envuelve el texto (respeta <em>, <b>…) y devuelve las unidades animables
  function split(el, mode = 'words', mask = true) {
    const units = [];
    const unit = txt => {
      const u = document.createElement('span'); u.textContent = txt; u.className = 'mk-u'; u.style.display = 'inline-block'; units.push(u);
      if (!mask) return u;
      const m = document.createElement('span'); m.className = 'mk-m';
      m.style.cssText = 'display:inline-block;overflow:hidden;vertical-align:top;padding:.14em .06em .18em;margin:-.14em -.06em -.18em';  // holgura para tildes y descendentes
      m.appendChild(u); return m;
    };
    const walk = node => [...node.childNodes].forEach(c => {
      if (c.nodeType === 3) {
        const frag = document.createDocumentFragment();
        for (const w of c.textContent.split(/(\s+)/)) {
          if (!w) continue;
          if (/^\s+$/.test(w)) { frag.appendChild(document.createTextNode(w)); continue; }
          if (mode === 'chars') { const ww = document.createElement('span'); ww.style.cssText = 'display:inline-block;white-space:nowrap'; [...w].forEach(ch => ww.appendChild(unit(ch))); frag.appendChild(ww); }
          else frag.appendChild(unit(w));
        }
        c.replaceWith(frag);
      } else if (c.nodeType === 1) walk(c);
    });
    walk(el); return units;
  }
  // agrupa unidades por línea visual (llamar después de cargar fuentes)
  const lines = units => { const L = []; let last = -1e9; for (const u of units) { const y = u.offsetTop; if (Math.abs(y - last) > 4) { L.push([]); last = y; } L[L.length - 1].push(u); } return L; };
  // rise: subida enmascarada palabra a palabra (o letra a letra); out = tiempo de salida opcional
  // from / outFrom: orden del escalonado ('start' | 'end' | 'center' | 'edges' | 'random')
  function rise(units, t, t0, { each = 0.06, dur = 0.8, ease = E.enter, y = 140, r = 0, from = 'start', out = null, outDur = 0.45, outEach = 0.03, outFrom = 'start' } = {}) {
    const n = units.length;
    units.forEach((u, i) => {
      const p = seg(t, t0 + stagger(i, n, each, from), dur, ease), q = out == null ? 0 : seg(t, out + stagger(i, n, outEach, outFrom), outDur, E.exit);
      u.style.transform = `translateY(${((1 - p) * y - q * y).toFixed(2)}%) rotate(${((1 - p) * r).toFixed(2)}deg)`;
    });
  }
  // trim(path, a, b): como "Trim Paths" de After Effects (fracción visible a..b); trim(path, u) dibuja 0..u
  function trim(path, a, b = null) {
    if (b == null) { b = a; a = 0; }
    const L = path.__L || (path.__L = path.getTotalLength());
    path.style.visibility = b - a <= 0.0005 ? 'hidden' : 'inherit';
    path.style.strokeDasharray = `${(Math.max(0, b - a) * L).toFixed(2)} ${(L + 2).toFixed(2)}`;
    path.style.strokeDashoffset = (-a * L).toFixed(2);
  }
  // along(path, u): punto y ángulo sobre un trazado SVG (motion path)
  function along(path, u) { const L = path.__L || (path.__L = path.getTotalLength()), d = clamp(u) * L, p = path.getPointAtLength(d), q = path.getPointAtLength(Math.min(L, d + 0.5)), o = path.getPointAtLength(Math.max(0, d - 0.5)); return { x: p.x, y: p.y, a: (Math.atan2(q.y - o.y, q.x - o.x) * 180) / Math.PI }; }
  // morphPath: interpola dos "d" con la MISMA estructura de comandos
  const morphPath = (d0, d1, u) => { const B = d1.match(/-?[\d.]+(?:e-?\d+)?/g).map(Number); let i = 0; return d0.replace(/-?[\d.]+(?:e-?\d+)?/g, a => (+a + (B[i++] - +a) * u).toFixed(2)); };
  // radio para que un círculo con centro (cx,cy) cubra todo el cuadro (floods / iris)
  const coverR = (cx, cy, W, H) => Math.hypot(Math.max(cx, W - cx), Math.max(cy, H - cy)) * 1.04;
  const num = (v, dec = 0, loc = 'es-CO') => v.toLocaleString(loc, { minimumFractionDigits: dec, maximumFractionDigits: dec });
  const typed = (s, t, t0, cps = 16) => s.slice(0, Math.max(0, Math.floor((t - t0) * cps)));
  // fit: ajusta font-size para que el texto quepa en maxW (el debe ser inline-block + nowrap)
  function fit(el, maxW, max = 220, min = 10) { let lo = min, hi = max; for (let i = 0; i < 18; i++) { const m = (lo + hi) / 2; el.style.fontSize = m + 'px'; if (el.scrollWidth <= maxW) lo = m; else hi = m; } el.style.fontSize = lo + 'px'; return lo; }

  // ---------- Cámara: keys [[t, zoom, x, y, ease?]]; (x,y) = punto del mundo al centro; zoom interpolado en escala log
  // opt.shake = [amplitud px, velocidad] (deriva orgánica), opt.punch = zoom extra (p. ej. pulse() en los beats)
  function camera(el, t, keys, W, H, opt = {}) {
    let a = keys[0], b = keys[0], u = 0;
    if (t > keys[0][0]) { a = b = keys[keys.length - 1]; for (let i = 1; i < keys.length; i++) if (t < keys[i][0]) { a = keys[i - 1]; b = keys[i]; u = (b[4] || E.emph)((t - a[0]) / (b[0] - a[0])); break; } }
    const z = Math.exp(lerp(Math.log(a[1]), Math.log(b[1]), u)) * (1 + (opt.punch || 0)); let x = lerp(a[2], b[2], u), y = lerp(a[3], b[3], u);
    if (opt.shake) { const [dx, dy] = drift(t, opt.shake[0], opt.shake[1] || 0.3); x += dx; y += dy; }
    el.style.transformOrigin = '0 0';
    el.style.transform = `translate(${(W / 2 - x * z).toFixed(2)}px,${(H / 2 - y * z).toFixed(2)}px) scale(${z.toFixed(5)})`;
    return { z, x, y };
  }

  // ---------- Grano de película (teselas precalculadas: barato y determinista)
  // blend 'film' = dos capas (screen + multiply): se ve en fondos oscuros y claros; o cualquier mix-blend-mode CSS
  function grain(stage, { opacity = 0.05, size = 180, tiles = 6, fps = 24, blend = 'film', seed = 1 } = {}) {
    if (MK.alpha) return () => {};               // con fondo transparente el grano taparía la transparencia
    const r = rng(seed), urls = [];
    for (let k = 0; k < tiles; k++) {
      const c = document.createElement('canvas'); c.width = c.height = size; const g = c.getContext('2d'), im = g.createImageData(size, size);
      for (let i = 0; i < im.data.length; i += 4) { const v = (r() * 255) | 0; im.data[i] = im.data[i + 1] = im.data[i + 2] = v; im.data[i + 3] = 255; }
      g.putImageData(im, 0, 0); urls.push(c.toDataURL());
    }
    const els = (blend === 'film' ? ['screen', 'multiply'] : [blend]).map(m => {
      const el = document.createElement('div');
      el.style.cssText = `position:absolute;inset:0;pointer-events:none;z-index:9000;opacity:${opacity};mix-blend-mode:${m};background-size:${size}px ${size}px`;
      stage.appendChild(el); return el;
    });
    return t => { const f = Math.floor(t * fps + 1e-6); els.forEach((el, j) => { el.style.backgroundImage = `url(${urls[(f + 3 * j) % tiles]})`; el.style.backgroundPosition = `${(hash(f, 3 + j) * size) | 0}px ${(hash(f, 5 + j) * size) | 0}px`; }); };
  }

  // ---------- Reproductor de preview (Space, ←/→ = 1 frame, Shift+←/→ = 1 s, Home, L = loop). En ?render no aparece.
  function player(F, t0, startPaused) {
    const stage = $('#stage'), H = 46;
    const css = document.createElement('style');
    css.textContent = `html,body{margin:0;height:100%;background:#0e0e10;overflow:hidden}#stage{position:absolute!important;left:0;top:0;transform-origin:0 0}
#mk-hud{position:fixed;left:0;right:0;bottom:0;height:${H}px;display:flex;align-items:center;gap:12px;padding:0 14px;background:#151517;color:#cfcfd4;font:12px/1 ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;z-index:2147483647;user-select:none;box-sizing:border-box}
#mk-hud button{all:unset;cursor:pointer;min-width:30px;height:30px;padding:0 8px;box-sizing:border-box;display:grid;place-items:center;border-radius:7px;background:#26262b;color:#fff}
#mk-bar{position:relative;flex:1;height:30px;display:flex;align-items:center}#mk-bar input{width:100%;accent-color:#f5f5f2;margin:0}
#mk-bar i{position:absolute;top:2px;width:2px;height:7px;background:#ff6a2a;pointer-events:none}#mk-bar i.c{background:#5aa9ff;height:11px}`;
    document.head.appendChild(css);
    const hud = document.createElement('div'); hud.id = 'mk-hud';
    hud.innerHTML = `<button id="mk-pp">▶</button><button id="mk-lp" title="loop (L)">⟲</button><span id="mk-tc">0.00</span><div id="mk-bar"><input id="mk-sl" type="range" min="0" max="${F.duration}" step="${1 / F.fps}" value="0"></div><span>${F.w}×${F.h} · ${F.fps}fps · ${F.duration}s</span>`;
    document.body.appendChild(hud);
    const bar = $('#mk-bar'), sl = $('#mk-sl'), tc = $('#mk-tc'), pp = $('#mk-pp'), lp = $('#mk-lp');
    const mark = (tt, c) => { const i = document.createElement('i'); if (c) i.className = c; i.style.left = `calc(${(tt / F.duration) * 100}% - 1px)`; bar.appendChild(i); };
    (F.cues || []).forEach(c => mark(c[0])); (F.cuts || []).forEach(c => mark(c, 'c'));
    const au = F.audio ? new Audio(F.audio) : null;
    let t = t0, playing = !startPaused, loop = F.loop !== false, last = performance.now();
    const fitStage = () => { const aw = innerWidth, ah = innerHeight - H, k = Math.min(aw / F.w, ah / F.h); stage.style.transform = `translate(${(aw - F.w * k) / 2}px,${(ah - F.h * k) / 2}px) scale(${k})`; };
    const draw = () => { window.seek(t); sl.value = t; tc.textContent = t.toFixed(2) + 's  f' + Math.round(t * F.fps); pp.textContent = playing ? '❚❚' : '▶'; lp.style.opacity = loop ? 1 : 0.35; };
    const syncAudio = () => { if (!au) return; if (playing) { au.currentTime = t; au.play().catch(() => {}); } else au.pause(); };
    const toggle = () => { if (!playing && t >= F.duration - 1e-3) t = 0; playing = !playing; last = performance.now(); syncAudio(); draw(); };
    const go = v => { t = clamp(v, 0, F.duration); if (au && playing) au.currentTime = t; draw(); };
    pp.onclick = toggle; lp.onclick = () => { loop = !loop; draw(); }; stage.onclick = toggle;
    sl.oninput = () => { playing = false; syncAudio(); go(+sl.value); };
    addEventListener('keydown', e => {
      const fr = 1 / F.fps;
      if (e.code === 'Space') { e.preventDefault(); toggle(); }
      else if (e.code === 'ArrowRight' || e.code === 'ArrowLeft') { e.preventDefault(); playing = false; syncAudio(); go(t + (e.code === 'ArrowRight' ? 1 : -1) * (e.shiftKey ? 1 : fr)); }
      else if (e.code === 'Home') go(0); else if (e.key === 'l' || e.key === 'L') { loop = !loop; draw(); }
    });
    addEventListener('resize', fitStage); fitStage();
    const tick = now => {
      if (playing) {
        if (au && !au.paused) t = au.currentTime; else t += (now - last) / 1000;
        if (t >= F.duration) { if (loop) { t %= F.duration; if (au) au.currentTime = t; } else { t = F.duration; playing = false; syncAudio(); } }
        draw();
      }
      last = now; requestAnimationFrame(tick);
    };
    draw(); requestAnimationFrame(tick);
  }

  // ---------- Arranque: fuentes -> imágenes -> setup() -> seek(t0) -> ready
  async function boot(F, seekFn, setup) {
    try { await boot2(F, seekFn, setup); }
    catch (e) { window.bootError = String((e && e.stack) || e); console.error('BOOT: ' + window.bootError); throw e; }
  }
  async function boot2(F, seekFn, setup) {
    F.fps = F.fps || 60;
    window.FILM = F;
    const q = new URLSearchParams(location.search), render = q.has('render');
    MK.render = render; MK.alpha = q.has('alpha');
    if (render) document.documentElement.classList.add('mk-render');
    if (MK.alpha) { document.documentElement.classList.add('mk-alpha'); const st = document.createElement('style'); st.textContent = 'html,body,#stage{background:transparent!important}'; document.head.appendChild(st); }
    const stage = $('#stage'); stage.style.width = F.w + 'px'; stage.style.height = F.h + 'px';
    for (const f of F.fonts || []) { try { await document.fonts.load(f); } catch (e) { /* noop */ } }
    await document.fonts.ready;
    for (const f of F.fonts || []) if (!document.fonts.check(f)) console.warn('FUENTE NO CARGADA (se usará otra): ' + f);
    await Promise.all([...document.images].map(i => (i.complete ? Promise.resolve() : new Promise(r => { i.onload = i.onerror = r; })).then(() => (i.decode ? i.decode().catch(() => console.warn('IMAGEN NO CARGADA: ' + i.src)) : 0))));
    if (setup) await setup();
    document.getAnimations().forEach(a => a.pause());
    window.seek = tt => seekFn(clamp(+tt, 0, F.duration));
    const t0 = q.has('t') ? +q.get('t') : 0;
    window.seek(t0);
    window.ready = true;
    if (!render) player(F, t0, q.has('t'));
  }

  const MK = { clamp, lerp, inv, E, bezier, spring, SPRING, sp, mix, mixColor, seg, on, kf, springKf, stagger, beats, pulse, vel, hash, noise, rng, drift,
    $, $$, set, show, split, lines, rise, trim, along, morphPath, coverR, num, typed, fit, camera, grain, boot, render: false, alpha: false };
  window.MK = MK;
})();
