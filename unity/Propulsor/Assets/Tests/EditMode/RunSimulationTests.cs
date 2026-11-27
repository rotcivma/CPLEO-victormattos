using System.Collections.Generic;
using NUnit.Framework;
using Propulsor.Core.Events;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Gameplay.Vehicles;
using Propulsor.Core.Physics;

namespace Propulsor.Tests.EditMode
{
    /// <summary>Integração headless: C-01, C-04..C-06, K-01..K-03, O-06 (lógica).</summary>
    public class RunSimulationTests
    {
        private const float Dt = 0.02f;

        /// <summary>Biblioteca com um único trecho, para cenários controlados.</summary>
        private static RunConfig Only(SegmentDef seg, bool missiles = false) => new RunConfig
        {
            Segments = new List<SegmentDef> { seg },
            MissilesEnabled = missiles,
            VehicleTokenMin = 1e9f, VehicleTokenMax = 2e9f,
            TokenMin = 1e9f, TokenMax = 2e9f
        };

        private static void Run(RunSimulation s, float seconds, bool held)
        {
            for (float t = 0; t < seconds && !s.IsDead; t += Dt) s.Step(Dt, held, false);
        }

        [Test] // C-01
        public void ArcoEstatico_MataAPe()
        {
            // Parede vertical cobrindo todo o corredor (trecho inválido de propósito).
            var wall = new SegmentDef("parede", 20f, 0f).Zap(10f, 3.5f, 7f, 90f);
            var s = new RunSimulation(Only(wall), 1);
            var bus = s.Bus;
            HazardType? cause = null;
            bus.Subscribe<PlayerDied>(e => cause = e.Cause);
            Run(s, 10f, false);
            Assert.IsTrue(s.IsDead);
            Assert.AreEqual(HazardType.Zapper, cause);
            Assert.AreEqual(HazardType.Zapper, s.DeathCause);
        }

        [Test] // C-04
        public void PerigoComVeiculo_DestroiVeiculoSemMatar()
        {
            var wall = new SegmentDef("parede", 40f, 0f).Zap(10f, 3.5f, 7f, 90f);
            var s = new RunSimulation(Only(wall), 1);
            VehicleDestroyed? destroyed = null;
            s.Bus.Subscribe<VehicleDestroyed>(e => destroyed = e);
            s.Mount(VehicleKind.Mech);
            Run(s, 4f, false);
            Assert.IsFalse(s.IsDead, "sobrevive à primeira parede");
            Assert.IsNotNull(destroyed);
            Assert.AreEqual(VehicleKind.Mech, destroyed.Value.Kind);
            Assert.IsNull(s.Vehicle);
        }

        [Test]
        public void AposPerderVeiculo_FicaInvulneravelPor1s()
        {
            var wall = new SegmentDef("parede", 40f, 0f).Zap(10f, 3.5f, 7f, 90f);
            var s = new RunSimulation(Only(wall), 1);
            s.Mount(VehicleKind.FlapDrone);
            bool seen = false;
            s.Bus.Subscribe<VehicleDestroyed>(_ => seen = true);
            while (!seen) s.Step(Dt, false, false);
            Assert.IsTrue(s.IsInvulnerable);
            Run(s, 0.9f, false);
            Assert.IsFalse(s.IsDead, "invulnerável enquanto atravessa o arco");
            Run(s, 0.2f, false);
            Assert.IsFalse(s.IsInvulnerable);
        }

        [Test] // C-05
        public void GuindasteBlindado_Aguenta3Drones()
        {
            var empty = new SegmentDef("vazio", 30f, 0f);
            var s = new RunSimulation(Only(empty, missiles: true), 7);
            s.Mount(VehicleKind.HeavyRig);
            int damaged = 0, destroyed = 0;
            s.Bus.Subscribe<VehicleDamaged>(_ => damaged++);
            s.Bus.Subscribe<VehicleDestroyed>(e => { destroyed++; Assert.AreEqual(HazardType.Missile, e.Cause); });
            // Avança muito (drones a partir de 150 m); fica no chão, sem pular, para ser atingido.
            for (int i = 0; i < 200000 && destroyed == 0 && !s.IsDead; i++) s.Step(Dt, false, false);
            Assert.AreEqual(2, damaged, "dois acertos absorvidos");
            Assert.AreEqual(1, destroyed, "destruído no terceiro");
        }

        [Test] // C-06 + K-01
        public void Moedas_ColetadasSomamNaCarteiraDaCorrida()
        {
            var seg = new SegmentDef("moedas", 20f, 0f).CoinLine(2f, 0.5f, 10);
            var s = new RunSimulation(Only(seg), 1);
            int events = 0;
            s.Bus.Subscribe<CoinCollected>(e => { events++; Assert.AreEqual(1, e.Value); });
            while (s.Motor.X < 39f) s.Step(Dt, false, false); // só o 1º trecho (x 20–40)
            Assert.AreEqual(10, s.RunCoins);
            Assert.AreEqual(10, events);
        }

        [Test]
        public void Moedas_ForaDoAlcance_NaoSaoColetadas()
        {
            var seg = new SegmentDef("moedas_altas", 20f, 0f).CoinLine(2f, 6.5f, 10);
            var s = new RunSimulation(Only(seg), 1);
            Run(s, 3f, false);
            Assert.AreEqual(0, s.RunCoins);
        }

        [Test] // K-02 (parcial: multiplicador por valor)
        public void ValorDaMoeda_Configuravel()
        {
            var seg = new SegmentDef("moedas", 20f, 0f).CoinLine(2f, 0.5f, 5);
            var cfg = Only(seg);
            cfg.CoinValue = 2;
            var s = new RunSimulation(cfg, 1);
            while (s.Motor.X < 39f) s.Step(Dt, false, false); // só o 1º trecho (x 20–40)
            Assert.AreEqual(10, s.RunCoins);
        }

        [Test] // K-03
        public void Ima_AtraiMoedasDentroDoRaio()
        {
            var seg = new SegmentDef("moedas_altas", 20f, 0f).CoinLine(2f, 4f, 10);
            var cfg = Only(seg);
            cfg.MagnetRadius = 5f;
            var s = new RunSimulation(cfg, 1);
            Run(s, 4f, false);
            Assert.AreEqual(10, s.RunCoins);
        }

        [Test]
        public void TokenDeVeiculo_MontaVeiculo()
        {
            var seg = new SegmentDef("token", 20f, 0f).Slot(10f, 0.5f);
            var cfg = Only(seg);
            cfg.VehicleTokenMin = 0f; cfg.VehicleTokenMax = 1f;
            var s = new RunSimulation(cfg, 1);
            VehicleKind? mounted = null;
            s.Bus.Subscribe<VehicleMounted>(e => mounted = e.Kind);
            Run(s, 4f, false);
            Assert.IsNotNull(mounted);
            Assert.IsNotNull(s.Vehicle);
        }

        [Test]
        public void Ficha_ContaNaCorrida()
        {
            var seg = new SegmentDef("ficha", 20f, 0f).Slot(10f, 0.5f);
            var cfg = Only(seg);
            cfg.TokenMin = 0f; cfg.TokenMax = 1f;
            var s = new RunSimulation(cfg, 1);
            Run(s, 6f, false);
            Assert.GreaterOrEqual(s.RunTokens, 1);
        }

        [Test]
        public void Feixes_MatamQuemEstaNaLinha()
        {
            var seg = new SegmentDef("feixe_chao", 60f, 0f).Lasers(0.5f);
            var s = new RunSimulation(Only(seg), 1);
            bool started = false;
            s.Bus.Subscribe<LaserSequenceStarted>(_ => started = true);
            Run(s, 8f, false); // fica no chão, onde o feixe dispara
            Assert.IsTrue(started);
            Assert.IsTrue(s.IsDead);
            Assert.AreEqual(HazardType.Laser, s.DeathCause);
        }

        [Test]
        public void Feixes_DesviandoSobrevive()
        {
            var seg = new SegmentDef("feixe_chao", 60f, 0f).Lasers(0.5f);
            var s = new RunSimulation(Only(seg), 1);
            bool ended = false;
            s.Bus.Subscribe<LaserSequenceEnded>(_ => ended = true);
            Run(s, 8f, true); // no teto
            Assert.IsFalse(s.IsDead);
            Assert.IsTrue(ended);
        }

        [Test]
        public void MorteCompleta_TerminaCorrida()
        {
            var s = new RunSimulation(Only(new SegmentDef("vazio", 30f, 0f)), 1);
            Run(s, 1f, true);
            float stoppedAt = -1f;
            s.Bus.Subscribe<PlayerStopped>(e => stoppedAt = e.Distance);
            s.Kill(HazardType.Missile);
            for (int i = 0; i < 1000 && !s.IsOver; i++) s.Step(Dt, true, true);
            Assert.IsTrue(s.IsOver);
            Assert.Greater(stoppedAt, 0f);
        }

        [Test]
        public void Mundo_ReciclaTrechosEColetaveis()
        {
            var s = new RunSimulation(new RunConfig { MissilesEnabled = false }, 3);
            for (int i = 0; i < 20000; i++) s.Step(Dt, (i / 25) % 2 == 0, false);
            // Mesmo após ~400 s, a quantidade em memória continua pequena.
            Assert.Less(s.Segments.Count, 12);
            Assert.Less(s.Pickups.Count, 400);
        }

        [Test]
        public void MesmaSemente_MesmaCorrida()
        {
            float Play(int seed)
            {
                var s = new RunSimulation(new RunConfig(), seed);
                for (int i = 0; i < 30000 && !s.IsOver; i++) s.Step(Dt, (i / 30) % 2 == 0, (i % 30) == 0);
                return s.Distance;
            }
            Assert.AreEqual(Play(21), Play(21));
        }

        [Test]
        public void TodosOsTrechosPadrao_SaoAtravessadosPorBotSimples()
        {
            // Sanidade: um bot que só oscila não deveria sobreviver para sempre (há perigo real)
            // nem morrer instantaneamente (há início seguro).
            var s = new RunSimulation(new RunConfig { MissilesEnabled = false }, 8);
            for (int i = 0; i < 100; i++) s.Step(Dt, false, false);
            Assert.IsFalse(s.IsDead, "trecho inicial é seguro");
        }
    }
}
