using Propulsor.Core.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace Propulsor.UI
{
    /// <summary>S04 · HUD: distância, recorde, porcas, pausa, contagem e avisos em fila.</summary>
    public sealed class HudScreen : UIScreen
    {
        private Text _distance, _best, _coins, _countdown, _popup;
        private RectTransform _popupBox;
        private int _lastCoins = -1;
        private float _coinPop;

        protected override void Build(RectTransform root)
        {
            _distance = UIFactory.Label("Distancia", root, "0 m", 84, Theme.Text, TextAnchor.UpperLeft, true);
            _distance.rectTransform.Anchor(0.03f, 0.84f, 0.6f, 0.97f);
            _best = UIFactory.Label("Melhor", root, "", 30, Theme.TextDim, TextAnchor.UpperLeft);
            _best.rectTransform.Anchor(0.03f, 0.79f, 0.6f, 0.85f);
            _coins = UIFactory.Label("Porcas", root, "", 44, Theme.Gold, TextAnchor.UpperLeft, true);
            _coins.rectTransform.Anchor(0.03f, 0.72f, 0.4f, 0.79f);

            UIFactory.Button("Pausa", root, "II", Theme.BgPanel, () => Ui.Controller.Pause(), 44)
                .GetComponent<RectTransform>().Place(0.95f, 0.92f, 96f, 96f);

            _countdown = UIFactory.Label("Contagem", root, "", 200, Theme.Text, bold: true);
            _countdown.rectTransform.Place(0.5f, 0.55f, 400f, 260f);

            var box = UIFactory.Panel("Aviso", root, Theme.BgPanel);
            box.raycastTarget = false;
            _popupBox = box.rectTransform.Place(0.5f, 0.1f, 900f, 90f);
            _popup = UIFactory.Label("Texto", _popupBox, "", 38, Theme.Text, bold: true);
            _popup.rectTransform.Anchor(0f, 0f, 1f, 1f);
        }

        protected override void OnShow() => _lastCoins = -1;

        public override void Tick(float dt)
        {
            var c = Ui.Controller;
            var sim = c.Driver.Sim;
            int d = Mathf.FloorToInt(sim.Distance);
            _distance.text = $"{d:N0} m";
            bool record = c.Profile.BestDistance > 0 && d > c.Profile.BestDistance;
            _distance.color = record ? Theme.Gold : Theme.Text; // dourado após passar o recorde
            _best.text = c.Profile.BestDistance > 0 ? $"melhor {c.Profile.BestDistance:N0} m" : "";

            if (sim.RunCoins != _lastCoins)
            {
                if (_lastCoins >= 0) _coinPop = 0.08f;
                _lastCoins = sim.RunCoins;
                _coins.text = $"● {sim.RunCoins}";
            }
            _coinPop = Mathf.Max(0f, _coinPop - dt);
            _coins.rectTransform.localScale = Vector3.one * (_coinPop > 0f ? 1.15f : 1f);

            _countdown.text = c.Flow.State == GameState.Countdown
                ? Mathf.CeilToInt(c.Flow.Remaining / GameFlow.CountdownTime * 3f).ToString()
                : "";

            var q = c.Popups;
            _popupBox.gameObject.SetActive(q.Current != null);
            if (q.Current != null)
            {
                _popup.text = q.Current;
                float v = q.Visibility;
                _popupBox.localScale = new Vector3(1f, v, 1f);
            }
        }
    }

    /// <summary>S05 · Pausa: continuar (com contagem), sons, desistir com confirmação.</summary>
    public sealed class PauseScreen : UIScreen
    {
        protected override ScreenAnim Anim => ScreenAnim.Pop;
        private Button _giveUp;
        private bool _confirming;

        protected override void Build(RectTransform root)
        {
            UIFactory.Panel("Escurecer", root, Theme.Dim).rectTransform.Anchor(-0.1f, -0.1f, 1.1f, 1.1f);
            var panel = UIFactory.Panel("Painel", root, Theme.BgPanel).rectTransform.Place(0.5f, 0.5f, 1150f, 760f);
            UIFactory.Label("Titulo", panel, "PAUSADO", 64, Theme.Text, bold: true).rectTransform.Place(0.5f, 0.88f, 900f, 90f);
            UIFactory.Label("Missoes", panel, "Missões\n(chegam na Fase 8)", 34, Theme.TextDim).rectTransform.Place(0.5f, 0.64f, 900f, 120f);

            UIFactory.Button("Continuar", panel, "▶ CONTINUAR", Theme.Primary, () => Ui.Controller.Resume())
                .GetComponent<RectTransform>().Place(0.5f, 0.38f, 520f, 120f);
            UIFactory.Button("Config", panel, "SOM", Theme.BgDeep, () => Ui.Settings.Show(), 36)
                .GetComponent<RectTransform>().Place(0.22f, 0.13f, 260f, 96f);
            _giveUp = UIFactory.Button("Desistir", panel, "✖ DESISTIR", Theme.Danger, OnGiveUp, 36);
            _giveUp.GetComponent<RectTransform>().Place(0.76f, 0.13f, 360f, 96f);
        }

        protected override void OnShow()
        {
            _confirming = false;
            _giveUp.SetLabel("✖ DESISTIR");
        }

        private void OnGiveUp()
        {
            if (!_confirming) { _confirming = true; _giveUp.SetLabel("CONFIRMAR?"); return; }
            Ui.Controller.GiveUp();
        }
    }

    /// <summary>S08 · Oferta de Segunda Chance com anel de contagem de 4 s.</summary>
    public sealed class ReviveScreen : UIScreen
    {
        protected override ScreenAnim Anim => ScreenAnim.SlideUp;
        private Text _title, _seconds;
        private RectTransform _ring;

        protected override void Build(RectTransform root)
        {
            var panel = UIFactory.Panel("Painel", root, Theme.BgPanel).rectTransform.Place(0.5f, 0.5f, 960f, 540f);
            _title = UIFactory.Label("Titulo", panel, "", 48, Theme.Text, bold: true);
            _title.rectTransform.Place(0.5f, 0.82f, 900f, 80f);
            _ring = UIFactory.ProgressBar("Tempo", panel, Theme.Accent);
            ((RectTransform)_ring.parent).Place(0.5f, 0.6f, 600f, 24f);
            _seconds = UIFactory.Label("Segundos", panel, "", 64, Theme.Accent, bold: true);
            _seconds.rectTransform.Place(0.5f, 0.45f, 300f, 90f);
            UIFactory.Button("Usar", panel, "USAR", Theme.Ok, () => Ui.Controller.AcceptRevive())
                .GetComponent<RectTransform>().Place(0.3f, 0.17f, 340f, 110f);
            UIFactory.Button("Nao", panel, "NÃO", Theme.Danger, () => Ui.Controller.DeclineRevive())
                .GetComponent<RectTransform>().Place(0.7f, 0.17f, 340f, 110f);
        }

        protected override void OnShow() =>
            _title.text = $"Usar Segunda Chance? (x{Ui.Controller.Profile.SecondChances})";

        public override void Tick(float dt)
        {
            float r = Ui.Controller.Flow.Remaining;
            _ring.localScale = new Vector3(r / GameFlow.ReviveOfferTime, 1f, 1f);
            _seconds.text = Mathf.CeilToInt(r).ToString();
        }
    }
}
