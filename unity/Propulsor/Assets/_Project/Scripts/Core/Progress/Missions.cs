using System;
using System.Collections.Generic;
using System.Linq;
using Propulsor.Core.Events;
using Propulsor.Core.Flow;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Gameplay.Vehicles;

namespace Propulsor.Core.Progress
{
    public enum MissionScope
    {
        /// <summary>Precisa ser feito numa única corrida.</summary>
        Run,
        /// <summary>Acumula entre corridas.</summary>
        Lifetime
    }

    /// <summary>Definição de missão (texto, contador medido, alvo, estrelas).</summary>
    public sealed class MissionDef
    {
        public string Id;
        /// <summary>Texto com {0} para o alvo.</summary>
        public string Text;
        public string Counter;
        public float Target;
        public MissionScope Scope;
        public int Stars;
        /// <summary>Patente mínima para aparecer.</summary>
        public int MinRank = 1;

        public string Describe() => string.Format(Text, Target.ToString("N0"));
    }

    /// <summary>Uma patente do ciclo de 15 (nomes próprios; valores [E] — docs/fase-4/02).</summary>
    public readonly struct RankLevel
    {
        public readonly string Name;
        public readonly int StarsToNext;
        public readonly int Reward;
        public RankLevel(string name, int stars, int reward) { Name = name; StarsToNext = stars; Reward = reward; }
    }

    public static class RankTable
    {
        public const int MaxRank = 15;

        public static readonly RankLevel[] Levels =
        {
            new RankLevel("Estagiária", 3, 200),
            new RankLevel("Aprendiz de Chave", 4, 250),
            new RankLevel("Ajudante", 4, 300),
            new RankLevel("Aprendiz", 5, 350),
            new RankLevel("Operária", 5, 400),
            new RankLevel("Técnica", 6, 450),
            new RankLevel("Soldadora", 6, 500),
            new RankLevel("Montadora", 7, 600),
            new RankLevel("Mestre de Linha", 7, 700),
            new RankLevel("Engenheira", 8, 800),
            new RankLevel("Inventora", 8, 900),
            new RankLevel("Pilota", 9, 1000),
            new RankLevel("Ás do Jato", 9, 1200),
            new RankLevel("Lenda da Fábrica", 10, 1500),
            new RankLevel("Nina", 0, 0),
        };

        public static RankLevel Get(int rank) => Levels[Math.Max(1, Math.Min(MaxRank, rank)) - 1];
    }

    /// <summary>Banco de missões próprias.</summary>
    public static class MissionCatalog
    {
        public static List<MissionDef> CreateDefault()
        {
            var list = new List<MissionDef>
            {
                M("porcas_200", "Colete {0} porcas numa corrida", Counters.Coins, 200, MissionScope.Run, 1),
                M("porcas_500", "Colete {0} porcas numa corrida", Counters.Coins, 500, MissionScope.Run, 2, 4),
                M("porcas_1000", "Colete {0} porcas numa corrida", Counters.Coins, 1000, MissionScope.Run, 3, 9),
                M("porcas_total_3000", "Colete {0} porcas no total", Counters.Coins, 3000, MissionScope.Lifetime, 2),
                M("dist_500", "Alcance {0} m", Counters.Distance, 500, MissionScope.Run, 1),
                M("dist_1000", "Alcance {0} m", Counters.Distance, 1000, MissionScope.Run, 2, 3),
                M("dist_2000", "Alcance {0} m", Counters.Distance, 2000, MissionScope.Run, 3, 7),
                M("dist_3000", "Alcance {0} m", Counters.Distance, 3000, MissionScope.Run, 3, 11),
                M("dist_total_10000", "Percorra {0} m no total", Counters.Distance, 10000, MissionScope.Lifetime, 2),
                M("voo_150", "Voe {0} m sem tocar o chão", Counters.LongestFlight, 150, MissionScope.Run, 1),
                M("voo_400", "Voe {0} m sem tocar o chão", Counters.LongestFlight, 400, MissionScope.Run, 2, 5),
                M("chao_300", "Corra {0} m pelo chão numa corrida", Counters.GroundDistance, 300, MissionScope.Run, 1),
                M("chao_800", "Corra {0} m pelo chão numa corrida", Counters.GroundDistance, 800, MissionScope.Run, 2, 6),
                M("sem_porcas_400", "Alcance {0} m sem pegar nenhuma porca", Counters.NoCoinDistance, 400, MissionScope.Run, 2, 3),
                M("sem_porcas_1000", "Alcance {0} m sem pegar nenhuma porca", Counters.NoCoinDistance, 1000, MissionScope.Run, 3, 8),
                M("fichas_3", "Pegue {0} fichas no total", Counters.Tokens, 3, MissionScope.Lifetime, 1),
                M("fichas_10", "Pegue {0} fichas no total", Counters.Tokens, 10, MissionScope.Lifetime, 2, 5),
                M("veiculos_5", "Monte em {0} veículos no total", Counters.VehicleMounts, 5, MissionScope.Lifetime, 1),
                M("veiculos_3_corrida", "Monte em {0} veículos numa corrida", Counters.VehicleMounts, 3, MissionScope.Run, 3, 6),
                M("perder_veiculo_5", "Perca {0} veículos no total", Counters.VehicleLosses, 5, MissionScope.Lifetime, 1),
                M("feixes_3", "Sobreviva a {0} sequências de feixes numa corrida", Counters.LasersSurvived, 3, MissionScope.Run, 2, 4),
                M("feixes_total_10", "Sobreviva a {0} sequências de feixes no total", Counters.LasersSurvived, 10, MissionScope.Lifetime, 2),
                M("morte_drone_3", "Seja atingida por um drone {0} vezes", Counters.DeathBy(HazardType.Missile), 3, MissionScope.Lifetime, 1),
                M("morte_arco_3", "Esbarre num arco de faísca {0} vezes", Counters.DeathBy(HazardType.Zapper), 3, MissionScope.Lifetime, 1),
                M("garra_5", "Jogue {0} vezes na Garra da Sorte", Counters.ClawPlays, 5, MissionScope.Lifetime, 2),
                M("arrancada_1", "Use uma Arrancada", Counters.HeadStarts, 1, MissionScope.Lifetime, 1, 2),
            };
            foreach (var k in VehicleFactory.All)
                list.Add(M("usar_" + k.ToString().ToLowerInvariant(), "Monte {0}× no veículo " + VehicleNames.Get(k),
                           Counters.MountsOf(k), 2, MissionScope.Lifetime, 2, 2));
            return list;
        }

        private static MissionDef M(string id, string text, string counter, float target, MissionScope scope, int stars, int minRank = 1) =>
            new MissionDef { Id = id, Text = text, Counter = counter, Target = target, Scope = scope, Stars = stars, MinRank = minRank };
    }

    public static class VehicleNames
    {
        public static string Get(VehicleKind k) => k switch
        {
            VehicleKind.Mech => "Mecha Pisão",
            VehicleKind.FlapDrone => "Drone-Asa",
            VehicleKind.BlinkPod => "Cápsula Blink",
            VehicleKind.RailBike => "Moto-Trilho",
            VehicleKind.SkySerpent => "Serpente-Céu",
            VehicleKind.FlipSuit => "Traje Inverso",
            VehicleKind.SplashSki => "Jet-Esqui",
            VehicleKind.HeavyRig => "Guindaste Blindado",
            _ => k.ToString()
        };
    }

    public readonly struct MissionCompleted
    {
        public readonly string Id;
        public readonly string Text;
        public readonly int Stars;
        public MissionCompleted(string id, string text, int stars) { Id = id; Text = text; Stars = stars; }
    }

    public readonly struct RankUp
    {
        public readonly int Rank;
        public readonly string Name;
        public readonly int Reward;
        public RankUp(int rank, string name, int reward) { Rank = rank; Name = name; Reward = reward; }
    }

    /// <summary>
    /// Três missões ativas, progresso, conclusão (aviso na hora), estrelas e patentes
    /// (contabilizadas ao fim da corrida), pular missão (500 porcas × estrelas) e prestígio.
    /// </summary>
    public sealed class MissionManager
    {
        public const int ActiveCount = 3;
        public const int SkipCostPerStar = 500;

        private readonly SessionProfile _p;
        private readonly Dictionary<string, MissionDef> _defs;
        private readonly IRandom _rng;
        private readonly EventBus _bus;
        private RunTracker _tracker;

        public MissionManager(SessionProfile profile, IEnumerable<MissionDef> defs, IRandom rng, EventBus bus = null)
        {
            _p = profile;
            _defs = defs.ToDictionary(d => d.Id);
            _rng = rng;
            _bus = bus;
            _p.ActiveMissions.RemoveAll(m => !_defs.ContainsKey(m.Id));
            Refill();
        }

        public IReadOnlyList<MissionState> Active => _p.ActiveMissions;
        public MissionDef Def(string id) => _defs.TryGetValue(id, out var d) ? d : null;
        public int Rank => _p.Rank;
        public RankLevel RankInfo => RankTable.Get(_p.Rank);
        public bool CanPrestige => _p.Rank >= RankTable.MaxRank;

        /// <summary>Liga o medidor de corrida (avisos de conclusão em tempo real).</summary>
        public void Attach(RunTracker tracker)
        {
            Detach();
            _tracker = tracker;
            tracker.Changed += CheckLive;
        }

        public void Detach()
        {
            if (_tracker != null) _tracker.Changed -= CheckLive;
            _tracker = null;
        }

        /// <summary>Progresso atual da missão (inclui a corrida em andamento).</summary>
        public float Current(MissionState s)
        {
            var d = Def(s.Id);
            if (d == null) return 0f;
            if (s.Done) return d.Target;
            float run = _tracker?.Get(d.Counter) ?? 0f;
            return d.Scope == MissionScope.Run ? run : s.Progress + run;
        }

        private void CheckLive()
        {
            foreach (var s in _p.ActiveMissions)
            {
                if (s.Done) continue;
                var d = Def(s.Id);
                if (Current(s) >= d.Target)
                {
                    s.Done = true;
                    _bus?.Publish(new MissionCompleted(d.Id, d.Describe(), d.Stars));
                }
            }
        }

        /// <summary>
        /// Fim da corrida: acumula progresso, converte concluídas em estrelas/patentes e repõe missões.
        /// Retorna as porcas de recompensa de patente (a creditar).
        /// </summary>
        public int FinishRun(List<RankUp> rankUps = null)
        {
            CheckLive();
            int reward = 0;
            foreach (var s in _p.ActiveMissions)
            {
                var d = Def(s.Id);
                if (!s.Done && d.Scope == MissionScope.Lifetime) s.Progress += _tracker?.Get(d.Counter) ?? 0f;
            }
            foreach (var s in _p.ActiveMissions.Where(m => m.Done).ToList())
            {
                reward += AddStars(Def(s.Id).Stars, rankUps);
                _p.CompletedMissions.Add(s.Id);
                _p.AddStat(Economy.StatKeys.MissionsCompleted, 1);
                _p.ActiveMissions.Remove(s);
            }
            Refill();
            return reward;
        }

        /// <summary>Pula (conclui) uma missão pagando 500 porcas por estrela.</summary>
        public bool Skip(string id, Economy.Wallet wallet)
        {
            var s = _p.ActiveMissions.FirstOrDefault(m => m.Id == id);
            if (s == null || s.Done) return false;
            int cost = SkipCost(id);
            if (!wallet.TryDebit(Economy.Currency.Coins, cost, "pular_missao:" + id)) return false;
            s.Done = true;
            _bus?.Publish(new MissionCompleted(id, Def(id).Describe(), Def(id).Stars));
            return true;
        }

        public int SkipCost(string id) => (Def(id)?.Stars ?? 0) * SkipCostPerStar;

        /// <summary>Na patente 15: troca por um selo de prestígio e recomeça o ciclo.</summary>
        public bool PrestigeReset()
        {
            if (!CanPrestige) return false;
            _p.Prestige++;
            _p.Rank = 1;
            _p.StarsInRank = 0;
            _p.CompletedMissions.Clear();
            _p.ActiveMissions.Clear();
            Refill();
            return true;
        }

        private int AddStars(int stars, List<RankUp> rankUps)
        {
            int reward = 0;
            if (_p.Rank >= RankTable.MaxRank) return 0;
            _p.StarsInRank += stars;
            while (_p.Rank < RankTable.MaxRank && _p.StarsInRank >= RankTable.Get(_p.Rank).StarsToNext)
            {
                var level = RankTable.Get(_p.Rank);
                _p.StarsInRank -= level.StarsToNext;
                reward += level.Reward;
                _p.Rank++;
                var up = new RankUp(_p.Rank, RankTable.Get(_p.Rank).Name, level.Reward);
                rankUps?.Add(up);
                _bus?.Publish(up);
            }
            if (_p.Rank >= RankTable.MaxRank) _p.StarsInRank = 0;
            return reward;
        }

        private void Refill()
        {
            while (_p.ActiveMissions.Count < ActiveCount)
            {
                var activeIds = new HashSet<string>(_p.ActiveMissions.Select(m => m.Id));
                var activeCounters = new HashSet<string>(_p.ActiveMissions.Select(m => Def(m.Id).Counter));
                var pool = _defs.Values
                    .Where(d => d.MinRank <= _p.Rank && !activeIds.Contains(d.Id) && !_p.CompletedMissions.Contains(d.Id))
                    .ToList();
                // Prefere contadores diferentes das ativas (variedade).
                var varied = pool.Where(d => !activeCounters.Contains(d.Counter)).ToList();
                if (varied.Count > 0) pool = varied;
                if (pool.Count == 0)
                {
                    // Banco esgotado no ciclo: libera as já concluídas.
                    pool = _defs.Values.Where(d => !activeIds.Contains(d.Id)).ToList();
                    if (pool.Count == 0) return;
                }
                var pick = pool.OrderBy(d => d.Id, StringComparer.Ordinal).ElementAt(_rng.Range(0, pool.Count));
                _p.ActiveMissions.Add(new MissionState { Id = pick.Id });
            }
        }
    }
}
