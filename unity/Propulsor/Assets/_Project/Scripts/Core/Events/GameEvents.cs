using Propulsor.Core.Gameplay;
using Propulsor.Core.Gameplay.Vehicles;
using Propulsor.Core.Physics;

namespace Propulsor.Core.Events
{
    // Catálogo completo: docs/fase-3/01-mapeamento-sistemas.md §7.

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

    public readonly struct PlayerDied
    {
        public readonly HazardType Cause;
        public PlayerDied(HazardType cause) { Cause = cause; }
    }

    /// <summary>Corpo parou após a morte: a corrida pode ser encerrada.</summary>
    public readonly struct PlayerStopped
    {
        public readonly float Distance;
        public PlayerStopped(float distance) { Distance = distance; }
    }

    public readonly struct CoinCollected
    {
        public readonly int Value;
        public readonly int RunTotal;
        public CoinCollected(int value, int runTotal) { Value = value; RunTotal = runTotal; }
    }

    public readonly struct TokenCollected
    {
        public readonly int RunTotal;
        public TokenCollected(int runTotal) { RunTotal = runTotal; }
    }

    public readonly struct VehicleMounted
    {
        public readonly VehicleKind Kind;
        public VehicleMounted(VehicleKind kind) { Kind = kind; }
    }

    public readonly struct VehicleDestroyed
    {
        public readonly VehicleKind Kind;
        public readonly HazardType Cause;
        public VehicleDestroyed(VehicleKind kind, HazardType cause) { Kind = kind; Cause = cause; }
    }

    /// <summary>Veículo blindado absorveu um acerto.</summary>
    public readonly struct VehicleDamaged
    {
        public readonly VehicleKind Kind;
        public readonly int HitPointsLeft;
        public VehicleDamaged(VehicleKind kind, int left) { Kind = kind; HitPointsLeft = left; }
    }

    public readonly struct LaserSequenceStarted { }
    public readonly struct LaserSequenceEnded { }

    public readonly struct MissileWarning
    {
        public readonly int Id;
        public MissileWarning(int id) { Id = id; }
    }

    public readonly struct MissileLaunched
    {
        public readonly int Id;
        public MissileLaunched(int id) { Id = id; }
    }

    public readonly struct PlayerRevived { }

    public readonly struct HeadStartStarted
    {
        public readonly float Meters;
        public HeadStartStarted(float meters) { Meters = meters; }
    }

    public readonly struct HeadStartEnded { }
}
