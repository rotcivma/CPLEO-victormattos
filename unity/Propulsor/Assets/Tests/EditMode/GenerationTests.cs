using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Physics;

namespace Propulsor.Tests.EditMode
{
    /// <summary>O-01..O-05 da matriz + validador de trechos.</summary>
    public class GenerationTests
    {
        private static IEnumerable<TestCaseData> AllSegments() =>
            SegmentLibrary.CreateDefault().Select(s => new TestCaseData(s.Id).SetName("TrechoValido_" + s.Id));

        [TestCaseSource(nameof(AllSegments))] // O-03
        public void TodoTrecho_TemCaminhoLivre(string id)
        {
            var seg = SegmentLibrary.CreateDefault().First(s => s.Id == id);
            var r = PathValidator.Validate(seg, new PlayerPhysicsParams());
            Assert.IsTrue(r.Ok, r.Reason);
        }

        [Test]
        public void Validador_RejeitaParedeCompleta()
        {
            var wall = new SegmentDef("parede", 20f, 0f).Zap(10f, 1.5f, 3f, 90f).Zap(10f, 5.0f, 4f, 90f);
            Assert.IsFalse(PathValidator.Validate(wall, new PlayerPhysicsParams()).Ok);
        }

        [Test]
        public void Validador_RejeitaZigueZagueImpossivel()
        {
            // Abertura alta e logo em seguida abertura baixa: não dá tempo de descer a 20 u/s.
            var seg = new SegmentDef("zigue_impossivel", 20f, 0f)
                .Zap(8f, 2.0f, 4f, 90f).Zap(9.5f, 5.0f, 4f, 90f);
            Assert.IsFalse(PathValidator.Validate(seg, new PlayerPhysicsParams()).Ok);
        }

        [Test]
        public void Validador_RejeitaFeixesSemFaixaLivre()
        {
            var seg = new SegmentDef("feixes_cheios", 40f, 0f).Lasers(0.8f, 2.2f, 3.6f, 5.0f, 6.4f);
            Assert.IsFalse(PathValidator.Validate(seg, new PlayerPhysicsParams()).Ok);
        }

        [Test]
        public void Validador_RejeitaArcoNaMargem()
        {
            var seg = new SegmentDef("margem", 16f, 0f).Zap(1f, 3f, 2f, 90f);
            Assert.IsFalse(PathValidator.Validate(seg, new PlayerPhysicsParams()).Ok);
        }

        [Test] // O-01
        public void MesmaSemente_MesmaSequencia()
        {
            var a = Sequence(1234, 200);
            var b = Sequence(1234, 200);
            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreNotEqual(a, Sequence(999, 200));
        }

        [Test] // O-02
        public void SemRepeticao_NosUltimos3()
        {
            var seq = Sequence(42, 2000, distanceStep: 2f);
            for (int i = 1; i < seq.Count; i++)
                for (int k = 1; k <= SegmentSelector.NoRepeatWindow && i - k >= 0; k++)
                    Assert.AreNotEqual(seq[i - k], seq[i], $"repetição na posição {i}");
        }

        [Test]
        public void TrechoNaoApareceAntesDaDistanciaMinima()
        {
            var lib = SegmentLibrary.CreateDefault();
            var sel = new SegmentSelector(lib, new XorShiftRandom(5));
            for (int i = 0; i < 500; i++)
            {
                var s = sel.Next(50f);
                Assert.LessOrEqual(s.MinDistance, 50f, s.Id);
            }
        }

        [Test]
        public void DificuldadeAlta_FavoreceTrechosDificeis()
        {
            var lib = SegmentLibrary.CreateDefault();
            float AvgDifficulty(float d)
            {
                var sel = new SegmentSelector(lib, new XorShiftRandom(77));
                float sum = 0f;
                for (int i = 0; i < 3000; i++) sum += sel.Next(d).Difficulty;
                return sum / 3000f;
            }
            Assert.Greater(AvgDifficulty(3000f), AvgDifficulty(100f) + 0.15f);
        }

        [Test] // O-05
        public void TempoDeRastreio_DecresceComADistancia()
        {
            Assert.AreEqual(1.4f, Difficulty.MissileTrackTime(0f), 1e-4);
            Assert.AreEqual(0.6f, Difficulty.MissileTrackTime(3000f), 1e-4);
            Assert.AreEqual(0.6f, Difficulty.MissileTrackTime(9000f), 1e-4);
            float prev = float.MaxValue;
            for (float d = 0; d <= 3000; d += 100)
            {
                float t = Difficulty.MissileTrackTime(d);
                Assert.LessOrEqual(t, prev);
                prev = t;
            }
            Assert.AreEqual(0f, Difficulty.BurstChance(1499f));
            Assert.Greater(Difficulty.BurstChance(3000f), 0f);
        }

        [Test]
        public void XorShift_FaixasCorretas()
        {
            var r = new XorShiftRandom(0);
            for (int i = 0; i < 10000; i++)
            {
                float f = r.NextFloat();
                Assert.That(f, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
                int n = r.Range(3, 7);
                Assert.That(n, Is.InRange(3, 6));
            }
        }

        private static List<string> Sequence(int seed, int count, float distanceStep = 30f)
        {
            var sel = new SegmentSelector(SegmentLibrary.CreateDefault(), new XorShiftRandom(seed));
            var list = new List<string>();
            for (int i = 0; i < count; i++) list.Add(sel.Next(i * distanceStep).Id);
            return list;
        }
    }
}

namespace Propulsor.Tests.EditMode
{
    public class SafeStartTests
    {
        [Test] // v2: início sem perigos
        public void Primeiros150m_SoTrechosSemPerigo()
        {
            var s = new Propulsor.Core.Gameplay.RunSimulation(new Propulsor.Core.Gameplay.RunConfig(), 9);
            foreach (var seg in s.Segments)
            {
                if (seg.StartX >= 150f) continue;
                Assert.AreEqual(0, seg.Def.Zappers.Count, seg.Def.Id);
                Assert.IsFalse(seg.Def.HasLasers, seg.Def.Id);
            }
        }

        [Test]
        public void SemTrechoSeguro_UsaQualquerUm()
        {
            var only = new System.Collections.Generic.List<Propulsor.Core.Gameplay.SegmentDef>
                { new Propulsor.Core.Gameplay.SegmentDef("parede", 20f, 0f).Zap(10f, 3.5f, 7f, 90f) };
            var sel = new Propulsor.Core.Gameplay.SegmentSelector(only, new Propulsor.Core.Gameplay.XorShiftRandom(1));
            Assert.AreEqual("parede", sel.Next(0f, safeOnly: true).Id);
        }
    }
}
