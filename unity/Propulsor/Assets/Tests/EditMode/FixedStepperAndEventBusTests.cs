using NUnit.Framework;
using Propulsor.Core.Events;
using Propulsor.Core.Physics;

namespace Propulsor.Tests.EditMode
{
    public class FixedStepperTests
    {
        [Test]
        public void Acumula_EExecutaPassosInteiros()
        {
            var s = new FixedStepper(0.02f);
            int calls = 0;
            Assert.AreEqual(0, s.Advance(0.015f, _ => calls++));
            Assert.AreEqual(1, s.Advance(0.015f, _ => calls++)); // 0,030 acumulado
            Assert.AreEqual(1, calls);
            Assert.AreEqual(0.5f, s.Alpha, 1e-3);
        }

        [Test]
        public void Travamento_LimitaPassosEDescartaExcesso()
        {
            var s = new FixedStepper(0.02f, maxStepsPerAdvance: 5);
            int calls = 0;
            Assert.AreEqual(5, s.Advance(10f, _ => calls++));
            Assert.AreEqual(0, s.Advance(0f, _ => calls++));
        }

        [Test]
        public void DeltaNegativo_EhIgnorado()
        {
            var s = new FixedStepper(0.02f);
            Assert.AreEqual(0, s.Advance(-1f, _ => { }));
            Assert.AreEqual(0f, s.Alpha);
        }
    }

    public class EventBusTests
    {
        [Test]
        public void Publicar_EntregaAosInscritosDoTipo()
        {
            var bus = new EventBus();
            float got = -1f;
            int other = 0;
            bus.Subscribe<DistanceChanged>(e => got = e.Meters);
            bus.Subscribe<PlayerDied>(_ => other++);
            bus.Publish(new DistanceChanged(12.5f));
            Assert.AreEqual(12.5f, got);
            Assert.AreEqual(0, other);
        }

        [Test]
        public void Dispose_CancelaInscricao()
        {
            var bus = new EventBus();
            int n = 0;
            var sub = bus.Subscribe<PlayerDied>(_ => n++);
            bus.Publish(new PlayerDied());
            sub.Dispose();
            sub.Dispose(); // idempotente
            bus.Publish(new PlayerDied());
            Assert.AreEqual(1, n);
            Assert.AreEqual(0, bus.CountSubscribers<PlayerDied>());
        }

        [Test]
        public void DesinscreverDurantePublicacao_NaoQuebra()
        {
            var bus = new EventBus();
            int n = 0;
            System.IDisposable sub = null;
            sub = bus.Subscribe<PlayerDied>(_ => { n++; sub.Dispose(); });
            bus.Subscribe<PlayerDied>(_ => n++);
            bus.Publish(new PlayerDied());
            bus.Publish(new PlayerDied());
            Assert.AreEqual(3, n);
        }

        [Test]
        public void SemInscritos_NaoFazNada()
        {
            Assert.DoesNotThrow(() => new EventBus().Publish(new PlayerLanded()));
        }
    }
}
