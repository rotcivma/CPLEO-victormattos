/* Gera sprite sheets PNG (com transparência) a partir de web/art.js para o projeto Unity.
   Uso: node web/tools/bake-sprites.js
   Saída: unity/Propulsor/Assets/_Project/Resources/Art/<Categoria>/<nome>.png + Art/manifest.json */
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..');
const OUT = path.join(ROOT, 'unity', 'Propulsor', 'Assets', '_Project', 'Resources', 'Art');
const ART = 'file://' + path.join(ROOT, 'web', 'art.js');

// Cada folha: quadros horizontais; pivot em fração (x da esquerda, y de BAIXO, como no Unity).
// draw(ctx, frame, frames) roda no navegador; ctx já está em unidades de mundo com origem no pivot.
const SHEETS = [];
const add = (cat, name, o) => SHEETS.push({ cat, name, ...o });
const VEH = ['mech', 'drone', 'blink', 'bike', 'serpent', 'flip', 'ski', 'rig'];
const VDIM = { mech: [1.4, 2], drone: [1.4, 1.2], blink: [1.2, 1.2], bike: [1.8, 1.2], serpent: [2, 1], flip: [.6, 1.1], ski: [1.8, .9], rig: [1.8, 1.8] };

// ---- Personagem
for (const [pose, frames, fps, extra] of [
  ['idle', 8, 8, {}], ['run', 12, 14, {}], ['fly', 8, 12, { thrust: true }], ['rise', 8, 12, { thrust: true, vy: 9 }],
  ['fall', 8, 12, { vy: -8 }], ['hit', 4, 12, {}], ['tumble', 12, 18, {}], ['down', 1, 1, {}]]) {
  add('Characters', `Nina_${pose}`, { frames, fps, w: 2.2, h: 2.2, px: .5, py: .12, ppu: 128, args: { pose, extra } ,
    draw: `(c, f, n, a) => { const p = a.pose === 'rise' ? 'fly' : a.pose; const t = f / n * (a.pose==='run'?0.5:1);
      const rot = a.pose === 'tumble' ? f / n * Math.PI * 2 : a.pose === 'down' ? -Math.PI / 2 : 0;
      PropulsorArt.nina(c, 0, a.pose === 'tumble' ? .6 : a.pose === 'down' ? .3 : 0, Object.assign({ t, pose: p, phase: f / n * Math.PI * 2, outfit: '#2ec4b6', jet: 'jet_padrao', vy: 0 }, a.extra), rot); }` });
}
// ---- Propulsores (mochila sozinha) e roupas
for (const id of ['jet_padrao', 'jet_bolhas', 'jet_confete'])
  add('Jetpacks', id, { frames: 6, fps: 12, w: 1.2, h: 1.4, px: .7, py: .2, ppu: 128, args: { id },
    draw: `(c, f, n, a) => { c.save(); c.scale(1, -1); c.translate(.45, -.7); PropulsorArt.jetpack(c, a.id, f / n, true); c.restore(); }` });
// ---- Veículos
for (const k of VEH) {
  const [vw, vh] = VDIM[k];
  add('Vehicles', `Vehicle_${k}`, { frames: 8, fps: 12, w: vw + 1.6, h: vh + 1.4, px: .5, py: .3 / (vh + 1.4) + .12, ppu: 96, args: { k, vw, vh },
    draw: `(c, f, n, a) => { PropulsorArt.vehicle(c, 0, { kind: a.k, w: a.vw, h: a.vh, y: 0, vy: 0, t: f / n, hp: 3, sign: -1 }, { t: f / n, appear: 9, outfit: '#2ec4b6', held: true, pointer: Math.sin(f / n * 6.28) * 60, inWater: true }); }` });
}
// ---- Obstáculos
add('Obstacles', 'Zapper_Coil', { frames: 1, fps: 1, w: 1, h: 1, px: .5, py: .5, ppu: 128, args: {}, draw: `(c) => { PropulsorArt.zapper(c, [-9, 0, 0, 0], 0, false, 1); }` , clipAround: true });
add('Obstacles', 'Zapper_Glow', { frames: 8, fps: 16, w: 1.4, h: 1.4, px: .5, py: .5, ppu: 128, args: {}, draw: `(c, f, n) => PropulsorArt.burst(c, 0, 0, .55, '#7df9ff', f / n, 10)` });
add('Obstacles', 'Zapper_Arc', { frames: 6, fps: 20, w: 3, h: .8, px: 0, py: .5, ppu: 128, args: {},
  draw: `(c, f, n) => { c.save(); c.translate(0, 0); const e = [0, 0, 3, 0]; c.beginPath(); for (let a = 0; a < 3; a++) {} c.restore();
    c.globalCompositeOperation = 'lighter'; for (let a = 0; a < 3; a++) { c.beginPath(); c.moveTo(0, 0); for (let k = 1; k < 10; k++) c.lineTo(k * .3, (Math.sin(f * 7.1 + k * 3.7 + a * 11) ) * .22); c.lineTo(3, 0);
    c.lineWidth = a ? .035 : .09; c.strokeStyle = a ? '#7df9ff' : '#ffffff'; c.stroke(); } }` });
add('Obstacles', 'Laser_Emitter', { frames: 3, fps: 1, w: 1.6, h: 1, px: .5, py: .5, ppu: 128, args: {}, draw: `(c, f) => PropulsorArt.laserEmitter(c, 0, 0, 1, [0, .5, 1][f])` });
add('Obstacles', 'Laser_Beam', { frames: 4, fps: 20, w: 2, h: 1.4, px: 0, py: .5, ppu: 64, args: {}, draw: `(c, f, n) => { c.save(); c.beginPath(); c.rect(0, -1, 2, 2); c.clip(); PropulsorArt.laserBeam(c, -1, 3, 0, 'fire', 1, f / n); c.restore(); }` });
add('Obstacles', 'Rocket', { frames: 6, fps: 20, w: 2, h: 1, px: .35, py: .5, ppu: 128, args: {}, draw: `(c, f, n) => PropulsorArt.rocket(c, 0, 0, f / n)` });
add('Obstacles', 'Warning', { frames: 8, fps: 12, w: 1.1, h: 1.1, px: .5, py: .5, ppu: 128, args: {}, draw: `(c, f, n) => PropulsorArt.warningBadge(c, 0, 0, f / n, false)` });
add('Obstacles', 'Warning_Locked', { frames: 8, fps: 20, w: 1.1, h: 1.1, px: .5, py: .5, ppu: 128, args: {}, draw: `(c, f, n) => PropulsorArt.warningBadge(c, 0, 0, f / n, true)` });
// ---- Partículas
add('Particles', 'Explosion', { frames: 10, fps: 15, w: 3.4, h: 3.4, px: .5, py: .5, ppu: 96, args: {}, draw: `(c, f, n) => PropulsorArt.explosion(c, 0, 0, f / n * .7, 1)` });
add('Particles', 'Smoke', { frames: 1, fps: 1, w: .6, h: .6, px: .5, py: .5, ppu: 128, args: {}, draw: `(c) => PropulsorArt.smoke(c, 0, 0, .25, .8)` });
add('Particles', 'Burst', { frames: 6, fps: 20, w: 1.6, h: 1.6, px: .5, py: .5, ppu: 128, args: {}, draw: `(c, f, n) => PropulsorArt.burst(c, 0, 0, .6, '#ffd23f', f / n, 9)` });
// ---- Coletáveis e robôs (categoria Obstacles/Characters conforme o papel)
add('Pickups', 'Nut', { frames: 8, fps: 12, w: .6, h: .6, px: .5, py: .5, ppu: 128, args: {}, draw: `(c, f, n) => PropulsorArt.nut(c, 0, 0, f / n * Math.PI / 3 * 2 / 1)` });
add('Pickups', 'Token', { frames: 1, fps: 1, w: .8, h: .8, px: .5, py: .5, ppu: 128, args: {}, draw: `(c) => PropulsorArt.token(c, 0, 0, 0)` });
add('Pickups', 'VehicleCrate', { frames: 8, fps: 10, w: 1.4, h: 1.4, px: .5, py: .5, ppu: 128, args: {}, draw: `(c, f, n) => PropulsorArt.crate(c, 0, 0, f / n * Math.PI)` });
add('Characters', 'Worker_walk', { frames: 8, fps: 10, w: 1, h: 1.4, px: .5, py: .1, ppu: 128, args: {}, draw: `(c, f, n) => PropulsorArt.worker(c, 0, 0, f / n * .628, 1, false, 0, 0)` });
add('Characters', 'Worker_knocked', { frames: 8, fps: 16, w: 1.4, h: 1.4, px: .5, py: .5, ppu: 128, args: {}, draw: `(c, f, n) => PropulsorArt.worker(c, 0, -.5, f / n, 1, true, f / n * 360, 0)` });

async function main() {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.setContent('<!doctype html><html><head><link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Lilita+One&display=swap"></head><body></body></html>');
  await page.addScriptTag({ path: path.join(ROOT, 'web', 'art.js') });
  await page.evaluate(() => document.fonts && document.fonts.ready);
  const manifest = { generated: new Date().toISOString().slice(0, 10), note: 'Arte própria do PROPULSOR gerada a partir de web/art.js', sheets: [] };

  for (const s of SHEETS) {
    const fw = Math.round(s.w * s.ppu), fh = Math.round(s.h * s.ppu);
    const png = await page.evaluate(({ s, fw, fh }) => {
      const cv = document.createElement('canvas'); cv.width = fw * s.frames; cv.height = fh;
      const c = cv.getContext('2d');
      const draw = eval(s.draw);
      for (let f = 0; f < s.frames; f++) {
        c.save(); c.beginPath(); c.rect(f * fw, 0, fw, fh); c.clip();
        c.setTransform(s.ppu, 0, 0, -s.ppu, f * fw + s.px * fw, fh - s.py * fh);
        draw(c, f, s.frames, s.args);
        c.restore();
      }
      return cv.toDataURL('image/png');
    }, { s, fw, fh });
    const dir = path.join(OUT, s.cat); fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(path.join(dir, s.name + '.png'), Buffer.from(png.split(',')[1], 'base64'));
    manifest.sheets.push({ category: s.cat, name: s.name, path: `Art/${s.cat}/${s.name}`, frames: s.frames, fps: s.fps,
                           frameWidth: fw, frameHeight: fh, pixelsPerUnit: s.ppu, pivotX: s.px, pivotY: s.py });
    process.stdout.write('.');
  }

  // ---- Ícones da Oficina: 3 estados por item (bloqueado / liberado / equipado)
  const ids = ['jet_padrao', 'jet_bolhas', 'jet_confete', 'macacao_verde', 'macacao_laranja', 'macacao_roxo', 'macacao_dourado', 'capacete', 'bone',
    'ima_porcas', 'cinto_lastro', 'botas_mola', 'para_choque', 'ficha_sortuda', 'segunda_chance', 'arrancada', 'pacote_fichas', 'slot_3'];
  const colors = { macacao_verde: '#2ec4b6', macacao_laranja: '#ff9f1c', macacao_roxo: '#b388ff', macacao_dourado: '#ffd23f' };
  for (const id of ids) for (const st of ['locked', 'owned', 'equipped']) {
    const png = await page.evaluate(({ id, st, col }) => {
      const cv = document.createElement('canvas'); cv.width = cv.height = 256;
      PropulsorArt.icon(cv.getContext('2d'), id, 256, st, { c: [col || '#2ec4b6'] });
      return cv.toDataURL('image/png');
    }, { id, st, col: colors[id] });
    const dir = path.join(OUT, 'Icons'); fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(path.join(dir, `${id}_${st}.png`), Buffer.from(png.split(',')[1], 'base64'));
  }
  manifest.icons = { path: 'Art/Icons/<id>_<estado>', states: ['locked', 'owned', 'equipped'], size: 256, ids };

  // ---- Fundos: camada distante e do meio, por setor (tiles que emendam)
  const bg = await page.evaluate(() => {
    const out = [];
    const view = (W) => ({ W, H: 9 * 64, SC: 64, camX: 0, t: 0, DPR: 1, sy: y => 9 * 64 - (y + 1) * 64 });
    for (let s = 0; s < 4; s++) {
      const cv = document.createElement('canvas'); cv.width = 64 * 40; cv.height = 64 * 9;
      const c = cv.getContext('2d');
      const v = view(cv.width); v.camX = s * 400 + 1; // posição dentro do setor s
      PropulsorArt.background(c, v);
      out.push(cv.toDataURL('image/png'));
    }
    return out;
  });
  const sect = ['Montagem', 'Caldeiras', 'Testes', 'Deposito'];
  fs.mkdirSync(path.join(OUT, 'Backgrounds'), { recursive: true });
  bg.forEach((d, i) => fs.writeFileSync(path.join(OUT, 'Backgrounds', `Setor_${sect[i]}.png`), Buffer.from(d.split(',')[1], 'base64')));
  manifest.backgrounds = sect.map(n => ({ name: `Setor_${n}`, path: `Art/Backgrounds/Setor_${n}`, pixelsPerUnit: 64, widthUnits: 40, heightUnits: 9, note: 'y=-1 na base; parallax já composto' }));

  // ---- Blocos de parallax que emendam (distante 10 u, meio 7 u) e faixas de chão/teto (18 u)
  const tiles = await page.evaluate(() => {
    const out = { far: [], mid: [], floor: null, ceiling: null };
    for (let s = 0; s < 4; s++) {
      for (let i = 0; i < 4; i++) out.far.push({ s, i, d: PropulsorArt.tile('far', i * 3 + 1, s, 64).toDataURL('image/png') });
      const seen = new Set();
      for (let i = 0; i < 200 && seen.size < 6; i++) { const v = PropulsorArt.midVariant(i); if (seen.has(v)) continue; seen.add(v);
        out.mid.push({ s, i: v, d: PropulsorArt.tile('mid', i, s, 64).toDataURL('image/png') }); }
    }
    const band = (y0, y1) => { const cv = document.createElement('canvas'); cv.width = 18 * 64; cv.height = Math.round((y1 - y0) * 64);
      const c = cv.getContext('2d'); c.setTransform(64, 0, 0, -64, 0, y1 * 64); PropulsorArt.floorCeiling(c, 2, 14, 0); return cv.toDataURL('image/png'); };
    out.floor = band(-1.5, .2); out.ceiling = band(6.75, 8.6);
    return out;
  });
  fs.mkdirSync(path.join(OUT, 'Backgrounds', 'Tiles'), { recursive: true });
  const save = (n, d) => fs.writeFileSync(path.join(OUT, 'Backgrounds', 'Tiles', n + '.png'), Buffer.from(d.split(',')[1], 'base64'));
  tiles.far.forEach(t => save(`Far_${sect[t.s]}_${t.i}`, t.d));
  tiles.mid.forEach(t => save(`Mid_${sect[t.s]}_${t.i}`, t.d));
  save('Floor', tiles.floor); save('Ceiling', tiles.ceiling);
  manifest.tiles = { pixelsPerUnit: 64, far: { widthUnits: 10, heightUnits: 9, parallax: .22, perSector: 4 }, mid: { widthUnits: 7, heightUnits: 9, parallax: .55, perSector: 6 },
                     floor: { widthUnits: 18, y0: -1.5, y1: .2 }, ceiling: { widthUnits: 18, y0: 6.75, y1: 8.6 }, sectors: sect, sectorLength: 400,
                     naming: 'Art/Backgrounds/Tiles/{Far|Mid}_<Setor>_<n>, Floor, Ceiling; base dos blocos de camada em y=-1' };

  fs.writeFileSync(path.join(OUT, 'manifest.json'), JSON.stringify(manifest, null, 2));
  await browser.close();
  console.log(`\n${manifest.sheets.length} folhas, ${ids.length * 3} ícones, ${bg.length} fundos → ${path.relative(ROOT, OUT)}`);
}
main().catch(e => { console.error(e); process.exit(1); });
