using System;
using Propulsor.Core.Economy;
using Propulsor.Core.Events;
using Propulsor.Core.Flow;
using Propulsor.Core.Physics;
using Propulsor.Core.Progress;

namespace Propulsor.Core.Audio
{
    public enum Sfx
    {
        Coin, Token, VehicleMount, Explosion, ArmorHit, Warning, MissileWhoosh, LaserCharge,
        Hit, Land, Bonk, NearMiss, Mission, RankUp, Achievement, Purchase, Boost, Revive,
        UiClick, ClawWin, ClawLose
    }

    public enum Music { None, Menu, Run, Claw }

    /// <summary>Saída de áudio (no Unity: AudioManager com sons sintetizados).</summary>
    public interface IAudioOutput
    {
        void PlaySfx(Sfx sfx, float volume, float pitch);
        void SetMusic(Music music);
        /// <summary>Atenuação da música em dB (0 = normal, negativo = mais baixo).</summary>
        void SetMusicDuck(float db);
        /// <summary>Som contínuo do propulsor.</summary>
        void SetJetLoop(bool on);
    }

    /// <summary>
    /// Decide o que tocar a partir dos eventos do jogo (docs/fase-4/01 §3.18):
    /// efeitos por evento, música por estado, "ducking" de −6 dB durante avisos de drone
    /// e −10 dB na pausa, e moedas em sequência subindo de tom.
    /// </summary>
    public sealed class AudioDirector
    {
        public const float WarningDuckDb = -6f;
        public const float PauseDuckDb = -10f;
        public const float CoinStreakWindow = 0.3f;
        public const int CoinStreakMaxSteps = 12;

        private readonly IAudioOutput _out;
        private int _activeWarnings;
        private bool _paused;
        private float _sinceCoin = 99f;
        private int _coinStreak;

        public Music CurrentMusic { get; private set; } = Music.None;
        public float CurrentDuck { get; private set; }

        public AudioDirector(EventBus bus, IAudioOutput output)
        {
            _out = output ?? throw new ArgumentNullException(nameof(output));
            bus.Subscribe<CoinCollected>(_ => OnCoin());
            bus.Subscribe<TokenCollected>(_ => Play(Sfx.Token));
            bus.Subscribe<VehicleMounted>(_ => Play(Sfx.VehicleMount));
            bus.Subscribe<VehicleDestroyed>(_ => Play(Sfx.Explosion));
            bus.Subscribe<VehicleDamaged>(_ => Play(Sfx.ArmorHit));
            bus.Subscribe<MissileWarning>(_ => { _activeWarnings++; Play(Sfx.Warning, 0.8f); UpdateDuck(); });
            bus.Subscribe<MissileLaunched>(_ => { _activeWarnings = Math.Max(0, _activeWarnings - 1); Play(Sfx.MissileWhoosh); UpdateDuck(); });
            bus.Subscribe<LaserSequenceStarted>(_ => { _activeWarnings = 0; Play(Sfx.LaserCharge); UpdateDuck(); });
            bus.Subscribe<PlayerDied>(_ => { _out.SetJetLoop(false); Play(Sfx.Hit); _activeWarnings = 0; UpdateDuck(); });
            bus.Subscribe<PlayerLanded>(_ => Play(Sfx.Land, 0.5f));
            bus.Subscribe<WorkerKnocked>(_ => Play(Sfx.Bonk, 0.7f));
            bus.Subscribe<HazardNearMiss>(_ => Play(Sfx.NearMiss, 0.8f));
            bus.Subscribe<MissionCompleted>(_ => Play(Sfx.Mission));
            bus.Subscribe<RankUp>(_ => Play(Sfx.RankUp));
            bus.Subscribe<AchievementUnlocked>(_ => Play(Sfx.Achievement));
            bus.Subscribe<PurchaseCompleted>(_ => Play(Sfx.Purchase));
            bus.Subscribe<HeadStartStarted>(_ => Play(Sfx.Boost));
            bus.Subscribe<PlayerRevived>(_ => Play(Sfx.Revive));
            bus.Subscribe<PlayerStateChanged>(e => _out.SetJetLoop(e.To == MotorState.Thrusting));
            bus.Subscribe<RunStarted>(_ => { _activeWarnings = 0; _out.SetJetLoop(false); UpdateDuck(); });
            bus.Subscribe<GameStateChanged>(e => OnState(e.To));
        }

        public void Tick(float dt) => _sinceCoin += dt;

        /// <summary>Resultado de uma tentativa na Garra (chamado pela UI ao revelar o prêmio).</summary>
        public void ClawResult(bool won) => Play(won ? Sfx.ClawWin : Sfx.ClawLose);

        public void UiClick() => Play(Sfx.UiClick, 0.6f);

        private void OnCoin()
        {
            _coinStreak = _sinceCoin <= CoinStreakWindow ? Math.Min(_coinStreak + 1, CoinStreakMaxSteps) : 0;
            _sinceCoin = 0f;
            // Cada porca seguida sobe meio tom (até uma oitava).
            Play(Sfx.Coin, 0.7f, (float)Math.Pow(2.0, _coinStreak / 12.0));
        }

        private void OnState(GameState s)
        {
            _paused = s == GameState.Paused;
            Music m = s switch
            {
                GameState.Splash => Music.None,
                GameState.Loading => Music.None,
                GameState.MainMenu => Music.Menu,
                GameState.Results => Music.Menu,
                GameState.ClawMachine => Music.Claw,
                _ => Music.Run
            };
            if (s != GameState.Running && s != GameState.Intro && s != GameState.Countdown) _out.SetJetLoop(false);
            if (m != CurrentMusic) { CurrentMusic = m; _out.SetMusic(m); }
            UpdateDuck();
        }

        private void UpdateDuck()
        {
            float db = _paused ? PauseDuckDb : (_activeWarnings > 0 ? WarningDuckDb : 0f);
            if (Math.Abs(db - CurrentDuck) < 0.01f) return;
            CurrentDuck = db;
            _out.SetMusicDuck(db);
        }

        private void Play(Sfx s, float volume = 1f, float pitch = 1f) => _out.PlaySfx(s, volume, pitch);
    }
}
