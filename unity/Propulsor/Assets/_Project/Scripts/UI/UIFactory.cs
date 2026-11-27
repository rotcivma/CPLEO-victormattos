using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Propulsor.UI
{
    /// <summary>Tokens de design da identidade própria (docs/fase-2/01-identidade-visual-original.md).</summary>
    public static class Theme
    {
        public static readonly Color BgDeep = new Color32(0x1B, 0x1F, 0x3B, 255);
        public static readonly Color BgPanel = new Color32(0x2B, 0x31, 0x63, 255);
        public static readonly Color Primary = new Color32(0x2E, 0xC4, 0xB6, 255);
        public static readonly Color Accent = new Color32(0xFF, 0x9F, 0x1C, 255);
        public static readonly Color Gold = new Color32(0xFF, 0xD2, 0x3F, 255);
        public static readonly Color Danger = new Color32(0xE7, 0x1D, 0x36, 255);
        public static readonly Color Text = new Color32(0xF6, 0xF7, 0xFB, 255);
        public static readonly Color TextDim = new Color32(0xA8, 0xAE, 0xC9, 255);
        public static readonly Color Ok = new Color32(0x6B, 0xCB, 0x77, 255);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);

        private static Font _font;
        /// <summary>Fonte embutida do Unity (as fontes OFL definidas no guia entram na Fase 9).</summary>
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _font;
            }
        }
    }

    /// <summary>Construtores de UI em código (uGUI), com âncoras em fração da tela.</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Estica até as âncoras min/max (frações 0..1 do pai) com margem em pixels de referência.</summary>
        public static RectTransform Anchor(this RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Tamanho fixo (px de referência) centrado num ponto relativo do pai.</summary>
        public static RectTransform Place(this RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
                                 TextAnchor align = TextAnchor.MiddleCenter, bool bold = false)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Theme.Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = Theme.BgDeep;
            outline.effectDistance = new Vector2(3f, -3f);
            return t;
        }

        /// <summary>Botão com sombra e retorno ao toque (desce 6 px e encolhe para 96 %).</summary>
        public static Button Button(string name, Transform parent, string label, Color color, UnityAction onClick,
                                    int fontSize = 44)
        {
            var rt = Rect(name, parent);
            var shadow = rt.gameObject.AddComponent<Image>();
            shadow.color = Color.Lerp(color, Color.black, 0.45f);

            var face = Rect("Face", rt).Anchor(0f, 0f, 1f, 1f);
            face.offsetMin = new Vector2(0f, 8f);
            var faceImg = face.gameObject.AddComponent<Image>();
            faceImg.color = color;

            var text = Label("Texto", face, label, fontSize, Theme.Text, TextAnchor.MiddleCenter, true);
            text.rectTransform.Anchor(0f, 0f, 1f, 1f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = faceImg;
            var colors = btn.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            rt.gameObject.AddComponent<PressFeedback>().Face = face;
            return btn;
        }

        public static void SetLabel(this Button b, string text) => b.GetComponentInChildren<Text>().text = text;

        /// <summary>Área invisível que recebe toques (ex.: "toque para começar").</summary>
        public static Button TouchArea(string name, Transform parent, UnityAction onClick)
        {
            var rt = Rect(name, parent).Anchor(0f, 0f, 1f, 1f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(onClick);
            return b;
        }

        public static Slider Slider(string name, Transform parent, float value, Action<float> onChange)
        {
            var rt = Rect(name, parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = Theme.BgDeep;

            var fillArea = Rect("Area", rt).Anchor(0f, 0f, 1f, 1f);
            var fill = Panel("Preenchimento", fillArea, Theme.Primary);
            fill.rectTransform.Anchor(0f, 0f, 1f, 1f);

            var handleArea = Rect("AreaAlca", rt).Anchor(0f, 0f, 1f, 1f);
            var handle = Panel("Alca", handleArea, Theme.Text);
            handle.rectTransform.sizeDelta = new Vector2(40f, 0f);

            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = value;
            s.onValueChanged.AddListener(v => onChange(v));
            return s;
        }

        /// <summary>Barra de progresso simples (escala horizontal do preenchimento).</summary>
        public static RectTransform ProgressBar(string name, Transform parent, Color fillColor)
        {
            var bg = Panel(name, parent, Theme.BgDeep);
            var fill = Panel("Preenchimento", bg.transform, fillColor);
            fill.rectTransform.Anchor(0f, 0f, 1f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            return fill.rectTransform;
        }

        /// <summary>Cria (se preciso) o EventSystem com o módulo de input certo.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    /// <summary>Retorno visual ao pressionar um botão (estado "pressionado" do guia).</summary>
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform Face;
        private bool _down;

        public void OnPointerDown(PointerEventData e) => Set(true);
        public void OnPointerUp(PointerEventData e) => Set(false);
        public void OnPointerExit(PointerEventData e) => Set(false);

        private void Set(bool down)
        {
            if (_down == down || Face == null) return;
            _down = down;
            Face.offsetMin = new Vector2(0f, down ? 2f : 8f);
            Face.offsetMax = new Vector2(0f, down ? -6f : 0f);
            Face.localScale = down ? new Vector3(0.96f, 0.96f, 1f) : Vector3.one;
        }
    }
}
