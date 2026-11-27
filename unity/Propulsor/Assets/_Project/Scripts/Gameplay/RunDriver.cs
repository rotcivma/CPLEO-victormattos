using System;
using Propulsor.Core.Events;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Gameplay.Vehicles;
using Propulsor.Core.Physics;
using Propulsor.Data;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Conduz a <see cref="RunSimulation"/> (lógica pura) dentro do Unity: lê o input,
    /// avança em passo fixo de 50 Hz e expõe posições interpoladas para as views.
    /// Não desenha nada — ver <see cref="PlayerView"/> e <see cref="WorldView"/>.
    /// </summary>
    public sealed class RunDriver : MonoBehaviour
    {
        [SerializeField] private PlayerPhysicsDef physicsDef;
        [Tooltip("Reaplica os valores do asset a cada quadro (ajuste ao vivo no Play Mode).")]
        [SerializeField] private bool liveTuning = true;

        public RunSimulation Sim { get; private set; }
        public EventBus Bus { get; } = new EventBus();
        public PlayerMotor Motor => Sim?.Motor;

        /// <summary>Posição interpolada do jogador/veículo para desenho.</summary>
        public Vector2 RenderPosition { get; private set; }
        /// <summary>Disparado após cada reinício (views limpam seus pools).</summary>
        public event Action Restarted;

        private IThrustInput _input;
        private Func<RunConfig> _configFactory;
        private readonly FixedStepper _stepper = new FixedStepper(0.02f);
        private Vector2 _prev, _curr;
        private bool _pressedLatch;
        private int _debugVehicle;

        public void Configure(PlayerPhysicsDef def, IThrustInput input, Func<RunConfig> configFactory = null)
        {
            physicsDef = def;
            _input = input;
            _configFactory = configFactory;
            Restart();
        }

        private void Awake()
        {
            if (Sim == null && physicsDef != null) Configure(physicsDef, new PlayerInput());
        }

        public void Restart()
        {
            _input ??= new PlayerInput();
            var config = _configFactory != null ? _configFactory() : new RunConfig();
            config.Physics = physicsDef != null ? physicsDef.ToParams() : config.Physics;
            Sim = new RunSimulation(config, UnityEngine.Random.Range(1, int.MaxValue), Bus);
            _stepper.Reset();
            _prev = _curr = new Vector2(Sim.Motor.X, Sim.PlayerY);
            RenderPosition = _curr;
            _pressedLatch = false;
            Restarted?.Invoke();
        }

        /// <summary>Mata o jogador (tecla K / testes).</summary>
        public void Kill() => Sim?.Kill(HazardType.Missile);

        private void Update()
        {
            if (Sim == null) return;
            if (liveTuning && physicsDef != null) physicsDef.CopyTo(Sim.Config.Physics);

            if (PlayerInput.DebugKeyDown('K')) Kill();
            if (PlayerInput.DebugKeyDown('V') && Sim.Vehicle == null)
            {
                Sim.Mount(VehicleFactory.All[_debugVehicle % VehicleFactory.All.Length]);
                _debugVehicle++;
            }
            bool pressedNow = _input.PressedThisFrame;
            if (PlayerInput.DebugKeyDown('R') || (Sim.IsOver && pressedNow)) { Restart(); return; }

            if (pressedNow) _pressedLatch = true;
            bool held = _input.IsHeld;

            _stepper.Advance(Time.deltaTime, dt =>
            {
                _prev = _curr;
                Sim.Step(dt, held, _pressedLatch);
                _pressedLatch = false; // o toque conta uma única vez
                _curr = new Vector2(Sim.Motor.X, Sim.PlayerY);
            });

            RenderPosition = Vector2.Lerp(_prev, _curr, _stepper.Alpha);
        }
    }
}
