using System;
using System.Collections.Generic;
using System.Linq;
using Propulsor.Core.Events;
using Propulsor.Core.Flow;

namespace Propulsor.Core.Economy
{
    public enum Currency { Coins, Tokens }

    public readonly struct BalanceChanged
    {
        public readonly Currency Currency;
        public readonly int Balance;
        public readonly int Delta;
        public readonly string Reason;
        public BalanceChanged(Currency c, int balance, int delta, string reason)
        {
            Currency = c; Balance = balance; Delta = delta; Reason = reason;
        }
    }

    /// <summary>Saldos do jogador. Toda alteração passa por aqui, com motivo (rastreável em teste).</summary>
    public sealed class Wallet
    {
        private readonly SessionProfile _p;
        private readonly EventBus _bus;

        public Wallet(SessionProfile profile, EventBus bus = null) { _p = profile; _bus = bus; }

        public int Coins => _p.Coins;
        public int Tokens => _p.BankedTokens;

        public void Credit(Currency c, int amount, string reason)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;
            Set(c, Get(c) + amount, amount, reason);
        }

        /// <summary>Debita se houver saldo; nunca deixa negativo.</summary>
        public bool TryDebit(Currency c, int amount, string reason)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Get(c) < amount) return false;
            if (amount > 0) Set(c, Get(c) - amount, -amount, reason);
            return true;
        }

        public int Get(Currency c) => c == Currency.Coins ? _p.Coins : _p.BankedTokens;

        private void Set(Currency c, int value, int delta, string reason)
        {
            if (c == Currency.Coins) _p.Coins = value; else _p.BankedTokens = value;
            _bus?.Publish(new BalanceChanged(c, value, delta, reason));
        }
    }

    public enum EquipResult { Ok, NotOwned, NotEquippable, NoFreeSlot, NotFound }

    /// <summary>Posse e equipamento de itens.</summary>
    public sealed class Inventory
    {
        private readonly SessionProfile _p;
        private readonly Dictionary<string, ItemDef> _byId;

        public IReadOnlyCollection<ItemDef> Items => _byId.Values;

        public Inventory(SessionProfile profile, IEnumerable<ItemDef> catalog)
        {
            _p = profile;
            _byId = catalog.ToDictionary(i => i.Id);
            // Itens gratuitos sempre possuídos e equipados se o espaço estiver vazio.
            foreach (var item in _byId.Values.Where(i => i.FreeByDefault))
            {
                _p.Owned.Add(item.Id);
                if (item.Slot != null && !_p.Equipped.ContainsKey(item.Slot)) _p.Equipped[item.Slot] = item.Id;
            }
            if (_p.Owned.Contains(ItemIds.GadgetSlot3)) _p.GadgetSlots = Math.Max(_p.GadgetSlots, 3);
            // Remove gadgets inválidos (ex.: save antigo).
            _p.Gadgets.RemoveAll(g => !_byId.ContainsKey(g) || !_p.Owned.Contains(g));
        }

        public ItemDef Find(string id) => id != null && _byId.TryGetValue(id, out var i) ? i : null;
        public bool Owns(string id) => _p.Owned.Contains(id);
        public int Count(string id) => _p.Count(id);
        public IReadOnlyList<string> Gadgets => _p.Gadgets;
        public int GadgetSlots => _p.GadgetSlots;

        public bool IsEquipped(string id)
        {
            var item = Find(id);
            if (item == null) return false;
            if (item.Category == ItemCategory.Gadget) return _p.Gadgets.Contains(id);
            return item.Slot != null && _p.Equipped.TryGetValue(item.Slot, out var cur) && cur == id;
        }

        public string EquippedIn(string slot) => _p.Equipped.TryGetValue(slot, out var id) ? id : null;

        public IEnumerable<ItemDef> EquippedGadgets() => _p.Gadgets.Select(Find).Where(i => i != null);

        public EquipResult Equip(string id)
        {
            var item = Find(id);
            if (item == null) return EquipResult.NotFound;
            if (!item.IsEquippable) return EquipResult.NotEquippable;
            if (!Owns(id)) return EquipResult.NotOwned;
            if (item.Category == ItemCategory.Gadget)
            {
                if (_p.Gadgets.Contains(id)) return EquipResult.Ok;
                if (_p.Gadgets.Count >= _p.GadgetSlots) return EquipResult.NoFreeSlot;
                _p.Gadgets.Add(id);
                return EquipResult.Ok;
            }
            _p.Equipped[item.Slot] = id;
            return EquipResult.Ok;
        }

        /// <summary>Desequipa (gadgets e acessórios; roupas/propulsor voltam ao padrão).</summary>
        public void Unequip(string id)
        {
            var item = Find(id);
            if (item == null) return;
            if (item.Category == ItemCategory.Gadget) { _p.Gadgets.Remove(id); return; }
            if (item.Slot == null || EquippedIn(item.Slot) != id) return;
            var fallback = _byId.Values.FirstOrDefault(i => i.Slot == item.Slot && i.FreeByDefault);
            if (fallback != null) _p.Equipped[item.Slot] = fallback.Id;
            else _p.Equipped.Remove(item.Slot);
        }

        /// <summary>Concede o item (compra). Consumíveis somam quantidade.</summary>
        internal void Grant(ItemDef item)
        {
            switch (item.Category)
            {
                case ItemCategory.Consumable when item.Id == ItemIds.TokenPack:
                    _p.BankedTokens += item.Amount;
                    break;
                case ItemCategory.Consumable:
                    _p.Consumables[item.Id] = _p.Count(item.Id) + item.Amount;
                    break;
                case ItemCategory.Upgrade:
                    _p.Owned.Add(item.Id);
                    if (item.Id == ItemIds.GadgetSlot3) _p.GadgetSlots = 3;
                    break;
                default:
                    _p.Owned.Add(item.Id);
                    break;
            }
        }

        /// <summary>Gasta um consumível. Retorna false se não houver.</summary>
        public bool Consume(string id)
        {
            int n = _p.Count(id);
            if (n <= 0) return false;
            _p.Consumables[id] = n - 1;
            return true;
        }
    }

    public enum PurchaseResult { Ok, InsufficientFunds, AlreadyOwned, StackFull, NotFound }

    public readonly struct PurchaseCompleted
    {
        public readonly string ItemId;
        public PurchaseCompleted(string id) { ItemId = id; }
    }

    /// <summary>Compras da Oficina: valida → debita → concede (atômico).</summary>
    public sealed class Store
    {
        private readonly Wallet _wallet;
        private readonly Inventory _inv;
        private readonly SessionProfile _p;
        private readonly EventBus _bus;

        public Store(Wallet wallet, Inventory inventory, SessionProfile profile, EventBus bus = null)
        {
            _wallet = wallet; _inv = inventory; _p = profile; _bus = bus;
        }

        public PurchaseResult CanBuy(string id)
        {
            var item = _inv.Find(id);
            if (item == null) return PurchaseResult.NotFound;
            if (item.IsConsumable)
            {
                int have = item.Id == ItemIds.TokenPack ? _p.BankedTokens : _inv.Count(id);
                if (have + item.Amount > item.MaxStack) return PurchaseResult.StackFull;
            }
            else if (_inv.Owns(id)) return PurchaseResult.AlreadyOwned;
            if (_wallet.Coins < item.Price) return PurchaseResult.InsufficientFunds;
            return PurchaseResult.Ok;
        }

        public PurchaseResult Buy(string id)
        {
            var check = CanBuy(id);
            if (check != PurchaseResult.Ok) return check;
            var item = _inv.Find(id);
            if (!_wallet.TryDebit(Currency.Coins, item.Price, "compra:" + id)) return PurchaseResult.InsufficientFunds;
            _inv.Grant(item);
            _p.AddStat(StatKeys.ItemsBought, 1);
            _bus?.Publish(new PurchaseCompleted(id));
            return PurchaseResult.Ok;
        }
    }

    /// <summary>Chaves das estatísticas acumuladas (perfil).</summary>
    public static class StatKeys
    {
        public const string Runs = "corridas";
        public const string TotalDistance = "distancia_total";
        public const string TotalCoins = "porcas_total";
        public const string TotalTokens = "fichas_total";
        public const string VehiclesMounted = "veiculos_montados";
        public const string VehiclesLost = "veiculos_perdidos";
        public const string LasersSurvived = "feixes_sobrevividos";
        public const string ClawPlays = "garra_jogadas";
        public const string ClawLosses = "garra_perdas";
        public const string MissionsCompleted = "missoes_concluidas";
        public const string ItemsBought = "itens_comprados";
        public static string DeathsBy(Gameplay.HazardType h) => "mortes_" + h.ToString().ToLowerInvariant();
    }
}
