using System;
using System.Collections.Generic;
using Propulsor.Core.Events;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Physics;

namespace Propulsor.Core.Progress
{
    /// <summary>Contadores medidos durante uma corrida (chaves usadas por missões e estatísticas).</summary>
    public static class Counters
    {
        public const string Coins = "porcas";
        public const string Distance = "distancia";
        /// <summary>Maior distância voando sem tocar o chão (a pé).</summary>
        public const string LongestFlight = "voo_mais_longo";
        /// <summary>Distância percorrida correndo no chão (a pé).</summary>
        public const string GroundDistance = "distancia_chao";
        /// <summary>Distância alcançada antes da primeira porca.</summary>
        public const string NoCoinDistance = "distancia_sem_porcas";
        public const string Tokens = "fichas";
        public const string VehicleMounts = "veiculos";
        public const string VehicleLosses = "veiculos_perdidos";
        public const string LasersSurvived = "feixes_sobrevividos";
        public const string HeadStarts = "arrancadas";
        public const string ClawPlays = "garra_jogadas";
        public const string Workers = "robos_derrubados";
        public const string NearMisses = "raspoes";
        private static readonly string[] MountKeys = BuildKeys<Gameplay.Vehicles.VehicleKind>("veiculo_");
        private static readonly string[] DeathKeys = BuildKeys<HazardType>("morte_");
        public static string MountsOf(Gameplay.Vehicles.VehicleKind k) => MountKeys[(int)k];
        public static string DeathBy(HazardType h) => DeathKeys[(int)h];

        // Textos pré-montados: medir eventos não aloca memória.
        private static string[] BuildKeys<T>(string prefix) where T : Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            var keys = new string[values.Length];
            foreach (var v in values) keys[Convert.ToInt32(v)] = prefix + v;
            return keys;
        }
    }

    /// <summary>
    /// Mede os contadores de uma corrida a partir dos eventos do <see cref="EventBus"/>.
    /// Zera em <see cref="RunStarted"/>. Dispara <see cref="Changed"/> quando algo muda.
    /// </summary>
    public sealed class RunTracker
    {
        private readonly Dictionary<string, float> _c = new Dictionary<string, float>();
        private float _distance, _takeoffAt = -1f, _lastGroundAt;
        private bool _grounded = true, _anyCoin, _dead;

        public event Action Changed;

        public RunTracker(EventBus bus)
        {
            bus.Subscribe<RunStarted>(_ => Reset());
            bus.Subscribe<DistanceChanged>(OnDistance);
            bus.Subscribe<PlayerStateChanged>(OnState);
            bus.Subscribe<CoinCollected>(e => { _anyCoin = true; Add(Counters.Coins, e.Value); });
            bus.Subscribe<TokenCollected>(_ => Add(Counters.Tokens, 1));
            bus.Subscribe<VehicleMounted>(e => { Add(Counters.VehicleMounts, 1); Add(Counters.MountsOf(e.Kind), 1); _grounded = false; _takeoffAt = -1f; });
            bus.Subscribe<VehicleDestroyed>(_ => Add(Counters.VehicleLosses, 1));
            bus.Subscribe<LaserSequenceEnded>(_ => { if (!_dead) Add(Counters.LasersSurvived, 1); });
            bus.Subscribe<PlayerDied>(e => { _dead = true; Add(Counters.DeathBy(e.Cause), 1); });
            bus.Subscribe<PlayerRevived>(_ => _dead = false);
            bus.Subscribe<HeadStartStarted>(_ => Add(Counters.HeadStarts, 1));
            bus.Subscribe<WorkerKnocked>(_ => Add(Counters.Workers, 1));
            bus.Subscribe<HazardNearMiss>(_ => { if (!_dead) Add(Counters.NearMisses, 1); });
        }

        public float Get(string key) => _c.TryGetValue(key, out var v) ? v : 0f;
        public IReadOnlyDictionary<string, float> All => _c;

        /// <summary>Contador externo (ex.: jogadas na Garra, que acontecem após a morte).</summary>
        public void Add(string key, float amount)
        {
            _c[key] = Get(key) + amount;
            Changed?.Invoke();
        }

        private void Set(string key, float value)
        {
            if (Math.Abs(Get(key) - value) < 1e-4f) return;
            _c[key] = value;
            Changed?.Invoke();
        }

        private void Reset()
        {
            _c.Clear();
            _distance = 0f;
            _takeoffAt = -1f;
            _lastGroundAt = 0f;
            _grounded = true;
            _anyCoin = false;
            _dead = false;
            Changed?.Invoke();
        }

        private void OnState(PlayerStateChanged e)
        {
            if (e.To == MotorState.Grounded)
            {
                _grounded = true;
                _takeoffAt = -1f;
                _lastGroundAt = _distance;
            }
            else if (e.To == MotorState.Thrusting || e.To == MotorState.Falling)
            {
                if (_grounded) _takeoffAt = _distance;
                _grounded = false;
            }
        }

        private void OnDistance(DistanceChanged e)
        {
            if (_dead) return;
            float delta = e.Meters - _distance;
            _distance = e.Meters;
            Set(Counters.Distance, _distance);
            if (!_anyCoin) Set(Counters.NoCoinDistance, _distance);
            if (_grounded && delta > 0f) Set(Counters.GroundDistance, Get(Counters.GroundDistance) + delta);
            if (!_grounded && _takeoffAt >= 0f)
                Set(Counters.LongestFlight, Math.Max(Get(Counters.LongestFlight), _distance - _takeoffAt));
        }
    }
}
