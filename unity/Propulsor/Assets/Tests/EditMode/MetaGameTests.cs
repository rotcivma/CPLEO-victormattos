using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Propulsor.Core;
using Propulsor.Core.Economy;
using Propulsor.Core.Events;
using Propulsor.Core.Flow;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Gameplay.Vehicles;
using Propulsor.Core.Physics;
using Propulsor.Core.Progress;
using Propulsor.Core.Save;

namespace Propulsor.Tests.EditMode
{
    public class MiniJsonTests
    {
        [Test]
        public void IdaEVolta_PreservaValores()
        {
            var obj = new Dictionary<string, object>
            {
                ["texto"] = "Olá \"Nina\"\n\\ é",
                ["n"] = 12.5, ["i"] = 3, ["b"] = true, ["nulo"] = null,
                ["lista"] = new List<object> { 1.0, "x", false },
                ["obj"] = new Dictionary<string, object> { ["k"] = -2e-3 }
            };
            var back = (IDictionary<string, object>)MiniJson.Parse(MiniJson.Serialize(obj));
            Assert.AreEqual("Olá \"Nina\"\n\\ é", back["texto"]);
            Assert.AreEqual(12.5, back["n"]);
            Assert.AreEqual(3.0, back["i"]);
            Assert.AreEqual(true, back["b"]);
            Assert.IsNull(back["nulo"]);
            CollectionAssert.AreEqual(new object[] { 1.0, "x", false }, (List<object>)back["lista"]);
            Assert.AreEqual(-2e-3, ((IDictionary<string, object>)back["obj"])["k"]);
        }

        [TestCase("{")]
        [TestCase("{\"a\":}")]
        [TestCase("[1,2")]
        [TestCase("{\"a\":1} lixo")]
        [TestCase("")]
        public void JsonInvalido_LancaFormatException(string json)
        {
            Assert.Throws<System.FormatException>(() => MiniJson.Parse(json));
        }
    }

    /// <summary>S-01..S-03.</summary>
    public class SaveTests
    {
        private static SessionProfile Sample()
        {
            var p = new SessionProfile
            {
                Coins = 1234, BankedTokens = 5, BestDistance = 987, PendingHeadStart = true,
                GadgetSlots = 3, Rank = 4, StarsInRank = 2, Prestige = 1, MusicVolume = 0.3f, Vibration = false
            };
            p.Owned.Add("ima_porcas");
            p.Gadgets.Add("ima_porcas");
            p.Equipped["jetpack"] = "jet_bolhas";
            p.SecondChances = 2;
            p.ActiveMissions.Add(new MissionState { Id = "dist_500", Progress = 12.5f, Done = true });
            p.CompletedMissions.Add("porcas_200");
            p.Stats[StatKeys.Runs] = 7;
            p.Achievements.Add("primeira_saida");
            return p;
        }

        [Test] // S-01
        public void SalvarECarregar_IdaEVolta()
        {
            var storage = new MemorySaveStorage();
            var sm = new SaveManager(storage);
            sm.Save(Sample());
            var p = sm.Load();
            Assert.AreEqual(LoadOutcome.Loaded, sm.LastOutcome);
            Assert.AreEqual(1234, p.Coins);
            Assert.AreEqual(5, p.BankedTokens);
            Assert.AreEqual(987, p.BestDistance);
            Assert.IsTrue(p.PendingHeadStart);
            Assert.AreEqual(3, p.GadgetSlots);
            Assert.AreEqual(4, p.Rank);
            Assert.AreEqual(2, p.StarsInRank);
            Assert.AreEqual(1, p.Prestige);
            Assert.AreEqual(0.3f, p.MusicVolume, 1e-5);
            Assert.IsFalse(p.Vibration);
            Assert.IsTrue(p.Owned.Contains("ima_porcas"));
            CollectionAssert.AreEqual(new[] { "ima_porcas" }, p.Gadgets);
            Assert.AreEqual("jet_bolhas", p.Equipped["jetpack"]);
            Assert.AreEqual(2, p.SecondChances);
            Assert.AreEqual("dist_500", p.ActiveMissions[0].Id);
            Assert.AreEqual(12.5f, p.ActiveMissions[0].Progress);
            Assert.IsTrue(p.ActiveMissions[0].Done);
            Assert.IsTrue(p.CompletedMissions.Contains("porcas_200"));
            Assert.AreEqual(7, p.Stat(StatKeys.Runs));
            Assert.IsTrue(p.Achievements.Contains("primeira_saida"));
        }

        [Test]
        public void SemArquivo_PerfilNovo()
        {
            var sm = new SaveManager(new MemorySaveStorage());
            var p = sm.Load();
            Assert.AreEqual(LoadOutcome.NewProfile, sm.LastOutcome);
            Assert.AreEqual(0, p.Coins);
        }

        [Test] // S-02
        public void ArquivoCorrompido_UsaBackup()
        {
            var storage = new MemorySaveStorage();
            var sm = new SaveManager(storage);
            var p = Sample();
            sm.Save(p);              // vira o principal
            p.Coins = 9999;
            sm.Save(p);              // anterior vira .bak
            Assert.IsTrue(storage.Exists(SaveManager.BackupName));
            storage.Files[SaveManager.FileName] = "{ corrompido";
            var loaded = sm.Load();
            Assert.AreEqual(LoadOutcome.LoadedFromBackup, sm.LastOutcome);
            Assert.AreEqual(1234, loaded.Coins);
            Assert.IsNotNull(sm.LastError);
        }

        [Test]
        public void AmbosCorrompidos_PerfilNovo()
        {
            var storage = new MemorySaveStorage();
            storage.Files[SaveManager.FileName] = "nada";
            storage.Files[SaveManager.BackupName] = "[1,";
            var sm = new SaveManager(storage);
            sm.Load();
            Assert.AreEqual(LoadOutcome.NewProfile, sm.LastOutcome);
        }

        [Test]
        public void EscritaAtomica_NaoDeixaTemporario()
        {
            var storage = new MemorySaveStorage();
            new SaveManager(storage).Save(Sample());
            Assert.IsFalse(storage.Exists(SaveManager.TempName));
        }

        [Test] // S-03
        public void Migracao_V0ParaV1()
        {
            var storage = new MemorySaveStorage();
            storage.Files[SaveManager.FileName] = "{\"coins\": 300, \"best\": 450, \"tokens\": 2, \"secondChances\": 1}";
            var sm = new SaveManager(storage);
            var p = sm.Load();
            Assert.AreEqual(LoadOutcome.Loaded, sm.LastOutcome);
            Assert.AreEqual(300, p.Coins);
            Assert.AreEqual(450, p.BestDistance);
            Assert.AreEqual(2, p.BankedTokens);
            Assert.AreEqual(1, p.SecondChances);
            Assert.AreEqual(SessionProfile.CurrentSchema, p.SchemaVersion);
        }

        [Test]
        public void VersaoFutura_Rejeitada()
        {
            Assert.Throws<System.FormatException>(() => ProfileSerializer.FromJson("{\"schemaVersion\": 99}"));
        }

        [Test]
        public void ValoresInvalidos_SaoSaneados()
        {
            var p = ProfileSerializer.FromJson(
                "{\"schemaVersion\":1,\"wallet\":{\"porcas\":-50},\"progress\":{\"rank\":99},\"settings\":{\"music\":7}}");
            Assert.AreEqual(0, p.Coins);
            Assert.AreEqual(15, p.Rank);
            Assert.AreEqual(1f, p.MusicVolume);
        }

        [Test]
        public void ArquivoDeVerdade_NoDisco()
        {
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "propulsor_teste_" + System.Guid.NewGuid());
            try
            {
                var sm = new SaveManager(new FileSaveStorage(dir));
                sm.Save(Sample());
                sm.Save(Sample());
                Assert.AreEqual(1234, sm.Load().Coins);
                Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(dir, SaveManager.BackupName)));
            }
            finally { System.IO.Directory.Delete(dir, true); }
        }
    }

    /// <summary>L-01..L-03, I-01, I-02 e carteira.</summary>
    public class EconomyTests
    {
        private SessionProfile _p;
        private Wallet _w;
        private Inventory _inv;
        private Store _store;
        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _p = new SessionProfile();
            _bus = new EventBus();
            _w = new Wallet(_p, _bus);
            _inv = new Inventory(_p, ItemCatalog.CreateDefault());
            _store = new Store(_w, _inv, _p, _bus);
        }

        [Test]
        public void ItensGratis_PossuidosEEquipados()
        {
            Assert.IsTrue(_inv.Owns(ItemIds.DefaultJetpack));
            Assert.AreEqual(ItemIds.DefaultJetpack, _inv.EquippedIn("jetpack"));
            Assert.AreEqual(ItemIds.DefaultOutfit, _inv.EquippedIn("outfit"));
        }

        [Test]
        public void Carteira_NuncaNegativa()
        {
            _w.Credit(Currency.Coins, 100, "teste");
            Assert.IsFalse(_w.TryDebit(Currency.Coins, 101, "teste"));
            Assert.AreEqual(100, _w.Coins);
            Assert.IsTrue(_w.TryDebit(Currency.Coins, 100, "teste"));
            Assert.AreEqual(0, _w.Coins);
        }

        [Test]
        public void Carteira_PublicaMudancas()
        {
            var seen = new List<BalanceChanged>();
            _bus.Subscribe<BalanceChanged>(seen.Add);
            _w.Credit(Currency.Coins, 50, "missao");
            _w.TryDebit(Currency.Coins, 20, "compra");
            Assert.AreEqual(2, seen.Count);
            Assert.AreEqual(-20, seen[1].Delta);
            Assert.AreEqual("compra", seen[1].Reason);
        }

        [Test] // L-01
        public void Compra_ComSaldo_DebitaConcede()
        {
            _p.Coins = 5000;
            Assert.AreEqual(PurchaseResult.Ok, _store.Buy("ima_porcas"));
            Assert.AreEqual(1000, _p.Coins);
            Assert.IsTrue(_inv.Owns("ima_porcas"));
            Assert.AreEqual(1, _p.Stat(StatKeys.ItemsBought));
        }

        [Test] // L-02
        public void Compra_SemSaldo_NadaMuda()
        {
            _p.Coins = 100;
            Assert.AreEqual(PurchaseResult.InsufficientFunds, _store.Buy("ima_porcas"));
            Assert.AreEqual(100, _p.Coins);
            Assert.IsFalse(_inv.Owns("ima_porcas"));
        }

        [Test] // L-03
        public void Compra_JaPossuido()
        {
            _p.Coins = 99999;
            _store.Buy("bone");
            Assert.AreEqual(PurchaseResult.AlreadyOwned, _store.Buy("bone"));
            Assert.AreEqual(PurchaseResult.AlreadyOwned, _store.Buy(ItemIds.DefaultJetpack));
        }

        [Test]
        public void Consumiveis_Empilham_AteOLimite()
        {
            _p.Coins = 999999;
            for (int i = 0; i < 9; i++) Assert.AreEqual(PurchaseResult.Ok, _store.Buy(ItemIds.SecondChance));
            Assert.AreEqual(9, _p.SecondChances);
            Assert.AreEqual(PurchaseResult.StackFull, _store.Buy(ItemIds.SecondChance));
            Assert.AreEqual(PurchaseResult.Ok, _store.Buy(ItemIds.TokenPack));
            Assert.AreEqual(3, _p.BankedTokens);
        }

        [Test]
        public void ItemInexistente()
        {
            Assert.AreEqual(PurchaseResult.NotFound, _store.Buy("nao_existe"));
        }

        [Test] // I-01
        public void TerceiroGadget_SoComEspacoExtra()
        {
            _p.Coins = 999999;
            foreach (var id in new[] { "ima_porcas", "cinto_lastro", "botas_mola" }) _store.Buy(id);
            Assert.AreEqual(EquipResult.Ok, _inv.Equip("ima_porcas"));
            Assert.AreEqual(EquipResult.Ok, _inv.Equip("cinto_lastro"));
            Assert.AreEqual(EquipResult.NoFreeSlot, _inv.Equip("botas_mola"));
            Assert.AreEqual(PurchaseResult.Ok, _store.Buy(ItemIds.GadgetSlot3));
            Assert.AreEqual(EquipResult.Ok, _inv.Equip("botas_mola"));
            Assert.AreEqual(3, _inv.Gadgets.Count);
        }

        [Test]
        public void Equipar_ExigePosse()
        {
            Assert.AreEqual(EquipResult.NotOwned, _inv.Equip("jet_bolhas"));
            Assert.AreEqual(EquipResult.NotEquippable, _inv.Equip(ItemIds.SecondChance));
        }

        [Test]
        public void TrocarERetirarVisual()
        {
            _p.Coins = 99999;
            _store.Buy("macacao_roxo");
            _inv.Equip("macacao_roxo");
            Assert.AreEqual("macacao_roxo", _inv.EquippedIn("outfit"));
            _inv.Unequip("macacao_roxo");
            Assert.AreEqual(ItemIds.DefaultOutfit, _inv.EquippedIn("outfit"), "volta ao padrão");
            _store.Buy("bone");
            _inv.Equip("bone");
            _inv.Unequip("bone");
            Assert.IsNull(_inv.EquippedIn("hat"));
        }

        [Test] // I-02
        public void ConsumirSegundaChance_Decrementa()
        {
            _p.SecondChances = 2;
            Assert.IsTrue(_inv.Consume(ItemIds.SecondChance));
            Assert.AreEqual(1, _p.SecondChances);
            Assert.IsTrue(_inv.Consume(ItemIds.SecondChance));
            Assert.IsFalse(_inv.Consume(ItemIds.SecondChance));
            Assert.AreEqual(0, _p.SecondChances);
        }

        [Test]
        public void PinturaDeVeiculo_UmaPorVeiculo()
        {
            var skins = ItemCatalog.CreateDefault().Where(i => i.Category == ItemCategory.VehicleSkin).ToList();
            Assert.AreEqual(VehicleFactory.All.Length, skins.Count);
            Assert.AreEqual(skins.Count, skins.Select(s => s.Slot).Distinct().Count());
        }

        [Test]
        public void Catalogo_IdsUnicos()
        {
            var ids = ItemCatalog.CreateDefault().Select(i => i.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
        }
    }

    /// <summary>Gadgets e bônus aplicados à corrida (K-02 completo).</summary>
    public class RunModifierTests
    {
        private static ItemDef G(string id) => ItemCatalog.CreateDefault().First(i => i.Id == id);
        private static RunConfig Empty() => new RunConfig
        {
            Segments = new List<SegmentDef> { new SegmentDef("vazio", 30f, 0f) },
            MissilesEnabled = false
        };

        [Test]
        public void Gadgets_AlteramAConfiguracao()
        {
            var baseCfg = Empty();
            float g0 = baseCfg.Physics.Gravity;
            var cfg = RunModifiers.Apply(baseCfg, new[] { G("ima_porcas"), G("cinto_lastro"), G("botas_mola"), G("robo_coletor"),
                                                         G("ficha_sortuda"), G("para_choque") }, false, false);
            Assert.AreEqual(4f, cfg.MagnetRadius);
            Assert.AreEqual(g0 * 1.3f, cfg.Physics.Gravity, 1e-4);
            Assert.AreEqual(9f, cfg.LandingBounceSpeed);
            Assert.AreEqual(3f, cfg.HelperRange);
            Assert.IsTrue(cfg.LuckyLastToken);
            Assert.AreEqual(2f, cfg.InvulnerableAfterVehicle);
        }

        [Test]
        public void Gravidade_NaoAlteraParametrosCompartilhados()
        {
            var shared = new PlayerPhysicsParams();
            var cfg = new RunConfig { Physics = shared };
            RunModifiers.Apply(cfg, new[] { G("cinto_lastro") }, false, false);
            Assert.AreEqual(-38f, shared.Gravity);
        }

        [Test] // K-02
        public void PorcasEmDobro_SoNaCorrida()
        {
            var seg = new SegmentDef("moedas", 20f, 0f).CoinLine(2f, 0.5f, 5);
            var cfg = Empty();
            cfg.Segments = new List<SegmentDef> { seg };
            cfg = RunModifiers.Apply(cfg, new ItemDef[0], doubleCoins: true, headStart: false);
            var s = new RunSimulation(cfg, 1);
            while (s.Motor.X < 39f) s.Step(0.02f, false, false); // só o 1º trecho (x 20–40)
            Assert.AreEqual(10, s.RunCoins);
        }

        [Test]
        public void Arrancada_Avanca750mInvulneravel()
        {
            var wall = new SegmentDef("parede", 20f, 0f).Zap(10f, 3.5f, 7f, 90f); // mataria a pé
            var cfg = RunModifiers.Apply(new RunConfig { Segments = new List<SegmentDef> { wall }, MissilesEnabled = false },
                                         new ItemDef[0], false, headStart: true);
            var s = new RunSimulation(cfg, 1);
            bool ended = false;
            s.Bus.Subscribe<HeadStartEnded>(_ => ended = true);
            Assert.IsTrue(s.IsHeadStarting);
            for (int i = 0; i < 2000 && !ended; i++) s.Step(0.02f, false, false);
            Assert.IsTrue(ended);
            Assert.IsFalse(s.IsDead, "invulnerável atravessando paredes");
            Assert.GreaterOrEqual(s.Distance, 750f);
            Assert.IsTrue(s.IsInvulnerable, "1 s extra de proteção ao terminar");
        }

        [Test]
        public void BotasDeMola_QuicamAoPousar()
        {
            var cfg = Empty();
            cfg.LandingBounceSpeed = 9f;
            var s = new RunSimulation(cfg, 1);
            for (int i = 0; i < 15; i++) s.Step(0.02f, true, false); // sobe um pouco
            float maxAfterLanding = 0f; bool landed = false;
            s.Bus.Subscribe<PlayerLanded>(_ => landed = true);
            for (int i = 0; i < 200; i++)
            {
                s.Step(0.02f, false, false);
                if (landed) maxAfterLanding = System.Math.Max(maxAfterLanding, s.Motor.Y);
            }
            Assert.IsTrue(landed);
            Assert.Greater(maxAfterLanding, 0.5f, "quicou depois de tocar o chão");
        }

        [Test]
        public void RoboColetor_PegaPorcasBaixasAFrente()
        {
            var seg = new SegmentDef("moedas_baixas", 20f, 0f).CoinLine(2f, 1.2f, 6);
            var cfg = Empty();
            cfg.Segments = new List<SegmentDef> { seg };
            cfg.HelperRange = 3f;
            var s = new RunSimulation(cfg, 1);
            for (int i = 0; i < 200; i++) s.Step(0.02f, true, false); // voando alto: sem o robô não pegaria
            Assert.AreEqual(6, s.RunCoins);
        }

        [Test]
        public void FichaSortuda_ChegaNaGarra()
        {
            var services = new GameServices(new EventBus(), null, new XorShiftRandom(1));
            services.Profile.BankedTokens = 1;
            var claw = services.CreateClaw(new RunConfig { LuckyLastToken = true });
            Assert.IsTrue(claw.GuaranteeLastToken);
            Assert.AreEqual(1, claw.Tokens);
            Assert.AreEqual(0, services.Profile.BankedTokens);
        }
    }

    /// <summary>Q-01..Q-05, A-01, medidor de corrida.</summary>
    public class ProgressTests
    {
        private static MissionDef Def(string id, string counter, float target, MissionScope scope, int stars) =>
            new MissionDef { Id = id, Text = id + " {0}", Counter = counter, Target = target, Scope = scope, Stars = stars };

        private static (MissionManager mm, SessionProfile p, RunTracker tr, EventBus bus) Setup(params MissionDef[] defs)
        {
            var p = new SessionProfile();
            var bus = new EventBus();
            var tr = new RunTracker(bus);
            var mm = new MissionManager(p, defs, new XorShiftRandom(3), bus);
            mm.Attach(tr);
            return (mm, p, tr, bus);
        }

        [Test] // Q-01
        public void SempreTresAtivas()
        {
            var services = new GameServices(new EventBus(), null, new XorShiftRandom(9));
            Assert.AreEqual(3, services.Missions.Active.Count);
            Assert.AreEqual(3, services.Missions.Active.Select(a => a.Id).Distinct().Count());
            for (int r = 0; r < 20; r++)
            {
                services.Profile.ActiveMissions[0].Done = true;
                services.Missions.FinishRun();
                Assert.AreEqual(3, services.Missions.Active.Count);
            }
        }

        [Test] // Q-02
        public void EscopoCorrida_Zera_EscopoTotal_Acumula()
        {
            var (mm, p, tr, bus) = Setup(
                Def("run", Counters.Coins, 10, MissionScope.Run, 1),
                Def("life", Counters.Coins, 10, MissionScope.Lifetime, 1),
                Def("outra", Counters.Distance, 9999, MissionScope.Run, 1));
            for (int run = 0; run < 2; run++)
            {
                bus.Publish(new RunStarted(run));
                for (int i = 0; i < 6; i++) bus.Publish(new CoinCollected(1, i + 1));
                mm.FinishRun();
            }
            Assert.IsTrue(p.CompletedMissions.Contains("life"), "missão de total (6+6 ≥ 10) foi concluída");
            Assert.IsFalse(p.CompletedMissions.Contains("run"));
            // Banco de teste pequeno: a missão volta como nova, com progresso zerado.
            var life = p.ActiveMissions.FirstOrDefault(m => m.Id == "life");
            if (life != null) { Assert.AreEqual(0f, life.Progress); Assert.IsFalse(life.Done); }
            Assert.IsNotNull(p.ActiveMissions.FirstOrDefault(m => m.Id == "run"), "6 por corrida nunca chega a 10");
        }

        [Test]
        public void ConclusaoAvisaNaHora()
        {
            var (mm, p, tr, bus) = Setup(Def("a", Counters.Coins, 3, MissionScope.Run, 2),
                                         Def("b", Counters.Distance, 999, MissionScope.Run, 1),
                                         Def("c", Counters.Tokens, 99, MissionScope.Run, 1));
            var done = new List<MissionCompleted>();
            bus.Subscribe<MissionCompleted>(done.Add);
            bus.Publish(new RunStarted(1));
            for (int i = 0; i < 3; i++) bus.Publish(new CoinCollected(1, i + 1));
            Assert.AreEqual(1, done.Count);
            Assert.AreEqual("a", done[0].Id);
            Assert.AreEqual(2, done[0].Stars);
            for (int i = 0; i < 3; i++) bus.Publish(new CoinCollected(1, i + 4));
            Assert.AreEqual(1, done.Count, "avisa uma vez só");
        }

        [Test] // Q-03
        public void PularMissao_Custa500PorEstrela()
        {
            var (mm, p, tr, bus) = Setup(Def("tres", Counters.Coins, 9999, MissionScope.Run, 3),
                                         Def("x", Counters.Distance, 9999, MissionScope.Run, 1),
                                         Def("y", Counters.Tokens, 9999, MissionScope.Run, 1));
            var w = new Wallet(p);
            p.Coins = 1400;
            Assert.AreEqual(1500, mm.SkipCost("tres"));
            Assert.IsFalse(mm.Skip("tres", w));
            p.Coins = 1500;
            Assert.IsTrue(mm.Skip("tres", w));
            Assert.AreEqual(0, p.Coins);
            Assert.IsTrue(p.ActiveMissions.First(m => m.Id == "tres").Done);
        }

        [Test] // Q-04
        public void Estrelas_SobemPatenteComRecompensa()
        {
            var (mm, p, tr, bus) = Setup(Def("a", Counters.Coins, 1, MissionScope.Run, 3),
                                         Def("b", Counters.Distance, 9999, MissionScope.Run, 1),
                                         Def("c", Counters.Tokens, 9999, MissionScope.Run, 1));
            bus.Publish(new RunStarted(1));
            bus.Publish(new CoinCollected(1, 1));
            var ups = new List<RankUp>();
            int reward = mm.FinishRun(ups);
            Assert.AreEqual(2, p.Rank, "3 estrelas completam a patente 1");
            Assert.AreEqual(0, p.StarsInRank);
            Assert.AreEqual(RankTable.Get(1).Reward, reward);
            Assert.AreEqual(1, ups.Count);
            Assert.AreEqual(RankTable.Get(2).Name, ups[0].Name);
        }

        [Test] // Q-05
        public void Patente15_PrestigioReiniciaCiclo()
        {
            var services = new GameServices(new EventBus(), null, new XorShiftRandom(2));
            var p = services.Profile;
            Assert.IsFalse(services.PrestigeReset());
            p.Rank = 15;
            p.CompletedMissions.Add("dist_500");
            Assert.IsTrue(services.Missions.CanPrestige);
            Assert.IsTrue(services.PrestigeReset());
            Assert.AreEqual(1, p.Rank);
            Assert.AreEqual(1, p.Prestige);
            Assert.AreEqual(0, p.CompletedMissions.Count);
            Assert.AreEqual(3, p.ActiveMissions.Count);
            Assert.IsTrue(p.Achievements.Contains("selo_de_prestigio"));
        }

        [Test]
        public void TabelaDePatentes_15Niveis()
        {
            Assert.AreEqual(15, RankTable.Levels.Length);
            Assert.AreEqual("Nina", RankTable.Get(15).Name);
            Assert.AreEqual(RankTable.Get(15).Name, RankTable.Get(99).Name);
        }

        [Test]
        public void Medidor_VooChaoESemPorcas()
        {
            var bus = new EventBus();
            var tr = new RunTracker(bus);
            bus.Publish(new RunStarted(1));
            for (int m = 1; m <= 50; m++) bus.Publish(new DistanceChanged(m));          // 50 m no chão
            bus.Publish(new PlayerStateChanged(MotorState.Grounded, MotorState.Thrusting));
            for (int m = 51; m <= 120; m++) bus.Publish(new DistanceChanged(m));        // 70 m voando
            bus.Publish(new CoinCollected(1, 1));
            bus.Publish(new PlayerStateChanged(MotorState.Falling, MotorState.Grounded));
            for (int m = 121; m <= 130; m++) bus.Publish(new DistanceChanged(m));
            Assert.AreEqual(60f, tr.Get(Counters.GroundDistance), 1.01f);
            Assert.AreEqual(70f, tr.Get(Counters.LongestFlight), 1.01f);
            Assert.AreEqual(120f, tr.Get(Counters.NoCoinDistance));
            Assert.AreEqual(130f, tr.Get(Counters.Distance));
            bus.Publish(new RunStarted(2));
            Assert.AreEqual(0f, tr.Get(Counters.Distance), "zera a cada corrida");
        }

        [Test]
        public void Medidor_VeiculosFeixesMortes()
        {
            var bus = new EventBus();
            var tr = new RunTracker(bus);
            bus.Publish(new VehicleMounted(VehicleKind.Mech));
            bus.Publish(new VehicleDestroyed(VehicleKind.Mech, HazardType.Zapper));
            bus.Publish(new LaserSequenceEnded());
            bus.Publish(new PlayerDied(HazardType.Missile));
            bus.Publish(new LaserSequenceEnded()); // morta: não conta
            Assert.AreEqual(1f, tr.Get(Counters.VehicleMounts));
            Assert.AreEqual(1f, tr.Get(Counters.MountsOf(VehicleKind.Mech)));
            Assert.AreEqual(1f, tr.Get(Counters.VehicleLosses));
            Assert.AreEqual(1f, tr.Get(Counters.LasersSurvived));
            Assert.AreEqual(1f, tr.Get(Counters.DeathBy(HazardType.Missile)));
        }

        [Test] // A-01
        public void Conquista_DesbloqueiaUmaVezSo()
        {
            var p = new SessionProfile();
            var bus = new EventBus();
            var am = new AchievementManager(p, AchievementCatalog.CreateDefault(), bus);
            var w = new Wallet(p);
            int events = 0;
            bus.Subscribe<AchievementUnlocked>(_ => events++);
            p.Stats[StatKeys.Runs] = 1;
            Assert.AreEqual(100, am.Evaluate(w));
            Assert.AreEqual(0, am.Evaluate(w));
            Assert.AreEqual(1, events);
            Assert.AreEqual(100, p.Coins);
            Assert.IsTrue(am.IsUnlocked("primeira_saida"));
        }

        [Test]
        public void Catalogos_IdsUnicosEMissoesValidas()
        {
            var ids = MissionCatalog.CreateDefault().Select(m => m.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
            Assert.IsTrue(MissionCatalog.CreateDefault().All(m => m.Stars >= 1 && m.Stars <= 3 && m.Target > 0));
            var a = AchievementCatalog.CreateDefault().Select(x => x.Id).ToList();
            Assert.AreEqual(a.Count, a.Distinct().Count());
        }
    }

    /// <summary>Integração do fim de corrida (economia + progressão + salvamento).</summary>
    public class GameServicesTests
    {
        [Test]
        public void FimDeCorrida_CreditaEstatisticasMissoesConquistasESalva()
        {
            var bus = new EventBus();
            var storage = new MemorySaveStorage();
            var services = new GameServices(bus, new SaveManager(storage), new XorShiftRandom(4));
            var cfg = services.BuildRunConfig(new RunConfig
            {
                Segments = new List<SegmentDef> { new SegmentDef("moedas", 20f, 0f).CoinLine(2f, 0.5f, 30, 0.6f) },
                MissilesEnabled = false
            });
            var sim = new RunSimulation(cfg, 1, bus);
            for (int i = 0; i < 1500 && !sim.IsDead; i++) sim.Step(0.02f, false, false);
            sim.Kill(HazardType.Laser);
            while (!sim.IsOver) sim.Step(0.02f, false, false);

            var claw = services.CreateClaw(cfg);
            var report = services.EndRun(sim, claw);

            Assert.Greater(sim.RunCoins, 0);
            Assert.AreEqual(1, services.Profile.Stat(StatKeys.Runs));
            Assert.AreEqual(1, services.Profile.Stat(StatKeys.DeathsBy(HazardType.Laser)));
            Assert.AreEqual((int)sim.Distance, services.Profile.BestDistance);
            Assert.IsTrue(services.Profile.Achievements.Contains("primeira_saida"));
            Assert.AreEqual(report.Summary.BalanceAfter, services.Profile.Coins);
            Assert.GreaterOrEqual(report.Summary.RewardCoins, 100);
            Assert.IsTrue(storage.Exists(SaveManager.FileName), "salvou");
            var reloaded = new SaveManager(storage).Load();
            Assert.AreEqual(services.Profile.Coins, reloaded.Coins);
        }

        [Test] // R-01
        public void Reiniciar_MantemProgresso()
        {
            var storage = new MemorySaveStorage();
            var a = new GameServices(new EventBus(), new SaveManager(storage), new XorShiftRandom(1));
            a.Profile.Coins = 777;
            a.Persist();
            var b = new GameServices(new EventBus(), new SaveManager(storage), new XorShiftRandom(1));
            Assert.AreEqual(777, b.Profile.Coins);
            CollectionAssert.AreEqual(a.Profile.ActiveMissions.Select(m => m.Id), b.Profile.ActiveMissions.Select(m => m.Id));
        }

        [Test]
        public void BonusPendentes_SaoConsumidosNaProximaCorrida()
        {
            var s = new GameServices(new EventBus(), null, new XorShiftRandom(1));
            s.Profile.PendingDoubleCoins = true;
            s.Profile.PendingHeadStart = true;
            var cfg = s.BuildRunConfig(new RunConfig());
            Assert.AreEqual(2, cfg.CoinValue);
            Assert.AreEqual(RunModifiers.HeadStartMeters, cfg.HeadStartMeters);
            var cfg2 = s.BuildRunConfig(new RunConfig());
            Assert.AreEqual(1, cfg2.CoinValue);
            Assert.AreEqual(0f, cfg2.HeadStartMeters);
        }

        [Test]
        public void ApagarProgresso_ComecaDoZero()
        {
            var storage = new MemorySaveStorage();
            var s = new GameServices(new EventBus(), new SaveManager(storage), new XorShiftRandom(1));
            s.Profile.Coins = 5000;
            s.Buy("ima_porcas");
            s.ResetProgress();
            Assert.AreEqual(0, s.Profile.Coins);
            Assert.IsFalse(s.Inventory.Owns("ima_porcas"));
            Assert.AreEqual(0, new SaveManager(storage).Load().Coins);
        }

        [Test]
        public void ArrancadaDoInventario_DuranteACorrida()
        {
            var s = new GameServices(new EventBus(), null, new XorShiftRandom(1));
            var sim = new RunSimulation(s.BuildRunConfig(new RunConfig { MissilesEnabled = false }), 1);
            Assert.IsFalse(s.UseHeadStart(sim), "sem item");
            s.Profile.HeadStarts = 1;
            Assert.IsTrue(s.UseHeadStart(sim));
            Assert.IsTrue(sim.IsHeadStarting);
            Assert.AreEqual(0, s.Profile.HeadStarts);
        }
    }
}
