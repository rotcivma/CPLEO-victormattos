using System;
using System.Collections.Generic;

namespace Propulsor.Core.Gameplay
{
    /// <summary>
    /// Escolhe o próximo trecho por peso (peso base × afinidade com a dificuldade atual),
    /// respeitando a distância mínima de cada trecho e sem repetir nenhum dos últimos 3.
    /// </summary>
    public sealed class SegmentSelector
    {
        public const int NoRepeatWindow = 3;

        private readonly IReadOnlyList<SegmentDef> _segments;
        private readonly IRandom _rng;
        private readonly Queue<string> _recent = new Queue<string>();
        private readonly List<SegmentDef> _candidates = new List<SegmentDef>();
        private readonly List<float> _weights = new List<float>();

        public SegmentSelector(IReadOnlyList<SegmentDef> segments, IRandom rng)
        {
            if (segments == null || segments.Count == 0) throw new ArgumentException("Biblioteca vazia", nameof(segments));
            _segments = segments;
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        /// <param name="safeOnly">Só trechos sem arcos nem feixes (início da corrida). Se não houver nenhum, ignora a regra.</param>
        public SegmentDef Next(float distance, bool safeOnly = false)
        {
            if (safeOnly)
            {
                var safe = Pick(distance, true);
                if (safe != null) return Remember(safe);
            }
            return Remember(Pick(distance, false) ?? _segments[0]);
        }

        private SegmentDef Pick(float distance, bool safeOnly)
        {
            _candidates.Clear();
            _weights.Clear();
            float total = 0f;
            for (int i = 0; i < _segments.Count; i++) // laço com índice: não aloca enumerador
            {
                var s = _segments[i];
                if (s.MinDistance > distance || _recent.Contains(s.Id)) continue;
                if (safeOnly && (s.Zappers.Count > 0 || s.HasLasers)) continue;
                float w = s.BaseWeight * Difficulty.SegmentAffinity(s.Difficulty, distance);
                _candidates.Add(s);
                _weights.Add(w);
                total += w;
            }

            if (_candidates.Count == 0) return null;
            float r = _rng.NextFloat() * total;
            for (int i = 0; i < _candidates.Count; i++)
            {
                r -= _weights[i];
                if (r < 0f) return _candidates[i];
            }
            return _candidates[_candidates.Count - 1];
        }

        private SegmentDef Remember(SegmentDef chosen)
        {
            _recent.Enqueue(chosen.Id);
            while (_recent.Count > NoRepeatWindow) _recent.Dequeue();
            return chosen;
        }
    }
}
