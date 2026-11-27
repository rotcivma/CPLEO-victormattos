using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Propulsor.Bootstrap;
using Propulsor.Core.Flow;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Save;
using Propulsor.Gameplay;
using Propulsor.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Propulsor.Tests.PlayMode
{
    /// <summary>N-01..N-04 dentro do Unity: telas certas em cada estado.</summary>
    public class MenuFlowTests
    {
        private sealed class NoInput : IThrustInput
        {
            public bool IsHeld => false;
            public bool PressedThisFrame => false;
        }

        private FlowController _flow;
        private UIManager _ui;

        private static RunConfig Safe() => new RunConfig
        {
            Segments = new List<SegmentDef> { new SegmentDef("vazio", 30f, 0f) },
            MissilesEnabled = false
        };

        [SetUp]
        public void SetUp()
        {
            PrototypeBootstrap.AutoBuildEnabled = false;
            // Salvamento em memória: os testes nunca tocam no perfil real do jogador.
            _flow = PrototypeBootstrap.BuildGame(new NoInput(), null, Safe, new MemorySaveStorage());
            _ui = Object.FindAnyObjectByType<UIManager>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var t in new[] { typeof(UIManager), typeof(RunDriver), typeof(CorridorView), typeof(WorldView),
                                      typeof(PlayerView), typeof(CameraFollow) })
                foreach (var o in Object.FindObjectsByType(t, FindObjectsSortMode.None))
                    Object.Destroy(((Component)o).gameObject);
        }

        private IEnumerator WaitFor(GameState s, float timeout = 6f)
        {
            float t = 0f;
            while (_flow.Flow.State != s && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(s, _flow.Flow.State);
        }

        [UnityTest] // N-01
        public IEnumerator Splash_Carregamento_Menu_ComTelasCertas()
        {
            Assert.IsTrue(_ui.Splash.Visible);
            yield return WaitFor(GameState.Loading);
            Assert.IsTrue(_ui.Loading.Visible);
            yield return WaitFor(GameState.MainMenu);
            Assert.IsTrue(_ui.MainMenu.Visible);
            Assert.IsFalse(_ui.Hud.Visible);
        }

        [UnityTest] // N-02 + N-03
        public IEnumerator Menu_Corrida_Pausa_Continuar()
        {
            _flow.SkipSplash();
            yield return WaitFor(GameState.MainMenu);
            float x0 = _flow.Driver.Sim.Motor.X;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(x0, _flow.Driver.Sim.Motor.X, "no menu a corrida está parada");

            _flow.StartRun();
            yield return WaitFor(GameState.Running);
            Assert.IsTrue(_ui.Hud.Visible);
            Assert.Greater(_flow.Driver.Sim.Motor.X, x0);

            _flow.Pause();
            Assert.IsTrue(_ui.PauseMenu.Visible);
            float xp = _flow.Driver.Sim.Motor.X;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(xp, _flow.Driver.Sim.Motor.X, "pausado não anda");

            _flow.Resume();
            Assert.AreEqual(GameState.Countdown, _flow.Flow.State);
            yield return WaitFor(GameState.Running);
        }

        [UnityTest] // N-04
        public IEnumerator Morte_ComFichas_Garra_Resultados_DeNovo()
        {
            _flow.Profile.BankedTokens = 2;
            _flow.SkipSplash();
            yield return WaitFor(GameState.MainMenu);
            _flow.StartRun();
            yield return WaitFor(GameState.Running);
            _flow.Driver.Kill();
            yield return WaitFor(GameState.ClawMachine);
            Assert.IsTrue(_ui.ClawScreen.Visible);
            Assert.AreEqual(2, _flow.Claw.Tokens);
            _flow.CashOutClaw();
            _flow.FinishClaw(false);
            yield return WaitFor(GameState.Results);
            Assert.IsTrue(_ui.Results.Visible);
            Assert.AreEqual(200, _flow.LastSummary.ClawCoins);
            var sum = _flow.LastSummary;
            Assert.AreEqual(sum.BalanceBefore + sum.RunCoins + sum.ClawCoins + sum.RewardCoins, _flow.Profile.Coins);
            Assert.IsTrue(_flow.Profile.Achievements.Contains("primeira_saida"));

            _flow.PlayAgain();
            yield return WaitFor(GameState.Running);
            Assert.Less(_flow.Driver.Sim.Distance, 20f, "nova corrida começou do zero");
        }

        [UnityTest] // L-01 + aparência
        public IEnumerator Oficina_CompraEEquipaPropulsor()
        {
            _flow.SkipSplash();
            yield return WaitFor(GameState.MainMenu);
            _flow.Profile.Coins = 10000;
            Assert.AreEqual(Propulsor.Core.Economy.PurchaseResult.Ok, _flow.Buy("jet_bolhas"));
            Assert.AreEqual(Propulsor.Core.Economy.EquipResult.Ok, _flow.Equip("jet_bolhas"));
            Assert.AreEqual("jet_bolhas", _flow.Services.Inventory.EquippedIn("jetpack"));
            _ui.Workshop.Show();
            yield return null;
            Assert.IsTrue(_ui.Workshop.Visible);
        }

        [UnityTest]
        public IEnumerator Revive_VoltaParaACorrida()
        {
            _flow.Profile.SecondChances = 1;
            _flow.SkipSplash();
            yield return WaitFor(GameState.MainMenu);
            _flow.StartRun();
            yield return WaitFor(GameState.Running);
            _flow.Driver.Kill();
            yield return WaitFor(GameState.ReviveOffer);
            Assert.IsTrue(_ui.Revive.Visible);
            _flow.AcceptRevive();
            Assert.AreEqual(GameState.Running, _flow.Flow.State);
            Assert.IsFalse(_flow.Driver.Sim.IsDead);
            Assert.AreEqual(0, _flow.Profile.SecondChances);
        }
    }
}
