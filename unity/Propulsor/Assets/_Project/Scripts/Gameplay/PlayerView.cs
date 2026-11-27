using Propulsor.Core.Events;
using Propulsor.Core.Physics;
using Propulsor.Data;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Ponte entre a lógica pura (<see cref="PlayerMotor"/>) e a cena Unity:
    /// lê o input, avança a simulação em passo fixo, posiciona o transform
    /// (com interpolação) e publica eventos no <see cref="EventBus"/>.
    /// </summary>
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private PlayerPhysicsDef physicsDef;
        [Tooltip("Reaplica os valores do asset a cada quadro (ajuste ao vivo no Play Mode).")]
        [SerializeField] private bool liveTuning = true;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private ParticleSystem thrustParticles;

        public PlayerMotor Motor { get; private set; }
        public EventBus Bus { get; private set; }

        private IThrustInput _input;
        private PlayerPhysicsParams _params;
        private readonly FixedStepper _stepper = new FixedStepper(0.02f);
        private Vector2 _prevPos, _currPos;
        private float _lastPublishedMeters = -1f;

        /// <summary>Configuração feita pelo bootstrap (alternativa ao Inspector).</summary>
        public void Configure(PlayerPhysicsDef def, EventBus bus, IThrustInput input,
                              SpriteRenderer bodyRenderer, ParticleSystem particles)
        {
            physicsDef = def;
            Bus = bus;
            _input = input;
            body = bodyRenderer;
            thrustParticles = particles;
            Init();
        }

        private void Awake()
        {
            if (Motor == null && physicsDef != null) Init();
        }

        private void Init()
        {
            Bus ??= new EventBus();
            _input ??= new PlayerInput();
            _params = physicsDef != null ? physicsDef.ToParams() : new PlayerPhysicsParams();
            Motor = new PlayerMotor(_params);
            Motor.StateChanged += (from, to) =>
            {
                Bus.Publish(new PlayerStateChanged(from, to));
                if (to == MotorState.Dead) Bus.Publish(new PlayerDied());
            };
            Motor.Landed += () => Bus.Publish(new PlayerLanded());
            Motor.Stopped += () => Bus.Publish(new PlayerStopped(Motor.Distance));
            Restart();
        }

        public void Restart()
        {
            Motor.Reset(0f, _params.FloorY);
            _stepper.Reset();
            _prevPos = _currPos = new Vector2(Motor.X, Motor.Y);
            _lastPublishedMeters = -1f;
            transform.localRotation = Quaternion.identity;
            Bus.Publish(new RunStarted(Random.Range(int.MinValue, int.MaxValue)));
        }

        public void Kill() => Motor?.Kill();

        private void Update()
        {
            if (Motor == null) return;

            if (liveTuning && physicsDef != null) physicsDef.CopyTo(_params);

            if (PlayerInput.DebugKeyDown('K')) Kill();
            if (PlayerInput.DebugKeyDown('R') || (Motor.State == MotorState.DeadStopped && _input.PressedThisFrame))
                Restart();

            bool thrust = _input.IsHeld;
            _stepper.Advance(Time.deltaTime, dt =>
            {
                _prevPos = _currPos;
                Motor.Step(dt, thrust);
                _currPos = new Vector2(Motor.X, Motor.Y);
            });

            // Interpola entre os dois últimos passos fixos para movimento suave em qualquer framerate.
            var p = Vector2.Lerp(_prevPos, _currPos, _stepper.Alpha);
            transform.position = new Vector3(p.x, p.y, 0f);

            UpdateVisuals(thrust);

            float meters = Mathf.Floor(Motor.Distance);
            if (meters != _lastPublishedMeters)
            {
                _lastPublishedMeters = meters;
                Bus.Publish(new DistanceChanged(meters));
            }
        }

        private void UpdateVisuals(bool thrust)
        {
            if (thrustParticles != null)
            {
                var emission = thrustParticles.emission;
                emission.enabled = thrust && !Motor.IsDead;
            }

            if (body == null) return;

            switch (Motor.State)
            {
                case MotorState.Grounded:
                    // "Corrida": pequeno balanço vertical proporcional à velocidade.
                    float bob = Mathf.Abs(Mathf.Sin(Time.time * Motor.VelocityX * 1.2f)) * 0.06f;
                    body.transform.localPosition = new Vector3(0f, 0.5f + bob, 0f);
                    body.transform.localRotation = Quaternion.identity;
                    break;
                case MotorState.Thrusting:
                case MotorState.Falling:
                    body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                    // Inclina levemente conforme a velocidade vertical.
                    body.transform.localRotation = Quaternion.Euler(0f, 0f, -Motor.VelocityY * 1.2f);
                    break;
                case MotorState.Dead:
                    body.transform.Rotate(0f, 0f, -Motor.VelocityX * 40f * Time.deltaTime);
                    break;
                case MotorState.DeadStopped:
                    body.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                    body.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                    break;
            }
        }
    }
}
