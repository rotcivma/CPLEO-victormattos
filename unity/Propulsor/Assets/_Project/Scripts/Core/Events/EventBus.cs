using System;
using System.Collections.Generic;

namespace Propulsor.Core.Events
{
    /// <summary>
    /// Barramento de eventos tipado e síncrono. Os managers se comunicam por aqui
    /// em vez de se referenciarem diretamente (docs/fase-4/01-arquitetura.md).
    /// Publicar não aloca memória: a lista de ouvintes é um array "copy-on-write",
    /// recriado só quando alguém entra ou sai.
    /// </summary>
    public sealed class EventBus
    {
        private sealed class Channel<T>
        {
            public Action<T>[] Handlers = Array.Empty<Action<T>>();

            public void Add(Action<T> h)
            {
                var a = new Action<T>[Handlers.Length + 1];
                Array.Copy(Handlers, a, Handlers.Length);
                a[Handlers.Length] = h;
                Handlers = a;
            }

            public void Remove(Action<T> h)
            {
                int i = Array.IndexOf(Handlers, h);
                if (i < 0) return;
                var a = new Action<T>[Handlers.Length - 1];
                Array.Copy(Handlers, 0, a, 0, i);
                Array.Copy(Handlers, i + 1, a, i, Handlers.Length - i - 1);
                Handlers = a;
            }
        }

        private readonly Dictionary<Type, object> _channels = new Dictionary<Type, object>();

        private Channel<T> Get<T>(bool create)
        {
            if (_channels.TryGetValue(typeof(T), out var c)) return (Channel<T>)c;
            if (!create) return null;
            var ch = new Channel<T>();
            _channels[typeof(T)] = ch;
            return ch;
        }

        /// <summary>Inscreve um ouvinte. Retorna um token que cancela a inscrição ao ser descartado.</summary>
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var ch = Get<T>(true);
            ch.Add(handler);
            return new Subscription(() => ch.Remove(handler));
        }

        /// <summary>Publica um evento para todos os ouvintes do tipo, na ordem de inscrição.</summary>
        public void Publish<T>(T evt)
        {
            var ch = Get<T>(false);
            if (ch == null) return;
            // O array atual é imutável: (des)inscrições durante a publicação valem a partir da próxima.
            var handlers = ch.Handlers;
            for (int i = 0; i < handlers.Length; i++) handlers[i](evt);
        }

        public int CountSubscribers<T>() => Get<T>(false)?.Handlers.Length ?? 0;

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;
            public Subscription(Action dispose) { _dispose = dispose; }
            public void Dispose() { _dispose?.Invoke(); _dispose = null; }
        }
    }
}
