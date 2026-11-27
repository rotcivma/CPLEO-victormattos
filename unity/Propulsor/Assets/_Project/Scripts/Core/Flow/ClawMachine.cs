using System;
using System.Collections.Generic;
using Propulsor.Core.Gameplay;

namespace Propulsor.Core.Flow
{
    public enum PrizeKind
    {
        Nothing,
        Coins,
        SecondChance,
        /// <summary>Arrancada de 750 m na próxima corrida.</summary>
        HeadStart,
        /// <summary>Porcas em dobro na próxima corrida.</summary>
        DoubleCoins,
        ExtraTokens
    }

    public readonly struct PrizeEntry
    {
        public readonly PrizeKind Kind;
        public readonly int Weight;
        public readonly int Value;
        public PrizeEntry(PrizeKind kind, int weight, int value = 0) { Kind = kind; Weight = weight; Value = value; }
        public override string ToString() => Value > 0 ? $"{Kind}({Value})" : Kind.ToString();
    }

    /// <summary>Tabela de prêmios da Garra da Sorte (docs/fase-4/02-modelo-de-dados.md). Pesos [E].</summary>
    public static class PrizeTable
    {
        public static readonly IReadOnlyList<PrizeEntry> Default = new[]
        {
            new PrizeEntry(PrizeKind.Nothing, 30),
            new PrizeEntry(PrizeKind.Coins, 30, 100),
            new PrizeEntry(PrizeKind.Coins, 15, 500),
            new PrizeEntry(PrizeKind.Coins, 5, 1000),
            new PrizeEntry(PrizeKind.SecondChance, 6),
            new PrizeEntry(PrizeKind.HeadStart, 6, 750),
            new PrizeEntry(PrizeKind.DoubleCoins, 4, 2),
            new PrizeEntry(PrizeKind.ExtraTokens, 4, 3),
        };
    }

    /// <summary>
    /// Garra da Sorte: 1 ficha = 1 tentativa; o prêmio é sorteado antes da animação
    /// (a UI só encena). Fichas restantes podem ser trocadas por porcas.
    /// </summary>
    public sealed class ClawMachine
    {
        public const int CoinsPerToken = 100;

        private readonly IReadOnlyList<PrizeEntry> _table;
        private readonly IRandom _rng;
        private readonly int _totalWeight;

        public int Tokens { get; private set; }
        /// <summary>Porcas ganhas (prêmios + troca) ainda não creditadas.</summary>
        public int CoinsWon { get; private set; }
        public bool HeadStartWon { get; private set; }
        public bool DoubleCoinsWon { get; private set; }
        public int Plays { get; private set; }
        public int Losses { get; private set; }
        /// <summary>Garante prêmio na última ficha (gadget "Último Sortudo", Fase 8).</summary>
        public bool GuaranteeLastToken { get; set; }

        public ClawMachine(int tokens, IRandom rng, IReadOnlyList<PrizeEntry> table = null)
        {
            if (tokens < 0) throw new ArgumentOutOfRangeException(nameof(tokens));
            Tokens = tokens;
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _table = table ?? PrizeTable.Default;
            foreach (var e in _table) _totalWeight += e.Weight;
        }

        public bool CanPlay => Tokens > 0;

        /// <summary>Gasta uma ficha e sorteia o prêmio. Aplica efeitos internos (porcas, fichas extras, bônus).</summary>
        public PrizeEntry Play()
        {
            if (!CanPlay) throw new InvalidOperationException("Sem fichas");
            Tokens--;
            Plays++;
            var prize = Roll(GuaranteeLastToken && Tokens == 0);
            switch (prize.Kind)
            {
                case PrizeKind.Nothing: Losses++; break;
                case PrizeKind.Coins: CoinsWon += prize.Value; break;
                case PrizeKind.ExtraTokens: Tokens += prize.Value; break;
                case PrizeKind.HeadStart: HeadStartWon = true; break;
                case PrizeKind.DoubleCoins: DoubleCoinsWon = true; break;
                case PrizeKind.SecondChance: break; // o fluxo decide (revive imediato)
            }
            return prize;
        }

        /// <summary>Troca todas as fichas restantes por porcas.</summary>
        public int CashOut()
        {
            int coins = Tokens * CoinsPerToken;
            CoinsWon += coins;
            Tokens = 0;
            return coins;
        }

        /// <summary>Adiciona fichas (ex.: coletadas após um revive).</summary>
        public void AddTokens(int n) { if (n > 0) Tokens += n; }

        /// <summary>Retira e zera as porcas ganhas (para creditar no perfil).</summary>
        public int TakeCoins()
        {
            int c = CoinsWon;
            CoinsWon = 0;
            return c;
        }

        private PrizeEntry Roll(bool mustWin)
        {
            int total = _totalWeight;
            if (mustWin) foreach (var e in _table) if (e.Kind == PrizeKind.Nothing) total -= e.Weight;
            int r = _rng.Range(0, total);
            foreach (var e in _table)
            {
                if (mustWin && e.Kind == PrizeKind.Nothing) continue;
                if (r < e.Weight) return e;
                r -= e.Weight;
            }
            return _table[_table.Count - 1];
        }
    }
}
