using System.Collections.Generic;

namespace Propulsor.Core.Flow
{
    /// <summary>
    /// Fila de avisos do HUD (ex.: missão concluída): um por vez,
    /// 0,3 s entrando, 2 s visível, 0,3 s saindo (docs/fase-2/01 §2).
    /// </summary>
    public sealed class PopupQueue
    {
        public const float InTime = 0.3f, HoldTime = 2f, OutTime = 0.3f;
        public const float Total = InTime + HoldTime + OutTime;

        private readonly Queue<string> _queue = new Queue<string>();
        private float _t;

        public string Current { get; private set; }
        public int Pending => _queue.Count;

        /// <summary>Visibilidade 0..1 (para animar escala/opacidade).</summary>
        public float Visibility
        {
            get
            {
                if (Current == null) return 0f;
                if (_t < InTime) return _t / InTime;
                if (_t < InTime + HoldTime) return 1f;
                return 1f - (_t - InTime - HoldTime) / OutTime;
            }
        }

        public void Push(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _queue.Enqueue(message);
            if (Current == null) Next();
        }

        public void Tick(float dt)
        {
            if (Current == null) return;
            _t += dt;
            if (_t >= Total) Next();
        }

        public void Clear()
        {
            _queue.Clear();
            Current = null;
            _t = 0f;
        }

        private void Next()
        {
            _t = 0f;
            Current = _queue.Count > 0 ? _queue.Dequeue() : null;
        }
    }
}
