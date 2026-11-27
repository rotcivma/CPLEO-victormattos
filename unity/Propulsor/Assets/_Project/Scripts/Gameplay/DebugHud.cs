using Propulsor.Core.Events;
using Propulsor.Core.Physics;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// HUD de depuração do protótipo (IMGUI): distância, recorde da sessão,
    /// estado, velocidades e FPS. Será substituído pelo HUD definitivo na Fase 7.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        private PlayerView _player;
        private float _best;
        private float _fps, _fpsTimer;
        private int _frames;
        private float _airTime, _lastAirTime;
        private GUIStyle _big, _small;

        public void Bind(PlayerView player)
        {
            _player = player;
            player.Bus.Subscribe<PlayerStopped>(e => _best = Mathf.Max(_best, Mathf.Floor(e.Distance)));
            player.Bus.Subscribe<PlayerLanded>(_ => { _lastAirTime = _airTime; _airTime = 0f; });
        }

        private void Update()
        {
            _frames++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f) { _fps = _frames / _fpsTimer; _frames = 0; _fpsTimer = 0f; }
            if (_player != null && _player.Motor != null && !_player.Motor.IsOnFloor && !_player.Motor.IsDead)
                _airTime += Time.deltaTime;
        }

        private void OnGUI()
        {
            if (_player == null || _player.Motor == null) return;
            float scale = Screen.height / 1080f;
            _big ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _small ??= new GUIStyle(GUI.skin.label);
            _big.fontSize = Mathf.RoundToInt(64 * scale);
            _small.fontSize = Mathf.RoundToInt(26 * scale);

            var m = _player.Motor;
            var safe = Screen.safeArea;
            float x = safe.xMin + Screen.width * 0.04f;
            float y = (Screen.height - safe.yMax) + Screen.height * 0.05f;

            GUI.Label(new Rect(x, y, 900 * scale, 80 * scale), $"{Mathf.FloorToInt(m.Distance):N0} m", _big);
            y += 80 * scale;
            GUI.Label(new Rect(x, y, 900 * scale, 40 * scale), $"Recorde (sessão): {_best:N0} m", _small);
            y += 40 * scale;
            GUI.Label(new Rect(x, y, 1400 * scale, 40 * scale),
                $"Estado: {m.State}  vx {m.VelocityX:0.00}  vy {m.VelocityY:0.00}  y {m.Y:0.00}  " +
                $"últ. tempo no ar {_lastAirTime:0.00}s  FPS {_fps:0}", _small);
            y += 40 * scale;
            GUI.Label(new Rect(x, y, 1400 * scale, 40 * scale),
                "Segure toque/clique/espaço = subir · K = morrer (teste) · R = reiniciar", _small);

            if (m.State == MotorState.DeadStopped)
            {
                var r = new Rect(0, Screen.height * 0.4f, Screen.width, 100 * scale);
                var center = new GUIStyle(_big) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(r, "Toque para tentar de novo", center);
            }
        }
    }
}
