using System.Collections.Generic;
using NUnit.Framework;
using Propulsor.Core.Physics;

namespace Propulsor.Tests.EditMode
{
    /// <summary>Casos F-01..F-05 da matriz de testes (docs/fase-4/04-plano-de-testes.md).</summary>
    public class PlayerMotorTests
    {
        private const float Dt = 0.02f;

        private static PlayerMotor NewMotor(out PlayerPhysicsParams p)
        {
            p = new PlayerPhysicsParams();
            return new PlayerMotor(p);
        }

        private static void Run(PlayerMotor m, float seconds, bool thrust)
        {
            int steps = (int)System.Math.Round(seconds / Dt);
            for (int i = 0; i < steps; i++) m.Step(Dt, thrust);
        }

        [Test] // F-01
        public void SemInput_CaiAtePararNoChao()
        {
            var m = NewMotor(out var p);
            m.Reset(0f, 3f);
            Run(m, 2f, thrust: false);
            Assert.AreEqual(p.FloorY, m.Y, 1e-5);
            Assert.AreEqual(0f, m.VelocityY, 1e-5);
            Assert.AreEqual(MotorState.Grounded, m.State);
        }

        [Test] // F-02
        public void InputContinuo_SobeAteOTetoEPara()
        {
            var m = NewMotor(out var p);
            Run(m, 3f, thrust: true);
            Assert.AreEqual(p.MaxY, m.Y, 1e-5);
            Assert.AreEqual(0f, m.VelocityY, 1e-5);
            Assert.AreEqual(MotorState.Thrusting, m.State);
        }

        [Test] // F-03
        public void VelocidadeVertical_NuncaExcedeLimites()
        {
            var m = NewMotor(out var p);
            var rng = new System.Random(42);
            for (int i = 0; i < 20000; i++)
            {
                m.Step(Dt, rng.NextDouble() < 0.5);
                Assert.That(m.VelocityY, Is.InRange(p.MaxFallSpeed, p.MaxRiseSpeed));
                Assert.That(m.Y, Is.InRange(p.FloorY, p.MaxY));
            }
        }

        [Test] // F-04
        public void VelocidadeHorizontal_CresceESatura()
        {
            var m = NewMotor(out var p);
            float prev = m.VelocityX;
            Assert.AreEqual(p.StartSpeed, prev, 1e-5);
            for (int i = 0; i < 20000; i++) // 400 s: tempo suficiente para saturar
            {
                m.Step(Dt, false);
                Assert.GreaterOrEqual(m.VelocityX, prev);
                Assert.LessOrEqual(m.VelocityX, p.MaxSpeed);
                prev = m.VelocityX;
            }
            Assert.AreEqual(p.MaxSpeed, m.VelocityX, 1e-4);
        }

        [TestCase(1f / 30f)]
        [TestCase(1f / 60f)]
        [TestCase(1f / 120f)]
        [TestCase(1f / 144f)]
        public void Framerate_NaoAlteraSimulacao(float frameDelta) // F-05
        {
            // Referência: passos fixos puros.
            var reference = new PlayerMotor(new PlayerPhysicsParams());
            var trace = BuildInputTrace();
            for (int i = 0; i < trace.Count; i++) reference.Step(Dt, trace[i]);

            // Mesmo roteiro de input, mas alimentado por quadros de duração variável.
            var m = new PlayerMotor(new PlayerPhysicsParams());
            var stepper = new FixedStepper(Dt, maxStepsPerAdvance: 100);
            int stepIndex = 0;
            float total = trace.Count * Dt;
            float elapsed = 0f;
            while (stepIndex < trace.Count && elapsed < total + 1f)
            {
                stepper.Advance(frameDelta, dt => { m.Step(dt, trace[stepIndex]); stepIndex++; });
                elapsed += frameDelta;
                if (stepIndex >= trace.Count) break;
            }

            Assert.AreEqual(trace.Count, stepIndex);
            Assert.AreEqual(reference.X, m.X, reference.X * 0.01f);
            Assert.AreEqual(reference.Y, m.Y, 0.07f); // 1 % da altura do corredor
        }

        private static List<bool> BuildInputTrace()
        {
            var list = new List<bool>();
            var rng = new System.Random(7);
            bool held = false;
            for (int i = 0; i < 1500; i++)
            {
                if (rng.NextDouble() < 0.05) held = !held;
                list.Add(held);
            }
            return list;
        }

        [Test]
        public void Pousar_DisparaEventoLanded()
        {
            var m = NewMotor(out _);
            int landed = 0;
            m.Landed += () => landed++;
            Run(m, 0.5f, true);
            Run(m, 2f, false);
            Assert.AreEqual(1, landed);
        }

        [Test]
        public void StateChanged_InformaTransicoes()
        {
            var m = NewMotor(out _);
            var seen = new List<MotorState>();
            m.StateChanged += (_, to) => seen.Add(to);
            Run(m, 0.3f, true);
            Run(m, 2f, false);
            CollectionAssert.AreEqual(
                new[] { MotorState.Thrusting, MotorState.Falling, MotorState.Grounded }, seen);
        }

        [Test]
        public void Distancia_ComecaEmZeroEAcompanhaX()
        {
            var m = NewMotor(out var p);
            m.Reset(100f, 0f);
            Assert.AreEqual(0f, m.Distance, 1e-5);
            Run(m, 1f, false);
            Assert.AreEqual(p.StartSpeed, m.Distance, 0.05f);
        }

        [Test]
        public void Morte_QuicaDeslizaEPara()
        {
            var m = NewMotor(out var p);
            Run(m, 0.5f, true);
            int stopped = 0;
            m.Stopped += () => stopped++;
            m.Kill();
            Assert.AreEqual(MotorState.Dead, m.State);

            float maxYAfterKill = m.Y;
            float xAtKill = m.X;
            for (int i = 0; i < 1000 && m.State != MotorState.DeadStopped; i++)
            {
                m.Step(Dt, thrust: true); // input é ignorado quando morto
                maxYAfterKill = System.Math.Max(maxYAfterKill, m.Y);
            }

            Assert.AreEqual(MotorState.DeadStopped, m.State);
            Assert.AreEqual(1, stopped);
            Assert.AreEqual(p.FloorY, m.Y, 1e-5);
            Assert.AreEqual(0f, m.VelocityX);
            Assert.Greater(m.X, xAtKill, "o corpo continua avançando após a morte");

            float finalX = m.X;
            Run(m, 1f, true);
            Assert.AreEqual(finalX, m.X, "parado não se move mais");
            Assert.AreEqual(1, stopped, "Stopped dispara só uma vez");
        }

        [Test]
        public void Kill_DuasVezes_EhIgnorado()
        {
            var m = NewMotor(out _);
            m.Kill();
            float vy = m.VelocityY;
            m.Kill();
            Assert.AreEqual(vy, m.VelocityY);
        }
    }
}
