using System;
using Propulsor.Core.Events;

namespace Propulsor.Core.Flow
{
    public enum GameState
    {
        Splash,
        Loading,
        MainMenu,
        /// <summary>Nina sai correndo pela porta (sem controle).</summary>
        Intro,
        Running,
        Paused,
        /// <summary>Contagem 3-2-1 antes de retomar.</summary>
        Countdown,
        /// <summary>Morta, corpo ainda deslizando.</summary>
        Dying,
        ReviveOffer,
        ClawMachine,
        Results
    }

    public readonly struct GameStateChanged
    {
        public readonly GameState From, To;
        public GameStateChanged(GameState from, GameState to) { From = from; To = to; }
    }

    /// <summary>
    /// Máquina de estados da partida (docs/fase-3/01-mapeamento-sistemas.md §1), em C# puro.
    /// Não conhece Unity nem a simulação: recebe comandos e consulta o contexto por delegates.
    /// </summary>
    public sealed class GameFlow
    {
        public const float SplashTime = 1.8f;
        public const float LoadingMinTime = 0.8f;
        public const float IntroTime = 0.8f;
        public const float CountdownTime = 0.9f;
        public const float ReviveOfferTime = 4f;

        public GameState State { get; private set; } = GameState.Splash;
        /// <summary>Tempo no estado atual (s).</summary>
        public float StateTime { get; private set; }
        /// <summary>Segundos restantes da contagem/oferta (para a UI).</summary>
        public float Remaining => State switch
        {
            GameState.Countdown => Math.Max(0f, CountdownTime - StateTime),
            GameState.ReviveOffer => Math.Max(0f, ReviveOfferTime - StateTime),
            _ => 0f
        };

        /// <summary>A simulação deve avançar neste estado?</summary>
        public bool SimulationRuns => State == GameState.Intro || State == GameState.Running || State == GameState.Dying;
        /// <summary>O input do jogador controla a personagem neste estado?</summary>
        public bool PlayerControls => State == GameState.Running;

        // Contexto (fornecido pelo controlador)
        public Func<bool> IsLoaded = () => true;
        public Func<int> RevivesAvailable = () => 0;
        public Func<int> TokensAvailable = () => 0;

        // Efeitos pedidos ao controlador
        public Action RequestNewRun;
        public Action RequestRevive;
        public Action ConsumeRevive;

        private readonly EventBus _bus;

        public GameFlow(EventBus bus = null) { _bus = bus; }

        public void Tick(float dt)
        {
            StateTime += dt;
            switch (State)
            {
                case GameState.Splash when StateTime >= SplashTime: Go(GameState.Loading); break;
                case GameState.Loading when StateTime >= LoadingMinTime && IsLoaded(): Go(GameState.MainMenu); break;
                case GameState.Intro when StateTime >= IntroTime: Go(GameState.Running); break;
                case GameState.Countdown when StateTime >= CountdownTime: Go(GameState.Running); break;
                case GameState.ReviveOffer when StateTime >= ReviveOfferTime: DeclineRevive(); break;
            }
        }

        // ---------- comandos ----------

        /// <summary>Toque durante o splash pula para o carregamento.</summary>
        public void SkipSplash() { if (State == GameState.Splash) Go(GameState.Loading); }

        /// <summary>Toque livre no menu principal.</summary>
        public void StartRun() { if (State == GameState.MainMenu) Go(GameState.Intro); }

        public void Pause() { if (State == GameState.Running || State == GameState.Countdown) Go(GameState.Paused); }

        public void Resume() { if (State == GameState.Paused) Go(GameState.Countdown); }

        /// <summary>Desistir pela pausa: conta como morte; não oferece revive.</summary>
        public void GiveUp()
        {
            if (State != GameState.Paused) return;
            _gaveUp = true;
            AfterDeath();
        }

        /// <summary>A personagem morreu (corpo começa a deslizar).</summary>
        public void PlayerDied() { if (State == GameState.Running || State == GameState.Intro) Go(GameState.Dying); }

        /// <summary>O corpo parou: decide revive → garra → resultados.</summary>
        public void PlayerStopped() { if (State == GameState.Dying) AfterDeath(); }

        public void AcceptRevive()
        {
            if (State != GameState.ReviveOffer) return;
            ConsumeRevive?.Invoke();
            RequestRevive?.Invoke();
            Go(GameState.Running);
        }

        public void DeclineRevive()
        {
            if (State != GameState.ReviveOffer) return;
            _reviveDeclined = true;
            AfterDeath();
        }

        /// <summary>A Garra da Sorte terminou. Se deu "Segunda Chance", volta à corrida.</summary>
        public void ClawFinished(bool wonSecondChance)
        {
            if (State != GameState.ClawMachine) return;
            if (wonSecondChance)
            {
                RequestRevive?.Invoke();
                Go(GameState.Running);
            }
            else Go(GameState.Results);
        }

        public void PlayAgain()
        {
            if (State != GameState.Results) return;
            RequestNewRun?.Invoke();
            Go(GameState.Intro);
        }

        public void BackToMenu()
        {
            if (State != GameState.Results) return;
            RequestNewRun?.Invoke();
            Go(GameState.MainMenu);
        }

        // ---------- interno ----------

        private bool _gaveUp, _reviveDeclined;

        private void AfterDeath()
        {
            if (!_gaveUp && !_reviveDeclined && RevivesAvailable() > 0) { Go(GameState.ReviveOffer); return; }
            if (TokensAvailable() > 0) { Go(GameState.ClawMachine); return; }
            Go(GameState.Results);
        }

        private void Go(GameState next)
        {
            if (next == State) return;
            var prev = State;
            State = next;
            StateTime = 0f;
            if (next == GameState.Intro || next == GameState.Running && prev != GameState.Countdown)
                _reviveDeclined = false;
            if (next == GameState.Intro) _gaveUp = false;
            _bus?.Publish(new GameStateChanged(prev, next));
        }
    }
}
