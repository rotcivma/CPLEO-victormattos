using System.Collections.Generic;

namespace Propulsor.Core.Flow
{
    /// <summary>Resumo de uma corrida encerrada.</summary>
    public readonly struct RunSummary
    {
        public readonly int Distance;
        public readonly int RunCoins;
        public readonly int ClawCoins;
        public readonly bool IsNewBest;
        public readonly int PreviousBest;
        public readonly int BalanceBefore;
        public readonly int BalanceAfter;
        /// <summary>Porcas de recompensas (missões, patentes, conquistas) creditadas nesta corrida.</summary>
        public readonly int RewardCoins;

        public RunSummary(int distance, int runCoins, int clawCoins, bool isNewBest, int previousBest,
                          int balanceBefore, int balanceAfter, int rewardCoins = 0)
        {
            Distance = distance; RunCoins = runCoins; ClawCoins = clawCoins; IsNewBest = isNewBest;
            PreviousBest = previousBest; BalanceBefore = balanceBefore; BalanceAfter = balanceAfter;
            RewardCoins = rewardCoins;
        }

        public RunSummary WithRewards(int rewardCoins, int balanceAfter) =>
            new RunSummary(Distance, RunCoins, ClawCoins, IsNewBest, PreviousBest, BalanceBefore, balanceAfter, rewardCoins);
    }

    /// <summary>Estado de uma missão ativa.</summary>
    public sealed class MissionState
    {
        public string Id;
        /// <summary>Progresso acumulado em corridas anteriores (missões "no total").</summary>
        public float Progress;
        public bool Done;
    }

    /// <summary>
    /// Perfil do jogador — é exatamente o que é salvo em disco (docs/fase-4/02-modelo-de-dados.md §2).
    /// As regras que o alteram ficam em Wallet, Inventory, Store, MissionManager e AchievementManager.
    /// </summary>
    public sealed class SessionProfile
    {
        public const int CurrentSchema = 1;
        public int SchemaVersion = CurrentSchema;

        // Carteira
        public int Coins;
        /// <summary>Fichas guardadas (fora da corrida).</summary>
        public int BankedTokens;

        // Recordes e bônus pendentes (ganhos na Garra da Sorte)
        public int BestDistance;
        public bool PendingHeadStart;
        public bool PendingDoubleCoins;

        // Inventário
        public readonly HashSet<string> Owned = new HashSet<string>();
        public readonly Dictionary<string, int> Consumables = new Dictionary<string, int>();
        /// <summary>Item equipado por espaço: jetpack, outfit, hat, skin:&lt;veículo&gt;.</summary>
        public readonly Dictionary<string, string> Equipped = new Dictionary<string, string>();
        public readonly List<string> Gadgets = new List<string>();
        public int GadgetSlots = 2;

        // Progressão
        public int Rank = 1;
        public int StarsInRank;
        public int Prestige;
        public readonly List<MissionState> ActiveMissions = new List<MissionState>();
        /// <summary>Missões já concluídas no ciclo atual de patentes (não voltam até o prestígio).</summary>
        public readonly HashSet<string> CompletedMissions = new HashSet<string>();

        // Estatísticas acumuladas e conquistas
        public readonly Dictionary<string, double> Stats = new Dictionary<string, double>();
        public readonly HashSet<string> Achievements = new HashSet<string>();

        // Configurações
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public bool Vibration = true;

        /// <summary>Consumível "Segunda Chance".</summary>
        public int SecondChances
        {
            get => Count(ItemIds.SecondChance);
            set => Consumables[ItemIds.SecondChance] = value < 0 ? 0 : value;
        }

        /// <summary>Consumível "Arrancada".</summary>
        public int HeadStarts
        {
            get => Count(ItemIds.HeadStart);
            set => Consumables[ItemIds.HeadStart] = value < 0 ? 0 : value;
        }

        public int Count(string consumableId) => Consumables.TryGetValue(consumableId, out var n) ? n : 0;

        public double Stat(string key) => Stats.TryGetValue(key, out var v) ? v : 0;
        public void AddStat(string key, double amount) => Stats[key] = Stat(key) + amount;
        public void MaxStat(string key, double value) { if (value > Stat(key)) Stats[key] = value; }

        /// <summary>Credita a corrida (porcas da corrida + garra) e atualiza o recorde.</summary>
        public RunSummary ApplyRun(float distance, int runCoins, int clawCoins)
        {
            int d = (int)distance;
            int before = Coins;
            int prevBest = BestDistance;
            bool best = d > BestDistance;
            if (best) BestDistance = d;
            Coins += runCoins + clawCoins;
            return new RunSummary(d, runCoins, clawCoins, best, prevBest, before, Coins);
        }
    }

    /// <summary>Ids fixos usados pelas regras.</summary>
    public static class ItemIds
    {
        public const string SecondChance = "segunda_chance";
        public const string HeadStart = "arrancada";
        public const string TokenPack = "pacote_fichas";
        public const string GadgetSlot3 = "slot_gadget_3";
        public const string DefaultJetpack = "jet_padrao";
        public const string DefaultOutfit = "macacao_verde";
    }
}
