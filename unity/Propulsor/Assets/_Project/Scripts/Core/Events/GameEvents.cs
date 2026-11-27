using Propulsor.Core.Physics;

namespace Propulsor.Core.Events
{
    // Eventos usados no protótipo de movimentação (Fase 5).
    // O catálogo completo está em docs/fase-3/01-mapeamento-sistemas.md §7.

    public readonly struct RunStarted
    {
        public readonly int Seed;
        public RunStarted(int seed) { Seed = seed; }
    }

    public readonly struct DistanceChanged
    {
        public readonly float Meters;
        public DistanceChanged(float meters) { Meters = meters; }
    }

    public readonly struct PlayerStateChanged
    {
        public readonly MotorState From;
        public readonly MotorState To;
        public PlayerStateChanged(MotorState from, MotorState to) { From = from; To = to; }
    }

    public readonly struct PlayerLanded { }

    public readonly struct PlayerDied { }

    /// <summary>Corpo parou após a morte: a corrida pode ser encerrada.</summary>
    public readonly struct PlayerStopped
    {
        public readonly float Distance;
        public PlayerStopped(float distance) { Distance = distance; }
    }
}
