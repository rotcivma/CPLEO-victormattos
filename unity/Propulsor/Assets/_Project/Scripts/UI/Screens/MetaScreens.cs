using System.Collections.Generic;
using System.Linq;
using Propulsor.Core.Economy;
using Propulsor.Core.Progress;
using UnityEngine;
using UnityEngine.UI;

namespace Propulsor.UI
{
    /// <summary>Base dos painéis modais com título, saldo e botão de voltar.</summary>
    public abstract class ModalScreen : UIScreen
    {
        protected override ScreenAnim Anim => ScreenAnim.SlideUp;
        protected RectTransform Body;
        private Text _balance;

        protected abstract string Title { get; }

        protected override void Build(RectTransform root)
        {
            UIFactory.Panel("Fundo", root, new Color(0.106f, 0.122f, 0.231f, 0.97f)).rectTransform.Anchor(-0.1f, -0.1f, 1.1f, 1.1f);
            UIFactory.Button("Voltar", root, "←", Theme.BgPanel, () => Hide(), 56)
                .GetComponent<RectTransform>().Place(0.05f, 0.92f, 110f, 96f);
            UIFactory.Label("Titulo", root, Title, 64, Theme.Text, bold: true).rectTransform.Place(0.5f, 0.92f, 1000f, 90f);
            _balance = UIFactory.Label("Saldo", root, "", 38, Theme.Gold, TextAnchor.MiddleRight, true);
            _balance.rectTransform.Place(0.83f, 0.92f, 560f, 60f);
            Body = UIFactory.Rect("Corpo", root).Anchor(0.03f, 0.03f, 0.97f, 0.84f);
            BuildBody(Body);
        }

        protected abstract void BuildBody(RectTransform body);

        protected override void OnShow() => Refresh();

        protected virtual void Refresh()
        {
            var p = Ui.Controller.Profile;
            _balance.text = $"Porcas {p.Coins:N0}   Fichas {p.BankedTokens}";
        }
    }

    /// <summary>
    /// S10 · Oficina (loja + inventário): abas por categoria, grade paginada de cartões,
    /// detalhe do item com Comprar / Equipar / Retirar e a fileira de gadgets equipados.
    /// </summary>
    public sealed class WorkshopScreen : ModalScreen
    {
        private const int PerPage = 6;

        private static readonly (string label, ItemCategory[] cats)[] Tabs =
        {
            ("Propulsores", new[] { ItemCategory.Jetpack }),
            ("Veículos", new[] { ItemCategory.VehicleSkin }),
            ("Gadgets", new[] { ItemCategory.Gadget }),
            ("Consumíveis", new[] { ItemCategory.Consumable }),
            ("Visual", new[] { ItemCategory.Outfit, ItemCategory.Hat }),
            ("Melhorias", new[] { ItemCategory.Upgrade }),
        };

        protected override string Title => "OFICINA";

        private int _tab, _page;
        private string _selected;
        private readonly List<Button> _cards = new List<Button>();
        private readonly List<Button> _tabButtons = new List<Button>();
        private Text _pageLabel, _name, _desc, _status, _loadout;
        private Button _action;
        private RectTransform _swatch;
        private List<ItemDef> _items = new List<ItemDef>();

        protected override void BuildBody(RectTransform body)
        {
            for (int i = 0; i < Tabs.Length; i++)
            {
                int t = i;
                var b = UIFactory.Button("Aba" + i, body, Tabs[i].label, Theme.BgPanel, () => { _tab = t; _page = 0; _selected = null; Refresh(); }, 30);
                b.GetComponent<RectTransform>().Place((i + 0.5f) / Tabs.Length, 0.94f, 280f, 80f);
                _tabButtons.Add(b);
            }

            var grid = UIFactory.Rect("Grade", body).Anchor(0f, 0.12f, 0.55f, 0.86f);
            for (int i = 0; i < PerPage; i++)
            {
                int slot = i;
                var card = UIFactory.Button("Cartao" + i, grid, "", Theme.BgPanel, () => Select(slot), 26);
                card.GetComponent<RectTransform>().Place((i % 3 + 0.5f) / 3f, i < 3 ? 0.75f : 0.27f, 300f, 300f);
                _cards.Add(card);
            }
            UIFactory.Button("Anterior", body, "◀", Theme.BgPanel, () => { _page--; Refresh(); }, 40)
                .GetComponent<RectTransform>().Place(0.08f, 0.05f, 110f, 80f);
            _pageLabel = UIFactory.Label("Pagina", body, "", 30, Theme.TextDim);
            _pageLabel.rectTransform.Place(0.275f, 0.05f, 300f, 60f);
            UIFactory.Button("Proxima", body, "▶", Theme.BgPanel, () => { _page++; Refresh(); }, 40)
                .GetComponent<RectTransform>().Place(0.47f, 0.05f, 110f, 80f);

            var detail = UIFactory.Panel("Detalhe", body, Theme.BgPanel).rectTransform.Anchor(0.58f, 0.12f, 1f, 0.86f);
            _swatch = UIFactory.Panel("Amostra", detail, Theme.Primary).rectTransform.Place(0.5f, 0.78f, 180f, 180f);
            _name = UIFactory.Label("Nome", detail, "", 44, Theme.Text, bold: true);
            _name.rectTransform.Place(0.5f, 0.56f, 700f, 70f);
            _desc = UIFactory.Label("Descricao", detail, "", 30, Theme.TextDim);
            _desc.rectTransform.Place(0.5f, 0.45f, 700f, 60f);
            _status = UIFactory.Label("Status", detail, "", 32, Theme.Gold, bold: true);
            _status.rectTransform.Place(0.5f, 0.33f, 700f, 60f);
            _action = UIFactory.Button("Acao", detail, "", Theme.Accent, OnAction, 40);
            _action.GetComponent<RectTransform>().Place(0.5f, 0.14f, 480f, 110f);

            _loadout = UIFactory.Label("Equipados", body, "", 30, Theme.Text, TextAnchor.MiddleLeft);
            _loadout.rectTransform.Place(0.79f, 0.05f, 760f, 60f);
        }

        private Inventory Inv => Ui.Controller.Services.Inventory;

        protected override void Refresh()
        {
            base.Refresh();
            var cats = Tabs[_tab].cats;
            _items = Ui.Controller.Services.Catalog.Where(i => cats.Contains(i.Category)).ToList();
            int pages = Mathf.Max(1, Mathf.CeilToInt(_items.Count / (float)PerPage));
            _page = Mathf.Clamp(_page, 0, pages - 1);
            _pageLabel.text = $"{_page + 1}/{pages}";

            for (int i = 0; i < _tabButtons.Count; i++)
                _tabButtons[i].transform.Find("Face").GetComponent<Image>().color = i == _tab ? Theme.Primary : Theme.BgPanel;

            for (int i = 0; i < PerPage; i++)
            {
                int idx = _page * PerPage + i;
                var card = _cards[i];
                card.gameObject.SetActive(idx < _items.Count);
                if (idx >= _items.Count) continue;
                var item = _items[idx];
                card.SetLabel($"{item.Name}\n\n{CardStatus(item)}");
                card.transform.Find("Face").GetComponent<Image>().color = item.Id == _selected ? Theme.Accent : Theme.BgPanel;
            }

            if (_selected == null && _items.Count > 0) _selected = _items[0].Id;
            ShowDetail(Inv.Find(_selected));

            var gadgetNames = Inv.EquippedGadgets().Select(g => g.Name).ToList();
            while (gadgetNames.Count < Inv.GadgetSlots) gadgetNames.Add("—");
            if (Inv.GadgetSlots < 3) gadgetNames.Add("🔒");
            _loadout.text = "Gadgets: " + string.Join("  |  ", gadgetNames);
        }

        private string CardStatus(ItemDef item)
        {
            if (item.IsConsumable)
                return item.Id == Propulsor.Core.Flow.ItemIds.TokenPack ? $"{item.Price:N0}" : $"x{Inv.Count(item.Id)}  ·  {item.Price:N0}";
            if (Inv.IsEquipped(item.Id)) return "EQUIPADO ✓";
            if (Inv.Owns(item.Id)) return "✓ COMPRADO";
            return $"{item.Price:N0} porcas";
        }

        private void Select(int slot)
        {
            int idx = _page * PerPage + slot;
            if (idx < _items.Count) _selected = _items[idx].Id;
            Refresh();
        }

        private void ShowDetail(ItemDef item)
        {
            _action.gameObject.SetActive(item != null);
            if (item == null) { _name.text = _desc.text = _status.text = ""; return; }
            _name.text = item.Name;
            _desc.text = item.Description;
            _swatch.GetComponent<Image>().color = item.ColorA != 0 ? Propulsor.Gameplay.PlayerView.Hex(item.ColorA) : Theme.Primary;

            var store = Ui.Controller.Services.Store;
            if (item.IsConsumable || !Inv.Owns(item.Id))
            {
                var can = store.CanBuy(item.Id);
                _status.text = can switch
                {
                    PurchaseResult.InsufficientFunds => "Porcas insuficientes",
                    PurchaseResult.StackFull => "Limite atingido",
                    _ => item.IsConsumable ? $"Você tem: {(item.Id == Propulsor.Core.Flow.ItemIds.TokenPack ? Ui.Controller.Profile.BankedTokens : Inv.Count(item.Id))}" : ""
                };
                _status.color = can == PurchaseResult.Ok ? Theme.Gold : Theme.Danger;
                _action.SetLabel($"COMPRAR {item.Price:N0}");
                _action.interactable = can == PurchaseResult.Ok;
            }
            else if (!item.IsEquippable)
            {
                _status.text = "Já é seu";
                _status.color = Theme.Ok;
                _action.gameObject.SetActive(false);
            }
            else if (Inv.IsEquipped(item.Id))
            {
                _status.text = "Equipado";
                _status.color = Theme.Ok;
                bool removable = item.Category == ItemCategory.Gadget || item.Category == ItemCategory.Hat ||
                                 item.Category == ItemCategory.VehicleSkin || !item.FreeByDefault;
                _action.gameObject.SetActive(removable);
                _action.SetLabel("RETIRAR");
                _action.interactable = true;
            }
            else
            {
                _status.text = "Na sua oficina";
                _status.color = Theme.TextDim;
                _action.SetLabel("EQUIPAR");
                _action.interactable = true;
            }
        }

        private void OnAction()
        {
            var item = Inv.Find(_selected);
            if (item == null) return;
            var c = Ui.Controller;
            if (item.IsConsumable || !Inv.Owns(item.Id))
            {
                var r = c.Buy(item.Id);
                Ui.ShowToast(r == PurchaseResult.Ok ? $"Comprado: {item.Name}" : "Não foi possível comprar");
                if (r == PurchaseResult.Ok && item.IsEquippable && !item.IsConsumable && c.Equip(item.Id) == EquipResult.NoFreeSlot)
                    Ui.ShowToast("Espaços de gadget cheios: retire um para equipar");
            }
            else if (Inv.IsEquipped(item.Id)) c.Unequip(item.Id);
            else if (c.Equip(item.Id) == EquipResult.NoFreeSlot) Ui.ShowToast("Espaços de gadget cheios: retire um primeiro");
            Refresh();
        }
    }

    /// <summary>S09 · Missões: patente, barra de estrelas, 3 missões com progresso e "pular".</summary>
    public sealed class MissionsScreen : ModalScreen
    {
        protected override string Title => "MISSÕES";
        private Text _rank, _prestige;
        private RectTransform _bar;
        private readonly Text[] _rows = new Text[MissionManager.ActiveCount];
        private readonly Button[] _skips = new Button[MissionManager.ActiveCount];
        private Button _prestigeButton;

        protected override void BuildBody(RectTransform body)
        {
            _rank = UIFactory.Label("Patente", body, "", 44, Theme.Text, TextAnchor.MiddleLeft, true);
            _rank.rectTransform.Place(0.35f, 0.9f, 1200f, 70f);
            _bar = UIFactory.ProgressBar("Estrelas", body, Theme.Gold);
            ((RectTransform)_bar.parent).Place(0.35f, 0.8f, 1200f, 30f);
            _prestige = UIFactory.Label("Selos", body, "", 32, Theme.TextDim, TextAnchor.MiddleRight);
            _prestige.rectTransform.Place(0.85f, 0.9f, 500f, 60f);

            for (int i = 0; i < _rows.Length; i++)
            {
                float y = 0.62f - i * 0.17f;
                UIFactory.Panel("Linha" + i, body, Theme.BgPanel).rectTransform.Place(0.4f, y, 1400f, 130f);
                _rows[i] = UIFactory.Label("Texto" + i, body, "", 34, Theme.Text, TextAnchor.MiddleLeft);
                _rows[i].rectTransform.Place(0.38f, y, 1300f, 120f);
                int idx = i;
                _skips[i] = UIFactory.Button("Pular" + i, body, "", Theme.Accent, () => Skip(idx), 30);
                _skips[i].GetComponent<RectTransform>().Place(0.88f, y, 360f, 100f);
            }

            _prestigeButton = UIFactory.Button("Prestigio", body, "TROCAR POR SELO DE PRESTÍGIO", Theme.Gold, () =>
            {
                if (Ui.Controller.Services.PrestigeReset()) Ui.ShowToast("Novo selo! As missões recomeçam.");
                Refresh();
            }, 36);
            _prestigeButton.GetComponent<RectTransform>().Place(0.5f, 0.07f, 900f, 100f);
        }

        protected override void Refresh()
        {
            base.Refresh();
            var mm = Ui.Controller.Services.Missions;
            var p = Ui.Controller.Profile;
            var info = mm.RankInfo;
            _rank.text = $"Nível {p.Rank} · \"{info.Name}\"" + (info.StarsToNext > 0 ? $"   {p.StarsInRank}/{info.StarsToNext} ★" : "");
            _bar.localScale = new Vector3(info.StarsToNext > 0 ? p.StarsInRank / (float)info.StarsToNext : 1f, 1f, 1f);
            _prestige.text = $"Selos de prestígio: {p.Prestige}";
            _prestigeButton.gameObject.SetActive(mm.CanPrestige);

            for (int i = 0; i < _rows.Length; i++)
            {
                bool has = i < mm.Active.Count;
                _rows[i].gameObject.SetActive(has);
                _skips[i].gameObject.SetActive(has && !mm.Active[i].Done);
                if (!has) continue;
                _rows[i].text = MissionLine(mm, mm.Active[i]);
                _skips[i].SetLabel($"PULAR {mm.SkipCost(mm.Active[i].Id):N0}");
                _skips[i].interactable = p.Coins >= mm.SkipCost(mm.Active[i].Id);
            }
        }

        public static string MissionLine(MissionManager mm, Propulsor.Core.Flow.MissionState s)
        {
            var d = mm.Def(s.Id);
            string stars = new string('★', d.Stars);
            if (s.Done) return $"{stars}  {d.Describe()}   ✓ concluída";
            return $"{stars}  {d.Describe()}   {Mathf.FloorToInt(mm.Current(s)):N0}/{d.Target:N0}";
        }

        private void Skip(int i)
        {
            var mm = Ui.Controller.Services.Missions;
            if (i < mm.Active.Count && Ui.Controller.Services.SkipMission(mm.Active[i].Id))
                Ui.ShowToast("Missão concluída! As estrelas contam ao fim da próxima corrida.");
            Refresh();
        }
    }

    /// <summary>S15 · Conquistas: lista paginada com progresso.</summary>
    public sealed class AchievementsScreen : ModalScreen
    {
        private const int PerPage = 6;
        protected override string Title => "CONQUISTAS";
        private readonly Text[] _rows = new Text[PerPage];
        private readonly Image[] _icons = new Image[PerPage];
        private Text _page;
        private int _p;

        protected override void BuildBody(RectTransform body)
        {
            for (int i = 0; i < PerPage; i++)
            {
                float y = 0.9f - i * 0.135f;
                _icons[i] = UIFactory.Panel("Icone" + i, body, Theme.TextDim);
                _icons[i].rectTransform.Place(0.05f, y, 90f, 90f);
                _rows[i] = UIFactory.Label("Linha" + i, body, "", 32, Theme.Text, TextAnchor.MiddleLeft);
                _rows[i].rectTransform.Place(0.52f, y, 1600f, 100f);
            }
            UIFactory.Button("Anterior", body, "◀", Theme.BgPanel, () => { _p--; Refresh(); }, 40)
                .GetComponent<RectTransform>().Place(0.38f, 0.05f, 110f, 80f);
            _page = UIFactory.Label("Pagina", body, "", 30, Theme.TextDim);
            _page.rectTransform.Place(0.5f, 0.05f, 200f, 60f);
            UIFactory.Button("Proxima", body, "▶", Theme.BgPanel, () => { _p++; Refresh(); }, 40)
                .GetComponent<RectTransform>().Place(0.62f, 0.05f, 110f, 80f);
        }

        protected override void Refresh()
        {
            base.Refresh();
            var am = Ui.Controller.Services.Achievements;
            var all = am.All;
            int pages = Mathf.Max(1, Mathf.CeilToInt(all.Count / (float)PerPage));
            _p = Mathf.Clamp(_p, 0, pages - 1);
            _page.text = $"{_p + 1}/{pages}  ·  {all.Count(a => am.IsUnlocked(a.Id))}/{all.Count}";
            for (int i = 0; i < PerPage; i++)
            {
                int idx = _p * PerPage + i;
                bool has = idx < all.Count;
                _rows[i].gameObject.SetActive(has);
                _icons[i].gameObject.SetActive(has);
                if (!has) continue;
                var a = all[idx];
                bool done = am.IsUnlocked(a.Id);
                double v = System.Math.Min(am.Value(a), a.Threshold);
                _icons[i].color = done ? Theme.Gold : Theme.BgPanel;
                _rows[i].color = done ? Theme.Text : Theme.TextDim;
                _rows[i].text = $"{a.Name} — {a.Description}   " + (done ? $"✓ (+{a.Reward} porcas)" : $"{v:N0}/{a.Threshold:N0}");
            }
        }
    }
}
