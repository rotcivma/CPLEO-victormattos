using Propulsor.Core.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace Propulsor.UI
{
    /// <summary>
    /// S07 · Garra da Sorte: 1 ficha = 1 tentativa. O prêmio já está sorteado
    /// quando a animação começa (a garra desce, pega e sobe em 1 s).
    /// </summary>
    public sealed class ClawScreen : UIScreen
    {
        private const float AnimTime = 1f;

        private Text _tokens, _result;
        private RectTransform _claw;
        private Button _play, _cashOut, _continue;
        private RectTransform[] _prizeSlots;
        private float _animT = -1f;
        private PrizeEntry _pending;
        private bool _wonSecondChance;

        protected override void Build(RectTransform root)
        {
            UIFactory.Panel("Fundo", root, new Color(0.106f, 0.122f, 0.231f, 0.92f)).rectTransform.Anchor(-0.1f, -0.1f, 1.1f, 1.1f);
            UIFactory.Label("Titulo", root, "GARRA DA SORTE", 72, Theme.Accent, bold: true).rectTransform.Place(0.5f, 0.9f, 1200f, 100f);
            _tokens = UIFactory.Label("Fichas", root, "", 44, Theme.Text, TextAnchor.MiddleLeft, true);
            _tokens.rectTransform.Place(0.18f, 0.9f, 400f, 70f);

            var machine = UIFactory.Panel("Maquina", root, Theme.BgPanel).rectTransform.Place(0.5f, 0.52f, 1100f, 520f);
            // Esteira de prêmios (ícones abstratos coloridos)
            Color[] colors = { Theme.Gold, Theme.Primary, Theme.Danger, Theme.Accent, Theme.Gold, new Color32(0xB3, 0x88, 0xFF, 255) };
            _prizeSlots = new RectTransform[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                _prizeSlots[i] = UIFactory.Panel("Premio" + i, machine, colors[i]).rectTransform
                    .Place((i + 0.5f) / colors.Length, 0.18f, 110f, 110f);
            }
            _claw = UIFactory.Panel("Garra", machine, Theme.TextDim).rectTransform.Place(0.5f, 0.8f, 140f, 60f);
            UIFactory.Panel("Cabo", _claw, Theme.TextDim).rectTransform.Place(0.5f, 1.6f, 16f, 120f);

            _result = UIFactory.Label("Resultado", root, "", 52, Theme.Gold, bold: true);
            _result.rectTransform.Place(0.5f, 0.21f, 1400f, 80f);

            _play = UIFactory.Button("Pegar", root, "PEGAR! (1 ficha)", Theme.Accent, OnPlay);
            _play.GetComponent<RectTransform>().Place(0.3f, 0.08f, 560f, 120f);
            _cashOut = UIFactory.Button("Trocar", root, "", Theme.BgPanel, OnCashOut, 34);
            _cashOut.GetComponent<RectTransform>().Place(0.72f, 0.08f, 620f, 120f);
            _continue = UIFactory.Button("Continuar", root, "CONTINUAR", Theme.Primary, OnContinue);
            _continue.GetComponent<RectTransform>().Place(0.5f, 0.08f, 520f, 120f);
        }

        protected override void OnShow()
        {
            _animT = -1f;
            _wonSecondChance = false;
            _result.text = "";
            Refresh();
        }

        private ClawMachine Claw => Ui.Controller.Claw;

        private void Refresh()
        {
            bool idle = _animT < 0f;
            int tokens = Claw?.Tokens ?? 0;
            _tokens.text = $"Fichas: {tokens}";
            bool canPlay = idle && tokens > 0 && !_wonSecondChance;
            _play.gameObject.SetActive(canPlay);
            _cashOut.gameObject.SetActive(canPlay);
            _cashOut.SetLabel($"Trocar: +{tokens * ClawMachine.CoinsPerToken} porcas");
            _continue.gameObject.SetActive(idle && (tokens == 0 || _wonSecondChance));
            _continue.SetLabel(_wonSecondChance ? "VOLTAR À CORRIDA!" : "CONTINUAR");
        }

        private void OnPlay()
        {
            if (Claw == null || !Claw.CanPlay || _animT >= 0f) return;
            _pending = Ui.Controller.PlayClaw();
            _animT = 0f;
            _result.text = "";
            Refresh();
        }

        private void OnCashOut()
        {
            int coins = Ui.Controller.CashOutClaw();
            _result.text = $"+{coins} porcas";
            Refresh();
        }

        private void OnContinue() => Ui.Controller.FinishClaw(_wonSecondChance);

        public override void Tick(float dt)
        {
            // Esteira gira devagar
            for (int i = 0; i < _prizeSlots.Length; i++)
                _prizeSlots[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 2f + i) * 8f);

            if (_animT < 0f) return;
            _animT += dt;
            float k = Mathf.Clamp01(_animT / AnimTime);
            // desce até a esteira e volta
            float down = k < 0.5f ? k * 2f : (1f - k) * 2f;
            _claw.anchorMin = _claw.anchorMax = new Vector2(0.5f + 0.25f * Mathf.Sin(k * Mathf.PI * 2f), Mathf.Lerp(0.8f, 0.35f, down));
            if (k < 1f) return;

            _animT = -1f;
            _result.text = Describe(_pending);
            if (_pending.Kind == PrizeKind.SecondChance) _wonSecondChance = true;
            Refresh();
        }

        public static string Describe(PrizeEntry p) => p.Kind switch
        {
            PrizeKind.Nothing => "Nada dessa vez...",
            PrizeKind.Coins => $"+{p.Value} porcas!",
            PrizeKind.SecondChance => "SEGUNDA CHANCE!",
            PrizeKind.HeadStart => $"Arrancada de {p.Value} m na próxima corrida!",
            PrizeKind.DoubleCoins => "Porcas em dobro na próxima corrida!",
            PrizeKind.ExtraTokens => $"+{p.Value} fichas!",
            _ => p.ToString()
        };
    }

    /// <summary>S06 · Resultados: contadores animados; toque acelera para o valor final.</summary>
    public sealed class ResultsScreen : UIScreen
    {
        protected override ScreenAnim Anim => ScreenAnim.SlideUp;
        private Text _distance, _record, _runCoins, _clawCoins, _total;
        private float _t;
        private RunSummary _s;

        protected override void Build(RectTransform root)
        {
            UIFactory.Panel("Fundo", root, new Color(0.106f, 0.122f, 0.231f, 0.9f)).rectTransform.Anchor(-0.1f, -0.1f, 1.1f, 1.1f);
            UIFactory.TouchArea("Acelerar", root, () => _t = 10f);

            _distance = UIFactory.Label("Distancia", root, "", 130, Theme.Text, bold: true);
            _distance.rectTransform.Place(0.5f, 0.83f, 1200f, 160f);
            _record = UIFactory.Label("Recorde", root, "", 48, Theme.Gold, bold: true);
            _record.rectTransform.Place(0.5f, 0.7f, 1200f, 70f);

            _runCoins = UIFactory.Label("PorcasCorrida", root, "", 40, Theme.Text, TextAnchor.MiddleLeft);
            _runCoins.rectTransform.Place(0.5f, 0.56f, 900f, 60f);
            _clawCoins = UIFactory.Label("PorcasGarra", root, "", 40, Theme.Text, TextAnchor.MiddleLeft);
            _clawCoins.rectTransform.Place(0.5f, 0.48f, 900f, 60f);
            _total = UIFactory.Label("Total", root, "", 46, Theme.Gold, TextAnchor.MiddleLeft, true);
            _total.rectTransform.Place(0.5f, 0.38f, 900f, 70f);

            UIFactory.Button("Oficina", root, "OFICINA", Theme.BgPanel, () => Ui.ShowToast("OFICINA: chega na Fase 8"))
                .GetComponent<RectTransform>().Place(0.2f, 0.12f, 380f, 120f);
            UIFactory.Button("DeNovo", root, "▶ DE NOVO", Theme.Accent, () => Ui.Controller.PlayAgain(), 52)
                .GetComponent<RectTransform>().Place(0.5f, 0.12f, 480f, 130f);
            UIFactory.Button("Menu", root, "MENU", Theme.BgPanel, () => Ui.Controller.BackToMenu())
                .GetComponent<RectTransform>().Place(0.8f, 0.12f, 380f, 120f);
        }

        protected override void OnShow()
        {
            _s = Ui.Controller.LastSummary;
            _t = 0f;
            _record.text = _s.IsNewBest ? "★ NOVO RECORDE! ★" : "";
        }

        public override void Tick(float dt)
        {
            _t += dt;
            // Sequência: distância (0,6 s) → porcas (0,4 s cada) → total (1,0 s).
            _distance.text = $"{Count(_s.Distance, 0f, 0.6f):N0} m";
            _runCoins.text = $"Porcas da corrida ........ {Count(_s.RunCoins, 0.6f, 0.4f):N0}";
            _clawCoins.text = $"Garra da Sorte ............ {Count(_s.ClawCoins, 1.0f, 0.4f):N0}";
            _total.text = $"Saldo ............ {Mathf.RoundToInt(Mathf.Lerp(_s.BalanceBefore, _s.BalanceAfter, Ease(1.4f, 1.0f))):N0}";
        }

        private int Count(int value, float start, float dur) => Mathf.RoundToInt(value * Ease(start, dur));

        private float Ease(float start, float dur)
        {
            float k = Mathf.Clamp01((_t - start) / dur);
            return 1f - (1f - k) * (1f - k); // ease-out
        }
    }
}
