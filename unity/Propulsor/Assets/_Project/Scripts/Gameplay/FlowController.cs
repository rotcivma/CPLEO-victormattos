using Propulsor.Core.Events;
using Propulsor.Core.Flow;
using Propulsor.Core.Gameplay;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Liga o <see cref="GameFlow"/> (lógica pura) à corrida (<see cref="RunDriver"/>):
    /// decide quando a simulação avança, credita resultados no perfil, conduz a
    /// Garra da Sorte e a oferta de revive. A UI só chama os métodos públicos.
    /// </summary>
    public sealed class FlowController : MonoBehaviour
    {
        public GameFlow Flow { get; private set; }
        public RunDriver Driver { get; private set; }
        public SessionProfile Profile { get; private set; }
        public PopupQueue Popups { get; } = new PopupQueue();
        public ClawMachine Claw { get; private set; }
        /// <summary>Resumo da última corrida (válido em Results).</summary>
        public RunSummary LastSummary { get; private set; }

        private int _tokensFedToClaw;
        private bool _announcedBest;
        private readonly XorShiftRandom _clawRng = new XorShiftRandom(System.Environment.TickCount);

        public void Bind(RunDriver driver, SessionProfile profile = null)
        {
            Driver = driver;
            Profile = profile ?? new SessionProfile();
            Flow = new GameFlow(driver.Bus)
            {
                RevivesAvailable = () => Profile.SecondChances,
                ConsumeRevive = () => Profile.SecondChances--,
                TokensAvailable = PrepareClawTokens,
                RequestRevive = () => Driver.Sim.Revive(),
                RequestNewRun = NewRun
            };

            driver.AutoRestart = false;
            driver.CanSimulate = () => Flow.SimulationRuns;
            driver.CanControl = () => Flow.PlayerControls;

            var bus = driver.Bus;
            bus.Subscribe<PlayerDied>(_ => Flow.PlayerDied());
            bus.Subscribe<PlayerStopped>(_ => Flow.PlayerStopped());
            bus.Subscribe<GameStateChanged>(OnStateChanged);
            bus.Subscribe<TokenCollected>(_ => Popups.Push("+1 ficha"));
            bus.Subscribe<VehicleMounted>(e => Popups.Push(VehicleName(e.Kind)));
            bus.Subscribe<DistanceChanged>(e =>
            {
                if (!_announcedBest && Profile.BestDistance > 0 && e.Meters > Profile.BestDistance)
                {
                    _announcedBest = true;
                    Popups.Push("Novo recorde!");
                }
            });
        }

        private void Update()
        {
            if (Flow == null) return;
            Flow.Tick(Time.unscaledDeltaTime);
            Popups.Tick(Time.unscaledDeltaTime);
        }

        private void OnApplicationPause(bool paused) { if (paused) Flow?.Pause(); }
        private void OnApplicationFocus(bool focus) { if (!focus) Flow?.Pause(); }

        // ---------- comandos para a UI ----------

        public void SkipSplash() => Flow.SkipSplash();
        public void StartRun() => Flow.StartRun();
        public void Pause() => Flow.Pause();
        public void Resume() => Flow.Resume();
        public void GiveUp() => Flow.GiveUp();
        public void AcceptRevive() => Flow.AcceptRevive();
        public void DeclineRevive() => Flow.DeclineRevive();
        public void PlayAgain() => Flow.PlayAgain();
        public void BackToMenu() => Flow.BackToMenu();

        /// <summary>Uma tentativa na Garra. A UI anima e depois chama <see cref="FinishClaw"/> se acabar.</summary>
        public PrizeEntry PlayClaw() => Claw.Play();

        public int CashOutClaw() => Claw.CashOut();

        public void FinishClaw(bool wonSecondChance) => Flow.ClawFinished(wonSecondChance);

        // ---------- interno ----------

        /// <summary>Fichas disponíveis para a Garra: as da corrida (ainda não usadas) + as guardadas.</summary>
        private int PrepareClawTokens()
        {
            if (Claw == null)
            {
                Claw = new ClawMachine(Profile.BankedTokens, _clawRng);
                Profile.BankedTokens = 0;
            }
            int runTokens = Driver.Sim.RunTokens;
            Claw.AddTokens(runTokens - _tokensFedToClaw);
            _tokensFedToClaw = runTokens;
            return Claw.Tokens;
        }

        private void OnStateChanged(GameStateChanged e)
        {
            if (e.To != GameState.Results) return;

            int clawCoins = 0;
            if (Claw != null)
            {
                if (Claw.Tokens > 0) Claw.CashOut(); // fichas restantes viram porcas
                clawCoins = Claw.TakeCoins();
                Profile.PendingHeadStart |= Claw.HeadStartWon;
                Profile.PendingDoubleCoins |= Claw.DoubleCoinsWon;
            }
            LastSummary = Profile.ApplyRun(Driver.Sim.Distance, Driver.Sim.RunCoins, clawCoins);
        }

        private void NewRun()
        {
            Driver.Restart();
            Claw = null;
            _tokensFedToClaw = 0;
            _announcedBest = false;
            Popups.Clear();
        }

        public static string VehicleName(Core.Gameplay.Vehicles.VehicleKind k) => k switch
        {
            Core.Gameplay.Vehicles.VehicleKind.Mech => "Mecha Pisão",
            Core.Gameplay.Vehicles.VehicleKind.FlapDrone => "Drone-Asa",
            Core.Gameplay.Vehicles.VehicleKind.BlinkPod => "Cápsula Blink",
            Core.Gameplay.Vehicles.VehicleKind.RailBike => "Moto-Trilho",
            Core.Gameplay.Vehicles.VehicleKind.SkySerpent => "Serpente-Céu",
            Core.Gameplay.Vehicles.VehicleKind.FlipSuit => "Traje Inverso",
            Core.Gameplay.Vehicles.VehicleKind.SplashSki => "Jet-Esqui",
            Core.Gameplay.Vehicles.VehicleKind.HeavyRig => "Guindaste Blindado",
            _ => k.ToString()
        };
    }
}
