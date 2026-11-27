using System.Collections;
using NUnit.Framework;
using Propulsor.Bootstrap;
using Propulsor.Core.Events;
using Propulsor.Core.Physics;
using Propulsor.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace Propulsor.Tests.PlayMode
{
    /// <summary>
    /// Testes de integração do protótipo (Fase 5) dentro do Unity.
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
        private PlayerView _player;

        [SetUp]
        public void SetUp()
        {
            PrototypeBootstrap.AutoBuildEnabled = false;
            _input = new FakeInput();
            _player = PrototypeBootstrap.Build(_input);
        }

        [TearDown]
        public void TearDown()
        {
            // Remove apenas o que o bootstrap criou (não destruir objetos do Test Runner).
            if (_player != null) Object.Destroy(_player.gameObject);
            foreach (var c in Object.FindObjectsByType<CorridorView>(FindObjectsSortMode.None)) Object.Destroy(c.gameObject);
            foreach (var h in Object.FindObjectsByType<DebugHud>(FindObjectsSortMode.None)) Object.Destroy(h.gameObject);
            foreach (var f in Object.FindObjectsByType<CameraFollow>(FindObjectsSortMode.None)) Object.Destroy(f.gameObject);
        }

        [UnityTest]
        public IEnumerator SegurarInput_LevaJogadorAoTeto()
        {
            _input.Held = true;
            yield return new WaitForSeconds(2f);
            var p = _player.Motor.Parameters;
            Assert.AreEqual(p.MaxY, _player.Motor.Y, 1e-3);
            Assert.AreEqual(p.MaxY, _player.transform.position.y, 0.05f);
        }

        [UnityTest]
        public IEnumerator SoltarInput_JogadorVoltaAoChao()
        {
            _input.Held = true;
            yield return new WaitForSeconds(1f);
            _input.Held = false;
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(MotorState.Grounded, _player.Motor.State);
            Assert.AreEqual(0f, _player.transform.position.y, 0.05f);
        }

        [UnityTest]
        public IEnumerator Camera_MantemJogadorA22PorCentoDaLargura()
        {
            yield return new WaitForSeconds(1f);
            var cam = Camera.main;
            float vx = cam.WorldToViewportPoint(_player.transform.position).x;
            Assert.AreEqual(0.22f, vx, 0.01f);
        }

        [UnityTest]
        public IEnumerator Morte_PublicaPlayerStoppedEPara()
        {
            bool stopped = false;
            _player.Bus.Subscribe<PlayerStopped>(_ => stopped = true);
            yield return new WaitForSeconds(0.5f);
            _player.Kill();
            float t = 0f;
            while (!stopped && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(stopped);
            Assert.AreEqual(MotorState.DeadStopped, _player.Motor.State);
        }

        [UnityTest]
        public IEnumerator Restart_ZeraDistancia()
        {
            yield return new WaitForSeconds(1f);
            Assert.Greater(_player.Motor.Distance, 5f);
            _player.Restart();
            Assert.AreEqual(0f, _player.Motor.Distance, 1e-4);
            Assert.AreEqual(MotorState.Grounded, _player.Motor.State);
        }
    }
}
