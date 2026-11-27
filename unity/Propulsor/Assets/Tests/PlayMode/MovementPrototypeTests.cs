using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Propulsor.Bootstrap;
using Propulsor.Core.Events;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Physics;
using Propulsor.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace Propulsor.Tests.PlayMode
{
    /// <summary>
    /// Testes de integração dentro do Unity (Fases 5 e 6).
    /// Executar em Window ▸ General ▸ Test Runner ▸ PlayMode.
    /// </summary>
    public class MovementPrototypeTests
    {
        private sealed class FakeInput : IThrustInput
        {
            public bool Held;
            public bool IsHeld => Held;
            public bool PressedThisFrame => false;
        }

        private FakeInput _input;
        private RunDriver _driver;

        /// <summary>Corredor sem perigos, para testar só o movimento.</summary>
        private static RunConfig Safe() => new RunConfig
        {
            Segments = new List<SegmentDef> { new SegmentDef("vazio", 30f, 0f) },
            MissilesEnabled = false
        };

        [SetUp]
        public void SetUp()
        {
            PrototypeBootstrap.AutoBuildEnabled = false;
            _input = new FakeInput();
            _driver = PrototypeBootstrap.Build(_input, null, Safe);
        }

        [TearDown]
        public void TearDown()
        {
            // Remove apenas o que o bootstrap criou (não destruir objetos do Test Runner).
            if (_driver != null) Object.Destroy(_driver.gameObject);
            foreach (var c in Object.FindObjectsByType<CorridorView>(FindObjectsSortMode.None)) Object.Destroy(c.gameObject);
            foreach (var w in Object.FindObjectsByType<WorldView>(FindObjectsSortMode.None)) Object.Destroy(w.gameObject);
            foreach (var p in Object.FindObjectsByType<PlayerView>(FindObjectsSortMode.None)) Object.Destroy(p.gameObject);
            foreach (var h in Object.FindObjectsByType<DebugHud>(FindObjectsSortMode.None)) Object.Destroy(h.gameObject);
            foreach (var f in Object.FindObjectsByType<CameraFollow>(FindObjectsSortMode.None)) Object.Destroy(f.gameObject);
        }

        private Transform PlayerTransform => Object.FindAnyObjectByType<PlayerView>().transform;

        [UnityTest]
        public IEnumerator SegurarInput_LevaJogadorAoTeto()
        {
            _input.Held = true;
            yield return new WaitForSeconds(2f);
            var p = _driver.Motor.Parameters;
            Assert.AreEqual(p.MaxY, _driver.Motor.Y, 1e-3);
            Assert.AreEqual(p.MaxY, PlayerTransform.position.y, 0.05f);
        }

        [UnityTest]
        public IEnumerator SoltarInput_JogadorVoltaAoChao()
        {
            _input.Held = true;
            yield return new WaitForSeconds(1f);
            _input.Held = false;
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(MotorState.Grounded, _driver.Motor.State);
            Assert.AreEqual(0f, PlayerTransform.position.y, 0.05f);
        }

        [UnityTest]
        public IEnumerator Camera_MantemJogadorA22PorCentoDaLargura()
        {
            yield return new WaitForSeconds(1f);
            float vx = Camera.main.WorldToViewportPoint(PlayerTransform.position).x;
            Assert.AreEqual(0.22f, vx, 0.01f);
        }

        [UnityTest]
        public IEnumerator Morte_PublicaPlayerStoppedEPara()
        {
            bool stopped = false;
            _driver.Bus.Subscribe<PlayerStopped>(_ => stopped = true);
            yield return new WaitForSeconds(0.5f);
            _driver.Kill();
            float t = 0f;
            while (!stopped && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(stopped);
            Assert.AreEqual(MotorState.DeadStopped, _driver.Motor.State);
        }

        [UnityTest] // R-01 (parcial)
        public IEnumerator Restart_ZeraDistancia()
        {
            yield return new WaitForSeconds(1f);
            Assert.Greater(_driver.Motor.Distance, 5f);
            _driver.Restart();
            Assert.AreEqual(0f, _driver.Motor.Distance, 1e-4);
            Assert.AreEqual(MotorState.Grounded, _driver.Motor.State);
        }

        [UnityTest]
        public IEnumerator Veiculo_ApareceNaTela()
        {
            yield return null;
            _driver.Sim.Mount(Propulsor.Core.Gameplay.Vehicles.VehicleKind.FlapDrone);
            yield return null;
            yield return null;
            var vehicleGo = PlayerTransform.Find("Veiculo");
            Assert.IsNotNull(vehicleGo);
            Assert.IsTrue(vehicleGo.gameObject.activeSelf);
            Assert.IsFalse(PlayerTransform.Find("APe").gameObject.activeSelf);
        }
    }

    /// <summary>Corrida completa com o conteúdo padrão (perigos reais).</summary>
    public class FullRunTests
    {
        [UnityTest]
        public IEnumerator CorridaPadrao_RodaSemErrosEDesenhaPerigos()
        {
            PrototypeBootstrap.AutoBuildEnabled = false;
            var driver = PrototypeBootstrap.Build(new AlwaysOscillate());
            float t = 0f;
            int maxZapperObjects = 0;
            while (t < 15f && !driver.Sim.IsOver)
            {
                t += Time.deltaTime;
                maxZapperObjects = Mathf.Max(maxZapperObjects, GameObject.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Length);
                yield return null;
            }
            Assert.Greater(driver.Sim.Distance, 30f);
            Assert.Greater(maxZapperObjects, 20, "mundo desenhado (corredor, coletáveis, perigos)");
            Object.Destroy(driver.gameObject);
        }

        private sealed class AlwaysOscillate : IThrustInput
        {
            public bool IsHeld => Mathf.Repeat(Time.time, 0.8f) < 0.4f;
            public bool PressedThisFrame => false;
        }
    }
}
