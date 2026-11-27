using System.Collections;
using UnityEngine;

namespace Propulsor.UI
{
    public enum ScreenAnim { Fade, SlideUp, Pop }

    /// <summary>
    /// Base das telas: raiz com CanvasGroup, entrada/saída animadas em tempo real
    /// (funcionam com o jogo pausado). Entrada 250 ms ease-out-back; saída 180 ms ease-in.
    /// </summary>
    public abstract class UIScreen
    {
        public const float ShowTime = 0.25f, HideTime = 0.18f;

        public RectTransform Root { get; private set; }
        public bool Visible { get; private set; }
        protected UIManager Ui { get; private set; }
        protected virtual ScreenAnim Anim => ScreenAnim.Fade;

        private CanvasGroup _group;
        private Coroutine _running;

        public void Create(UIManager ui, Transform canvas, string name)
        {
            Ui = ui;
            Root = UIFactory.Rect(name, canvas).Anchor(0f, 0f, 1f, 1f);
            _group = Root.gameObject.AddComponent<CanvasGroup>();
            Build(Root);
            Root.gameObject.SetActive(false);
        }

        /// <summary>Monta os elementos da tela.</summary>
        protected abstract void Build(RectTransform root);
        /// <summary>Chamado ao aparecer (atualiza textos).</summary>
        protected virtual void OnShow() { }
        /// <summary>Chamado ao começar a sair.</summary>
        protected virtual void OnHide() { }
        /// <summary>Chamado a cada quadro enquanto visível.</summary>
        public virtual void Tick(float dt) { }

        public void Show()
        {
            if (Visible) return;
            Visible = true;
            Root.gameObject.SetActive(true);
            Root.SetAsLastSibling();
            OnShow();
            Restart(Animate(true));
        }

        public void Hide(bool instant = false)
        {
            if (!Visible) return;
            Visible = false;
            OnHide();
            if (instant || !Root.gameObject.activeInHierarchy) { Root.gameObject.SetActive(false); return; }
            Restart(Animate(false));
        }

        private void Restart(IEnumerator routine)
        {
            if (_running != null) Ui.StopCoroutine(_running);
            _running = Ui.StartCoroutine(routine);
        }

        private IEnumerator Animate(bool show)
        {
            float dur = show ? ShowTime : HideTime;
            _group.interactable = show;
            _group.blocksRaycasts = show;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                Apply(show ? EaseOutBack(t / dur) : 1f - EaseIn(t / dur), t / dur, show);
                yield return null;
            }
            Apply(show ? 1f : 0f, 1f, show);
            if (!show) Root.gameObject.SetActive(false);
            _running = null;
        }

        private void Apply(float k, float linear, bool show)
        {
            _group.alpha = Mathf.Clamp01(show ? linear * 2f : k);
            switch (Anim)
            {
                case ScreenAnim.SlideUp:
                    Root.anchoredPosition = new Vector2(0f, (1f - k) * -400f);
                    break;
                case ScreenAnim.Pop:
                    Root.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k);
                    break;
            }
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.2f, c3 = c1 + 1f;
            x = Mathf.Clamp01(x);
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private static float EaseIn(float x) => Mathf.Clamp01(x) * Mathf.Clamp01(x);
    }
}
