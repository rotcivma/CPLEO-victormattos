using System;

namespace Propulsor.Core.Physics
{
    public enum MotorState
    {
        Grounded,
        Thrusting,
        Falling,
        /// <summary>Morto, corpo ainda em movimento (quicando/deslizando).</summary>
        Dead,
        /// <summary>Morto e parado: a corrida pode terminar.</summary>
        DeadStopped
    }

    /// <summary>
    /// Movimento do jogador a pé, em C# puro (sem UnityEngine) para ser testável.
    /// Integração semi-implícita de Euler; deve ser chamado em passo fixo
    /// (ver <see cref="FixedStepper"/>). Ver docs/fase-3/01-mapeamento-sistemas.md §2.
    /// </summary>
    public sealed class PlayerMotor
    {
        private readonly PlayerPhysicsParams _p;

        public float X { get; private set; }
        public float Y { get; private set; }
        public float VelocityX { get; private set; }
        public float VelocityY { get; private set; }
        public MotorState State { get; private set; }

        /// <summary>Distância percorrida desde o início, em metros.</summary>
        public float Distance => X - _startX;
        public bool IsDead => State == MotorState.Dead || State == MotorState.DeadStopped;
        public bool IsOnFloor => Y <= _p.FloorY;
        public bool IsOnCeiling => Y >= _p.MaxY;

        public event Action<MotorState, MotorState> StateChanged;
        /// <summary>Disparado quando o jogador (vivo) toca o chão vindo do ar.</summary>
        public event Action Landed;
        /// <summary>Disparado uma vez quando o corpo para após a morte.</summary>
        public event Action Stopped;

        private float _startX;

        public PlayerMotor(PlayerPhysicsParams parameters)
        {
            _p = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Reset(0f, parameters.FloorY);
        }

        public PlayerPhysicsParams Parameters => _p;

        /// <summary>Reinicia o jogador numa posição, parado na vertical, com velocidade inicial.</summary>
        public void Reset(float x, float y)
        {
            X = _startX = x;
            Y = Clamp(y, _p.FloorY, _p.MaxY);
            VelocityX = _p.StartSpeed;
            VelocityY = 0f;
            State = IsOnFloor ? MotorState.Grounded : MotorState.Falling;
        }

        /// <summary>Mata o jogador: inicia o "ragdoll". Ignorado se já estiver morto.</summary>
        public void Kill()
        {
            if (IsDead) return;
            VelocityY = Math.Max(VelocityY, 0f) + _p.DeathPopSpeed;
            SetState(MotorState.Dead);
        }

        /// <summary>Avança a simulação em <paramref name="dt"/> segundos.</summary>
        /// <param name="thrust">true enquanto o botão está pressionado.</param>
        public void Step(float dt, bool thrust)
        {
            if (dt <= 0f) return;
            if (IsDead) { StepDead(dt); return; }

            // Horizontal: acelera lentamente até o teto.
            VelocityX = Math.Min(VelocityX + _p.HorizontalAcceleration * dt, _p.MaxSpeed);
            X += VelocityX * dt;

            // Vertical.
            float ay = _p.Gravity + (thrust ? _p.ThrustAcceleration : 0f);
            VelocityY = Clamp(VelocityY + ay * dt, _p.MaxFallSpeed, _p.MaxRiseSpeed);
            Y += VelocityY * dt;

            bool wasAirborne = State != MotorState.Grounded;

            if (Y <= _p.FloorY)
            {
                Y = _p.FloorY;
                if (VelocityY < 0f) VelocityY = 0f;
            }
            else if (Y >= _p.MaxY)
            {
                Y = _p.MaxY;
                if (VelocityY > 0f) VelocityY = 0f;
            }

            MotorState next;
            if (thrust) next = MotorState.Thrusting;
            else if (IsOnFloor) next = MotorState.Grounded;
            else next = MotorState.Falling;

            SetState(next);
            if (wasAirborne && next == MotorState.Grounded) Landed?.Invoke();
        }

        /// <summary>
        /// Avança só o eixo horizontal (usado enquanto um veículo controla o eixo vertical).
        /// </summary>
        /// <param name="speedMultiplier">Multiplicador de velocidade do veículo (ex.: moto).</param>
        public void AdvanceHorizontal(float dt, float speedMultiplier)
        {
            if (dt <= 0f || IsDead) return;
            VelocityX = Math.Min(VelocityX + _p.HorizontalAcceleration * dt, _p.MaxSpeed);
            X += VelocityX * speedMultiplier * dt;
        }

        /// <summary>
        /// Reposiciona verticalmente o jogador (ex.: ao ser ejetado de um veículo destruído).
        /// </summary>
        public void PlaceVertical(float y, float velocityY)
        {
            if (IsDead) return;
            Y = Clamp(y, _p.FloorY, _p.MaxY);
            VelocityY = Clamp(velocityY, _p.MaxFallSpeed, _p.MaxRiseSpeed);
            SetState(IsOnFloor && VelocityY <= 0f ? MotorState.Grounded : MotorState.Falling);
        }

        private void StepDead(float dt)
        {
            if (State == MotorState.DeadStopped) return;

            VelocityY = Math.Max(VelocityY + _p.Gravity * dt, _p.MaxFallSpeed);
            Y += VelocityY * dt;

            if (Y >= _p.MaxY)
            {
                Y = _p.MaxY;
                if (VelocityY > 0f) VelocityY = 0f;
            }

            if (Y <= _p.FloorY)
            {
                Y = _p.FloorY;
                if (VelocityY < 0f)
                {
                    float bounce = -VelocityY * _p.DeathRestitution;
                    VelocityY = bounce >= _p.MinBounceSpeed ? bounce : 0f;
                }
            }

            // Atrito somente em contato com o chão.
            if (Y <= _p.FloorY && VelocityY == 0f)
            {
                float decel = _p.DeathFriction * Math.Abs(_p.Gravity);
                VelocityX = Math.Max(0f, VelocityX - decel * dt);
            }

            X += VelocityX * dt;

            if (Y <= _p.FloorY && VelocityY == 0f && VelocityX < _p.DeathStopSpeed)
            {
                VelocityX = 0f;
                SetState(MotorState.DeadStopped);
                Stopped?.Invoke();
            }
        }

        private void SetState(MotorState next)
        {
            if (next == State) return;
            var prev = State;
            State = next;
            StateChanged?.Invoke(prev, next);
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }
}
