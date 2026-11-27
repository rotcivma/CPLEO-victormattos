using System.Collections.Generic;
using Propulsor.Core.Economy;
using Propulsor.Core.Events;
using Propulsor.Core.Flow;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Progress;
using Propulsor.Core.Save;

namespace Propulsor.Core
{
    /// <summary>Resultado completo do fim de corrida (para a tela de resultados).</summary>
    public sealed class RunEndReport
    {
        public RunSummary Summary;
        public readonly List<MissionCompleted> MissionsCompleted = new List<MissionCompleted>();
        public readonly List<RankUp> RankUps = new List<RankUp>();
        public readonly List<AchievementUnlocked> Achievements = new List<AchievementUnlocked>();
    }

    /// <summary>
    /// Raiz de composição dos sistemas de meta-jogo: perfil, carteira, inventário, loja,
    /// missões, conquistas e salvamento. Não depende de Unity (docs/fase-4/01 §3.12–3.19).
    /// </summary>
    public sealed class GameServices
    {
        public SessionProfile Profile { get; private set; }
        public Wallet Wallet { get; private set; }
        public Inventory Inventory { get; private set; }
        public Store Store { get; private set; }
        public MissionManager Missions { get; private set; }
        public AchievementManager Achievements { get; private set; }
        public RunTracker Tracker { get; }
        public SaveManager Save { get; }
        public EventBus Bus { get; }
        public IReadOnlyList<ItemDef> Catalog { get; }

        private readonly IRandom _rng;
        private readonly List<MissionCompleted> _completedThisRun = new List<MissionCompleted>();

        public GameServices(EventBus bus, SaveManager save = null, IRandom rng = null)
        {
            Bus = bus;
            Save = save;
            _rng = rng ?? new XorShiftRandom(System.Environment.TickCount);
            Catalog = ItemCatalog.CreateDefault();
            Tracker = new RunTracker(bus);
            bus.Subscribe<MissionCompleted>(e => _completedThisRun.Add(e));
            bus.Subscribe<RunStarted>(_ => _completedThisRun.Clear());
            Use(save != null ? save.Load() : new SessionProfile());
        }

        private void Use(SessionProfile profile)
        {
            Profile = profile;
            Wallet = new Wallet(profile, Bus);
            Inventory = new Inventory(profile, Catalog);
            Store = new Store(Wallet, Inventory, profile, Bus);
            Missions?.Detach();
            Missions = new MissionManager(profile, MissionCatalog.CreateDefault(), _rng, Bus);
            Missions.Attach(Tracker);
            Achievements = new AchievementManager(profile, AchievementCatalog.CreateDefault(), Bus);
        }

        /// <summary>
        /// Configuração da próxima corrida: gadgets equipados + bônus pendentes da Garra
        /// (consumidos aqui). <paramref name="useHeadStartItem"/> gasta uma Arrancada do inventário.
        /// </summary>
        public RunConfig BuildRunConfig(RunConfig baseConfig = null, bool useHeadStartItem = false)
        {
            bool head = Profile.PendingHeadStart || (useHeadStartItem && Inventory.Consume(ItemIds.HeadStart));
            bool dbl = Profile.PendingDoubleCoins;
            Profile.PendingHeadStart = false;
            Profile.PendingDoubleCoins = false;
            return RunModifiers.Apply(baseConfig ?? new RunConfig(), Inventory.EquippedGadgets(), dbl, head);
        }

        /// <summary>Usa uma Arrancada do inventário durante a corrida (botão do HUD).</summary>
        public bool UseHeadStart(RunSimulation sim)
        {
            if (sim.IsHeadStarting || sim.IsDead || !Inventory.Consume(ItemIds.HeadStart)) return false;
            sim.StartHeadStart(RunModifiers.HeadStartMeters);
            return true;
        }

        /// <summary>Cria a Garra da Sorte do fim de corrida já com os efeitos de gadgets.</summary>
        public ClawMachine CreateClaw(RunConfig config)
        {
            var claw = new ClawMachine(Profile.BankedTokens, _rng) { GuaranteeLastToken = config?.LuckyLastToken ?? false };
            Profile.BankedTokens = 0;
            return claw;
        }

        /// <summary>
        /// Fim de corrida: credita porcas, atualiza estatísticas, missões/patentes, conquistas e salva.
        /// </summary>
        public RunEndReport EndRun(RunSimulation sim, ClawMachine claw)
        {
            var report = new RunEndReport();
            int clawCoins = 0;
            if (claw != null)
            {
                if (claw.Tokens > 0) claw.CashOut();
                clawCoins = claw.TakeCoins();
                Profile.PendingHeadStart |= claw.HeadStartWon;
                Profile.PendingDoubleCoins |= claw.DoubleCoinsWon;
                Tracker.Add(Counters.ClawPlays, claw.Plays);
                Profile.AddStat(StatKeys.ClawPlays, claw.Plays);
                Profile.AddStat(StatKeys.ClawLosses, claw.Losses);
            }

            // Estatísticas
            Profile.AddStat(StatKeys.Runs, 1);
            Profile.AddStat(StatKeys.TotalDistance, (int)sim.Distance);
            Profile.AddStat(StatKeys.TotalCoins, sim.RunCoins);
            Profile.AddStat(StatKeys.TotalTokens, sim.RunTokens);
            Profile.AddStat(StatKeys.VehiclesMounted, Tracker.Get(Counters.VehicleMounts));
            Profile.AddStat(StatKeys.VehiclesLost, Tracker.Get(Counters.VehicleLosses));
            Profile.AddStat(StatKeys.LasersSurvived, Tracker.Get(Counters.LasersSurvived));
            Profile.AddStat(StatKeys.WorkersKnocked, Tracker.Get(Counters.Workers));
            Profile.AddStat(StatKeys.NearMisses, Tracker.Get(Counters.NearMisses));
            foreach (HazardType h in System.Enum.GetValues(typeof(HazardType)))
                Profile.AddStat(StatKeys.DeathsBy(h), Tracker.Get(Counters.DeathBy(h)));

            var summary = Profile.ApplyRun(sim.Distance, sim.RunCoins, clawCoins);

            int rewards = Missions.FinishRun(report.RankUps);
            Wallet.Credit(Currency.Coins, rewards, "patente");
            report.MissionsCompleted.AddRange(_completedThisRun);
            rewards += Achievements.Evaluate(Wallet, report.Achievements);

            report.Summary = summary.WithRewards(rewards, Profile.Coins);
            Persist();
            return report;
        }

        public bool PrestigeReset()
        {
            if (!Missions.PrestigeReset()) return false;
            Achievements.Evaluate(Wallet);
            Persist();
            return true;
        }

        public PurchaseResult Buy(string id)
        {
            var r = Store.Buy(id);
            if (r == PurchaseResult.Ok)
            {
                Achievements.Evaluate(Wallet);
                Persist();
            }
            return r;
        }

        public EquipResult Equip(string id)
        {
            var r = Inventory.Equip(id);
            if (r == EquipResult.Ok) Persist();
            return r;
        }

        public void Unequip(string id)
        {
            Inventory.Unequip(id);
            Persist();
        }

        public bool SkipMission(string id)
        {
            bool ok = Missions.Skip(id, Wallet);
            if (ok) Persist();
            return ok;
        }

        /// <summary>Apaga todo o progresso (configurações → perfil novo).</summary>
        public void ResetProgress()
        {
            Save?.DeleteAll();
            Use(new SessionProfile());
            Persist();
        }

        public void Persist() => Save?.Save(Profile);
    }
}
