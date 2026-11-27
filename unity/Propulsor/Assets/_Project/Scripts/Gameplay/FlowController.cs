using System;
using Propulsor.Core;
using Propulsor.Core.Economy;
using Propulsor.Core.Events;
using Propulsor.Core.Flow;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Progress;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Liga o <see cref="GameFlow"/> (lógica pura) à corrida (<see cref="RunDriver"/>) e aos
    /// sistemas de meta-jogo (<see cref="GameServices"/>): decide quando a simulação avança,
    /// encerra corridas (economia, missões, conquistas, salvamento), conduz a Garra da Sorte
    /// e a oferta de revive. A UI só chama os métodos públicos.
    /// </summary>
    public sealed class FlowController : MonoBehaviour
    {
        public GameFlow Flow { get; private set; }
        public RunDriver Driver { get; private set; }
        public GameServices Services { get; private set; }
        public SessionProfile Profile => Services.Profile;
        public PopupQueue Popups { get; } = new PopupQueue();
        public ClawMachine Claw { get; private set; }
        /// <summary>Relatório da última corrida (válido em Results).</summary>
        public RunEndReport LastReport { get; private set; }
        public RunSummary LastSummary => LastReport?.Summary ?? default;

        /// <summary>Decide os sons (nulo em testes sem áudio).</summary>
        public Propulsor.Core.Audio.AudioDirector Audio { get; set; }

        /// <summary>Disparado quando o equipamento visual muda (a personagem se redesenha).</summary>
        public event Action AppearanceChanged;

        private int _tokensFedToClaw;
        private bool _announcedBest;

        public void Bind(RunDriver driver, GameServices services)
        {
            Driver = driver;
            Services = services;
            Flow = new GameFlow(driver.Bus)
            {
                RevivesAvailable = () => Profile.SecondChances,
                ConsumeRevive = () => { Services.Inventory.Consume(ItemIds.SecondChance); Services.Persist(); },
                TokensAvailable = PrepareClawTokens,
                RequestRevive = () => Driver.Sim.Revive(),
                RequestNewRun = NewRun
            };

            // Cada corrida nasce com os gadgets equipados e os bônus pendentes.
            var baseFactory = driver.ConfigFactory;
            driver.ConfigFactory = () => Services.BuildRunConfig(baseFactory != null ? baseFactory() : new RunConfig());
            driver.AutoRestart = false;
            driver.CanSimulate = () => Flow.SimulationRuns;
            driver.CanControl = () => Flow.PlayerControls;
            driver.Restart();

            var bus = driver.Bus;
            bus.Subscribe<PlayerDied>(_ => Flow.PlayerDied());
            bus.Subscribe<PlayerStopped>(_ => Flow.PlayerStopped());
            bus.Subscribe<GameStateChanged>(OnStateChanged);
            bus.Subscribe<TokenCollected>(_ => Popups.Push("+1 ficha"));
            bus.Subscribe<VehicleMounted>(e => Popups.Push(VehicleNames.Get(e.Kind)));
            bus.Subscribe<MissionCompleted>(e => Popups.Push($"✓ {e.Text} {new string('★', e.Stars)}"));
            bus.Subscribe<AchievementUnlocked>(e => Popups.Push($"Conquista: {e.Name}"));
            bus.Subscribe<HeadStartStarted>(_ => Popups.Push("ARRANCADA!"));
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

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            Flow?.Pause();
            Services?.Persist();
        }

        private void OnApplicationFocus(bool focus) { if (!focus) Flow?.Pause(); }
        private void OnApplicationQuit() => Services?.Persist();

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

        /// <summary>Botão de Arrancada no HUD (só no começo da corrida).</summary>
        public bool CanUseHeadStart =>
            Flow.State == GameState.Running && Driver.Sim.Distance < 50f && !Driver.Sim.IsHeadStarting && Profile.HeadStarts > 0;

        public void UseHeadStart()
        {
            if (CanUseHeadStart && Services.UseHeadStart(Driver.Sim)) Services.Persist();
        }

        public PrizeEntry PlayClaw() => Claw.Play();
        public int CashOutClaw() => Claw.CashOut();
        public void FinishClaw(bool wonSecondChance) => Flow.ClawFinished(wonSecondChance);

        public PurchaseResult Buy(string id)
        {
            var r = Services.Buy(id);
            if (r == PurchaseResult.Ok) AppearanceChanged?.Invoke();
            return r;
        }

        public EquipResult Equip(string id)
        {
            var r = Services.Equip(id);
            AppearanceChanged?.Invoke();
            return r;
        }

        public void Unequip(string id)
        {
            Services.Unequip(id);
            AppearanceChanged?.Invoke();
        }

        public void ResetProgress()
        {
            Services.ResetProgress();
            AppearanceChanged?.Invoke();
        }

        // ---------- interno ----------

        /// <summary>Fichas para a Garra: as guardadas (na criação) + as da corrida ainda não usadas.</summary>
        private int PrepareClawTokens()
        {
            if (Claw == null) Claw = Services.CreateClaw(Driver.Sim.Config);
            int runTokens = Driver.Sim.RunTokens;
            Claw.AddTokens(runTokens - _tokensFedToClaw);
            _tokensFedToClaw = runTokens;
            return Claw.Tokens;
        }

        private void OnStateChanged(GameStateChanged e)
        {
            if (e.To == GameState.Results) LastReport = Services.EndRun(Driver.Sim, Claw);
        }

        private void NewRun()
        {
            Driver.Restart();
            Claw = null;
            _tokensFedToClaw = 0;
            _announcedBest = false;
            Popups.Clear();
        }
    }
}
