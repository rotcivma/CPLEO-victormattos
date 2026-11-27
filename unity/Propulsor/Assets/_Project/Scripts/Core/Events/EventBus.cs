using System;
using System.Collections.Generic;

namespace Propulsor.Core.Events
{
    /// <summary>
    /// Barramento de eventos tipado e síncrono. Os managers se comunicam por aqui
    /// em vez de se referenciarem diretamente (docs/fase-4/01-arquitetura.md).
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        /// <summary>Inscreve um ouvinte. Retorna um token que cancela a inscrição ao ser descartado.</summary>
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_handlers.TryGetValue(typeof(T), out var list))
                _handlers[typeof(T)] = list = new List<Delegate>();
            list.Add(handler);
            return new Subscription(() => list.Remove(handler));
        }

        /// <summary>Publica um evento para todos os ouvintes do tipo, na ordem de inscrição.</summary>
        public void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list) || list.Count == 0) return;
            // Cópia: permite (des)inscrição durante a publicação.
            var snapshot = list.ToArray();
            foreach (var d in snapshot) ((Action<T>)d)(evt);
        }

        public int CountSubscribers<T>() => _handlers.TryGetValue(typeof(T), out var l) ? l.Count : 0;

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;
            public Subscription(Action dispose) { _dispose = dispose; }
            public void Dispose() { _dispose?.Invoke(); _dispose = null; }
        }
    }
}
