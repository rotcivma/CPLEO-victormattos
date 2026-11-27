using System.Collections.Generic;

namespace Propulsor.Core.Gameplay
{
    public enum LaserPhase { Entering, Warning, Firing, Leaving, Done }

    /// <summary>
    /// Sequência de feixes de corte presa à tela:
    /// Entrada (0,5 s) → [Aviso (1,2 s) → Disparo (1,5 s)] por onda → Saída (0,4 s).
    /// Só causa dano em <see cref="LaserPhase.Firing"/>. Ver docs/fase-3/01 §4.2.
    /// </summary>
    public sealed class LaserSequence
    {
        public const float EnterTime = 0.5f;
        public const float WarnTime = 1.2f;
        public const float FireTime = 1.5f;
        public const float LeaveTime = 0.4f;

        private readonly IReadOnlyList<LaserWaveDef> _waves;

        public LaserPhase Phase { get; private set; } = LaserPhase.Entering;
        public float PhaseTime { get; private set; }
        public int WaveIndex { get; private set; }
        public bool IsDone => Phase == LaserPhase.Done;
        public bool IsLethal => Phase == LaserPhase.Firing;

        /// <summary>Alturas das linhas da onda atual (vazio na entrada/saída).</summary>
        public float[] CurrentRows =>
            (Phase == LaserPhase.Warning || Phase == LaserPhase.Firing) ? _waves[WaveIndex].Rows : System.Array.Empty<float>();

        /// <summary>Todas as linhas usadas pela sequência (para posicionar os emissores).</summary>
        public IReadOnlyList<LaserWaveDef> Waves => _waves;

        public LaserSequence(IReadOnlyList<LaserWaveDef> waves)
        {
            _waves = waves;
            if (waves == null || waves.Count == 0) Phase = LaserPhase.Done;
        }

        /// <summary>Duração total da sequência (s).</summary>
        public static float TotalDuration(int waveCount) => EnterTime + waveCount * (WarnTime + FireTime) + LeaveTime;

        public void Step(float dt)
        {
            if (IsDone) return;
            PhaseTime += dt;
            while (!IsDone && PhaseTime >= Duration(Phase))
            {
                PhaseTime -= Duration(Phase);
                switch (Phase)
                {
                    case LaserPhase.Entering: Phase = LaserPhase.Warning; break;
                    case LaserPhase.Warning: Phase = LaserPhase.Firing; break;
                    case LaserPhase.Firing:
                        if (WaveIndex + 1 < _waves.Count) { WaveIndex++; Phase = LaserPhase.Warning; }
                        else Phase = LaserPhase.Leaving;
                        break;
                    case LaserPhase.Leaving: Phase = LaserPhase.Done; break;
                }
            }
        }

        public bool Hits(in Aabb box)
        {
            if (!IsLethal) return false;
            foreach (var row in _waves[WaveIndex].Rows)
                if (box.MinY < row + Hazards.LaserHalfThickness && box.MaxY > row - Hazards.LaserHalfThickness)
                    return true;
            return false;
        }

        private static float Duration(LaserPhase phase) => phase switch
        {
            LaserPhase.Entering => EnterTime,
            LaserPhase.Warning => WarnTime,
            LaserPhase.Firing => FireTime,
            LaserPhase.Leaving => LeaveTime,
            _ => float.MaxValue
        };
    }
}
