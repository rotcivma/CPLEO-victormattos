using System.Collections.Generic;
using Propulsor.Core.Events;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Gameplay.Vehicles;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Desenha o estado do mundo da <see cref="RunSimulation"/>: arcos de faísca, feixes,
    /// drones-foguete e avisos, porcas, fichas, tokens de veículo e a água do Jet-Esqui.
    /// Usa pools: nada é instanciado depois do aquecimento.
    /// </summary>
    [DefaultExecutionOrder(100)] // ordem: jogador → câmera (0) → cenário
    public sealed class WorldView : MonoBehaviour
    {
        private static readonly Color Electric = new Color32(0x7D, 0xF9, 0xFF, 255);
        private static readonly Color Coil = new Color32(0xA8, 0xAE, 0xC9, 255);
        private static readonly Color Danger = new Color32(0xE7, 0x1D, 0x36, 255);
        private static readonly Color Gold = new Color32(0xFF, 0xD2, 0x3F, 255);
        private static readonly Color Token = new Color32(0xB3, 0x88, 0xFF, 255);
        private static readonly Color Accent = new Color32(0xFF, 0x9F, 0x1C, 255);

        private RunDriver _driver;
        private Camera _cam;
        private Sprite _square, _circle;

        private sealed class ZapperGo { public Transform Root, Beam, CoilA, CoilB; public SpriteRenderer BeamSr, GlowA, GlowB; }

        // Arte gerada (Resources/Art). Nulo = arte geométrica.
        private static SpriteLibrary.Anim Art(string name) => SpriteLibrary.Get(name);
        private float _artTime;

        private readonly Dictionary<long, ZapperGo> _zappers = new Dictionary<long, ZapperGo>();
        private readonly Stack<ZapperGo> _zapperPool = new Stack<ZapperGo>();
        private readonly Dictionary<int, SpriteRenderer> _pickups = new Dictionary<int, SpriteRenderer>();
        private readonly Stack<SpriteRenderer> _pickupPool = new Stack<SpriteRenderer>();
        private readonly Dictionary<int, SpriteRenderer> _missiles = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, SpriteRenderer> _warnings = new Dictionary<int, SpriteRenderer>();
        private readonly Stack<SpriteRenderer> _spritePool = new Stack<SpriteRenderer>();
        private readonly List<SpriteRenderer> _laserBeams = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _laserEmitters = new List<SpriteRenderer>();
        private SpriteRenderer _water;

        // Robôs operários, efeitos e câmera (Fase 9)
        private sealed class WorkerGo { public Transform Root; public SpriteRenderer Body, Visor; }
        private readonly Dictionary<int, WorkerGo> _workers = new Dictionary<int, WorkerGo>();
        private readonly Stack<WorkerGo> _workerPool = new Stack<WorkerGo>();
        private CameraFollow _shake;
        private ParticleSystem _explosion, _sparkle;
        private TextMesh _floating;
        private float _floatingT = -1f;
        private static readonly Color WorkerGrey = new Color32(0x8E, 0x9A, 0xAF, 255);

        private readonly HashSet<long> _seenLong = new HashSet<long>();
        private readonly HashSet<int> _seenInt = new HashSet<int>();
        private readonly List<long> _removeLong = new List<long>();
        private readonly List<int> _removeInt = new List<int>();

        public void Bind(RunDriver driver, Camera cam, Sprite square, Sprite circle, CameraFollow shake = null, Font font = null)
        {
            _driver = driver;
            _cam = cam;
            _square = square;
            _circle = circle;
            _shake = shake;
            _water = NewSprite("Agua", _square, new Color(0.12f, 0.56f, 1f, 0.35f), 15);
            _water.gameObject.SetActive(false);
            driver.Restarted += ClearAll;

            _explosion = MakeBurst("Explosao", 60, 0.6f, 9f, 0.35f, Accent, Danger);
            _sparkle = MakeBurst("Brilho", 6, 0.25f, 3f, 0.12f, Gold, Color.white);
            var ft = new GameObject("TextoFlutuante");
            ft.transform.SetParent(transform, false);
            _floating = ft.AddComponent<TextMesh>();
            _floating.font = font;
            if (font != null) ft.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            ft.GetComponent<MeshRenderer>().sortingOrder = 30;
            _floating.anchor = TextAnchor.MiddleCenter;
            _floating.characterSize = 0.1f;
            _floating.fontSize = 60;
            _floating.gameObject.SetActive(false);

            var bus = driver.Bus;
            bus.Subscribe<VehicleDestroyed>(_ => { Burst(_explosion, 60); _shake?.Shake(0.35f, 0.3f); });
            bus.Subscribe<VehicleDamaged>(_ => { Burst(_explosion, 15); _shake?.Shake(0.15f, 0.15f); });
            bus.Subscribe<PlayerDied>(_ => _shake?.Shake(0.25f, 0.3f));
            bus.Subscribe<CoinCollected>(_ => Burst(_sparkle, 6));
            bus.Subscribe<HazardNearMiss>(_ => Float("raspão!", Electric));
            bus.Subscribe<HeadStartStarted>(_ => _shake?.Shake(0.15f, 0.4f));
        }

        private ParticleSystem MakeBurst(string name, int max, float life, float speed, float size, Color a, Color b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = life;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = size;
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max * 3;
            main.gravityModifier = 0.5f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            r.sortingOrder = 25;
            ps.Play(); // emissão contínua zerada: só solta partículas via Emit()
            return ps;
        }

        private void Burst(ParticleSystem ps, int count)
        {
            if (ps == null || _driver.Sim == null) return;
            var box = _driver.Sim.PlayerBox;
            ps.transform.position = new Vector3(box.CenterX, box.CenterY, 0f);
            ps.Emit(count);
        }

        private void Float(string text, Color color)
        {
            if (_floating == null || _driver.Sim == null) return;
            _floating.text = text;
            _floating.color = color;
            _floatingT = 0f;
            _floating.gameObject.SetActive(true);
        }

        private void DrawFloating()
        {
            if (_floatingT < 0f) return;
            _floatingT += Time.deltaTime;
            var box = _driver.Sim.PlayerBox;
            _floating.transform.position = new Vector3(box.CenterX, box.MaxY + 0.4f + _floatingT, 0f);
            var c = _floating.color;
            c.a = Mathf.Clamp01(1f - _floatingT / 0.7f);
            _floating.color = c;
            if (_floatingT > 0.7f) { _floatingT = -1f; _floating.gameObject.SetActive(false); }
        }

        // ---------- robôs operários ----------
        private void DrawWorkers(RunSimulation sim)
        {
            _seenInt.Clear();
            foreach (var w in sim.Workers)
            {
                _seenInt.Add(w.Id);
                if (!_workers.TryGetValue(w.Id, out var go)) _workers[w.Id] = go = RentWorker();
                go.Root.position = new Vector3(w.X, w.Y + Worker.Height * 0.5f, 0f);
                go.Root.rotation = Quaternion.Euler(0f, 0f, -w.Spin);
                // passinho: balança enquanto anda
                float bob = w.Knocked ? 0f : Mathf.Abs(Mathf.Sin(Time.time * 8f + w.Id)) * 0.05f;
                go.Body.transform.localPosition = new Vector3(0f, bob, 0f);
                go.Visor.transform.localPosition = new Vector3(0.12f * w.Dir, 0.25f + bob, 0f);
                go.Visor.color = w.Knocked ? Danger : Gold;
                var wk = Art(w.Knocked ? "Worker_knocked" : "Worker_walk");
                if (wk != null)
                {
                    go.Visor.enabled = false;
                    go.Body.sprite = wk.Frame(_artTime + w.Id * 0.17f);
                    go.Body.color = Color.white;
                    go.Body.transform.localScale = new Vector3(w.Dir, 1f, 1f);
                    go.Body.transform.localPosition = new Vector3(0f, w.Knocked ? 0f : -Worker.Height * 0.5f, 0f);
                    go.Root.rotation = Quaternion.identity; // a folha "knocked" já gira
                }
            }
            _removeInt.Clear();
            foreach (var k in _workers.Keys) if (!_seenInt.Contains(k)) _removeInt.Add(k);
            foreach (var k in _removeInt)
            {
                var go = _workers[k];
                go.Root.gameObject.SetActive(false);
                _workerPool.Push(go);
                _workers.Remove(k);
            }
        }

        private WorkerGo RentWorker()
        {
            if (_workerPool.Count > 0) { var w = _workerPool.Pop(); w.Root.gameObject.SetActive(true); return w; }
            var root = new GameObject("RoboOperario").transform;
            root.SetParent(transform, false);
            var body = NewSprite("Corpo", _square, WorkerGrey, 3, root);
            body.transform.localScale = new Vector3(Worker.Width, Worker.Height, 1f);
            var visor = NewSprite("Visor", _square, Gold, 4, root);
            visor.transform.localScale = new Vector3(0.35f, 0.15f, 1f);
            return new WorkerGo { Root = root, Body = body, Visor = visor };
        }

        private void LateUpdate()
        {
            if (_driver == null || _driver.Sim == null) return;
            var sim = _driver.Sim;
            _artTime += Time.deltaTime;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            float camX = _cam.transform.position.x;

            DrawZappers(sim);
            DrawWorkers(sim);
            DrawPickups(sim);
            DrawFloating();
            DrawLasers(sim, camX, halfW);
            DrawMissiles(sim, camX, halfW);
            DrawWater(sim, camX, halfW);
        }

        // ---------- arcos ----------
        private void DrawZappers(RunSimulation sim)
        {
            _seenLong.Clear();
            foreach (var seg in sim.Segments)
            {
                for (int i = 0; i < seg.Def.Zappers.Count; i++)
                {
                    long key = ((long)seg.Id << 8) | (uint)i;
                    _seenLong.Add(key);
                    if (!_zappers.TryGetValue(key, out var go)) _zappers[key] = go = RentZapper();
                    var z = seg.Def.Zappers[i];
                    Hazards.ZapperEndpoints(z, seg.StartX, sim.Time, out var ax, out var ay, out var bx, out var by);
                    var a = new Vector3(ax, ay, 0f);
                    var b = new Vector3(bx, by, 0f);
                    go.CoilA.position = a;
                    go.CoilB.position = b;
                    go.Beam.position = (a + b) * 0.5f;
                    go.Beam.rotation = Quaternion.Euler(0f, 0f, Hazards.ZapperAngle(z, sim.Time));
                    // "Faísca": espessura tremula.
                    float flicker = 0.35f + 0.15f * Mathf.PerlinNoise(Time.time * 20f, i);
                    go.Beam.localScale = new Vector3(z.Length, flicker, 1f);
                    go.BeamSr.color = z.IsRotating ? Color.Lerp(Electric, Accent, 0.5f) : Electric;
                    var arc = Art("Zapper_Arc");
                    if (arc != null)
                    {
                        // folha do arco tem 3 u de comprimento com pivô na ponta esquerda
                        go.BeamSr.sprite = arc.Frame(_artTime + i * 0.13f);
                        go.Beam.position = a;
                        go.Beam.localScale = new Vector3(z.Length / 3f, 1f, 1f);
                        var coil = Art("Zapper_Coil"); var glow = Art("Zapper_Glow");
                        foreach (var c in new[] { go.CoilA, go.CoilB })
                        {
                            var csr = c.GetComponent<SpriteRenderer>();
                            csr.sprite = coil.Frame(0f); csr.color = Color.white; c.localScale = Vector3.one;
                        }
                        if (go.GlowA == null)
                        {
                            go.GlowA = NewSprite("BrilhoA", _circle, Color.white, 8, go.CoilA);
                            go.GlowB = NewSprite("BrilhoB", _circle, Color.white, 8, go.CoilB);
                        }
                        go.GlowA.sprite = glow.Frame(_artTime); go.GlowB.sprite = glow.Frame(_artTime + .3f);
                        go.GlowA.color = go.GlowB.color = z.IsRotating ? new Color(1f, .75f, .45f) : Color.white;
                        go.BeamSr.color = z.IsRotating ? new Color(1f, .8f, .5f) : Color.white;
                    }
                }
            }
            _removeLong.Clear();
            foreach (var k in _zappers.Keys) if (!_seenLong.Contains(k)) _removeLong.Add(k);
            foreach (var k in _removeLong) { ReturnZapper(_zappers[k]); _zappers.Remove(k); }
        }

        private ZapperGo RentZapper()
        {
            if (_zapperPool.Count > 0) { var z = _zapperPool.Pop(); z.Root.gameObject.SetActive(true); return z; }
            var root = new GameObject("Arco").transform;
            root.SetParent(transform, false);
            var beam = NewSprite("Faisca", _square, Electric, 6, root);
            var ca = NewSprite("BobinaA", _circle, Coil, 7, root);
            var cb = NewSprite("BobinaB", _circle, Coil, 7, root);
            ca.transform.localScale = cb.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            return new ZapperGo { Root = root, Beam = beam.transform, BeamSr = beam, CoilA = ca.transform, CoilB = cb.transform };
        }

        private void ReturnZapper(ZapperGo z) { z.Root.gameObject.SetActive(false); _zapperPool.Push(z); }

        // ---------- coletáveis ----------
        private void DrawPickups(RunSimulation sim)
        {
            _seenInt.Clear();
            foreach (var p in sim.Pickups)
            {
                if (p.Collected) continue;
                _seenInt.Add(p.Id);
                if (!_pickups.TryGetValue(p.Id, out var sr))
                {
                    _pickups[p.Id] = sr = RentPickup();
                    StylePickup(sr, p.Kind);
                }
                float pulse = p.Kind == PickupKind.Coin ? 1f : 1f + 0.08f * Mathf.Sin(Time.time * 6f);
                float size = p.Kind == PickupKind.Coin ? 0.45f : 0.8f;
                sr.transform.position = new Vector3(p.X, p.Y, 0f);
                sr.transform.localScale = new Vector3(size * pulse * (p.Kind == PickupKind.Coin ? Mathf.Abs(Mathf.Cos(Time.time * 4f + p.X)) * 0.6f + 0.4f : 1f),
                                                      size * pulse, 1f);
                if (p.Kind == PickupKind.VehicleToken) sr.transform.rotation = Quaternion.Euler(0f, 0f, Time.time * 90f);
                var pa = Art(p.Kind == PickupKind.Coin ? "Nut" : p.Kind == PickupKind.Token ? "Token" : "VehicleCrate");
                if (pa != null)
                {
                    sr.sprite = pa.Frame(_artTime + p.X * 0.1f);
                    sr.color = Color.white;
                    sr.transform.localScale = Vector3.one;
                    sr.transform.rotation = Quaternion.identity;
                }
            }
            _removeInt.Clear();
            foreach (var k in _pickups.Keys) if (!_seenInt.Contains(k)) _removeInt.Add(k);
            foreach (var k in _removeInt)
            {
                var sr = _pickups[k];
                sr.gameObject.SetActive(false);
                _pickupPool.Push(sr);
                _pickups.Remove(k);
            }
        }

        private SpriteRenderer RentPickup()
        {
            if (_pickupPool.Count > 0) { var s = _pickupPool.Pop(); s.gameObject.SetActive(true); return s; }
            return NewSprite("Coletavel", _circle, Gold, 4);
        }

        private void StylePickup(SpriteRenderer sr, PickupKind kind)
        {
            sr.transform.rotation = Quaternion.identity;
            switch (kind)
            {
                case PickupKind.Coin: sr.sprite = _circle; sr.color = Gold; break;          // porca
                case PickupKind.Token: sr.sprite = _circle; sr.color = Token; break;        // ficha
                case PickupKind.VehicleToken: sr.sprite = _square; sr.color = Accent; break; // caixa de veículo
            }
        }

        // ---------- feixes ----------
        private void DrawLasers(RunSimulation sim, float camX, float halfW)
        {
            var laser = sim.ActiveLaser;
            int rowCount = 0;
            if (laser != null)
            {
                // Emissores deslizam das bordas na entrada/saída.
                float slide = laser.Phase == LaserPhase.Entering ? laser.PhaseTime / LaserSequence.EnterTime
                            : laser.Phase == LaserPhase.Leaving ? 1f - laser.PhaseTime / LaserSequence.LeaveTime : 1f;
                float inset = Mathf.Lerp(-0.6f, 0.4f, slide);
                var rows = laser.CurrentRows;
                // Mostra emissores de todas as linhas da onda atual (ou da primeira na entrada).
                var emitterRows = rows.Length > 0 ? rows : laser.Waves[Mathf.Min(laser.WaveIndex, laser.Waves.Count - 1)].Rows;
                for (int i = 0; i < emitterRows.Length; i++)
                {
                    var left = Emitter(i * 2);
                    var right = Emitter(i * 2 + 1);
                    left.transform.position = new Vector3(camX - halfW + inset, emitterRows[i], 0f);
                    right.transform.position = new Vector3(camX + halfW - inset, emitterRows[i], 0f);
                    var le = Art("Laser_Emitter");
                    if (le != null)
                    {
                        int f = laser.Phase == LaserPhase.Firing ? 2 : laser.Phase == LaserPhase.Warning ? 1 : 0;
                        foreach (var e in new[] { left, right })
                        {
                            e.sprite = le.Frames[Mathf.Min(f, le.Frames.Length - 1)];
                            e.color = Color.white; e.transform.localScale = Vector3.one;
                        }
                        left.flipX = false; right.flipX = true;
                    }
                }
                rowCount = emitterRows.Length;

                for (int i = 0; i < rows.Length; i++)
                {
                    var beam = Beam(i);
                    bool firing = laser.Phase == LaserPhase.Firing;
                    float thickness = firing ? Hazards.LaserHalfThickness * 2f : 0.06f;
                    float alpha = firing ? 1f : (Mathf.Repeat(Time.time * 6f, 1f) < 0.5f ? 0.8f : 0.3f);
                    beam.color = new Color(Danger.r, Danger.g, Danger.b, alpha);
                    beam.transform.position = new Vector3(camX, rows[i], 0f);
                    beam.transform.localScale = new Vector3(halfW * 2f, thickness, 1f);
                    var lb = Art("Laser_Beam");
                    if (lb != null)
                    {
                        beam.sprite = lb.Frame(_artTime);
                        beam.color = firing ? Color.white : new Color(1f, 1f, 1f, alpha);
                        beam.transform.position = new Vector3(camX - halfW, rows[i], 0f); // folha com pivô à esquerda, 2 u
                        beam.transform.localScale = new Vector3(halfW, firing ? 1f : 0.12f, 1f);
                    }
                }
                for (int i = rows.Length; i < _laserBeams.Count; i++) _laserBeams[i].gameObject.SetActive(false);
            }
            else
            {
                foreach (var b in _laserBeams) b.gameObject.SetActive(false);
            }
            for (int i = rowCount * 2; i < _laserEmitters.Count; i++) _laserEmitters[i].gameObject.SetActive(false);
        }

        private SpriteRenderer Beam(int i)
        {
            while (_laserBeams.Count <= i) _laserBeams.Add(NewSprite("Feixe", _square, Danger, 9));
            _laserBeams[i].gameObject.SetActive(true);
            return _laserBeams[i];
        }

        private SpriteRenderer Emitter(int i)
        {
            while (_laserEmitters.Count <= i)
            {
                var e = NewSprite("Emissor", _square, Coil, 10);
                e.transform.localScale = new Vector3(0.8f, 0.6f, 1f);
                _laserEmitters.Add(e);
            }
            _laserEmitters[i].gameObject.SetActive(true);
            return _laserEmitters[i];
        }

        // ---------- drones ----------
        private void DrawMissiles(RunSimulation sim, float camX, float halfW)
        {
            _seenInt.Clear();
            foreach (var m in sim.Missiles)
            {
                _seenInt.Add(m.Id);
                if (m.Phase == MissilePhase.Flying)
                {
                    Hide(_warnings, m.Id);
                    var body = Get(_missiles, m.Id, "Drone", Danger, 11);
                    body.sprite = _square;
                    body.transform.position = new Vector3(m.X, m.Y, 0f);
                    body.transform.localScale = new Vector3(Missile.Width, Missile.Height, 1f);
                    var ra = Art("Rocket");
                    if (ra != null) { body.sprite = ra.Frame(_artTime); body.color = Color.white; body.transform.localScale = Vector3.one; }
                }
                else if (m.IsWarning)
                {
                    var w = Get(_warnings, m.Id, "Aviso", Danger, 20);
                    w.sprite = _circle;
                    float hz = m.Phase == MissilePhase.Locked ? 10f : 4f;
                    w.enabled = Mathf.Repeat(Time.time * hz, 1f) < 0.6f;
                    w.transform.position = new Vector3(camX + halfW - 0.7f, m.Y, 0f);
                    w.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
                    var wa = Art(m.Phase == MissilePhase.Locked ? "Warning_Locked" : "Warning");
                    if (wa != null) { w.sprite = wa.Frame(_artTime); w.color = Color.white; w.enabled = true; w.transform.localScale = Vector3.one; }
                }
            }
            Prune(_missiles);
            Prune(_warnings);
        }

        private SpriteRenderer Get(Dictionary<int, SpriteRenderer> map, int id, string name, Color c, int order)
        {
            if (map.TryGetValue(id, out var sr)) return sr;
            sr = _spritePool.Count > 0 ? _spritePool.Pop() : NewSprite(name, _square, c, order);
            sr.gameObject.SetActive(true);
            sr.enabled = true;
            sr.color = c;
            sr.sortingOrder = order;
            map[id] = sr;
            return sr;
        }

        private void Hide(Dictionary<int, SpriteRenderer> map, int id)
        {
            if (!map.TryGetValue(id, out var sr)) return;
            sr.gameObject.SetActive(false);
            _spritePool.Push(sr);
            map.Remove(id);
        }

        private void Prune(Dictionary<int, SpriteRenderer> map)
        {
            _removeInt.Clear();
            foreach (var k in map.Keys) if (!_seenInt.Contains(k)) _removeInt.Add(k);
            foreach (var k in _removeInt) Hide(map, k);
        }

        // ---------- água do Jet-Esqui ----------
        private void DrawWater(RunSimulation sim, float camX, float halfW)
        {
            bool on = sim.Vehicle != null && sim.Vehicle.Kind == VehicleKind.SplashSki;
            _water.gameObject.SetActive(on);
            if (!on) return;
            float h = SplashSkiMotor.WaterY - sim.Config.Physics.FloorY;
            _water.transform.position = new Vector3(camX, sim.Config.Physics.FloorY + h * 0.5f, 0f);
            _water.transform.localScale = new Vector3(halfW * 2f + 2f, h, 1f);
        }

        // ---------- util ----------
        private void ClearAll()
        {
            foreach (var z in _zappers.Values) ReturnZapper(z);
            _zappers.Clear();
            foreach (var p in _pickups.Values) { p.gameObject.SetActive(false); _pickupPool.Push(p); }
            _pickups.Clear();
            foreach (var w in _workers.Values) { w.Root.gameObject.SetActive(false); _workerPool.Push(w); }
            _workers.Clear();
            _seenInt.Clear();
            Prune(_missiles);
            Prune(_warnings);
        }

        private SpriteRenderer NewSprite(string name, Sprite sprite, Color color, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
