using System.Collections.Generic;
using Propulsor.Core.Economy;
using Propulsor.Core.Events;
using Propulsor.Core.Flow;
using Propulsor.Core.Gameplay;

namespace Propulsor.Core.Progress
{
    /// <summary>Conquista permanente: uma estatística acumulada atinge um limite.</summary>
    public sealed class AchievementDef
    {
        public string Id;
        public string Name;
        public string Description;
        /// <summary>Chave em <see cref="SessionProfile.Stats"/>, ou "recorde", "prestigio".</summary>
        public string Stat;
        public double Threshold;
        public int Reward;
    }

    public readonly struct AchievementUnlocked
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Reward;
        public AchievementUnlocked(string id, string name, int reward) { Id = id; Name = name; Reward = reward; }
    }

    public static class AchievementCatalog
    {
        public const string BestStat = "recorde";
        public const string PrestigeStat = "prestigio";

        public static List<AchievementDef> CreateDefault() => new List<AchievementDef>
        {
            A("primeira_saida", "Primeira Saída", "Termine uma corrida.", StatKeys.Runs, 1, 100),
            A("veterana", "Veterana", "Termine 100 corridas.", StatKeys.Runs, 100, 1000),
            A("maratonista", "Maratonista", "Percorra 10.000 m no total.", StatKeys.TotalDistance, 10000, 500),
            A("ultramaratonista", "Ultramaratonista", "Percorra 100.000 m no total.", StatKeys.TotalDistance, 100000, 3000),
            A("recorde_1000", "Mil Metros", "Alcance 1.000 m numa corrida.", BestStat, 1000, 300),
            A("recorde_3000", "Três Mil", "Alcance 3.000 m numa corrida.", BestStat, 3000, 1000),
            A("colecionadora", "Colecionadora", "Colete 5.000 porcas no total.", StatKeys.TotalCoins, 5000, 500),
            A("pilota_de_tudo", "Pilota de Tudo", "Monte em 25 veículos.", StatKeys.VehiclesMounted, 25, 400),
            A("ferro_velho", "Ferro-Velho", "Perca 25 veículos.", StatKeys.VehiclesLost, 25, 300),
            A("dancarina_de_feixes", "Dançarina de Feixes", "Sobreviva a 30 sequências de feixes.", StatKeys.LasersSurvived, 30, 500),
            A("fichada", "Fichada", "Jogue 100 vezes na Garra da Sorte.", StatKeys.ClawPlays, 100, 500),
            A("pe_frio", "Pé-Frio", "Saia sem prêmio da Garra 50 vezes.", StatKeys.ClawLosses, 50, 300),
            A("eletrizada", "Eletrizada", "Esbarre em arcos de faísca 25 vezes.", StatKeys.DeathsBy(HazardType.Zapper), 25, 200),
            A("fatiada", "Fatiada", "Seja atingida por feixes 25 vezes.", StatKeys.DeathsBy(HazardType.Laser), 25, 200),
            A("alvo_facil", "Alvo Fácil", "Seja atingida por drones 25 vezes.", StatKeys.DeathsBy(HazardType.Missile), 25, 200),
            A("missionaria", "Missionária", "Conclua 30 missões.", StatKeys.MissionsCompleted, 30, 800),
            A("consumista", "Cliente da Oficina", "Compre 10 itens.", StatKeys.ItemsBought, 10, 500),
            A("selo_de_prestigio", "Selo de Prestígio", "Troque a patente máxima por um selo.", PrestigeStat, 1, 2000),
        };

        private static AchievementDef A(string id, string name, string desc, string stat, double threshold, int reward) =>
            new AchievementDef { Id = id, Name = name, Description = desc, Stat = stat, Threshold = threshold, Reward = reward };
    }

    /// <summary>Avalia conquistas (idempotente) e credita recompensas.</summary>
    public sealed class AchievementManager
    {
        private readonly SessionProfile _p;
        private readonly List<AchievementDef> _defs;
        private readonly EventBus _bus;

        public IReadOnlyList<AchievementDef> All => _defs;

        public AchievementManager(SessionProfile profile, List<AchievementDef> defs, EventBus bus = null)
        {
            _p = profile; _defs = defs; _bus = bus;
        }

        public bool IsUnlocked(string id) => _p.Achievements.Contains(id);

        public double Value(AchievementDef d) => d.Stat switch
        {
            AchievementCatalog.BestStat => _p.BestDistance,
            AchievementCatalog.PrestigeStat => _p.Prestige,
            _ => _p.Stat(d.Stat)
        };

        /// <summary>Desbloqueia o que for devido; retorna as porcas de recompensa (já creditadas).</summary>
        public int Evaluate(Wallet wallet, List<AchievementUnlocked> unlocked = null)
        {
            int total = 0;
            foreach (var d in _defs)
            {
                if (IsUnlocked(d.Id) || Value(d) < d.Threshold) continue;
                _p.Achievements.Add(d.Id);
                wallet.Credit(Currency.Coins, d.Reward, "conquista:" + d.Id);
                total += d.Reward;
                var e = new AchievementUnlocked(d.Id, d.Name, d.Reward);
                unlocked?.Add(e);
                _bus?.Publish(e);
            }
            return total;
        }
    }
}
