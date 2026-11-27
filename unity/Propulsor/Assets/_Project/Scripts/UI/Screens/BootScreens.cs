using UnityEngine;
using UnityEngine.UI;

namespace Propulsor.UI
{
    /// <summary>S01 · Splash: logo do projeto; toque pula.</summary>
    public sealed class SplashScreen : UIScreen
    {
        protected override void Build(RectTransform root)
        {
            UIFactory.Panel("Fundo", root, Theme.BgDeep).rectTransform.Anchor(-0.1f, -0.1f, 1.1f, 1.1f);
            UIFactory.Label("Logo", root, "PROPULSOR", 140, Theme.Primary, bold: true).rectTransform.Place(0.5f, 0.55f, 1200f, 200f);
            UIFactory.Label("Sub", root, "um jogo original", 40, Theme.TextDim).rectTransform.Place(0.5f, 0.42f, 900f, 60f);
            UIFactory.TouchArea("Pular", root, () => Ui.Controller.SkipSplash());
        }
    }

    /// <summary>S02 · Carregamento: ícone girando, barra e dica.</summary>
    public sealed class LoadingScreen : UIScreen
    {
        private RectTransform _bar, _icon;

        private static readonly string[] Tips =
        {
            "Dica: segure para subir, solte para cair.",
            "Dica: o aviso vermelho na borda direita é um drone chegando.",
            "Dica: veículos aguentam um acerto por você.",
            "Dica: fichas viram tentativas na Garra da Sorte.",
        };

        protected override void Build(RectTransform root)
        {
            UIFactory.Panel("Fundo", root, Theme.BgDeep).rectTransform.Anchor(-0.1f, -0.1f, 1.1f, 1.1f);
            _icon = UIFactory.Panel("Icone", root, Theme.Primary).rectTransform.Place(0.5f, 0.55f, 120f, 120f);
            var barBg = UIFactory.ProgressBar("Barra", root, Theme.Accent);
            ((RectTransform)barBg.parent).Place(0.5f, 0.35f, 768f, 28f);
            _bar = barBg;
            UIFactory.Label("Dica", root, Tips[Random.Range(0, Tips.Length)], 30, Theme.TextDim)
                .rectTransform.Place(0.5f, 0.25f, 1400f, 50f);
        }

        public override void Tick(float dt)
        {
            _icon.Rotate(0f, 0f, -180f * dt);
            float k = Mathf.Clamp01(Ui.Controller.Flow.StateTime / Propulsor.Core.Flow.GameFlow.LoadingMinTime);
            _bar.localScale = new Vector3(k, 1f, 1f);
        }
    }

    /// <summary>Aviso curto no centro inferior (ex.: "chega na Fase 8").</summary>
    public sealed class ToastScreen : UIScreen
    {
        private Text _text;
        private float _until;
        protected override ScreenAnim Anim => ScreenAnim.Pop;

        protected override void Build(RectTransform root)
        {
            var box = UIFactory.Panel("Caixa", root, Theme.BgPanel);
            box.rectTransform.Place(0.5f, 0.12f, 1000f, 90f);
            box.raycastTarget = false;
            _text = UIFactory.Label("Texto", box.transform, "", 34, Theme.Text);
            _text.rectTransform.Anchor(0f, 0f, 1f, 1f);
        }

        public void Flash(string message, float seconds = 1.8f)
        {
            _text.text = message;
            _until = Time.unscaledTime + seconds;
            Show();
        }

        public override void Tick(float dt)
        {
            if (Time.unscaledTime > _until) Hide();
        }
    }
}
