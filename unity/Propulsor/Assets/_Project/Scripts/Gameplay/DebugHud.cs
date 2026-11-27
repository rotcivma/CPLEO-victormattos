using Propulsor.Core.Events;
using Propulsor.Core.Physics;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// HUD de depuração (IMGUI): distância, porcas, fichas, veículo, recorde da sessão,
    /// estado, velocidades e FPS. Será substituído pelo HUD definitivo na Fase 7.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        private RunDriver _driver;
        private float _best;
        private float _fps, _fpsTimer;
        private int _frames;
        private string _lastEvent = "";
        private float _lastEventTime;
        private GUIStyle _big, _small;

        public void Bind(RunDriver driver)
        {
            _driver = driver;
            var bus = driver.Bus;
            bus.Subscribe<PlayerStopped>(e => _best = Mathf.Max(_best, Mathf.Floor(e.Distance)));
            bus.Subscribe<PlayerDied>(e => Flash($"Morte: {e.Cause}"));
            bus.Subscribe<VehicleMounted>(e => Flash($"Veículo: {e.Kind}"));
            bus.Subscribe<VehicleDestroyed>(e => Flash($"Veículo destruído ({e.Cause})"));
            bus.Subscribe<VehicleDamaged>(e => Flash($"Blindagem: {e.HitPointsLeft} PV"));
            bus.Subscribe<TokenCollected>(_ => Flash("Ficha!"));
            bus.Subscribe<LaserSequenceStarted>(_ => Flash("Feixes de corte!"));
        }

        private void Flash(string msg) { _lastEvent = msg; _lastEventTime = Time.time; }

        private void Update()
        {
            _frames++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f) { _fps = _frames / _fpsTimer; _frames = 0; _fpsTimer = 0f; }
        }

        private void OnGUI()
        {
            if (_driver == null || _driver.Sim == null) return;
            var sim = _driver.Sim;
            var m = sim.Motor;
            float scale = Screen.height / 1080f;
            _big ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _small ??= new GUIStyle(GUI.skin.label);
            _big.fontSize = Mathf.RoundToInt(64 * scale);
            _small.fontSize = Mathf.RoundToInt(26 * scale);

            var safe = Screen.safeArea;
            float x = safe.xMin + Screen.width * 0.04f;
            float y = (Screen.height - safe.yMax) + Screen.height * 0.05f;
            float lh = 40 * scale;

            GUI.Label(new Rect(x, y, 900 * scale, 80 * scale), $"{Mathf.FloorToInt(sim.Distance):N0} m", _big);
            y += 80 * scale;
            GUI.Label(new Rect(x, y, 1400 * scale, lh),
                $"Porcas {sim.RunCoins}   Fichas {sim.RunTokens}   Recorde (sessão) {_best:N0} m", _small);
            y += lh;
            string vehicle = sim.Vehicle != null ? $"{sim.Vehicle.Kind} ({sim.Vehicle.HitPoints} PV)" : "a pé";
            GUI.Label(new Rect(x, y, 1600 * scale, lh),
                $"{vehicle} · {m.State} · vx {m.VelocityX:0.0} · vy {m.VelocityY:0.0} · FPS {_fps:0}", _small);
            y += lh;
            GUI.Label(new Rect(x, y, 1600 * scale, lh),
                "Segurar = subir · K morrer · V montar veículo · R reiniciar", _small);
            y += lh;
            if (Time.time - _lastEventTime < 2f)
                GUI.Label(new Rect(x, y, 1400 * scale, lh), _lastEvent, _small);

            if (m.State == MotorState.DeadStopped)
            {
                var r = new Rect(0, Screen.height * 0.4f, Screen.width, 100 * scale);
                var center = new GUIStyle(_big) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(r, "Toque para tentar de novo", center);
            }
        }
    }
}
