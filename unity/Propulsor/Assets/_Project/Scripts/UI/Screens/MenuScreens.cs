using UnityEngine;
using UnityEngine.UI;

namespace Propulsor.UI
{
    /// <summary>
    /// S03 · Menu principal: sobreposto à cena parada. Tocar na área livre inicia a
    /// corrida sem tela intermediária. Oficina, Missões e Conquistas abrem como painéis.
    /// </summary>
    public sealed class MainMenuScreen : UIScreen
    {
        private Text _balance, _best, _prompt;

        protected override void Build(RectTransform root)
        {
            // A área de toque fica atrás dos botões.
            UIFactory.TouchArea("ToqueParaComecar", root, () => Ui.Controller.StartRun());

            UIFactory.Button("Config", root, "⚙", Theme.BgPanel, () => Ui.Settings.Show(), 48)
                .GetComponent<RectTransform>().Place(0.05f, 0.92f, 96f, 96f);
            _balance = UIFactory.Label("Saldo", root, "", 40, Theme.Gold, TextAnchor.MiddleRight, true);
            _balance.rectTransform.Place(0.8f, 0.92f, 640f, 64f);

            UIFactory.Label("Logo", root, "PROPULSOR", 110, Theme.Primary, TextAnchor.MiddleLeft, true)
                .rectTransform.Place(0.27f, 0.72f, 900f, 160f);
            _best = UIFactory.Label("Recorde", root, "", 36, Theme.Text, TextAnchor.MiddleLeft);
            _best.rectTransform.Place(0.27f, 0.6f, 900f, 60f);

            UIFactory.Button("Oficina", root, "OFICINA", Theme.BgPanel, () => Ui.Workshop.Show())
                .GetComponent<RectTransform>().Place(0.85f, 0.7f, 320f, 110f);
            UIFactory.Button("Missoes", root, "MISSÕES", Theme.BgPanel, () => Ui.Missions.Show())
                .GetComponent<RectTransform>().Place(0.85f, 0.55f, 320f, 110f);
            UIFactory.Button("Conquistas", root, "CONQUISTAS", Theme.BgPanel, () => Ui.AchievementsList.Show())
                .GetComponent<RectTransform>().Place(0.85f, 0.4f, 320f, 110f);

            _prompt = UIFactory.Label("Prompt", root, "▶ TOQUE PARA COMEÇAR", 56, Theme.Text, bold: true);
            _prompt.rectTransform.Place(0.5f, 0.18f, 1200f, 90f);
        }

        public override void Tick(float dt) => Pulse();

        protected override void OnShow() => RefreshTexts();

        /// <summary>Atualiza saldo/recorde (também ao voltar da Oficina).</summary>
        public void RefreshTexts()
        {
            var p = Ui.Controller.Profile;
            _balance.text = $"Porcas {p.Coins:N0}   Fichas {p.BankedTokens}";
            _best.text = p.BestDistance > 0 ? $"Recorde: {p.BestDistance:N0} m" : "Recorde: —";
        }

        private void Pulse()
        {
            if (Ui.Workshop.Visible || Ui.Missions.Visible) RefreshTexts();
            // Pulso 1,00 ↔ 1,06 a 1,2 Hz (estado "destaque" do guia).
            float s = 1.03f + 0.03f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI * 1.2f);
            _prompt.rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>S16 · Configurações (modal): volumes, vibração e atalhos de teste.</summary>
    public sealed class SettingsScreen : UIScreen
    {
        protected override ScreenAnim Anim => ScreenAnim.Pop;
        private Button _vibration, _erase;
        private Slider _music, _sfx;
        private bool _confirmErase;

        protected override void Build(RectTransform root)
        {
            var dim = UIFactory.Panel("Escurecer", root, Theme.Dim);
            dim.rectTransform.Anchor(-0.1f, -0.1f, 1.1f, 1.1f);

            var panel = UIFactory.Panel("Painel", root, Theme.BgPanel).rectTransform.Place(0.5f, 0.5f, 1000f, 760f);
            UIFactory.Label("Titulo", panel, "CONFIGURAÇÕES", 56, Theme.Text, bold: true).rectTransform.Place(0.5f, 0.9f, 900f, 80f);

            UIFactory.Label("Musica", panel, "Música", 34, Theme.Text, TextAnchor.MiddleLeft).rectTransform.Place(0.25f, 0.74f, 300f, 50f);
            _music = UIFactory.Slider("SliderMusica", panel, 0.8f, v => Ui.Controller.Profile.MusicVolume = v);
            _music.GetComponent<RectTransform>().Place(0.62f, 0.74f, 480f, 36f);
            UIFactory.Label("Efeitos", panel, "Efeitos", 34, Theme.Text, TextAnchor.MiddleLeft).rectTransform.Place(0.25f, 0.62f, 300f, 50f);
            _sfx = UIFactory.Slider("SliderEfeitos", panel, 1f, v => Ui.Controller.Profile.SfxVolume = v);
            _sfx.GetComponent<RectTransform>().Place(0.62f, 0.62f, 480f, 36f);

            _vibration = UIFactory.Button("Vibracao", panel, "", Theme.BgDeep, ToggleVibration, 34);
            _vibration.GetComponent<RectTransform>().Place(0.5f, 0.48f, 520f, 90f);

            _erase = UIFactory.Button("Apagar", panel, "", Theme.Danger, OnErase, 30);
            _erase.GetComponent<RectTransform>().Place(0.5f, 0.35f, 560f, 80f);

            // Atalhos de teste: só no editor e em builds de desenvolvimento.
            if (Application.isEditor || Debug.isDebugBuild)
            {
                UIFactory.Button("MaisPorcas", panel, "+5.000 porcas (teste)", Theme.BgDeep, () =>
                {
                    Ui.Controller.Services.Wallet.Credit(Propulsor.Core.Economy.Currency.Coins, 5000, "teste");
                    Ui.Controller.Services.Persist();
                    Ui.ShowToast($"Porcas: {Ui.Controller.Profile.Coins:N0}");
                }, 26).GetComponent<RectTransform>().Place(0.28f, 0.22f, 400f, 70f);
                UIFactory.Button("MaisFichas", panel, "+3 Fichas (teste)", Theme.BgDeep, () =>
                {
                    Ui.Controller.Services.Wallet.Credit(Propulsor.Core.Economy.Currency.Tokens, 3, "teste");
                    Ui.Controller.Services.Persist();
                    Ui.ShowToast($"Fichas guardadas: {Ui.Controller.Profile.BankedTokens}");
                }, 26).GetComponent<RectTransform>().Place(0.72f, 0.22f, 400f, 70f);
            }

            UIFactory.Button("Fechar", panel, "FECHAR", Theme.Primary, () => Hide())
                .GetComponent<RectTransform>().Place(0.5f, 0.09f, 360f, 100f);
        }

        protected override void OnShow()
        {
            var p = Ui.Controller.Profile;
            _music.SetValueWithoutNotify(p.MusicVolume);
            _sfx.SetValueWithoutNotify(p.SfxVolume);
            _confirmErase = false;
            _erase.SetLabel("Apagar todo o progresso");
            RefreshVibration();
        }

        /// <summary>Salva volumes/vibração ao fechar.</summary>
        protected override void OnHide() => Ui.Controller.Services.Persist();

        private void OnErase()
        {
            if (!_confirmErase) { _confirmErase = true; _erase.SetLabel("Tem certeza? Toque de novo"); return; }
            Ui.Controller.ResetProgress();
            _confirmErase = false;
            _erase.SetLabel("Apagar todo o progresso");
            Ui.ShowToast("Progresso apagado");
            Ui.MainMenu.RefreshTexts();
        }

        private void ToggleVibration()
        {
            Ui.Controller.Profile.Vibration = !Ui.Controller.Profile.Vibration;
            RefreshVibration();
        }

        private void RefreshVibration() =>
            _vibration.SetLabel(Ui.Controller.Profile.Vibration ? "Vibração: LIGADA" : "Vibração: DESLIGADA");
    }
}
