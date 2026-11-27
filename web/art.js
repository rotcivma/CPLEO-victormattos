/* =====================================================================
   PROPULSOR — biblioteca de arte procedural (Módulo 05)
   Estilo: cartoon com contorno escuro, cel shading em 2–3 tons, brilhos
   em estrela, núcleos brancos nos efeitos. Identidade própria: Nina,
   fábrica de brinquedos Engrenópolis, robôs Parafusinhos.
   Convenção: funções de sprite recebem um ctx em coordenadas de MUNDO
   (y para cima, 1 u = 1 m) e desenham em espaço local y-para-baixo.
   ===================================================================== */
(function (global) {
  "use strict";
  const OUT = "#1a1630";
  const TAU = Math.PI * 2;

  // ---------------- cores ----------------
  function hex2rgb(h) { h = h.replace("#", ""); if (h.length === 3) h = h.split("").map(c => c + c).join(""); const n = parseInt(h, 16); return [n >> 16 & 255, n >> 8 & 255, n & 255]; }
  function rgb2hex(r, g, b) { return "#" + [r, g, b].map(v => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, "0")).join(""); }
  /** k > 0 clareia, k < 0 escurece (−1..1). */
  function shade(h, k) { const [r, g, b] = hex2rgb(h); const t = k > 0 ? 255 : 0, a = Math.abs(k); return rgb2hex(r + (t - r) * a, g + (t - g) * a, b + (t - b) * a); }
  function alpha(h, a) { const [r, g, b] = hex2rgb(h); return `rgba(${r},${g},${b},${a})`; }

  // ---------------- primitivas ----------------
  function rr(c, x, y, w, h, r) { c.beginPath(); if (c.roundRect) c.roundRect(x, y, w, h, r); else c.rect(x, y, w, h); }
  function ink(c, lw = 0.05) { c.lineWidth = lw; c.strokeStyle = OUT; c.lineJoin = "round"; c.lineCap = "round"; c.stroke(); }
  /** Preenche o caminho atual com cel shading vertical (luz no topo) dentro da caixa dada. */
  function cel(c, base, x, y, w, h, opts = {}) {
    const g = c.createLinearGradient(x, y, x, y + h);
    g.addColorStop(0, shade(base, opts.light ?? .28));
    g.addColorStop(.45, base);
    g.addColorStop(.46, shade(base, -(opts.dark ?? .16)));
    g.addColorStop(1, shade(base, -(opts.dark ?? .16) - .1));
    c.fillStyle = g; c.fill();
    if (opts.noInk) return;
    ink(c, opts.lw ?? .05);
  }
  function capsule(c, x1, y1, x2, y2, r) {
    const a = Math.atan2(y2 - y1, x2 - x1);
    c.beginPath(); c.arc(x1, y1, r, a + Math.PI / 2, a - Math.PI / 2); c.arc(x2, y2, r, a - Math.PI / 2, a + Math.PI / 2); c.closePath();
  }
  /** Membro de dois segmentos (coxa/canela ou braço/antebraço) com articulação. */
  function limb(c, x, y, a1, l1, a2, l2, w, color) {
    const kx = x + Math.sin(a1) * l1, ky = y + Math.cos(a1) * l1;
    const ex = kx + Math.sin(a1 + a2) * l2, ey = ky + Math.cos(a1 + a2) * l2;
    capsule(c, x, y, kx, ky, w); c.fillStyle = color; c.fill(); ink(c, .04);
    capsule(c, kx, ky, ex, ey, w * .92); c.fillStyle = color; c.fill(); ink(c, .04);
    return [ex, ey, a1 + a2];
  }
  function star(c, x, y, r1, r2, n, rot = 0) {
    c.beginPath();
    for (let i = 0; i < n * 2; i++) { const r = i % 2 ? r2 : r1, a = rot + i * Math.PI / n; c.lineTo(x + Math.cos(a) * r, y + Math.sin(a) * r); }
    c.closePath();
  }
  /** Brilho em estrela: camadas coloridas com núcleo branco (sem shadowBlur — leve no celular). */
  function burst(c, x, y, r, color, t = 0, spikes = 9) {
    const wob = 1 + .12 * Math.sin(t * 40);
    star(c, x, y, r * 1.15 * wob, r * .55, spikes, t * 3); c.fillStyle = alpha(color, .35); c.fill();
    star(c, x, y, r * .85 * wob, r * .45, spikes, -t * 2); c.fillStyle = alpha(color, .8); c.fill();
    c.beginPath(); c.arc(x, y, r * .38, 0, TAU); c.fillStyle = "#ffffff"; c.fill();
  }
  function local(c, x, y, fn, sx = 1, rot = 0) { c.save(); c.translate(x, y); c.scale(sx, -1); if (rot) c.rotate(rot); fn(); c.restore(); }
  function hash(a, b = 0) { let h = (a * 374761393 + b * 668265263) | 0; h = (h ^ (h >>> 13)) * 1274126177 | 0; return ((h ^ (h >>> 16)) >>> 0) / 4294967295; }
  function textUp(c, txt, x, y, size, color, font = "Lilita One") {
    c.save(); c.translate(x, y); c.scale(1, -1); c.font = `${size}px "${font}", sans-serif`; c.textAlign = "center"; c.textBaseline = "middle";
    c.fillStyle = color; c.fillText(txt, 0, 0); c.restore();
  }

  // =================================================================
  // PERSONAGEM — Nina (mecânica; cabeça grande, rabo de cavalo, óculos de solda)
  // =================================================================
  const SKIN = "#f2c29b", HAIR = "#7a3418", GLOVE = "#3a3f5c", BOOT = "#5b3a26";
  const JETS = {
    jet_padrao: { tank: "#e23a3a", trim: "#ffcc4d", flame: ["#fff6c2", "#ffcc4d", "#ff7a1a"] },
    jet_bolhas: { tank: "#5fd3e0", trim: "#ffffff", flame: ["#ffffff", "#bff6ff", "#5fd3e0"] },
    jet_confete:{ tank: "#9b5cff", trim: "#ff9f1c", flame: ["#ffffff", "#ff9f1c", "#ff4fa3"] },
  };

  function jetpack(c, id, t, thrust) {
    const J = JETS[id] || JETS.jet_padrao;
    if (id === "jet_bolhas") {
      for (const dx of [-.5, -.34]) { rr(c, dx - .07, -.62, .14, .5, .07); c.fillStyle = alpha(J.tank, .85); c.fill(); ink(c, .035);
        c.beginPath(); c.arc(dx, -.5 + .1 * Math.sin(t * 6 + dx * 9), .03, 0, TAU); c.fillStyle = "#fff"; c.fill(); }
      c.beginPath(); c.arc(-.42, -.08, .07, 0, TAU); c.lineWidth = .035; c.strokeStyle = J.trim; c.stroke();
    } else if (id === "jet_confete") {
      c.beginPath(); c.moveTo(-.55, -.64); c.lineTo(-.3, -.64); c.lineTo(-.36, -.1); c.lineTo(-.49, -.1); c.closePath(); cel(c, J.tank, -.55, -.64, .25, .54);
      for (let i = 0; i < 4; i++) { c.fillStyle = ["#ffd23f", "#2ec4b6", "#ff4fa3", "#fff"][i]; c.fillRect(-.5 + i * .04, -.55 + (i % 2) * .15, .03, .03); }
    } else {
      // extintor: cilindro vermelho, manômetro, mangueira
      rr(c, -.58, -.66, .26, .58, .12); cel(c, J.tank, -.58, -.66, .26, .58);
      rr(c, -.53, -.62, .05, .48, .03); c.fillStyle = alpha("#ffffff", .35); c.fill();
      c.beginPath(); c.arc(-.45, -.5, .065, 0, TAU); c.fillStyle = "#f6f7fb"; c.fill(); ink(c, .03);
      c.beginPath(); c.moveTo(-.45, -.5); c.lineTo(-.45 + Math.cos(t * 2) * .045, -.5 + Math.sin(t * 2) * .045); c.lineWidth = .02; c.strokeStyle = "#e23a3a"; c.stroke();
      c.beginPath(); c.moveTo(-.33, -.6); c.quadraticCurveTo(-.15, -.78, -.12, -.55); c.lineWidth = .05; c.strokeStyle = OUT; c.stroke();
    }
    // bocal
    rr(c, -.52, -.1, .14, .09, .03); c.fillStyle = "#c9a227"; c.fill(); ink(c, .03);
    if (thrust && id !== "jet_bolhas") {
      const f = .25 + .1 * Math.sin(t * 60) + .06 * Math.sin(t * 37);
      const cols = J.flame;
      for (let i = 0; i < 3; i++) {
        const s = 1 - i * .3;
        c.beginPath(); c.moveTo(-.52 + .02 * i, 0); c.quadraticCurveTo(-.45, (f + .2) * s * 1.6, -.38 - .02 * i, 0); c.closePath();
        c.fillStyle = cols[2 - i]; c.fill();
      }
      burst(c, -.45, .04, .11, cols[1], t, 7);
    }
  }

  /**
   * Nina. pose: idle | run | fly | fall | hit | tumble | down
   * o = { t, phase, outfit, hat, jet, vy, thrust, flash }
   * Origem: pés (y local para baixo). Altura ~1,3 u.
   */
  function nina(c, x, y, o, rot = 0, scale = 1) {
    local(c, x, y, () => {
      c.scale(scale, scale);
      const t = o.t || 0, pose = o.pose || "idle", outfit = o.outfit || "#2ec4b6";
      const dark = shade(outfit, -.28);
      let hip = 0, knee = 0, hipB = 0, kneeB = 0, sh = 0, el = 0, shB = 0, elB = 0, bob = 0, lean = 0, face = "smile";
      const p = o.phase || 0;
      if (pose === "idle") { bob = Math.sin(t * 3) * .015; sh = .15; el = -.3; shB = -.1; elB = -.2; face = "smile"; }
      else if (pose === "run") {
        hip = Math.sin(p) * .75; hipB = -Math.sin(p) * .75; knee = Math.max(0, -Math.cos(p)) * 1.1; kneeB = Math.max(0, Math.cos(p)) * 1.1;
        sh = -Math.sin(p) * .8; shB = Math.sin(p) * .8; el = -.9; elB = -.9; bob = Math.abs(Math.sin(p)) * .05; lean = .08; face = "determined";
      } else if (pose === "fly") {
        hip = -.35; knee = .5; hipB = -.6; kneeB = .7; sh = -1.9 + Math.sin(t * 9) * .06; el = -.4; shB = -1.6; elB = -.5; lean = -(o.vy || 0) * .012; face = "grin";
      } else if (pose === "fall") {
        hip = .35 + Math.sin(t * 10) * .2; knee = .3; hipB = -.2 - Math.sin(t * 10) * .2; kneeB = .5; sh = -2.6 + Math.sin(t * 14) * .3; el = .4; shB = -2.4 - Math.sin(t * 14) * .3; elB = .3;
        lean = -(o.vy || 0) * .012; face = "worried";
      } else if (pose === "hit" || pose === "tumble") {
        hip = .8; knee = .2; hipB = -.8; kneeB = .2; sh = -2.2; el = .2; shB = 2.2; elB = -.2; face = "shock";
      } else if (pose === "down") { hip = .1; knee = .1; hipB = -.1; kneeB = .2; sh = -2.9; el = .2; shB = -2.6; elB = .1; face = "ko"; }
      c.rotate(rot + lean);
      c.translate(0, -bob);
      // sombra no chão (só em pé)
      // perna de trás + braço de trás (tons escuros)
      const [bfx, bfy, bfa] = limb(c, -.05, -.42, hipB, .22, kneeB, .2, .075, dark);
      boot(c, bfx, bfy, bfa, shade(BOOT, -.2));
      limb(c, -.08, -.82, shB, .19, elB, .17, .06, dark);
      // mochila
      jetpack(c, o.jet || "jet_padrao", t, !!o.thrust);
      // tronco
      rr(c, -.25, -.9, .48, .52, .14); cel(c, outfit, -.25, -.9, .48, .52);
      c.beginPath(); c.moveTo(-.02, -.88); c.lineTo(-.02, -.42); c.lineWidth = .02; c.strokeStyle = dark; c.stroke(); // zíper
      rr(c, .05, -.8, .12, .1, .03); c.fillStyle = dark; c.fill();                                         // bolso
      rr(c, -.24, -.86, .1, .1, .03); c.fillStyle = "#ff9f1c"; c.fill(); ink(c, .025);                    // emblema
      rr(c, -.26, -.47, .5, .08, .03); c.fillStyle = "#5b3a26"; c.fill(); ink(c, .03);                    // cinto
      rr(c, -.03, -.47, .08, .08, .02); c.fillStyle = "#ffd23f"; c.fill();
      c.save(); c.translate(.17, -.43); c.rotate(.5); rr(c, -.02, 0, .04, .16, .02); c.fillStyle = "#a8aec9"; c.fill(); ink(c, .02); c.restore(); // chave inglesa
      // perna da frente
      const [ffx, ffy, ffa] = limb(c, .05, -.42, hip, .22, knee, .2, .08, outfit);
      boot(c, ffx, ffy, ffa, BOOT);
      // cabeça
      head(c, 0, -1.06, t, face, o.hat);
      // braço da frente
      const [hx, hy] = limb(c, .1, -.82, sh, .19, el, .17, .065, outfit);
      c.beginPath(); c.arc(hx, hy, .07, 0, TAU); c.fillStyle = GLOVE; c.fill(); ink(c, .035);
      if (o.flash) { c.globalCompositeOperation = "lighter"; rr(c, -.6, -1.4, 1.2, 1.4, .3); c.fillStyle = alpha("#7df9ff", .35 * o.flash); c.fill(); c.globalCompositeOperation = "source-over"; }
    });
  }
  function boot(c, x, y, a, color) {
    c.save(); c.translate(x, y); c.rotate(-a * .5);
    rr(c, -.08, -.04, .2, .11, .05); cel(c, color, -.08, -.04, .2, .11, { lw: .035 });
    c.fillStyle = "#2a1a12"; c.fillRect(-.08, .05, .2, .025);
    c.restore();
  }
  function head(c, x, y, t, face, hat) {
    c.save(); c.translate(x, y);
    // rabo de cavalo balançando
    const sw = Math.sin(t * 7) * .25;
    c.save(); c.translate(-.2, -.12); c.rotate(-.6 + sw);
    c.beginPath(); c.ellipse(-.12, 0, .16, .07, 0, 0, TAU); c.fillStyle = HAIR; c.fill(); ink(c, .035); c.restore();
    // cabeça
    c.beginPath(); c.ellipse(0, 0, .25, .24, 0, 0, TAU); cel(c, SKIN, -.25, -.24, .5, .48, { light: .15, dark: .08 });
    // orelha
    c.beginPath(); c.ellipse(-.1, .03, .05, .06, 0, 0, TAU); c.fillStyle = shade(SKIN, -.1); c.fill(); ink(c, .025);
    // cabelo (topo e franja)
    c.beginPath(); c.moveTo(-.25, -.02); c.quadraticCurveTo(-.24, -.3, .04, -.27); c.quadraticCurveTo(.26, -.24, .25, -.06);
    c.quadraticCurveTo(.15, -.16, .05, -.1); c.quadraticCurveTo(-.02, -.18, -.12, -.08); c.quadraticCurveTo(-.18, -.1, -.25, -.02); c.closePath();
    c.fillStyle = HAIR; c.fill(); ink(c, .035);
    c.beginPath(); c.moveTo(-.15, -.2); c.quadraticCurveTo(-.02, -.27, .12, -.22); c.lineWidth = .02; c.strokeStyle = shade(HAIR, .3); c.stroke();
    // óculos de solda na testa
    rr(c, -.2, -.19, .42, .05, .02); c.fillStyle = "#3a3f5c"; c.fill();
    for (const gx of [.04, .16]) { c.beginPath(); c.arc(gx, -.165, .055, 0, TAU); c.fillStyle = "#c9a227"; c.fill(); ink(c, .025);
      c.beginPath(); c.arc(gx, -.165, .035, 0, TAU); c.fillStyle = "#7df9ff"; c.fill(); c.beginPath(); c.arc(gx - .012, -.178, .01, 0, TAU); c.fillStyle = "#fff"; c.fill(); }
    // olho
    const blink = face !== "ko" && face !== "shock" && (t % 3.2) < .1;
    if (face === "ko") { c.lineWidth = .025; c.strokeStyle = OUT; c.beginPath(); c.moveTo(.06, -.06); c.lineTo(.14, .02); c.moveTo(.14, -.06); c.lineTo(.06, .02); c.stroke(); }
    else if (blink) { c.beginPath(); c.moveTo(.06, -.02); c.lineTo(.16, -.02); c.lineWidth = .025; c.strokeStyle = OUT; c.stroke(); }
    else {
      const r = face === "shock" ? .065 : .055;
      c.beginPath(); c.ellipse(.11, -.02, r * .85, r, 0, 0, TAU); c.fillStyle = "#fff"; c.fill(); ink(c, .025);
      c.beginPath(); c.arc(.13, -.015, r * .45, 0, TAU); c.fillStyle = "#2b1a14"; c.fill();
      c.beginPath(); c.arc(.14, -.03, r * .15, 0, TAU); c.fillStyle = "#fff"; c.fill();
    }
    // sobrancelha
    const brow = { determined: [.05, -.1, .18, -.07], grin: [.05, -.11, .18, -.1], smile: [.05, -.1, .18, -.1], worried: [.05, -.08, .18, -.12], shock: [.05, -.13, .18, -.13], ko: [.05, -.09, .18, -.09] }[face];
    c.beginPath(); c.moveTo(brow[0], brow[1]); c.lineTo(brow[2], brow[3]); c.lineWidth = .03; c.strokeStyle = HAIR; c.stroke();
    // bochecha
    c.beginPath(); c.ellipse(.15, .07, .04, .025, 0, 0, TAU); c.fillStyle = alpha("#ff7a7a", .45); c.fill();
    // boca
    c.lineWidth = .025; c.strokeStyle = OUT;
    if (face === "grin") { c.beginPath(); c.moveTo(.06, .09); c.quadraticCurveTo(.15, .16, .22, .07); c.closePath(); c.fillStyle = "#fff"; c.fill(); c.stroke(); }
    else if (face === "shock") { c.beginPath(); c.ellipse(.15, .1, .035, .045, 0, 0, TAU); c.fillStyle = "#5a1a1a"; c.fill(); c.stroke(); }
    else if (face === "worried") { c.beginPath(); c.moveTo(.08, .11); c.quadraticCurveTo(.14, .07, .2, .11); c.stroke(); }
    else if (face === "ko") { c.beginPath(); c.moveTo(.08, .1); c.lineTo(.2, .1); c.stroke(); }
    else { c.beginPath(); c.moveTo(.08, .08); c.quadraticCurveTo(.14, .13, .21, .07); c.stroke(); }
    // nariz
    c.beginPath(); c.moveTo(.23, -.01); c.quadraticCurveTo(.28, .03, .23, .04); c.lineWidth = .02; c.stroke();
    // acessório
    if (hat === "capacete") { c.beginPath(); c.ellipse(0, -.2, .27, .13, 0, Math.PI, TAU); c.closePath(); cel(c, "#ffd23f", -.27, -.33, .54, .13); rr(c, -.3, -.22, .6, .05, .02); c.fillStyle = "#e0b52f"; c.fill(); ink(c, .03); }
    if (hat === "bone") { c.beginPath(); c.ellipse(-.02, -.21, .25, .11, 0, Math.PI, TAU); c.closePath(); cel(c, "#e71d36", -.27, -.32, .5, .11); rr(c, -.42, -.23, .2, .05, .02); c.fillStyle = "#b3172b"; c.fill(); ink(c, .03); }
    c.restore();
  }

  // =================================================================
  // CENÁRIO — fábrica Engrenópolis, 4 setores, camadas parallax em cache
  // =================================================================
  const SECTORS = [
    { name: "MONTAGEM",  wall: "#3b4a8c", wall2: "#2e3a73", accent: "#2ec4b6", glow: "#7df9ff", liquid: "#7df9ff" },
    { name: "CALDEIRAS", wall: "#6b3a3a", wall2: "#552c30", accent: "#ff9f1c", glow: "#ffb347", liquid: "#ff7a1a" },
    { name: "TESTES",    wall: "#2f5c5a", wall2: "#244846", accent: "#6bcb77", glow: "#a6ff8f", liquid: "#6bff8f" },
    { name: "DEPÓSITO",  wall: "#4e3f6b", wall2: "#3d3156", accent: "#ffd23f", glow: "#ffe58a", liquid: "#ffd23f" },
  ];
  const SECTOR_LEN = 400;
  function sectorAt(x) { return ((Math.floor(x / SECTOR_LEN) % 4) + 4) % 4; }

  const cache = new Map();
  let cachePPU = 0;
  function tileCanvas(key, w, ppu, draw) {
    if (ppu !== cachePPU) { cache.clear(); cachePPU = ppu; }
    let e = cache.get(key);
    if (e) return e;
    const cv = document.createElement("canvas");
    cv.width = Math.max(1, Math.ceil(w * ppu)); cv.height = Math.ceil(9 * ppu);
    const c = cv.getContext("2d");
    c.setTransform(ppu, 0, 0, -ppu, 0, 8 * ppu); // y-up; y=8 no topo, y=-1 embaixo
    const anims = [];
    draw(c, anims);
    e = { cv, anims, w };
    cache.set(key, e);
    return e;
  }

  // ---- camada distante: parede de chapas, engrenagens gigantes, janelas altas ----
  function farTile(c, i, S) {
    const W = 10;
    const g = c.createLinearGradient(0, 8, 0, -1); g.addColorStop(0, shade(S.wall2, -.25)); g.addColorStop(1, shade(S.wall2, -.05));
    c.fillStyle = g; c.fillRect(0, -1, W, 9);
    // chapas com rebites
    c.lineWidth = .04; c.strokeStyle = shade(S.wall2, -.35);
    for (let x = 0; x < W; x += 2.5) for (let y = 0; y < 7; y += 1.75) {
      c.strokeRect(x + .05, y + .05, 2.4, 1.65);
      c.fillStyle = shade(S.wall2, .12);
      for (const [rx, ry] of [[.2, .2], [2.25, .2], [.2, 1.5], [2.25, 1.5]]) { c.beginPath(); c.arc(x + rx, y + ry, .045, 0, TAU); c.fill(); }
    }
    const h = hash(i, 11);
    if (h < .45) { // engrenagem gigante
      const gx = 3 + hash(i, 2) * 4, gy = 3 + hash(i, 3) * 3, r = 1.6 + hash(i, 4);
      c.save(); c.translate(gx, gy);
      star(c, 0, 0, r, r * .82, 14); c.fillStyle = shade(S.wall2, -.18); c.fill();
      c.beginPath(); c.arc(0, 0, r * .55, 0, TAU); c.fillStyle = shade(S.wall2, -.08); c.fill();
      c.beginPath(); c.arc(0, 0, r * .2, 0, TAU); c.fillStyle = shade(S.wall2, -.25); c.fill();
      c.restore();
    } else if (h < .8) { // janela alta com céu noturno e chaminés
      const wx = 2 + hash(i, 5) * 4;
      c.fillStyle = "#141a3a"; c.fillRect(wx, 4, 3, 2.2);
      c.fillStyle = "#232b5c"; for (let k = 0; k < 4; k++) c.fillRect(wx + .2 + k * .7, 4, .45, .6 + hash(i, k) * .8);
      c.fillStyle = "#ffe58a"; for (let k = 0; k < 6; k++) c.fillRect(wx + hash(i, k + 9) * 2.8, 4.1 + hash(i, k + 20) * .5, .06, .06);
      c.lineWidth = .1; c.strokeStyle = shade(S.wall2, -.4); c.strokeRect(wx, 4, 3, 2.2);
      c.beginPath(); c.moveTo(wx + 1.5, 4); c.lineTo(wx + 1.5, 6.2); c.moveTo(wx, 5.1); c.lineTo(wx + 3, 5.1); c.stroke();
    } else { // letreiro do setor
      rr(c, 2, 4.4, 5, 1.1, .15); c.fillStyle = shade(S.wall2, -.3); c.fill();
      textUp(c, "ENGRENÓPOLIS · " + S.name, 4.5, 4.95, .5, alpha(S.accent, .7));
    }
  }

  // ---- camada do meio: máquinas e tubulações (o que dá vida à fábrica) ----
  const MID_VARIANTS = 6;
  function midTile(c, i, S, anims) {
    const W = 7, v = Math.floor(hash(i, 7) * MID_VARIANTS);
    // tubulação horizontal contínua no alto (emenda perfeita entre tiles)
    for (const [py, pr, col] of [[6.1, .16, "#8e9aaf"], [5.7, .1, S.accent]]) {
      c.fillStyle = col; c.fillRect(0, py - pr, W, pr * 2);
      c.fillStyle = alpha("#ffffff", .25); c.fillRect(0, py + pr * .3, W, pr * .35);
      c.fillStyle = shade(col, -.3); c.fillRect(0, py - pr, W, pr * .4);
      c.fillStyle = shade(col, -.15); for (const fx of [1.5, 5]) c.fillRect(fx, py - pr * 1.4, .18, pr * 2.8);
    }
    // cabos pendurados
    c.lineWidth = .05; c.strokeStyle = "#1a1630";
    c.beginPath(); c.moveTo(0, 5.4); c.quadraticCurveTo(W / 2, 4.6 - hash(i, 3) * .5, W, 5.4); c.stroke();
    c.strokeStyle = "#5b2e6b"; c.beginPath(); c.moveTo(0, 5.2); c.quadraticCurveTo(W / 2, 4.9, W, 5.2); c.stroke();
    c.save(); c.translate(1 + hash(i, 1) * 3, 0);
    switch (v) {
      case 0: { // tanque de experimentos com líquido brilhante
        rr(c, 0, .9, 1.6, 3, .5); cel(c, "#8e9aaf", 0, .9, 1.6, 3);
        rr(c, .25, 1.4, 1.1, 2, .35); c.fillStyle = alpha(S.liquid, .55); c.fill(); ink(c, .04);
        c.fillStyle = alpha("#ffffff", .35); c.fillRect(.35, 1.6, .12, 1.6);
        rr(c, -.1, .8, 1.8, .25, .05); cel(c, "#5b6585", -.1, .8, 1.8, .25);
        anims.push({ k: "bubbles", x: 0.8 + c.getTransform().e / cachePPU, y: 1.5, w: .9, h: 1.8, col: S.liquid });
        break;
      }
      case 1: { // painel de controle com tela e luzes
        rr(c, 0, .9, 2.4, 2, .1); cel(c, "#5b6585", 0, .9, 2.4, 2);
        rr(c, .2, 1.9, 1.3, .8, .08); c.fillStyle = "#10162f"; c.fill(); ink(c, .03);
        c.strokeStyle = S.glow; c.lineWidth = .04; c.beginPath();
        for (let k = 0; k <= 12; k++) c.lineTo(.25 + k * .1, 2.3 + Math.sin(k * 1.3 + i) * .2); c.stroke();
        for (let k = 0; k < 4; k++) anims.push({ k: "led", x: 1.75 + (k % 2) * .3 + c.getTransform().e / cachePPU, y: 2.4 - Math.floor(k / 2) * .35, col: ["#e71d36", "#6bcb77", "#ffd23f", "#7df9ff"][k], ph: hash(i, k) * 6 });
        rr(c, .2, 1.1, 2, .5, .06); c.fillStyle = shade("#5b6585", -.2); c.fill();
        for (let k = 0; k < 5; k++) { c.beginPath(); c.arc(.4 + k * .4, 1.35, .1, 0, TAU); c.fillStyle = ["#e71d36", "#ffd23f", "#2ec4b6", "#ff9f1c", "#f6f7fb"][k]; c.fill(); ink(c, .025); }
        break;
      }
      case 2: { // esteira de brinquedos (assinatura da Engrenópolis)
        rr(c, -.5, 1.6, 4, .35, .17); cel(c, "#3a3f5c", -.5, 1.6, 4, .35);
        for (let k = 0; k < 8; k++) { c.beginPath(); c.arc(-.3 + k * .5, 1.78, .1, 0, TAU); c.fillStyle = "#8e9aaf"; c.fill(); }
        const toys = [["#e71d36", .5], ["#ffd23f", .4], ["#2ec4b6", .55]];
        toys.forEach(([col, s], k) => { rr(c, k * 1.2, 1.95, s, s, .06); cel(c, col, k * 1.2, 1.95, s, s, { lw: .03 });
          c.fillStyle = alpha("#ffffff", .6); c.fillRect(k * 1.2 + s * .45, 1.95, s * .1, s); });
        c.fillStyle = "#5b6585"; c.fillRect(.2, .9, .2, .7); c.fillRect(2.9, .9, .2, .7);
        break;
      }
      case 3: { // braço robótico parado
        rr(c, 0, .9, 1, .4, .1); cel(c, "#ff9f1c", 0, .9, 1, .4);
        c.save(); c.translate(.5, 1.3); c.rotate(-.5);
        rr(c, -.12, 0, .24, 1.6, .1); cel(c, "#ff9f1c", -.12, 0, .24, 1.6);
        c.translate(0, 1.6); c.rotate(1.3); rr(c, -.1, 0, .2, 1.2, .1); cel(c, "#ff9f1c", -.1, 0, .2, 1.2);
        c.translate(0, 1.2); c.beginPath(); c.arc(0, 0, .16, 0, TAU); c.fillStyle = "#3a3f5c"; c.fill(); ink(c, .03);
        c.restore();
        anims.push({ k: "beacon", x: .5 + c.getTransform().e / cachePPU, y: 1.45, col: "#ffd23f" });
        break;
      }
      case 4: { // prateleira com caixas
        for (const sy of [1.1, 2.4]) { c.fillStyle = "#5b6585"; c.fillRect(0, sy, 2.6, .1); }
        c.fillStyle = "#3a3f5c"; c.fillRect(0, .9, .1, 2.8); c.fillRect(2.5, .9, .1, 2.8);
        [[.2, 1.2, .7, .6], [1, 1.2, .5, .5], [1.6, 1.2, .7, .8], [.3, 2.5, .9, .6], [1.4, 2.5, .6, .4]].forEach(([bx, by, bw, bh], k) => {
          rr(c, bx, by, bw, bh, .04); cel(c, "#b07a45", bx, by, bw, bh, { lw: .03 });
          c.fillStyle = alpha("#3a2410", .5); c.fillRect(bx, by + bh * .45, bw, .05);
          textUp(c, ["⚙", "!", "★", "⚙", "?"][k], bx + bw / 2, by + bh * .7, .2, alpha("#3a2410", .7), "Nunito");
        });
        break;
      }
      default: { // porta industrial com faixas
        rr(c, 0, .9, 2.2, 3.4, .1); cel(c, "#5b6585", 0, .9, 2.2, 3.4);
        for (let k = 0; k < 6; k++) { c.fillStyle = k % 2 ? "#ffd23f" : "#1a1630"; c.save(); c.beginPath(); c.rect(.1, .95, 2, .4); c.clip();
          c.beginPath(); c.moveTo(.1 + k * .45, .95); c.lineTo(.4 + k * .45, .95); c.lineTo(.1 + k * .45 + .1, 1.35); c.lineTo(-.2 + k * .45 + .1, 1.35); c.fill(); c.restore(); }
        c.lineWidth = .04; c.strokeStyle = shade("#5b6585", -.3); for (let k = 1; k < 6; k++) { c.beginPath(); c.moveTo(.1, 1.35 + k * .48); c.lineTo(2.1, 1.35 + k * .48); c.stroke(); }
        anims.push({ k: "beacon", x: 1.1 + c.getTransform().e / cachePPU, y: 4.45, col: "#ff4d2e" });
      }
    }
    c.restore();
  }

  function liveAnims(c, anims, ox, t) {
    for (const a of anims) {
      const x = ox + a.x;
      if (a.k === "led") { const on = Math.sin(t * 3 + a.ph) > 0; c.beginPath(); c.arc(x, a.y, .07, 0, TAU); c.fillStyle = on ? a.col : shade(a.col, -.6); c.fill(); if (on) { c.beginPath(); c.arc(x, a.y, .16, 0, TAU); c.fillStyle = alpha(a.col, .25); c.fill(); } }
      else if (a.k === "beacon") {
        c.beginPath(); c.arc(x, a.y, .13, Math.PI, 0); c.fillStyle = a.col; c.fill(); ink(c, .03);
        const ang = t * 5; c.save(); c.globalCompositeOperation = "lighter";
        c.beginPath(); c.moveTo(x, a.y + .05); c.arc(x, a.y + .05, 1.4, ang - .25, ang + .25); c.closePath(); c.fillStyle = alpha(a.col, .13 + .1 * Math.max(0, Math.cos(ang))); c.fill(); c.restore();
      } else if (a.k === "bubbles") {
        c.fillStyle = alpha("#ffffff", .7);
        for (let k = 0; k < 5; k++) { const ph = (t * .5 + k * .21) % 1; c.beginPath(); c.arc(x - a.w / 2 + .15 + hash(k, 3) * (a.w - .3), a.y + ph * a.h, .04 + .03 * hash(k, 5), 0, TAU); c.fill(); }
      }
    }
  }

  /**
   * Fundo completo em espaço de tela. view = { W, H, SC, camX, sy(y), t, DPR }
   */
  function background(ctx, view) {
    const { W, SC, camX, t, DPR } = view;
    const ppu = Math.max(8, Math.round(SC * DPR));
    const top = view.sy(8), unitH = 9 * SC;
    const layers = [[.22, 10, "far"], [.55, 7, "mid"]];
    // vinheta de fundo fora do corredor (modo retrato)
    ctx.fillStyle = "#0f1228"; ctx.fillRect(0, 0, W, view.H);
    for (const [k, TW, name] of layers) {
      const L = camX * k;
      const i0 = Math.floor(L / TW) - 1, i1 = Math.ceil((L + W / SC) / TW) + 1;
      for (let i = i0; i <= i1; i++) {
        const S = SECTORS[sectorAt(i * TW / k)];
        const e = tileCanvas(`${name}:${i % 64}:${S.name}`, TW, ppu, (c, anims) => (name === "far" ? farTile(c, i, S) : midTile(c, i, S, anims)));
        const x = (i * TW - L) * SC;
        ctx.drawImage(e.cv, x, top, TW * SC, unitH);
        if (e.anims.length) {
          ctx.save(); ctx.setTransform(SC * DPR, 0, 0, -SC * DPR, x * DPR, (top + 8 * SC) * DPR);
          liveAnims(ctx, e.anims, 0, t); ctx.restore();
        }
      }
    }
    // luz ambiente (lâmpadas do teto) — camada da frente, presa ao mundo
    ctx.save(); ctx.globalCompositeOperation = "lighter";
    for (let m = Math.floor(camX / 9) * 9; m < camX + W / SC + 9; m += 9) {
      const x = (m - camX) * SC, y0 = view.sy(7), y1 = view.sy(0);
      const g = ctx.createLinearGradient(0, y0, 0, y1); g.addColorStop(0, "rgba(255,240,200,.16)"); g.addColorStop(1, "rgba(255,240,200,0)");
      ctx.fillStyle = g; ctx.beginPath(); ctx.moveTo(x - .25 * SC, y0); ctx.lineTo(x + .25 * SC, y0); ctx.lineTo(x + 1.8 * SC, y1); ctx.lineTo(x - 1.8 * SC, y1); ctx.fill();
    }
    ctx.restore();
  }

  /** Teto e chão (presos ao mundo, desenhados com transformação de mundo). */
  function floorCeiling(c, camX, viewW, t, yMin = -1.5, yMax = 8.6) {
    const x0 = Math.floor(camX) - 2, x1 = camX + viewW + 2, S = SECTORS[sectorAt(camX + viewW * .5)];
    // modo retrato: subsolo e casa de máquinas acima do teto preenchem a tela
    if (yMin < -1.5) {
      c.fillStyle = "#2a2f4f"; c.fillRect(x0, yMin, x1 - x0, -1.4 - yMin);
      c.strokeStyle = "#1a1630"; c.lineWidth = .05;
      for (let y = -2; y > yMin; y -= 1.2) { c.beginPath(); c.moveTo(x0, y); c.lineTo(x1, y); c.stroke();
        for (let x = Math.floor(x0 / 3) * 3 + (y * 7 % 3); x < x1; x += 3) { c.beginPath(); c.arc(x, y - .6, .35, 0, TAU); c.fillStyle = "#3a3f5c"; c.fill(); c.stroke(); } }
    }
    if (yMax > 8.6) {
      c.fillStyle = "#20253f"; c.fillRect(x0, 8.5, x1 - x0, yMax - 8.5);
      for (let y = 9.2; y < yMax; y += 1.4) { c.fillStyle = "#8e9aaf"; c.fillRect(x0, y, x1 - x0, .22); c.fillStyle = "#5b6585"; c.fillRect(x0, y, x1 - x0, .07); }
      for (let x = Math.floor(x0 / 4) * 4; x < x1; x += 4) { c.fillStyle = "#2f3555"; c.fillRect(x, 8.5, .3, yMax - 8.5); }
    }
    // chão: placas com borda de luz e grade
    let g = c.createLinearGradient(0, 0, 0, -1); g.addColorStop(0, "#c9cfe0"); g.addColorStop(.12, "#9aa3bd"); g.addColorStop(1, "#5b6585");
    c.fillStyle = g; c.fillRect(x0, -1.5, x1 - x0, 1.5);
    c.fillStyle = "#e9edf7"; c.fillRect(x0, -.06, x1 - x0, .06);
    c.lineWidth = .04; c.strokeStyle = "#3a3f5c";
    for (let x = Math.floor(x0 / 2) * 2; x < x1; x += 2) { c.beginPath(); c.moveTo(x, 0); c.lineTo(x - .3, -1); c.stroke(); }
    c.beginPath(); c.moveTo(x0, -.45); c.lineTo(x1, -.45); c.stroke();
    // faixa de segurança no rodapé da parede, cor do setor
    c.fillStyle = S.accent; c.fillRect(x0, 0, x1 - x0, .12);
    c.fillStyle = alpha("#1a1630", .55);
    for (let x = Math.floor(x0); x < x1; x += 1) { c.beginPath(); c.moveTo(x, 0); c.lineTo(x + .35, 0); c.lineTo(x + .47, .12); c.lineTo(x + .12, .12); c.fill(); }
    // teto: vigas treliçadas e grelhas
    g = c.createLinearGradient(0, 7, 0, 8.5); g.addColorStop(0, "#5b6585"); g.addColorStop(1, "#2a2f4f");
    c.fillStyle = g; c.fillRect(x0, 7, x1 - x0, 1.6);
    c.fillStyle = "#3a3f5c"; c.fillRect(x0, 7, x1 - x0, .14);
    c.strokeStyle = "#2a2f4f"; c.lineWidth = .06;
    for (let x = Math.floor(x0 / 1.5) * 1.5; x < x1; x += 1.5) { c.beginPath(); c.moveTo(x, 7.14); c.lineTo(x + .75, 7.9); c.lineTo(x + 1.5, 7.14); c.stroke(); }
    // lâmpadas
    for (let m = Math.floor(x0 / 9) * 9; m < x1; m += 9) {
      rr(c, m - .35, 6.82, .7, .2, .08); c.fillStyle = "#3a3f5c"; c.fill(); ink(c, .03);
      rr(c, m - .25, 6.82, .5, .08, .04); c.fillStyle = "#fff6c2"; c.fill();
    }
    // portas de setor a cada 400 m
    for (let m = Math.floor(x0 / SECTOR_LEN) * SECTOR_LEN; m < x1; m += SECTOR_LEN) {
      if (m <= 0) continue;
      const N = SECTORS[sectorAt(m + 1)];
      c.fillStyle = "#3a3f5c"; c.fillRect(m - .45, 0, .9, 7);
      c.fillStyle = N.accent; c.fillRect(m - .45, 6.3, .9, .4);
      textUp(c, N.name, m, 6.5, .26, "#1a1630");
      c.fillStyle = "#1a1630"; for (let y = 0; y < 6.2; y += .7) c.fillRect(m - .45, y, .9, .08);
    }
  }

  // =================================================================
  // PERIGOS
  // =================================================================
  /** Bobina de Tesla de cobre com isoladores cerâmicos. */
  function coil(c, x, y, ang, t) {
    local(c, x, y, () => {
      c.rotate(-ang);
      c.beginPath(); c.arc(0, 0, .34, 0, TAU); cel(c, "#8e9aaf", -.34, -.34, .68, .68);
      for (let k = -1; k <= 1; k++) { rr(c, -.3, k * .14 - .04, .6, .08, .04); c.fillStyle = "#c97a3a"; c.fill(); ink(c, .02); }
      c.beginPath(); c.arc(0, 0, .13, 0, TAU); c.fillStyle = "#e9fdff"; c.fill(); ink(c, .03);
    });
  }
  function zapper(c, e, t, rotating, seed) {
    const [ax, ay, bx, by] = e, len = Math.hypot(bx - ax, by - ay) || 1, nx = -(by - ay) / len, ny = (bx - ax) / len;
    const col = rotating ? "#ffb347" : "#7df9ff";
    // halo
    c.save(); c.globalCompositeOperation = "lighter";
    capsule(c, ax, ay, bx, by, .42); c.fillStyle = alpha(col, .12); c.fill();
    capsule(c, ax, ay, bx, by, .24); c.fillStyle = alpha(col, .28); c.fill();
    // arcos elétricos (3 linhas serrilhadas que mudam a cada quadro)
    for (let a = 0; a < 3; a++) {
      c.beginPath(); c.moveTo(ax, ay);
      const n = Math.max(5, Math.round(len * 3));
      for (let k = 1; k < n; k++) {
        const f = k / n, j = (hash(Math.floor(t * 30) + a * 97 + seed, k) - .5) * .5;
        c.lineTo(ax + (bx - ax) * f + nx * j, ay + (by - ay) * f + ny * j);
      }
      c.lineTo(bx, by);
      c.lineWidth = a === 0 ? .09 : .035; c.strokeStyle = a === 0 ? "#ffffff" : col; c.stroke();
    }
    c.restore();
    burst(c, ax, ay, .5, col, t + seed, 10);
    burst(c, bx, by, .5, col, t + seed + 1, 10);
    const ang = Math.atan2(by - ay, bx - ax);
    coil(c, ax, ay, ang, t); coil(c, bx, by, ang, t);
  }

  /** Cabeça de corte industrial (emissor do feixe), presa à tela. side = -1 esquerda, 1 direita. */
  function laserEmitter(c, x, y, side, glow) {
    local(c, x, y, () => {
      c.scale(side, 1);
      rr(c, -.55, -.32, .7, .64, .12); cel(c, "#ff9f1c", -.55, -.32, .7, .64);
      rr(c, -.7, -.18, .2, .36, .06); cel(c, "#5b6585", -.7, -.18, .2, .36);
      c.beginPath(); c.arc(.15, 0, .2, -Math.PI / 2, Math.PI / 2); c.closePath(); c.fillStyle = "#3a3f5c"; c.fill(); ink(c, .04);
      c.beginPath(); c.arc(.17, 0, .12, 0, TAU); c.fillStyle = glow > 0 ? `rgba(255,${120 - glow * 80},${140 - glow * 100},1)` : "#5a2030"; c.fill();
      c.fillStyle = "#1a1630"; c.fillRect(-.45, -.22, .05, .44); c.fillRect(-.3, -.22, .05, .44);
    });
  }
  /** Feixe: phase warn (linha piscando) | fire (feixe largo, núcleo branco) com abertura/fechamento. */
  function laserBeam(c, x0, x1, y, phase, k, t) {
    if (phase === "warn") {
      if (Math.floor(t * 12) % 2) return;
      c.fillStyle = alpha("#ff4d63", .8); c.fillRect(x0, y - .03, x1 - x0, .06);
      return;
    }
    const w = .3 * k;
    c.save(); c.globalCompositeOperation = "lighter";
    c.fillStyle = alpha("#ff2e4d", .25); c.fillRect(x0, y - w * 1.9, x1 - x0, w * 3.8);
    c.fillStyle = alpha("#ff4d63", .85); c.fillRect(x0, y - w, x1 - x0, w * 2);
    c.fillStyle = "#ffd6dc"; c.fillRect(x0, y - w * .45, x1 - x0, w * .9);
    c.fillStyle = "#ffffff"; c.fillRect(x0, y - w * .18, x1 - x0, w * .36);
    c.restore();
    burst(c, x0, y, .55 * k + .1, "#ff4d63", t, 9);
    burst(c, x1, y, .55 * k + .1, "#ff4d63", t + 1, 9);
  }

  /** Foguete de brinquedo desgovernado (drone-foguete). Aponta para a esquerda. */
  function rocket(c, x, y, t) {
    local(c, x, y, () => {
      // chama
      const f = .35 + .15 * Math.sin(t * 50);
      for (const [col, s] of [["#ff7a1a", 1], ["#ffcc4d", .7], ["#fff6c2", .4]]) {
        c.beginPath(); c.moveTo(.42, -.12 * s); c.quadraticCurveTo(.42 + f * 2 * s, 0, .42, .12 * s); c.closePath(); c.fillStyle = col; c.fill();
      }
      // corpo listrado
      rr(c, -.4, -.17, .82, .34, .17); cel(c, "#f6f7fb", -.4, -.17, .82, .34);
      c.save(); rr(c, -.4, -.17, .82, .34, .17); c.clip();
      for (let k = 0; k < 3; k++) { c.fillStyle = k % 2 ? "#2ec4b6" : "#e71d36"; c.fillRect(-.1 + k * .14, -.2, .08, .4); }
      c.restore(); rr(c, -.4, -.17, .82, .34, .17); ink(c, .035);
      // bico
      c.beginPath(); c.moveTo(-.38, -.15); c.quadraticCurveTo(-.66, 0, -.38, .15); c.closePath(); cel(c, "#e71d36", -.66, -.15, .3, .3, { lw: .035 });
      // aletas
      for (const s of [-1, 1]) { c.beginPath(); c.moveTo(.18, .12 * s); c.lineTo(.46, .34 * s); c.lineTo(.42, .1 * s); c.closePath(); c.fillStyle = "#e71d36"; c.fill(); ink(c, .03); }
      // luz de alerta piscando
      c.beginPath(); c.arc(-.05, -.17, .05, 0, TAU); c.fillStyle = Math.floor(t * 10) % 2 ? "#ffd23f" : "#ff4d2e"; c.fill();
    });
  }
  /** Selo de engrenagem com "!" na borda direita (aviso de drone). */
  function warningBadge(c, x, y, t, locked) {
    const pulse = 1 + .12 * Math.sin(t * (locked ? 40 : 14));
    local(c, x, y, () => {
      c.scale(pulse, pulse);
      star(c, 0, 0, .42, .34, 10, t * (locked ? 6 : 2)); c.fillStyle = locked ? "#ff4d2e" : "#ffd23f"; c.fill(); ink(c, .04);
      c.beginPath(); c.arc(0, 0, .26, 0, TAU); c.fillStyle = "#1a1630"; c.fill();
      c.fillStyle = locked ? "#ffd23f" : "#ff4d2e"; c.font = '.42px "Lilita One", sans-serif'; c.textAlign = "center"; c.textBaseline = "middle"; c.fillText("!", 0, .02);
    });
  }

  // =================================================================
  // COLETÁVEIS E ROBÔS
  // =================================================================
  function nut(c, x, y, t, s = 1) {
    local(c, x, y, () => {
      const sq = Math.abs(Math.cos(t * 3 + x * .7)) * .65 + .35; // giro
      c.scale(sq * s, s);
      c.beginPath(); for (let i = 0; i < 6; i++) { const a = Math.PI / 3 * i + Math.PI / 6; c.lineTo(Math.cos(a) * .2, Math.sin(a) * .2); } c.closePath();
      cel(c, "#ffd23f", -.2, -.2, .4, .4, { lw: .035 });
      c.beginPath(); c.arc(0, 0, .085, 0, TAU); c.fillStyle = "#b8862a"; c.fill(); ink(c, .025);
      c.beginPath(); c.arc(-.07, -.08, .03, 0, TAU); c.fillStyle = "#fff"; c.fill();
    });
  }
  function token(c, x, y, t) {
    local(c, x, y, () => {
      c.beginPath(); c.arc(0, 0, .3, 0, TAU); cel(c, "#9b5cff", -.3, -.3, .6, .6);
      c.beginPath(); c.arc(0, 0, .2, 0, TAU); c.lineWidth = .04; c.strokeStyle = "#e4d4ff"; c.stroke();
      star(c, 0, 0, .14, .06, 5, -Math.PI / 2 + Math.sin(t * 2) * .2); c.fillStyle = "#ffd23f"; c.fill(); ink(c, .02);
    });
  }
  function crate(c, x, y, t) {
    local(c, x, y, () => {
      c.rotate(Math.sin(t * 2) * .15);
      burst(c, 0, 0, .55, "#ff9f1c", t, 8);
      rr(c, -.32, -.32, .64, .64, .08); cel(c, "#ff9f1c", -.32, -.32, .64, .64);
      c.strokeStyle = shade("#ff9f1c", -.35); c.lineWidth = .05; c.beginPath(); c.moveTo(-.3, -.3); c.lineTo(.3, .3); c.moveTo(.3, -.3); c.lineTo(-.3, .3); c.stroke();
      rr(c, -.16, -.2, .32, .4, .06); c.fillStyle = "#1a1630"; c.fill();
      c.fillStyle = "#ffd23f"; c.font = '.36px "Lilita One", sans-serif'; c.textAlign = "center"; c.textBaseline = "middle"; c.fillText("?", 0, .02);
    });
  }
  /** Parafusinho: robô operário. */
  function worker(c, x, y, t, dir, down, spin, id) {
    local(c, x, y + .5, () => {
      c.rotate(-spin * Math.PI / 180); c.scale(dir, 1);
      const step = down ? 0 : Math.sin(t * 10 + id);
      // pernas
      for (const s of [-1, 1]) { rr(c, s * .12 - .05, .18, .1, .3 + step * s * .05, .04); c.fillStyle = "#3a3f5c"; c.fill(); ink(c, .025); }
      // corpo
      rr(c, -.25, -.2, .5, .45, .1); cel(c, "#a8aec9", -.25, -.2, .5, .45, { lw: .035 });
      rr(c, -.12, -.05, .24, .16, .04); c.fillStyle = "#ff9f1c"; c.fill(); ink(c, .02);
      // braços
      const wave = down ? 1.6 : Math.sin(t * 10 + id) * .5;
      c.save(); c.translate(-.25, -.08); c.rotate(.4 + wave); rr(c, -.04, 0, .08, .25, .04); c.fillStyle = "#8e9aaf"; c.fill(); ink(c, .02); c.restore();
      // cabeça
      c.beginPath(); c.arc(0, -.36, .19, 0, TAU); cel(c, "#cfd5e6", -.19, -.55, .38, .38, { lw: .035 });
      rr(c, -.02, -.42, .2, .1, .05); c.fillStyle = "#1a1630"; c.fill();
      c.beginPath(); c.arc(.1, -.37, .035, 0, TAU); c.fillStyle = down ? "#ff4d2e" : "#7df9ff"; c.fill();
      // antena
      c.beginPath(); c.moveTo(-.05, -.54); c.lineTo(-.08, -.68); c.lineWidth = .025; c.strokeStyle = OUT; c.stroke();
      c.beginPath(); c.arc(-.08, -.7, .04, 0, TAU); c.fillStyle = Math.floor(t * 4 + id) % 2 ? "#ffd23f" : "#ff9f1c"; c.fill();
      if (down) { c.strokeStyle = "#ffd23f"; c.lineWidth = .02; for (let k = 0; k < 3; k++) { const a = t * 8 + k * 2; c.beginPath(); c.arc(Math.cos(a) * .25, -.6 + Math.sin(a) * .06, .03, 0, TAU); c.stroke(); } }
    });
  }

  // =================================================================
  // VEÍCULOS
  // =================================================================
  /** v = { kind, w, h, y, vy, t, hp, sign } ; o = { outfit, appear (0..1), t } */
  function vehicle(c, x, v, o) {
    const t = o.t, pop = o.appear < 1 ? Math.sin(Math.min(1, o.appear) * Math.PI * .5) * (1 + .15 * Math.sin(o.appear * Math.PI)) : 1;
    local(c, x, v.y + v.h / 2, () => {
      c.scale(pop, pop);
      if (v.kind === "flip" && v.sign > 0) c.scale(1, -1);
      const tilt = ["drone", "mech", "bike", "rig"].includes(v.kind) ? (v.vy || 0) * -.025 : 0;
      c.rotate(tilt);
      const H = v.h, W = v.w;
      const pilot = (px, py, s = .8) => { c.save(); c.translate(px, py); c.scale(s, s); head(c, 0, 0, t, "grin", o.hat); c.restore(); };
      switch (v.kind) {
        case "mech": {
          const st = Math.sin(t * 8) * .12, crouch = v.y <= .01 ? 0 : -.1;
          for (const s of [-1, 1]) { // pernas de pistão
            c.save(); c.translate(s * .3, .2); rr(c, -.1, 0, .2, .55 + st * s + crouch, .06); cel(c, "#5b6585", -.1, 0, .2, .55);
            rr(c, -.18, .5 + st * s + crouch, .36, .14, .05); cel(c, "#3a3f5c", -.18, .5, .36, .14); c.restore(); }
          rr(c, -.6, -.75, 1.2, 1, .2); cel(c, "#8e9aaf", -.6, -.75, 1.2, 1);
          c.beginPath(); c.arc(.12, -.35, .32, 0, TAU); c.fillStyle = alpha("#bff6ff", .7); c.fill(); ink(c, .04);
          pilot(.12, -.32, .9);
          rr(c, -.75, -.4, .22, .5, .08); cel(c, "#ff9f1c", -.75, -.4, .22, .5); // propulsor de mão
          if ((v.vy || 0) < 0 && o.held) burst(c, -.64, .2, .25, "#ffcc4d", t, 7);
          break;
        }
        case "drone": {
          const flap = Math.sin(t * (o.held ? 28 : 12)) * .6;
          for (const s of [-1, 1]) { c.save(); c.translate(-.1, -.2); c.rotate(-.3 * s + flap * s * .5); c.scale(1, s);
            c.beginPath(); c.moveTo(0, 0); c.quadraticCurveTo(-.5, -.6, -1, -.35); c.quadraticCurveTo(-.5, -.15, 0, 0); c.fillStyle = alpha("#bff6ff", .8); c.fill(); ink(c, .03); c.restore(); }
          c.beginPath(); c.ellipse(0, 0, .65, .42, 0, 0, TAU); cel(c, "#6bcb77", -.65, -.42, 1.3, .84);
          c.beginPath(); c.moveTo(.6, -.05); c.lineTo(.85, .05); c.lineTo(.6, .15); c.fillStyle = "#ffd23f"; c.fill(); ink(c, .03); // bico
          c.beginPath(); c.arc(.38, -.12, .08, 0, TAU); c.fillStyle = "#fff"; c.fill(); ink(c, .02); c.beginPath(); c.arc(.4, -.12, .04, 0, TAU); c.fillStyle = OUT; c.fill();
          pilot(-.1, -.35, .75);
          break;
        }
        case "blink": {
          c.beginPath(); c.ellipse(0, 0, .55, .5, 0, 0, TAU); cel(c, "#b388ff", -.55, -.5, 1.1, 1);
          c.beginPath(); c.arc(.05, -.08, .3, 0, TAU); c.fillStyle = alpha("#e4d4ff", .8); c.fill(); ink(c, .035);
          pilot(.05, -.05, .8);
          c.beginPath(); c.moveTo(0, -.5); c.lineTo(0, -.75); c.lineWidth = .04; c.strokeStyle = OUT; c.stroke();
          burst(c, 0, -.78, .12, "#b388ff", t, 6);
          const a = (o.pointer || 0) * Math.PI / 180;
          c.save(); c.rotate(-a); c.fillStyle = "#ffd23f"; c.beginPath(); c.moveTo(.6, -.06); c.lineTo(1.3, 0); c.lineTo(.6, .06); c.fill(); ink(c, .025); c.restore();
          break;
        }
        case "bike": {
          for (const s of [-1, 1]) { c.save(); c.translate(s * .55, .35); c.rotate(t * 20);
            c.beginPath(); c.arc(0, 0, .27, 0, TAU); c.fillStyle = "#1a1630"; c.fill(); c.beginPath(); c.arc(0, 0, .14, 0, TAU); c.fillStyle = "#a8aec9"; c.fill();
            for (let k = 0; k < 3; k++) { c.rotate(TAU / 3); c.fillStyle = "#1a1630"; c.fillRect(-.02, 0, .04, .14); } c.restore(); }
          c.beginPath(); c.moveTo(-.85, .1); c.lineTo(-.4, -.3); c.lineTo(.5, -.35); c.lineTo(.9, .05); c.lineTo(.4, .2); c.lineTo(-.5, .2); c.closePath(); cel(c, "#e71d36", -.85, -.35, 1.75, .55);
          c.fillStyle = "#ffd23f"; c.fillRect(-.2, -.2, .5, .07);
          pilot(.1, -.55, .8);
          burst(c, -.95, .05, .2 + .05 * Math.sin(t * 30), "#ff7a1a", t, 6);
          break;
        }
        case "serpent": {
          for (let k = 4; k >= 0; k--) { const sx = -k * .38, sy = Math.sin(t * 6 - k * .9) * .15; c.beginPath(); c.arc(sx, sy, .22 - k * .02, 0, TAU); cel(c, k % 2 ? "#ff9f1c" : "#ffb347", sx - .2, sy - .2, .4, .4, { lw: .035 });
            star(c, sx, sy - .2, .08, .03, 3, -Math.PI / 2); c.fillStyle = "#e71d36"; c.fill(); }
          c.save(); c.translate(.35, Math.sin(t * 6) * .15);
          c.beginPath(); c.moveTo(-.2, -.25); c.lineTo(.45, -.1); c.lineTo(.5, .05); c.lineTo(-.2, .2); c.closePath(); cel(c, "#ff9f1c", -.2, -.25, .7, .45);
          c.beginPath(); c.moveTo(.0, .1); c.lineTo(.45, .2 + Math.abs(Math.sin(t * 4)) * .1); c.lineTo(.0, .2); c.fillStyle = "#e0861a"; c.fill(); ink(c, .03); // mandíbula
          c.beginPath(); c.arc(.15, -.1, .05, 0, TAU); c.fillStyle = "#ffd23f"; c.fill();
          c.restore();
          pilot(-.4, -.35 + Math.sin(t * 6 - 1) * .15, .65);
          break;
        }
        case "flip": {
          c.save(); c.globalCompositeOperation = "lighter";
          for (let k = 0; k < 3; k++) { c.beginPath(); c.ellipse(0, 0, .55, .14, 0, 0, TAU); c.translate(0, -.2); c.strokeStyle = alpha("#7df9ff", .5 + .3 * Math.sin(t * 6 + k)); c.lineWidth = .04; c.stroke(); }
          c.restore();
          nina(c, 0, H / 2 + .12, { t, pose: "fly", outfit: "#7df9ff", jet: "jet_padrao", hat: o.hat }, 0, .85); // traje (desenhado no espaço local)
          break;
        }
        case "ski": {
          c.beginPath(); c.moveTo(-.9, 0); c.lineTo(.9, .05); c.quadraticCurveTo(.7, .35, -.6, .3); c.closePath(); cel(c, "#1e90ff", -.9, 0, 1.8, .35);
          rr(c, -.3, -.35, .6, .35, .1); cel(c, "#f6f7fb", -.3, -.35, .6, .35);
          pilot(.05, -.55, .8);
          if (o.inWater) for (let k = 0; k < 5; k++) { c.beginPath(); c.arc(-.9 - k * .15, .2 - Math.abs(Math.sin(t * 20 + k)) * .3, .07, 0, TAU); c.fillStyle = alpha("#bff6ff", .8); c.fill(); }
          break;
        }
        case "rig": {
          const col = ["#6e6a5a", "#b8993a", "#ffd23f"][Math.max(1, Math.min(3, v.hp)) - 1];
          rr(c, -.85, .35, 1.7, .5, .25); cel(c, "#3a3f5c", -.85, .35, 1.7, .5);
          for (let k = 0; k < 5; k++) { c.beginPath(); c.arc(-.65 + k * .32, .6, .1, 0, TAU); c.fillStyle = "#a8aec9"; c.fill(); }
          rr(c, -.6, -.6, 1.2, 1, .15); cel(c, col, -.6, -.6, 1.2, 1);
          rr(c, -.05, -.45, .5, .4, .08); c.fillStyle = alpha("#bff6ff", .7); c.fill(); ink(c, .035);
          pilot(.2, -.25, .7);
          c.save(); c.translate(.55, -.4); c.rotate(-.6 + Math.sin(t * 2) * .2); rr(c, 0, -.08, .9, .16, .06); cel(c, col, 0, -.08, .9, .16);
          c.translate(.9, 0); c.strokeStyle = OUT; c.lineWidth = .06; c.beginPath(); c.moveTo(0, 0); c.lineTo(.15, -.15); c.moveTo(0, 0); c.lineTo(.15, .15); c.stroke(); c.restore();
          for (let k = 0; k < 3; k++) { c.beginPath(); c.arc(-.45 + k * .15, -.45, .05, 0, TAU); c.fillStyle = k < v.hp ? "#6bcb77" : "#5a2030"; c.fill(); }
          break;
        }
      }
    });
  }

  // =================================================================
  // EFEITOS
  // =================================================================
  function explosion(c, x, y, age, size) {
    const k = Math.min(1, age / .35), fade = 1 - Math.min(1, age / .7);
    if (fade <= 0) return;
    c.save(); c.globalAlpha = fade;
    star(c, x, y, size * (.4 + k), size * (.25 + k * .6), 11, age * 2); c.fillStyle = "#ff7a1a"; c.fill();
    star(c, x, y, size * (.3 + k * .7), size * (.2 + k * .4), 11, -age * 3); c.fillStyle = "#ffcc4d"; c.fill();
    c.beginPath(); c.arc(x, y, size * .3 * (1 - k * .5), 0, TAU); c.fillStyle = "#fff6c2"; c.fill();
    c.beginPath(); c.arc(x, y, size * (.5 + k * 1.2), 0, TAU); c.lineWidth = .06; c.strokeStyle = alpha("#ffffff", .7 * fade); c.stroke();
    c.restore();
  }
  function smoke(c, x, y, r, a) { c.beginPath(); c.arc(x, y, r, 0, TAU); c.fillStyle = `rgba(200,195,210,${a})`; c.fill(); }

  // =================================================================
  // ÍCONES DA OFICINA (canvas 2D comum, y para baixo, em px)
  // =================================================================
  /** state: "locked" | "owned" | "equipped" */
  function icon(c, id, size, state, item) {
    c.save(); c.clearRect(0, 0, size, size);
    if (state === "locked") c.filter = "grayscale(1) brightness(.7)";
    c.translate(size / 2, size / 2); c.scale(size / 1.6, size / 1.6);
    // sombra
    c.beginPath(); c.ellipse(0, .58, .45, .08, 0, 0, TAU); c.fillStyle = "rgba(0,0,0,.35)"; c.fill();
    const col = (item && item.c && item.c[0]) || "#2ec4b6";
    const flipY = fn => { c.save(); c.scale(1, -1); fn(); c.restore(); }; // helpers do mundo usam y-up
    switch (true) {
      case id.startsWith("jet_"): c.save(); c.translate(.45, .5); jetpack(c, id, 1, false); c.restore(); break;
      case id.startsWith("macacao"): rr(c, -.35, -.45, .7, .9, .18); cel(c, col, -.35, -.45, .7, .9); c.beginPath(); c.moveTo(0, -.45); c.lineTo(0, .45); c.lineWidth = .03; c.strokeStyle = shade(col, -.3); c.stroke(); rr(c, -.35, .15, .7, .1, .03); c.fillStyle = "#5b3a26"; c.fill(); break;
      case id === "capacete": case id === "bone": c.save(); c.translate(0, .3); c.scale(2, 2); head(c, 0, 0, 0, "smile", id); c.restore(); break;
      case id === "ima_porcas": c.lineWidth = .22; c.strokeStyle = "#e71d36"; c.beginPath(); c.arc(0, 0, .35, Math.PI, 0); c.stroke(); c.lineWidth = .02; c.strokeStyle = OUT; c.beginPath(); c.arc(0, 0, .46, Math.PI, 0); c.arc(0, 0, .24, 0, Math.PI, true); c.stroke();
        for (const s of [-1, 1]) { rr(c, s * .35 - .11, 0, .22, .25, .02); c.fillStyle = "#e9edf7"; c.fill(); ink(c, .03); } c.save(); c.translate(0, -.15); c.scale(.8, .8); flipY(() => nut(c, 0, 0, 0)); c.restore(); break;
      case id === "cinto_lastro": rr(c, -.5, -.08, 1, .16, .05); c.fillStyle = "#5b3a26"; c.fill(); ink(c, .03); rr(c, -.18, -.25, .36, .4, .06); cel(c, "#8e9aaf", -.18, -.25, .36, .4); c.fillStyle = OUT; c.font = '.18px "Lilita One"'; c.textAlign = "center"; c.fillText("KG", 0, .03); break;
      case id === "botas_mola": c.lineWidth = .05; c.strokeStyle = "#a8aec9"; c.beginPath(); for (let k = 0; k <= 8; k++) c.lineTo((k % 2 ? .12 : -.12), .1 + k * .05); c.stroke(); c.save(); c.translate(-.05, .05); c.scale(2.4, 2.4); boot(c, 0, 0, 0, "#6bcb77"); c.restore(); break;
      case id === "para_choque": c.beginPath(); c.moveTo(0, -.5); c.quadraticCurveTo(.45, -.4, .42, 0); c.quadraticCurveTo(.35, .35, 0, .5); c.quadraticCurveTo(-.35, .35, -.42, 0); c.quadraticCurveTo(-.45, -.4, 0, -.5); cel(c, "#7df9ff", -.45, -.5, .9, 1); c.fillStyle = OUT; c.fillRect(-.03, -.25, .06, .4); c.fillRect(-.18, -.08, .36, .06); break;
      case id === "ficha_sortuda": flipY(() => token(c, 0, 0, 0)); star(c, .28, -.28, .12, .05, 4); c.fillStyle = "#fff"; c.fill(); break;
      case id === "segunda_chance": c.save(); c.translate(0, .05); c.beginPath(); c.moveTo(0, .4); c.bezierCurveTo(-.6, 0, -.35, -.5, 0, -.2); c.bezierCurveTo(.35, -.5, .6, 0, 0, .4); cel(c, "#6bcb77", -.5, -.45, 1, .85); c.fillStyle = "#fff"; c.fillRect(-.04, -.15, .08, .3); c.fillRect(-.15, -.04, .3, .08); c.restore(); break;
      case id === "arrancada": c.save(); c.rotate(-.5); flipY(() => rocket(c, 0, 0, .3)); c.restore(); break;
      case id === "pacote_fichas": for (let k = 0; k < 3; k++) flipY(() => token(c, -.2 + k * .2, -.1 + (k % 2) * .15, k)); break;
      case id === "slot_3": rr(c, -.45, -.3, .9, .6, .1); cel(c, "#5b6585", -.45, -.3, .9, .6); for (let k = 0; k < 3; k++) { rr(c, -.36 + k * .26, -.18, .2, .36, .05); c.fillStyle = k === 2 ? "#2ec4b6" : "#1a1630"; c.fill(); } break;
      default: c.beginPath(); c.arc(0, 0, .4, 0, TAU); cel(c, col, -.4, -.4, .8, .8);
    }
    c.restore();
    c.filter = "none";
    if (state === "locked") {
      c.save(); c.translate(size * .78, size * .78); const s = size / 7;
      c.lineWidth = s * .28; c.strokeStyle = "#f6f7fb"; c.beginPath(); c.arc(0, -s * .3, s * .45, Math.PI, 0); c.stroke();
      rr(c, -s * .7, -s * .3, s * 1.4, s * 1.1, s * .2); c.fillStyle = "#ffd23f"; c.fill(); c.lineWidth = s * .12; c.strokeStyle = OUT; c.stroke(); c.restore();
    } else if (state === "equipped") {
      c.save(); c.translate(size * .8, size * .2); const s = size / 7;
      c.beginPath(); c.arc(0, 0, s, 0, TAU); c.fillStyle = "#6bcb77"; c.fill(); c.lineWidth = s * .15; c.strokeStyle = OUT; c.stroke();
      c.beginPath(); c.moveTo(-s * .45, 0); c.lineTo(-s * .1, s * .35); c.lineTo(s * .5, -s * .35); c.strokeStyle = "#fff"; c.lineWidth = s * .25; c.stroke(); c.restore();
    }
  }

  global.PropulsorArt = {
    nina, head, jetpack, background, floorCeiling, zapper, laserEmitter, laserBeam, rocket, warningBadge,
    nut, token, crate, worker, vehicle, explosion, smoke, icon, burst, SECTORS, sectorAt, shade, alpha,
  };
})(window);
