using System;
using Propulsor.Core.Physics;

namespace Propulsor.Core.Gameplay.Vehicles
{
    /// <summary>Veículos próprios (docs/fase-3/01-mapeamento-sistemas.md §3).</summary>
    public enum VehicleKind
    {
        /// <summary>Mecha Pisão: salta no toque, segurar planeia.</summary>
        Mech,
        /// <summary>Drone-Asa: voa por batidas, mais suave que o propulsor.</summary>
        FlapDrone,
        /// <summary>Cápsula Blink: teleporta na direção de um ponteiro oscilante.</summary>
        BlinkPod,
        /// <summary>Moto-Trilho: rápida, salta no toque, bloqueia drones.</summary>
        RailBike,
        /// <summary>Serpente-Céu: flutua no teto, segurar desce.</summary>
        SkySerpent,
        /// <summary>Traje Inverso: toque inverte a gravidade.</summary>
        FlipSuit,
        /// <summary>Jet-Esqui: segurar mergulha, soltar salta da água.</summary>
        SplashSki,
        /// <summary>Guindaste Blindado: aguenta 3 drones.</summary>
        HeavyRig
    }

    /// <summary>Controle vertical de um veículo. O eixo horizontal continua no <see cref="PlayerMotor"/>.</summary>
    public interface IVehicleMotor
    {
        VehicleKind Kind { get; }
        float Y { get; }
        float VelocityY { get; }
        float Width { get; }
        float Height { get; }
        int HitPoints { get; set; }
        float SpeedMultiplier { get; }
        bool BlocksMissiles { get; }
        void Mount(float y, PlayerPhysicsParams p);
        void Step(float dt, bool held, bool pressed);
        Aabb Hitbox(float x);
    }

    public abstract class VehicleMotorBase : IVehicleMotor
    {
        public abstract VehicleKind Kind { get; }
        public float Y { get; protected set; }
        public float VelocityY { get; protected set; }
        public abstract float Width { get; }
        public abstract float Height { get; }
        public int HitPoints { get; set; } = 1;
        public virtual float SpeedMultiplier => 1f;
        public virtual bool BlocksMissiles => false;
        /// <summary>Tempo desde a montagem (s).</summary>
        public float Time { get; private set; }

        protected PlayerPhysicsParams P;
        protected float Floor => P.FloorY;
        protected float Top => P.CeilingY - Height;
        protected bool OnFloor => Y <= Floor;
        protected bool OnCeiling => Y >= Top;

        public virtual void Mount(float y, PlayerPhysicsParams p)
        {
            P = p;
            Y = MathF2.Clamp(y, p.FloorY, p.CeilingY - Height);
            VelocityY = 0f;
            Time = 0f;
            HitPoints = 1; // veículos são reaproveitados entre montagens
        }

        public void Step(float dt, bool held, bool pressed)
        {
            Time += dt;
            OnStep(dt, held, pressed);
        }

        protected abstract void OnStep(float dt, bool held, bool pressed);

        /// <summary>Integra com aceleração e limites e aplica chão/teto.</summary>
        protected void Integrate(float dt, float ay, float maxUp, float maxDown)
        {
            VelocityY = MathF2.Clamp(VelocityY + ay * dt, maxDown, maxUp);
            Y += VelocityY * dt;
            ClampToCorridor();
        }

        protected void ClampToCorridor()
        {
            if (Y <= Floor) { Y = Floor; if (VelocityY < 0f) VelocityY = 0f; }
            if (Y >= Top) { Y = Top; if (VelocityY > 0f) VelocityY = 0f; }
        }

        public virtual Aabb Hitbox(float x) => Aabb.FromBase(x, Y + Height * 0.05f, Width * 0.8f, Height * 0.9f);
    }

    public sealed class MechMotor : VehicleMotorBase
    {
        public override VehicleKind Kind => VehicleKind.Mech;
        public override float Width => 1.4f;
        public override float Height => 2.0f;
        public const float Gravity = -45f, JumpSpeed = 16f, GlideFactor = 0.35f;

        protected override void OnStep(float dt, bool held, bool pressed)
        {
            if (pressed && OnFloor) VelocityY = JumpSpeed;
            float g = Gravity * (held && VelocityY < 0f ? GlideFactor : 1f);
            Integrate(dt, g, JumpSpeed, -16f);
        }
    }

    public sealed class FlapDroneMotor : VehicleMotorBase
    {
        public override VehicleKind Kind => VehicleKind.FlapDrone;
        public override float Width => 1.4f;
        public override float Height => 1.2f;

        protected override void OnStep(float dt, bool held, bool pressed) =>
            Integrate(dt, -30f + (held ? 60f : 0f), 9f, -11f);
    }

    public sealed class BlinkPodMotor : VehicleMotorBase
    {
        public override VehicleKind Kind => VehicleKind.BlinkPod;
        public override float Width => 1.2f;
        public override float Height => 1.2f;
        public const float MaxAngle = 60f, Frequency = 1.5f, Jump = 2.5f;

        /// <summary>Ângulo atual do ponteiro (graus, positivo = para cima).</summary>
        public float PointerAngleDeg => MaxAngle * (float)Math.Sin(2 * Math.PI * Frequency * Time);
        public int Teleports { get; private set; }

        protected override void OnStep(float dt, bool held, bool pressed)
        {
            VelocityY = 0f;
            if (!pressed) return;
            Y += Jump * (float)Math.Sin(PointerAngleDeg * MathF2.Deg2Rad);
            ClampToCorridor();
            Teleports++;
        }
    }

    public sealed class RailBikeMotor : VehicleMotorBase
    {
        public override VehicleKind Kind => VehicleKind.RailBike;
        public override float Width => 1.8f;
        public override float Height => 1.2f;
        public override bool BlocksMissiles => true;
        /// <summary>Acelera de 1,5× para 2× ao longo de 10 s.</summary>
        public override float SpeedMultiplier => Math.Min(2f, 1.5f + 0.05f * Time);

        protected override void OnStep(float dt, bool held, bool pressed)
        {
            if (pressed && OnFloor) VelocityY = 14f;
            Integrate(dt, -45f, 14f, -16f);
        }
    }

    public sealed class SkySerpentMotor : VehicleMotorBase
    {
        public override VehicleKind Kind => VehicleKind.SkySerpent;
        public override float Width => 2.0f;
        public override float Height => 1.0f;
        public const float Speed = 8f, WaveAmplitude = 0.4f, WaveFrequency = 1f;
        private float _baseY;

        public override void Mount(float y, PlayerPhysicsParams p)
        {
            base.Mount(y, p);
            _baseY = Y;
        }

        protected override void OnStep(float dt, bool held, bool pressed)
        {
            VelocityY = held ? -Speed : Speed;
            _baseY = MathF2.Clamp(_baseY + VelocityY * dt, Floor, Top);
            // Ondulação: mais forte longe do teto, some ao encostar nele.
            float wave = WaveAmplitude * (float)Math.Sin(2 * Math.PI * WaveFrequency * Time);
            Y = MathF2.Clamp(_baseY + (_baseY >= Top ? Math.Min(0f, wave) : wave), Floor, Top);
        }
    }

    public sealed class FlipSuitMotor : VehicleMotorBase
    {
        public override VehicleKind Kind => VehicleKind.FlipSuit;
        public override float Width => 0.6f;
        public override float Height => 1.1f;
        public const float GravityMagnitude = 40f;
        /// <summary>-1 = gravidade para o chão, +1 = para o teto.</summary>
        public int GravitySign { get; private set; } = -1;

        public override void Mount(float y, PlayerPhysicsParams p)
        {
            base.Mount(y, p);
            GravitySign = -1;
        }

        protected override void OnStep(float dt, bool held, bool pressed)
        {
            if (pressed) GravitySign = -GravitySign;
            Integrate(dt, GravityMagnitude * GravitySign, 14f, -14f);
        }
    }

    public sealed class SplashSkiMotor : VehicleMotorBase
    {
        public override VehicleKind Kind => VehicleKind.SplashSki;
        public override float Width => 1.8f;
        public override float Height => 0.9f;
        /// <summary>Superfície da água (30 % da altura do corredor de 7 u).</summary>
        public const float WaterY = 2.1f;
        public bool InWater => Y + Height * 0.5f < WaterY;

        protected override void OnStep(float dt, bool held, bool pressed)
        {
            float ay;
            if (InWater) ay = held ? -40f : 55f;        // mergulha / empuxo para fora
            else ay = -38f * (held ? 2f : 1f);           // no ar: toque acelera a queda
            Integrate(dt, ay, 12f, -14f);
        }
    }

    public sealed class HeavyRigMotor : VehicleMotorBase
    {
        public override VehicleKind Kind => VehicleKind.HeavyRig;
        public override float Width => 1.8f;
        public override float Height => 1.8f;
        public const int MaxHitPoints = 3;

        public override void Mount(float y, PlayerPhysicsParams p)
        {
            base.Mount(y, p);
            HitPoints = MaxHitPoints;
        }

        protected override void OnStep(float dt, bool held, bool pressed)
        {
            if (pressed && OnFloor) VelocityY = 10f;
            Integrate(dt, -45f, 10f, -16f);
        }
    }

    public static class VehicleFactory
    {
        public static readonly VehicleKind[] All = (VehicleKind[])Enum.GetValues(typeof(VehicleKind));

        public static IVehicleMotor Create(VehicleKind kind) => kind switch
        {
            VehicleKind.Mech => new MechMotor(),
            VehicleKind.FlapDrone => new FlapDroneMotor(),
            VehicleKind.BlinkPod => new BlinkPodMotor(),
            VehicleKind.RailBike => new RailBikeMotor(),
            VehicleKind.SkySerpent => new SkySerpentMotor(),
            VehicleKind.FlipSuit => new FlipSuitMotor(),
            VehicleKind.SplashSki => new SplashSkiMotor(),
            VehicleKind.HeavyRig => new HeavyRigMotor(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
