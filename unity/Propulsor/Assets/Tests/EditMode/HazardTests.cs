using NUnit.Framework;
using Propulsor.Core.Gameplay;

namespace Propulsor.Tests.EditMode
{
    public class HazardTests
    {
        [Test]
        public void Capsula_DetectaToqueEAusencia()
        {
            var box = new Aabb(0f, 0f, 1f, 1f);
            Assert.IsTrue(Geometry.CapsuleOverlapsAabb(-2f, 0.5f, 3f, 0.5f, 0.1f, box));     // atravessa
            Assert.IsTrue(Geometry.CapsuleOverlapsAabb(-2f, 1.25f, 3f, 1.25f, 0.3f, box));   // raspa pelo raio
            Assert.IsFalse(Geometry.CapsuleOverlapsAabb(-2f, 1.5f, 3f, 1.5f, 0.3f, box));
        }

        [Test] // C-02
        public void ArcoRotativo_ColisorAcompanhaRotacao()
        {
            // Arco horizontal de 4 u em (10, 3.5) girando 90°/s.
            var z = new ZapperDef(10f, 3.5f, 4f, 0f, 90f);
            var leftEnd = new Aabb(8f - 0.2f, 3.3f, 8f + 0.2f, 3.7f);   // ponta esquerda em t=0
            var topEnd = new Aabb(9.8f, 5.3f, 10.2f, 5.7f);             // ponta superior após 90°
            Assert.IsTrue(Hazards.ZapperHits(z, 0f, 0f, leftEnd));
            Assert.IsFalse(Hazards.ZapperHits(z, 0f, 0f, topEnd));
            Assert.IsFalse(Hazards.ZapperHits(z, 0f, 1f, leftEnd));
            Assert.IsTrue(Hazards.ZapperHits(z, 0f, 1f, topEnd));
        }

        [Test]
        public void Arco_UsaDeslocamentoDoTrecho()
        {
            var z = new ZapperDef(5f, 3f, 2f, 90f);
            var box = new Aabb(104.8f, 2.8f, 105.2f, 3.2f);
            Assert.IsTrue(Hazards.ZapperHits(z, 100f, 0f, box));
            Assert.IsFalse(Hazards.ZapperHits(z, 0f, 0f, box));
        }

        [Test] // C-03
        public void Feixe_SoMataDuranteODisparo()
        {
            var seq = new LaserSequence(new[] { new LaserWaveDef(3.5f) });
            var onRow = new Aabb(0f, 3.2f, 0.5f, 4.0f);
            var away = new Aabb(0f, 0.1f, 0.5f, 0.9f);

            Assert.AreEqual(LaserPhase.Entering, seq.Phase);
            Assert.IsFalse(seq.Hits(onRow));

            Step(seq, LaserSequence.EnterTime + 0.1f);
            Assert.AreEqual(LaserPhase.Warning, seq.Phase);
            Assert.IsFalse(seq.Hits(onRow));

            Step(seq, LaserSequence.WarnTime);
            Assert.AreEqual(LaserPhase.Firing, seq.Phase);
            Assert.IsTrue(seq.Hits(onRow));
            Assert.IsFalse(seq.Hits(away));

            Step(seq, LaserSequence.FireTime);
            Assert.AreEqual(LaserPhase.Leaving, seq.Phase);
            Assert.IsFalse(seq.Hits(onRow));

            Step(seq, LaserSequence.LeaveTime);
            Assert.IsTrue(seq.IsDone);
        }

        [Test]
        public void Feixe_PercorreTodasAsOndas()
        {
            var seq = new LaserSequence(new[] { new LaserWaveDef(1f), new LaserWaveDef(6f) });
            float total = LaserSequence.TotalDuration(2);
            int firingWaves = 0;
            int lastWave = -1;
            for (float t = 0; t < total + 0.1f; t += 0.02f)
            {
                seq.Step(0.02f);
                if (seq.IsLethal && seq.WaveIndex != lastWave) { firingWaves++; lastWave = seq.WaveIndex; }
            }
            Assert.AreEqual(2, firingWaves);
            Assert.IsTrue(seq.IsDone);
        }

        private static void Step(LaserSequence s, float seconds)
        {
            for (float t = 0; t < seconds - 1e-4f; t += 0.02f) s.Step(0.02f);
        }
    }

    public class MissileTests
    {
        private static void Run(MissileDirector d, float seconds, float distance, float playerY, bool blocked, ref float playerX)
        {
            for (float t = 0; t < seconds; t += 0.02f)
            {
                d.FollowWorld(10f * 0.02f);
                playerX += 10f * 0.02f;
                d.Step(0.02f, distance, playerX, playerY, blocked);
            }
        }

        [Test]
        public void NaoLancaAntesDaDistanciaMinima()
        {
            var d = new MissileDirector(new XorShiftRandom(1), 0f, 7f);
            float x = 0f;
            Run(d, 60f, 100f, 3f, false, ref x);
            Assert.AreEqual(0, d.Launched);
        }

        [Test]
        public void Lanca_ComAvisoAntes()
        {
            var d = new MissileDirector(new XorShiftRandom(1), 0f, 7f);
            int warnings = 0, launches = 0;
            d.WarningStarted = _ => warnings++;
            d.Launch = m =>
            {
                launches++;
                Assert.AreEqual(MissilePhase.Flying, m.Phase);
            };
            float x = 0f;
            Run(d, 30f, 200f, 3f, false, ref x);
            Assert.Greater(launches, 0);
            Assert.GreaterOrEqual(warnings, launches);
        }

        [Test] // O-04
        public void Bloqueado_NaoLancaECancelaAvisos()
        {
            var d = new MissileDirector(new XorShiftRandom(3), 0f, 7f);
            float x = 0f;
            Run(d, 120f, 2500f, 3f, blocked: true, ref x);
            Assert.AreEqual(0, d.Launched);
            Assert.AreEqual(0, d.Missiles.Count);
        }

        [Test]
        public void AvisoSegueAlturaDoJogador()
        {
            var d = new MissileDirector(new XorShiftRandom(9), 0f, 7f);
            float x = 0f;
            Missile seen = null;
            d.WarningStarted = m => seen = m;
            // espera surgir um aviso com o jogador em y=1
            for (int i = 0; i < 1000 && seen == null; i++) { d.Step(0.02f, 200f, x, 1f, false); }
            Assert.IsNotNull(seen);
            // jogador sobe para y=5; durante o rastreio o aviso acompanha
            for (int i = 0; i < 25 && seen.Phase == MissilePhase.Tracking; i++) d.Step(0.02f, 200f, x, 5f, false);
            Assert.Greater(seen.Y, 3f);
        }

        [Test]
        public void Drone_SaiPelaEsquerdaEDesaparece()
        {
            var d = new MissileDirector(new XorShiftRandom(2), 0f, 7f);
            float x = 0f;
            Run(d, 40f, 200f, 3f, false, ref x);
            foreach (var m in d.Missiles)
                if (m.Phase == MissilePhase.Flying)
                    Assert.Greater(m.X, x - MissileDirector.DespawnBehind - 1f);
        }

        [Test]
        public void Rajadas_SoDepoisDe1500m()
        {
            int MaxBatch(float distance)
            {
                var d = new MissileDirector(new XorShiftRandom(11), 0f, 7f);
                int max = 0;
                float x = 0f;
                for (int i = 0; i < 60000; i++)
                {
                    d.Step(0.02f, distance, x, 3f, false);
                    int pending = 0;
                    foreach (var m in d.Missiles) if (m.Phase != MissilePhase.Flying) pending++;
                    if (pending > max) max = pending;
                }
                return max;
            }
            Assert.AreEqual(1, MaxBatch(1000f));
            Assert.GreaterOrEqual(MaxBatch(3000f), 3);
        }
    }
}
