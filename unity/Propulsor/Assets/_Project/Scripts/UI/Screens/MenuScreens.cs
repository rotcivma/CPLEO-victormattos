using UnityEngine;
using UnityEngine.UI;

namespace Propulsor.UI
{
    /// <summary>
    /// S03 · Menu principal: sobreposto à cena parada. Tocar na área livre inicia a
    /// corrida sem tela intermediária. Oficina/Missões/Conquistas chegam na Fase 8.
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

            float[] ys = { 0.7f, 0.55f, 0.4f };
            string[] labels = { "OFICINA", "MISSÕES", "CONQUISTAS" };
            for (int i = 0; i < 3; i++)
            {
                string l = labels[i];
                UIFactory.Button(l, root, l, Theme.BgPanel, () => Ui.ShowToast($"{l}: chega na Fase 8"))
                    .GetComponent<RectTransform>().Place(0.85f, ys[i], 320f, 110f);
            }

            _prompt = UIFactory.Label("Prompt", root, "▶ TOQUE PARA COMEÇAR", 56, Theme.Text, bold: true);
            _prompt.rectTransform.Place(0.5f, 0.18f, 1200f, 90f);
        }

        protected override void OnShow()
        {
            var p = Ui.Controller.Profile;
            _balance.text = $"Porcas {p.Coins:N0}   Fichas {p.BankedTokens}";
            _best.text = p.BestDistance > 0 ? $"Recorde: {p.BestDistance:N0} m" : "Recorde: —";
        }

        public override void Tick(float dt)
        {
            // Pulso 1,00 ↔ 1,06 a 1,2 Hz (estado "destaque" do guia).
            float s = 1.03f + 0.03f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI * 1.2f);
            _prompt.rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>S16 · Configurações (modal): volumes, vibração e atalhos de teste.</summary>
    public sealed class SettingsScreen : UIScreen
    {
        protected override ScreenAnim Anim => ScreenAnim.Pop;
        private Button _vibration;

        protected override void Build(RectTransform root)
        {
            var dim = UIFactory.Panel("Escurecer", root, Theme.Dim);
            dim.rectTransform.Anchor(-0.1f, -0.1f, 1.1f, 1.1f);

            var panel = UIFactory.Panel("Painel", root, Theme.BgPanel).rectTransform.Place(0.5f, 0.5f, 1000f, 760f);
            UIFactory.Label("Titulo", panel, "CONFIGURAÇÕES", 56, Theme.Text, bold: true).rectTransform.Place(0.5f, 0.9f, 900f, 80f);

            UIFactory.Label("Musica", panel, "Música", 34, Theme.Text, TextAnchor.MiddleLeft).rectTransform.Place(0.25f, 0.74f, 300f, 50f);
            UIFactory.Slider("SliderMusica", panel, 0.8f, v => Ui.Controller.Profile.MusicVolume = v)
                .GetComponent<RectTransform>().Place(0.62f, 0.74f, 480f, 36f);
            UIFactory.Label("Efeitos", panel, "Efeitos", 34, Theme.Text, TextAnchor.MiddleLeft).rectTransform.Place(0.25f, 0.62f, 300f, 50f);
            UIFactory.Slider("SliderEfeitos", panel, 1f, v => Ui.Controller.Profile.SfxVolume = v)
                .GetComponent<RectTransform>().Place(0.62f, 0.62f, 480f, 36f);

            _vibration = UIFactory.Button("Vibracao", panel, "", Theme.BgDeep, ToggleVibration, 34);
            _vibration.GetComponent<RectTransform>().Place(0.5f, 0.48f, 520f, 90f);

            // Atalhos de teste enquanto a Oficina (Fase 8) não existe.
            UIFactory.Label("Teste", panel, "Teste (até a Fase 8):", 28, Theme.TextDim).rectTransform.Place(0.5f, 0.36f, 800f, 40f);
            UIFactory.Button("MaisRevive", panel, "+1 Segunda Chance", Theme.BgDeep, () =>
            {
                Ui.Controller.Profile.SecondChances++;
                Ui.ShowToast($"Segundas Chances: {Ui.Controller.Profile.SecondChances}");
            }, 30).GetComponent<RectTransform>().Place(0.28f, 0.25f, 400f, 80f);
            UIFactory.Button("MaisFichas", panel, "+3 Fichas", Theme.BgDeep, () =>
            {
                Ui.Controller.Profile.BankedTokens += 3;
                Ui.ShowToast($"Fichas guardadas: {Ui.Controller.Profile.BankedTokens}");
            }, 30).GetComponent<RectTransform>().Place(0.72f, 0.25f, 400f, 80f);

            UIFactory.Button("Fechar", panel, "FECHAR", Theme.Primary, () => Hide())
                .GetComponent<RectTransform>().Place(0.5f, 0.09f, 360f, 100f);
        }

        protected override void OnShow() => RefreshVibration();

        private void ToggleVibration()
        {
            Ui.Controller.Profile.Vibration = !Ui.Controller.Profile.Vibration;
            RefreshVibration();
        }

        private void RefreshVibration() =>
            _vibration.SetLabel(Ui.Controller.Profile.Vibration ? "Vibração: LIGADA" : "Vibração: DESLIGADA");
    }
}
