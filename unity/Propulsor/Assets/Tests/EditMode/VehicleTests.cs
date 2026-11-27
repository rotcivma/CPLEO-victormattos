using NUnit.Framework;
using Propulsor.Core.Gameplay.Vehicles;
using Propulsor.Core.Physics;

namespace Propulsor.Tests.EditMode
{
    /// <summary>V-01..V-08: comportamento de controle de cada veículo.</summary>
    public class VehicleTests
    {
        private const float Dt = 0.02f;
        private static readonly PlayerPhysicsParams P = new PlayerPhysicsParams();

        private static T Mounted<T>(float y = 0f) where T : IVehicleMotor, new()
        {
            var v = new T();
            v.Mount(y, P);
            return v;
        }

        private static void Hold(IVehicleMotor v, float seconds, bool held)
        {
            for (float t = 0; t < seconds; t += Dt) v.Step(Dt, held, false);
        }

        private static void Tap(IVehicleMotor v) => v.Step(Dt, true, true);

        [Test] // V-01
        public void Mecha_SaltaNoToqueESegurarPlaneia()
        {
            var a = Mounted<MechMotor>();
            Tap(a);
            Assert.Greater(a.VelocityY, 10f);
            Hold(a, 3f, false);
            Assert.AreEqual(0f, a.Y, 1e-4);

            // tempo no ar sem segurar vs segurando
            float AirTime(bool glide)
            {
                var m = Mounted<MechMotor>();
                Tap(m);
                float t = Dt;
                while (m.Y > 0f && t < 10f) { m.Step(Dt, glide, false); t += Dt; }
                return t;
            }
            Assert.Greater(AirTime(true), AirTime(false) + 0.2f);
        }

        [Test]
        public void Mecha_NaoSaltaNoAr()
        {
            var a = Mounted<MechMotor>();
            Tap(a);
            Hold(a, 0.2f, false);
            float vy = a.VelocityY;
            Tap(a);
            Assert.Less(a.VelocityY, vy + 1f);
        }

        [Test] // V-02
        public void DroneAsa_SegurarSobeSoltarDesce()
        {
            var d = Mounted<FlapDroneMotor>();
            Hold(d, 1.5f, true);
            Assert.AreEqual(P.CeilingY - d.Height, d.Y, 1e-3);
            Hold(d, 2f, false);
            Assert.AreEqual(0f, d.Y, 1e-3);
        }

        [Test] // V-03
        public void CapsulaBlink_TeleportaNaDirecaoDoPonteiro()
        {
            var b = Mounted<BlinkPodMotor>(3f);
            // Avança até o ponteiro apontar para cima (seno positivo).
            while (b.PointerAngleDeg < 30f) b.Step(Dt, false, false);
            float y0 = b.Y;
            Tap(b);
            Assert.Greater(b.Y, y0 + 1f);
            Assert.AreEqual(1, b.Teleports);
            // Sem toque não se move.
            float y1 = b.Y;
            Hold(b, 1f, true);
            Assert.AreEqual(y1, b.Y);
        }

        [Test] // V-04
        public void MotoTrilho_RapidaBloqueiaDronesESalta()
        {
            var m = Mounted<RailBikeMotor>();
            Assert.IsTrue(m.BlocksMissiles);
            Assert.AreEqual(1.5f, m.SpeedMultiplier, 1e-3);
            Hold(m, 20f, false);
            Assert.AreEqual(2f, m.SpeedMultiplier, 1e-3);
            Tap(m);
            Assert.Greater(m.VelocityY, 10f);
        }

        [Test] // V-05
        public void SerpenteCeu_FlutuaNoTetoESegurarDesce()
        {
            var s = Mounted<SkySerpentMotor>(1f);
            Hold(s, 2f, false);
            Assert.Greater(s.Y, P.CeilingY - s.Height - 0.5f);
            Hold(s, 1f, true);
            Assert.Less(s.Y, 2f);
        }

        [Test] // V-06
        public void TrajeInverso_ToqueInverteGravidade()
        {
            var f = Mounted<FlipSuitMotor>();
            Assert.AreEqual(-1, f.GravitySign);
            Tap(f);
            Assert.AreEqual(1, f.GravitySign);
            Hold(f, 2f, false);
            Assert.AreEqual(P.CeilingY - f.Height, f.Y, 1e-3);
            Tap(f);
            Hold(f, 2f, false);
            Assert.AreEqual(0f, f.Y, 1e-3);
        }

        [Test]
        public void TrajeInverso_InversaoNoArPrecisaVencerMomento()
        {
            var f = Mounted<FlipSuitMotor>();
            Tap(f);
            Hold(f, 0.3f, false);       // subindo rápido
            float vy = f.VelocityY;
            Assert.Greater(vy, 5f);
            Tap(f);                     // inverte no ar
            Assert.Greater(f.VelocityY, 0f, "logo após inverter ainda sobe por inércia");
        }

        [Test] // V-07
        public void JetEsqui_MergulhaESaltaDaAgua()
        {
            var j = Mounted<SplashSkiMotor>(1f);
            Hold(j, 1f, true);
            Assert.IsTrue(j.InWater);
            Assert.Less(j.Y, 0.5f);
            float maxY = 0f;
            for (int i = 0; i < 100; i++) { j.Step(Dt, false, false); maxY = System.Math.Max(maxY, j.Y); }
            Assert.Greater(maxY, SplashSkiMotor.WaterY, "salta acima da superfície");
        }

        [Test]
        public void JetEsqui_ToqueNoArAceleraQueda()
        {
            float FallTime(bool held)
            {
                var j = Mounted<SplashSkiMotor>(5f);
                float t = 0f;
                while (!j.InWater && t < 5f) { j.Step(Dt, held, false); t += Dt; }
                return t;
            }
            Assert.Less(FallTime(true), FallTime(false));
        }

        [Test] // V-08
        public void GuindasteBlindado_Comeca_Com3PV()
        {
            var h = Mounted<HeavyRigMotor>();
            Assert.AreEqual(3, h.HitPoints);
        }

        [Test]
        public void Fabrica_CriaTodosOsTipos()
        {
            foreach (var k in VehicleFactory.All)
                Assert.AreEqual(k, VehicleFactory.Create(k).Kind);
            Assert.AreEqual(8, VehicleFactory.All.Length);
        }

        [Test]
        public void TodosOsVeiculos_FicamDentroDoCorredor()
        {
            var rng = new System.Random(4);
            foreach (var k in VehicleFactory.All)
            {
                var v = VehicleFactory.Create(k);
                v.Mount(3f, P);
                for (int i = 0; i < 5000; i++)
                {
                    bool held = rng.NextDouble() < 0.5;
                    v.Step(Dt, held, held && rng.NextDouble() < 0.2);
                    Assert.That(v.Y, Is.InRange(P.FloorY, P.CeilingY - v.Height + 1e-4f), k.ToString());
                }
            }
        }
    }
}
