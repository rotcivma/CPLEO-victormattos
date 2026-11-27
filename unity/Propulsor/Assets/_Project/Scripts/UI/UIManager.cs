using System.Collections.Generic;
using Propulsor.Core.Flow;
using Propulsor.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Propulsor.UI
{
    /// <summary>
    /// Cria o Canvas (referência 1920×1080, match 0,5, safe area) e todas as telas,
    /// e mostra/esconde cada uma conforme o estado do <see cref="GameFlow"/>.
    /// Nenhuma regra de negócio fica aqui — só apresentação.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        public FlowController Controller { get; private set; }
        public RectTransform SafeArea { get; private set; }

        public SplashScreen Splash { get; } = new SplashScreen();
        public LoadingScreen Loading { get; } = new LoadingScreen();
        public MainMenuScreen MainMenu { get; } = new MainMenuScreen();
        public HudScreen Hud { get; } = new HudScreen();
        public PauseScreen PauseMenu { get; } = new PauseScreen();
        public ReviveScreen Revive { get; } = new ReviveScreen();
        public ClawScreen ClawScreen { get; } = new ClawScreen();
        public ResultsScreen Results { get; } = new ResultsScreen();
        public SettingsScreen Settings { get; } = new SettingsScreen();
        public WorkshopScreen Workshop { get; } = new WorkshopScreen();
        public MissionsScreen Missions { get; } = new MissionsScreen();
        public AchievementsScreen AchievementsList { get; } = new AchievementsScreen();
        public ToastScreen Toast { get; } = new ToastScreen();

        private readonly List<UIScreen> _all = new List<UIScreen>();
        private Rect _lastSafe;

        public void Bind(FlowController controller)
        {
            Controller = controller;
            UIFactory.EnsureEventSystem();

            var canvasGo = new GameObject("UI", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            SafeArea = UIFactory.Rect("SafeArea", canvasGo.transform).Anchor(0f, 0f, 1f, 1f);
            ApplySafeArea();

            Add(MainMenu, "MenuPrincipal");
            Add(Hud, "HUD");
            Add(PauseMenu, "Pausa");
            Add(Revive, "Revive");
            Add(ClawScreen, "GarraDaSorte");
            Add(Results, "Resultados");
            Add(Loading, "Carregamento");
            Add(Splash, "Splash");
            Add(Workshop, "Oficina");
            Add(Missions, "Missoes");
            Add(AchievementsList, "Conquistas");
            Add(Settings, "Configuracoes");
            Add(Toast, "Aviso");

            controller.Driver.Bus.Subscribe<GameStateChanged>(e => Route(e.To));
            Route(controller.Flow.State);
        }

        private void Add(UIScreen s, string name)
        {
            s.Create(this, SafeArea, name);
            _all.Add(s);
        }

        /// <summary>Quais telas aparecem em cada estado (docs/fase-2/02 · transições).</summary>
        private void Route(GameState state)
        {
            Set(Splash, state == GameState.Splash);
            Set(Loading, state == GameState.Loading);
            Set(MainMenu, state == GameState.MainMenu);
            Set(Hud, state == GameState.Intro || state == GameState.Running || state == GameState.Countdown ||
                     state == GameState.Dying || state == GameState.Paused);
            Set(PauseMenu, state == GameState.Paused);
            Set(Revive, state == GameState.ReviveOffer);
            Set(ClawScreen, state == GameState.ClawMachine);
            Set(Results, state == GameState.Results);
            if (state != GameState.MainMenu && state != GameState.Paused) Settings.Hide();
            if (state != GameState.MainMenu && state != GameState.Results)
            {
                Workshop.Hide();
                Missions.Hide();
                AchievementsList.Hide();
            }
        }

        private static void Set(UIScreen s, bool on) { if (on) s.Show(); else s.Hide(); }

        private void Update()
        {
            if (Controller == null) return;
            ApplySafeArea();
            float dt = Time.unscaledDeltaTime;
            foreach (var s in _all) if (s.Visible) s.Tick(dt);
        }

        /// <summary>Mostra um aviso curto (ex.: recurso que chega numa fase futura).</summary>
        public void ShowToast(string message) => Toast.Flash(message);

        private void ApplySafeArea()
        {
            var safe = Screen.safeArea;
            if (safe == _lastSafe || Screen.width == 0 || Screen.height == 0) return;
            _lastSafe = safe;
            SafeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            SafeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            SafeArea.offsetMin = SafeArea.offsetMax = Vector2.zero;
        }
    }
}
