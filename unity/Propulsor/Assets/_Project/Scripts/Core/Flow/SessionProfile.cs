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

        public RunSummary(int distance, int runCoins, int clawCoins, bool isNewBest, int previousBest,
                          int balanceBefore, int balanceAfter)
        {
            Distance = distance; RunCoins = runCoins; ClawCoins = clawCoins; IsNewBest = isNewBest;
            PreviousBest = previousBest; BalanceBefore = balanceBefore; BalanceAfter = balanceAfter;
        }
    }

    /// <summary>
    /// Perfil da sessão usado pelos menus (Fase 7). Na Fase 8 ele passa a ser
    /// persistido pelo SaveManager e alimentado pela economia/inventário.
    /// </summary>
    public sealed class SessionProfile
    {
        public int Coins;
        public int BestDistance;
        /// <summary>Fichas guardadas (fora da corrida).</summary>
        public int BankedTokens;
        /// <summary>Consumível "Segunda Chance".</summary>
        public int SecondChances;
        public bool PendingHeadStart;
        public bool PendingDoubleCoins;

        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public bool Vibration = true;

        /// <summary>Credita a corrida e atualiza o recorde.</summary>
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
}
