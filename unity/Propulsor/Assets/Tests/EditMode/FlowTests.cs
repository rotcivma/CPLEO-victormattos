using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Propulsor.Core.Events;
using Propulsor.Core.Flow;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Physics;

namespace Propulsor.Tests.EditMode
{
    /// <summary>N-01..N-04 (lógica de navegação), G-01, G-02, R-01 parcial.</summary>
    public class GameFlowTests
    {
        private static GameFlow AtMenu(int revives = 0, int tokens = 0)
        {
            var f = new GameFlow { RevivesAvailable = () => revives, TokensAvailable = () => tokens };
            Tick(f, GameFlow.SplashTime + GameFlow.LoadingMinTime + 0.2f);
            Assert.AreEqual(GameState.MainMenu, f.State);
            return f;
        }

        private static void Tick(GameFlow f, float seconds)
        {
            for (float t = 0; t < seconds; t += 0.02f) f.Tick(0.02f);
        }

        private static GameFlow Running(int revives = 0, int tokens = 0)
        {
            var f = AtMenu(revives, tokens);
            f.StartRun();
            Tick(f, GameFlow.IntroTime + 0.05f);
            Assert.AreEqual(GameState.Running, f.State);
            return f;
        }

        [Test] // N-01
        public void Splash_Carregamento_Menu()
        {
            var bus = new EventBus();
            var seen = new List<GameState>();
            bus.Subscribe<GameStateChanged>(e => seen.Add(e.To));
            var f = new GameFlow(bus);
            Assert.AreEqual(GameState.Splash, f.State);
            Tick(f, GameFlow.SplashTime + 0.05f);
            Assert.AreEqual(GameState.Loading, f.State);
            Tick(f, GameFlow.LoadingMinTime + 0.05f);
            Assert.AreEqual(GameState.MainMenu, f.State);
            CollectionAssert.AreEqual(new[] { GameState.Loading, GameState.MainMenu }, seen);
        }

        [Test]
        public void Splash_ToquePula()
        {
            var f = new GameFlow();
            f.SkipSplash();
            Assert.AreEqual(GameState.Loading, f.State);
        }

        [Test]
        public void Carregamento_EsperaTerminarDeCarregar()
        {
            bool loaded = false;
            var f = new GameFlow { IsLoaded = () => loaded };
            Tick(f, 5f);
            Assert.AreEqual(GameState.Loading, f.State);
            loaded = true;
            Tick(f, 0.05f);
            Assert.AreEqual(GameState.MainMenu, f.State);
        }

        [Test] // N-02
        public void ToqueNoMenu_IniciaCorrida()
        {
            var f = AtMenu();
            f.StartRun();
            Assert.AreEqual(GameState.Intro, f.State);
            Assert.IsTrue(f.SimulationRuns);
            Assert.IsFalse(f.PlayerControls, "sem controle durante a saída pela porta");
            Tick(f, GameFlow.IntroTime + 0.05f);
            Assert.AreEqual(GameState.Running, f.State);
            Assert.IsTrue(f.PlayerControls);
        }

        [Test] // N-03
        public void Pausa_ContinuarComContagem()
        {
            var f = Running();
            f.Pause();
            Assert.AreEqual(GameState.Paused, f.State);
            Assert.IsFalse(f.SimulationRuns);
            f.Resume();
            Assert.AreEqual(GameState.Countdown, f.State);
            Assert.IsFalse(f.SimulationRuns);
            Assert.Greater(f.Remaining, 0.8f);
            Tick(f, GameFlow.CountdownTime + 0.05f);
            Assert.AreEqual(GameState.Running, f.State);
        }

        [Test]
        public void ComandosForaDeHora_SaoIgnorados()
        {
            var f = AtMenu();
            f.Pause(); f.Resume(); f.PlayerDied(); f.PlayAgain(); f.AcceptRevive();
            Assert.AreEqual(GameState.MainMenu, f.State);
        }

        [Test] // N-04: sem revive e sem fichas
        public void Morte_SemNada_VaiParaResultados()
        {
            var f = Running();
            f.PlayerDied();
            Assert.AreEqual(GameState.Dying, f.State);
            Assert.IsTrue(f.SimulationRuns, "o corpo continua deslizando");
            f.PlayerStopped();
            Assert.AreEqual(GameState.Results, f.State);
        }

        [Test] // N-04: com fichas
        public void Morte_ComFichas_VaiParaGarra()
        {
            var f = Running(tokens: 2);
            f.PlayerDied(); f.PlayerStopped();
            Assert.AreEqual(GameState.ClawMachine, f.State);
            f.ClawFinished(false);
            Assert.AreEqual(GameState.Results, f.State);
        }

        [Test] // N-04: Segunda Chance ganha na garra
        public void Garra_SegundaChance_VoltaACorrida()
        {
            int revived = 0;
            var f = Running(tokens: 1);
            f.RequestRevive = () => revived++;
            f.PlayerDied(); f.PlayerStopped();
            f.ClawFinished(true);
            Assert.AreEqual(GameState.Running, f.State);
            Assert.AreEqual(1, revived);
        }

        [Test] // N-04: com revive
        public void Morte_ComRevive_Oferece_EAceitar_Revive()
        {
            int revives = 1, revived = 0;
            var f = AtMenu();
            f.RevivesAvailable = () => revives;
            f.ConsumeRevive = () => revives--;
            f.RequestRevive = () => revived++;
            f.StartRun(); Tick(f, 1f);
            f.PlayerDied(); f.PlayerStopped();
            Assert.AreEqual(GameState.ReviveOffer, f.State);
            f.AcceptRevive();
            Assert.AreEqual(GameState.Running, f.State);
            Assert.AreEqual(0, revives);
            Assert.AreEqual(1, revived);
        }

        [Test]
        public void OfertaDeRevive_ExpiraEm4s()
        {
            var f = Running(revives: 1, tokens: 1);
            f.PlayerDied(); f.PlayerStopped();
            Assert.AreEqual(GameState.ReviveOffer, f.State);
            Tick(f, GameFlow.ReviveOfferTime + 0.05f);
            Assert.AreEqual(GameState.ClawMachine, f.State, "recusa automática segue para a garra");
        }

        [Test]
        public void RecusarRevive_NaoOfereceDeNovoNaMesmaMorte()
        {
            var f = Running(revives: 3);
            f.PlayerDied(); f.PlayerStopped();
            f.DeclineRevive();
            Assert.AreEqual(GameState.Results, f.State);
        }

        [Test]
        public void Desistir_NaoOfereceRevive()
        {
            var f = Running(revives: 2, tokens: 0);
            f.Pause();
            f.GiveUp();
            Assert.AreEqual(GameState.Results, f.State);
        }

        [Test] // R-01 (fluxo)
        public void Resultados_DeNovoEMenu_PedemNovaCorrida()
        {
            int newRuns = 0;
            var f = Running();
            f.RequestNewRun = () => newRuns++;
            f.PlayerDied(); f.PlayerStopped();
            f.PlayAgain();
            Assert.AreEqual(GameState.Intro, f.State);
            Tick(f, 1f);
            f.PlayerDied(); f.PlayerStopped();
            f.BackToMenu();
            Assert.AreEqual(GameState.MainMenu, f.State);
            Assert.AreEqual(2, newRuns);
        }

        [Test]
        public void SegundaMorteAposRevive_OfereceDeNovoSeHouverConsumivel()
        {
            int revives = 2;
            var f = AtMenu();
            f.RevivesAvailable = () => revives;
            f.ConsumeRevive = () => revives--;
            f.StartRun(); Tick(f, 1f);
            f.PlayerDied(); f.PlayerStopped(); f.AcceptRevive();
            f.PlayerDied(); f.PlayerStopped();
            Assert.AreEqual(GameState.ReviveOffer, f.State);
        }
    }

    public class ClawMachineTests
    {
        [Test] // G-01
        public void UmaFicha_UmaTentativa_EFrequenciasSeguemOsPesos()
        {
            const int n = 100000;
            var counts = new Dictionary<string, int>();
            var rng = new XorShiftRandom(123);
            for (int i = 0; i < n; i++)
            {
                var m = new ClawMachine(1, rng);
                var p = m.Play();
                Assert.AreEqual(0, m.Tokens - (p.Kind == PrizeKind.ExtraTokens ? p.Value : 0));
                counts.TryGetValue(p.ToString(), out var c);
                counts[p.ToString()] = c + 1;
            }
            int total = PrizeTable.Default.Sum(e => e.Weight);
            foreach (var e in PrizeTable.Default)
            {
                double expected = (double)e.Weight / total;
                double got = counts.TryGetValue(e.ToString(), out var c) ? (double)c / n : 0;
                Assert.AreEqual(expected, got, 0.01, e.ToString());
            }
        }

        [Test] // G-02
        public void TrocaDeFichas_100PorcasCada()
        {
            var m = new ClawMachine(4, new XorShiftRandom(1));
            Assert.AreEqual(400, m.CashOut());
            Assert.AreEqual(0, m.Tokens);
            Assert.AreEqual(400, m.TakeCoins());
            Assert.AreEqual(0, m.CoinsWon);
        }

        [Test]
        public void SemFichas_NaoJoga()
        {
            var m = new ClawMachine(0, new XorShiftRandom(1));
            Assert.IsFalse(m.CanPlay);
            Assert.Throws<System.InvalidOperationException>(() => m.Play());
        }

        [Test]
        public void PremiosAplicamEfeitos()
        {
            var table = new[] { new PrizeEntry(PrizeKind.ExtraTokens, 1, 3) };
            var m = new ClawMachine(1, new XorShiftRandom(1), table);
            m.Play();
            Assert.AreEqual(3, m.Tokens);

            var coins = new ClawMachine(2, new XorShiftRandom(1), new[] { new PrizeEntry(PrizeKind.Coins, 1, 500) });
            coins.Play(); coins.Play();
            Assert.AreEqual(1000, coins.CoinsWon);

            var boosts = new ClawMachine(2, new XorShiftRandom(1),
                new[] { new PrizeEntry(PrizeKind.HeadStart, 1, 750), new PrizeEntry(PrizeKind.DoubleCoins, 1, 2) });
            while (boosts.CanPlay) boosts.Play();
            Assert.IsTrue(boosts.HeadStartWon || boosts.DoubleCoinsWon);
        }

        [Test]
        public void GarantiaNaUltimaFicha_NuncaPerde()
        {
            var rng = new XorShiftRandom(5);
            for (int i = 0; i < 2000; i++)
            {
                var m = new ClawMachine(1, rng) { GuaranteeLastToken = true };
                Assert.AreNotEqual(PrizeKind.Nothing, m.Play().Kind);
            }
        }

        [Test]
        public void PerdasSaoContadas()
        {
            var m = new ClawMachine(5, new XorShiftRandom(1), new[] { new PrizeEntry(PrizeKind.Nothing, 1) });
            while (m.CanPlay) m.Play();
            Assert.AreEqual(5, m.Losses);
            Assert.AreEqual(5, m.Plays);
        }
    }

    public class PopupQueueTests
    {
        [Test]
        public void UmPorVez_NaOrdem()
        {
            var q = new PopupQueue();
            q.Push("a"); q.Push("b"); q.Push("");
            Assert.AreEqual("a", q.Current);
            Assert.AreEqual(1, q.Pending);
            q.Tick(PopupQueue.InTime * 0.5f);
            Assert.AreEqual(0.5f, q.Visibility, 1e-3);
            q.Tick(PopupQueue.InTime);
            Assert.AreEqual(1f, q.Visibility);
            q.Tick(PopupQueue.Total);
            Assert.AreEqual("b", q.Current);
            q.Tick(PopupQueue.Total + 0.01f);
            Assert.IsNull(q.Current);
            Assert.AreEqual(0f, q.Visibility);
        }
    }

    public class ProfileAndReviveTests
    {
        [Test]
        public void AplicarCorrida_AtualizaRecordeESaldo()
        {
            var p = new SessionProfile { Coins = 100, BestDistance = 500 };
            var r = p.ApplyRun(612.7f, 50, 200);
            Assert.AreEqual(612, r.Distance);
            Assert.IsTrue(r.IsNewBest);
            Assert.AreEqual(500, r.PreviousBest);
            Assert.AreEqual(350, p.Coins);
            Assert.AreEqual(100, r.BalanceBefore);
            Assert.AreEqual(350, r.BalanceAfter);
            Assert.IsFalse(p.ApplyRun(10f, 0, 0).IsNewBest);
        }

        [Test]
        public void Revive_VoltaAoPontoDaMorteInvulneravel()
        {
            var cfg = new RunConfig
            {
                Segments = new List<SegmentDef> { new SegmentDef("vazio", 30f, 0f) },
                MissilesEnabled = false
            };
            var s = new RunSimulation(cfg, 1);
            for (int i = 0; i < 100; i++) s.Step(0.02f, false, false);
            float speed = s.Motor.VelocityX;
            s.Kill(HazardType.Laser);
            for (int i = 0; i < 500 && !s.IsOver; i++) s.Step(0.02f, false, false);
            float x = s.Motor.X;
            bool revivedEvent = false;
            s.Bus.Subscribe<PlayerRevived>(_ => revivedEvent = true);
            s.Revive();
            Assert.IsTrue(revivedEvent);
            Assert.IsFalse(s.IsDead);
            Assert.IsTrue(s.IsInvulnerable);
            Assert.AreEqual(x, s.Motor.X, 1e-4);
            Assert.AreEqual(speed, s.Motor.VelocityX, 0.01f);
            Assert.IsNull(s.DeathCause);
            s.Step(0.02f, false, false);
            Assert.Greater(s.Motor.X, x);
        }

        [Test]
        public void Revive_ComJogadorVivo_NaoFazNada()
        {
            var s = new RunSimulation(new RunConfig { MissilesEnabled = false }, 1);
            s.Revive();
            Assert.IsFalse(s.IsInvulnerable);
            Assert.AreEqual(MotorState.Grounded, s.Motor.State);
        }
    }
}
