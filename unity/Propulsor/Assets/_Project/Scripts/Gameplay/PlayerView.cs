using Propulsor.Core.Gameplay.Vehicles;
using Propulsor.Core.Physics;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Desenha a protagonista (a pé) ou o veículo atual, a partir do <see cref="RunDriver"/>.
    /// Arte provisória geométrica e original.
    /// </summary>
    [DefaultExecutionOrder(-50)] // ordem: jogador → câmera (0) → cenário
    public sealed class PlayerView : MonoBehaviour
    {
        private RunDriver _driver;
        private Transform _onFoot;
        private SpriteRenderer _body;
        private ParticleSystem _jet;
        private Transform _vehicleRoot;
        private SpriteRenderer _vehicleBody;
        private SpriteRenderer _vehicleCockpit;
        private Transform _pointer;
        private VehicleKind? _shownVehicle;

        public void Bind(RunDriver driver, Transform onFoot, SpriteRenderer body, ParticleSystem jet, Sprite square)
        {
            _driver = driver;
            _onFoot = onFoot;
            _body = body;
            _jet = jet;

            _vehicleRoot = new GameObject("Veiculo").transform;
            _vehicleRoot.SetParent(transform, false);
            _vehicleBody = MakeSprite("Carroceria", _vehicleRoot, square, 12);
            _vehicleCockpit = MakeSprite("Cabine", _vehicleRoot, square, 13);
            _vehicleCockpit.color = new Color32(0x2E, 0xC4, 0xB6, 255); // Nina dentro
            _pointer = MakeSprite("Ponteiro", _vehicleRoot, square, 14).transform;
            _pointer.GetComponent<SpriteRenderer>().color = new Color32(0xFF, 0xD2, 0x3F, 255);
            _vehicleRoot.gameObject.SetActive(false);
        }

        private static SpriteRenderer MakeSprite(string name, Transform parent, Sprite s, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        private void LateUpdate()
        {
            if (_driver == null || _driver.Sim == null) return;
            var sim = _driver.Sim;
            var pos = _driver.RenderPosition;
            transform.position = new Vector3(pos.x, pos.y, 0f);

            // Pisca enquanto invulnerável.
            bool visible = !sim.IsInvulnerable || Mathf.Repeat(Time.time * 8f, 1f) < 0.5f;

            var vehicle = sim.Vehicle;
            _onFoot.gameObject.SetActive(vehicle == null && visible);
            _vehicleRoot.gameObject.SetActive(vehicle != null && visible);

            if (vehicle != null) { DrawVehicle(vehicle); return; }
            _shownVehicle = null;
            DrawOnFoot(sim.Motor);
        }

        private void DrawOnFoot(PlayerMotor m)
        {
            var emission = _jet.emission;
            emission.enabled = m.State == MotorState.Thrusting;
            var t = _body.transform;
            switch (m.State)
            {
                case MotorState.Grounded:
                    float bob = Mathf.Abs(Mathf.Sin(Time.time * m.VelocityX * 1.2f)) * 0.06f;
                    t.localPosition = new Vector3(0f, 0.5f + bob, 0f);
                    t.localRotation = Quaternion.identity;
                    break;
                case MotorState.Thrusting:
                case MotorState.Falling:
                    t.localPosition = new Vector3(0f, 0.5f, 0f);
                    t.localRotation = Quaternion.Euler(0f, 0f, -m.VelocityY * 1.2f);
                    break;
                case MotorState.Dead:
                    t.Rotate(0f, 0f, -m.VelocityX * 40f * Time.deltaTime);
                    break;
                case MotorState.DeadStopped:
                    t.localRotation = Quaternion.Euler(0f, 0f, -90f);
                    t.localPosition = new Vector3(0f, 0.3f, 0f);
                    break;
            }
        }

        private void DrawVehicle(IVehicleMotor v)
        {
            var emission = _jet.emission;
            emission.enabled = false;

            if (_shownVehicle != v.Kind)
            {
                _shownVehicle = v.Kind;
                _vehicleBody.color = VehicleColor(v.Kind);
                _vehicleBody.transform.localScale = new Vector3(v.Width, v.Height, 1f);
                _vehicleBody.transform.localPosition = new Vector3(0f, v.Height * 0.5f, 0f);
                _vehicleCockpit.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
                _vehicleCockpit.transform.localPosition = new Vector3(v.Width * 0.15f, v.Height * 0.75f, 0f);
            }

            // Detalhes por tipo
            float tilt = 0f;
            switch (v)
            {
                case FlapDroneMotor _:
                case MechMotor _:
                case RailBikeMotor _:
                case HeavyRigMotor _:
                    tilt = -v.VelocityY * 1.5f;
                    break;
                case SkySerpentMotor _:
                    tilt = Mathf.Sin(Time.time * 6f) * 6f;
                    break;
                case FlipSuitMotor f:
                    _vehicleRoot.localScale = new Vector3(1f, f.GravitySign > 0 ? -1f : 1f, 1f);
                    break;
            }
            if (!(v is FlipSuitMotor)) _vehicleRoot.localScale = Vector3.one;
            _vehicleBody.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);

            bool hasPointer = v is BlinkPodMotor;
            _pointer.gameObject.SetActive(hasPointer);
            if (v is BlinkPodMotor b)
            {
                _pointer.localScale = new Vector3(1.2f, 0.12f, 1f);
                _pointer.localRotation = Quaternion.Euler(0f, 0f, b.PointerAngleDeg);
                _pointer.localPosition = new Vector3(0f, v.Height * 0.5f, 0f) +
                                         _pointer.localRotation * new Vector3(1.1f, 0f, 0f);
            }

            // Blindado: escurece a cada acerto
            if (v is HeavyRigMotor)
                _vehicleBody.color = Color.Lerp(new Color32(0x55, 0x55, 0x66, 255), VehicleColor(v.Kind),
                                                v.HitPoints / (float)HeavyRigMotor.MaxHitPoints);
        }

        public static Color VehicleColor(VehicleKind k) => k switch
        {
            VehicleKind.Mech => (Color)new Color32(0x8E, 0x9A, 0xAF, 255),
            VehicleKind.FlapDrone => (Color)new Color32(0x6B, 0xCB, 0x77, 255),
            VehicleKind.BlinkPod => (Color)new Color32(0xB3, 0x88, 0xFF, 255),
            VehicleKind.RailBike => (Color)new Color32(0xE7, 0x1D, 0x36, 255),
            VehicleKind.SkySerpent => (Color)new Color32(0xFF, 0x9F, 0x1C, 255),
            VehicleKind.FlipSuit => (Color)new Color32(0x7D, 0xF9, 0xFF, 255),
            VehicleKind.SplashSki => (Color)new Color32(0x1E, 0x90, 0xFF, 255),
            VehicleKind.HeavyRig => (Color)new Color32(0xFF, 0xD2, 0x3F, 255),
            _ => Color.white
        };
    }
}
