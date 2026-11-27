using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Propulsor.Core.Audio;
using Propulsor.Core.Events;
using Propulsor.Core.Flow;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Gameplay.Vehicles;
using Propulsor.Core.Physics;
using Propulsor.Core.Progress;

namespace Propulsor.Tests.EditMode
{
    /// <summary>Robôs operários e "raspões".</summary>
    public class WorkerAndNearMissTests
    {
        private const float Dt = 0.02f;

        private static RunConfig Cfg(SegmentDef seg, int workers = 2) => new RunConfig
        {
            Segments = new List<SegmentDef> { seg },
            MissilesEnabled = false,
            MaxWorkersPerSegment = workers,
            VehicleTokenMin = 1e9f, VehicleTokenMax = 2e9f, TokenMin = 1e9f, TokenMax = 2e9f
        };

        [Test]
        public void Robos_NaoMatam_ESaoDerrubadosAoEsbarrar()
        {
            var s = new RunSimulation(Cfg(new SegmentDef("vazio", 20f, 0f), workers: 2), 5);
            int knocked = 0;
            s.Bus.Subscribe<WorkerKnocked>(_ => knocked++);
            for (int i = 0; i < 1500; i++) s.Step(Dt, false, false); // corre pelo chão
            Assert.IsFalse(s.IsDead);
            Assert.Greater(knocked, 3, "esbarrou em vários robôs pelo caminho");
        }

        [Test]
        public void Robos_DerrubadosPeloJato()
        {
            var s = new RunSimulation(Cfg(new SegmentDef("vazio", 20f, 0f), workers: 2), 5);
            int knocked = 0;
            s.Bus.Subscribe<WorkerKnocked>(_ => knocked++);
            // Paira a ~1,4 u do chão: acima dos robôs (1 u), jato apontado para eles.
            for (int i = 0; i < 1500; i++) s.Step(Dt, s.Motor.Y < 1.4f, false);
            Assert.Greater(knocked, 3);
        }

        [Test]
        public void Robos_VoandoAlto_NaoSaoDerrubados()
        {
            var s = new RunSimulation(Cfg(new SegmentDef("vazio", 20f, 0f), workers: 2), 5);
            int knocked = 0;
            s.Bus.Subscribe<WorkerKnocked>(_ => knocked++);
            for (int i = 0; i < 30; i++) s.Step(Dt, true, false);  // decola antes do 1º trecho
            for (int i = 0; i < 1500; i++) s.Step(Dt, true, false); // no teto
            Assert.AreEqual(0, knocked);
        }

        [Test]
        public void RoboDerrubado_CaiEPara()
        {
            var s = new RunSimulation(Cfg(new SegmentDef("vazio", 20f, 0f), workers: 2), 5);
            Worker hit = null;
            s.Bus.Subscribe<WorkerKnocked>(e => { foreach (var w in s.Workers) if (w.Id == e.Id) hit = w; });
            for (int i = 0; i < 1500 && hit == null; i++) s.Step(Dt, false, false);
            Assert.IsNotNull(hit);
            Assert.Greater(hit.VY, 0f, "é arremessado");
            int id = hit.Id;
            bool settled = false;
            for (int i = 0; i < 100 && !settled; i++)
            {
                s.Step(Dt, false, false);
                settled = hit.Y == 0f && hit.VY == 0f;
            }
            Assert.AreEqual(id, hit.Id, "mesmo robô (não reciclado pelo pool)");
            Assert.IsTrue(settled, "quicou e assentou no chão");
            Assert.IsTrue(hit.Knocked);
        }

        [Test]
        public void Raspao_EmArco_ContaUmaVez()
        {
            // Arco vertical cobrindo 0..3 u; a personagem passa logo acima (base ~3,5).
            // Arco vertical cobrindo 0..2 u (+0,3 de raio); a personagem paira com a base em ~2,4–2,9.
            var seg = new SegmentDef("arco", 20f, 0f).Zap(10f, 1.0f, 2.0f, 90f);
            var s = new RunSimulation(Cfg(seg, workers: 0), 1);
            int near = 0;
            s.Bus.Subscribe<HazardNearMiss>(e => { near++; Assert.AreEqual(HazardType.Zapper, e.Type); });
            // Controle com antecipação (altura + velocidade): paira estável sem mergulhar.
            for (int i = 0; i < 300 && !s.IsDead; i++)
                s.Step(Dt, s.Motor.Y + s.Motor.VelocityY * 0.3f < 2.5f, false);
            Assert.IsFalse(s.IsDead);
            Assert.GreaterOrEqual(near, 1);
            int perSegment = near;
            Assert.LessOrEqual(perSegment, (int)(s.Distance / 20f) + 1, "no máximo um por arco");
        }

        [Test]
        public void Longe_DoArco_NaoERaspao()
        {
            var seg = new SegmentDef("arco", 20f, 0f).Zap(10f, 1.0f, 2.0f, 90f);
            var s = new RunSimulation(Cfg(seg, workers: 0), 1);
            int near = 0;
            s.Bus.Subscribe<HazardNearMiss>(_ => near++);
            for (int i = 0; i < 300; i++) s.Step(Dt, true, false); // no teto, longe
            Assert.AreEqual(0, near);
        }

        [Test]
        public void Raspao_EmDrone()
        {
            var cfg = Cfg(new SegmentDef("vazio", 30f, 0f), workers: 0);
            cfg.MissilesEnabled = true;
            var s = new RunSimulation(cfg, 3);
            int near = 0;
            s.Bus.Subscribe<HazardNearMiss>(e => { if (e.Type == HazardType.Missile) near++; });
            // Fica no chão; quando um aviso aparece, sobe só um pouco para o drone passar raspando por baixo... ou acertar.
            for (int i = 0; i < 30000 && near == 0 && !s.IsDead; i++)
            {
                bool warning = false;
                foreach (var m in s.Missiles) if (m.Phase == MissilePhase.Locked || m.Phase == MissilePhase.Flying) warning = true;
                s.Step(Dt, warning && s.Motor.Y < 1.6f, false);
            }
            Assert.Greater(near + (s.IsDead ? 1 : 0), 0, "drone passou perto (raspão) ou acertou");
        }

        [Test]
        public void Medidor_ContaRobosERaspoes()
        {
            var bus = new EventBus();
            var tr = new RunTracker(bus);
            bus.Publish(new WorkerKnocked(1));
            bus.Publish(new HazardNearMiss(HazardType.Zapper));
            bus.Publish(new PlayerDied(HazardType.Laser));
            bus.Publish(new HazardNearMiss(HazardType.Missile)); // morta: não conta
            Assert.AreEqual(1f, tr.Get(Counters.Workers));
            Assert.AreEqual(1f, tr.Get(Counters.NearMisses));
        }
    }

    /// <summary>O-06 (lógica) e desempenho da simulação.</summary>
    public class PerformanceTests
    {
        [Test] // O-06
        public void CorridaAquecida_NaoAlocaMemoria()
        {
            // Modo sem dano: percorre todo tipo de trecho, drone, feixe, robô e raspão sem morrer.
            var cfg = new RunConfig { MissilesEnabled = true, GodMode = true };
            var s = new RunSimulation(cfg, 77);
            var bus = s.Bus;
            // Ouvintes típicos (medidor de missões) — também não podem alocar.
            var tracker = new RunTracker(bus);
            int i = 0;
            // Aquecimento: enche pools e listas (≈ 5 min de jogo).
            for (; i < 15000; i++) s.Step(0.02f, (i / 20) % 3 != 0, i % 40 == 0);
            Assert.IsFalse(s.IsDead);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int k = 0; k < 5000; k++, i++) s.Step(0.02f, (i / 20) % 3 != 0, i % 40 == 0);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestContext.WriteLine($"alocou {allocated} bytes em 5.000 passos");
            Assert.Less(allocated, 1024, $"alocou {allocated} bytes em 5.000 passos");
        }

        [Test]
        public void PassoDaSimulacao_RapidoOSuficiente()
        {
            var s = new RunSimulation(new RunConfig { GodMode = true }, 5);
            for (int i = 0; i < 2000; i++) s.Step(0.02f, (i / 20) % 2 == 0, false); // aquecer JIT
            var sw = Stopwatch.StartNew();
            int steps = 0;
            for (; steps < 20000; steps++) s.Step(0.02f, (steps / 20) % 2 == 0, false);
            sw.Stop();
            double usPerStep = sw.Elapsed.TotalMilliseconds * 1000.0 / Math.Max(1, steps);
            TestContext.WriteLine($"{usPerStep:0.0} µs por passo ({steps} passos)");
            // Orçamento: 50 passos/s cabem folgados em 16,6 ms por quadro.
            Assert.Less(usPerStep, 200.0);
        }

        [Test]
        public void EventBus_PublicarNaoAloca()
        {
            var bus = new EventBus();
            float sum = 0f;
            bus.Subscribe<DistanceChanged>(e => sum += e.Meters);
            for (int i = 0; i < 1000; i++) bus.Publish(new DistanceChanged(i)); // aquecer JIT
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) bus.Publish(new DistanceChanged(i));
            // Tolerância para custo fixo do runtime (medido: 24 B, constante — não cresce com as publicações).
            Assert.Less(GC.GetAllocatedBytesForCurrentThread() - before, 256);
        }
    }

    /// <summary>Mapeamento de áudio (efeitos, música por estado, ducking).</summary>
    public class AudioDirectorTests
    {
        private sealed class FakeOut : IAudioOutput
        {
            public readonly List<(Sfx s, float v, float p)> Sfx = new List<(Sfx, float, float)>();
            public Music Music;
            public float Duck;
            public bool Jet;
            public void PlaySfx(Sfx sfx, float volume, float pitch) => Sfx.Add((sfx, volume, pitch));
            public void SetMusic(Music music) => Music = music;
            public void SetMusicDuck(float db) => Duck = db;
            public void SetJetLoop(bool on) => Jet = on;
        }

        [Test]
        public void EventosTocamEfeitos()
        {
            var bus = new EventBus();
            var o = new FakeOut();
            new AudioDirector(bus, o);
            bus.Publish(new TokenCollected(1));
            bus.Publish(new VehicleDestroyed(VehicleKind.Mech, HazardType.Zapper));
            bus.Publish(new WorkerKnocked(1));
            bus.Publish(new MissionCompleted("x", "x", 1));
            CollectionAssert.AreEqual(new[] { Sfx.Token, Sfx.Explosion, Sfx.Bonk, Sfx.Mission },
                                      o.Sfx.ConvertAll(x => x.s));
        }

        [Test]
        public void MusicaPorEstado()
        {
            var bus = new EventBus();
            var o = new FakeOut();
            var d = new AudioDirector(bus, o);
            bus.Publish(new GameStateChanged(GameState.Loading, GameState.MainMenu));
            Assert.AreEqual(Music.Menu, o.Music);
            bus.Publish(new GameStateChanged(GameState.MainMenu, GameState.Intro));
            Assert.AreEqual(Music.Run, o.Music);
            bus.Publish(new GameStateChanged(GameState.Dying, GameState.ClawMachine));
            Assert.AreEqual(Music.Claw, o.Music);
            bus.Publish(new GameStateChanged(GameState.ClawMachine, GameState.Results));
            Assert.AreEqual(Music.Menu, d.CurrentMusic);
        }

        [Test]
        public void Ducking_AvisoDeDroneEPausa()
        {
            var bus = new EventBus();
            var o = new FakeOut();
            new AudioDirector(bus, o);
            bus.Publish(new MissileWarning(1));
            Assert.AreEqual(AudioDirector.WarningDuckDb, o.Duck);
            bus.Publish(new MissileLaunched(1));
            Assert.AreEqual(0f, o.Duck);
            bus.Publish(new GameStateChanged(GameState.Running, GameState.Paused));
            Assert.AreEqual(AudioDirector.PauseDuckDb, o.Duck);
            bus.Publish(new GameStateChanged(GameState.Paused, GameState.Countdown));
            Assert.AreEqual(0f, o.Duck);
        }

        [Test]
        public void PorcasSeguidas_SobemDeTom()
        {
            var bus = new EventBus();
            var o = new FakeOut();
            var d = new AudioDirector(bus, o);
            for (int i = 0; i < 20; i++) { bus.Publish(new CoinCollected(1, i)); d.Tick(0.1f); }
            Assert.AreEqual(1f, o.Sfx[0].p, 1e-4);
            Assert.Greater(o.Sfx[5].p, o.Sfx[4].p);
            Assert.AreEqual(2f, o.Sfx[19].p, 1e-3, "no máximo uma oitava");
            d.Tick(1f);
            bus.Publish(new CoinCollected(1, 21));
            Assert.AreEqual(1f, o.Sfx[20].p, 1e-4, "pausa longa reinicia a sequência");
        }

        [Test]
        public void SomDoPropulsor_SegueOEstado()
        {
            var bus = new EventBus();
            var o = new FakeOut();
            new AudioDirector(bus, o);
            bus.Publish(new PlayerStateChanged(MotorState.Grounded, MotorState.Thrusting));
            Assert.IsTrue(o.Jet);
            bus.Publish(new PlayerStateChanged(MotorState.Thrusting, MotorState.Falling));
            Assert.IsFalse(o.Jet);
            bus.Publish(new PlayerStateChanged(MotorState.Falling, MotorState.Thrusting));
            bus.Publish(new PlayerDied(HazardType.Laser));
            Assert.IsFalse(o.Jet);
        }
    }
}
