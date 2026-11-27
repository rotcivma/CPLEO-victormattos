using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Propulsor.Core.Flow;

namespace Propulsor.Core.Save
{
    /// <summary>Armazenamento de texto por nome (arquivo, memória...).</summary>
    public interface ISaveStorage
    {
        bool Exists(string name);
        string Read(string name);
        void Write(string name, string content);
        void Delete(string name);
        /// <summary>Renomeia substituindo o destino (usado na escrita atômica).</summary>
        void Move(string from, string to);
    }

    /// <summary>Arquivos numa pasta (no Unity: Application.persistentDataPath).</summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _dir;
        public FileSaveStorage(string directory) { _dir = directory; Directory.CreateDirectory(directory); }
        private string P(string name) => Path.Combine(_dir, name);
        public bool Exists(string name) => File.Exists(P(name));
        public string Read(string name) => File.ReadAllText(P(name));
        public void Write(string name, string content) => File.WriteAllText(P(name), content);
        public void Delete(string name) { if (File.Exists(P(name))) File.Delete(P(name)); }
        public void Move(string from, string to)
        {
            if (File.Exists(P(to))) File.Delete(P(to));
            File.Move(P(from), P(to));
        }
    }

    /// <summary>Armazenamento em memória (testes).</summary>
    public sealed class MemorySaveStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
        public bool Exists(string name) => Files.ContainsKey(name);
        public string Read(string name) => Files.TryGetValue(name, out var s) ? s : throw new FileNotFoundException(name);
        public void Write(string name, string content) => Files[name] = content;
        public void Delete(string name) => Files.Remove(name);
        public void Move(string from, string to) { Files[to] = Files[from]; Files.Remove(from); }
    }

    public enum LoadOutcome { Loaded, LoadedFromBackup, NewProfile }

    /// <summary>
    /// Carrega/salva o perfil em JSON. Escrita atômica (temporário → principal) mantendo o
    /// arquivo anterior como .bak; arquivo corrompido ⇒ usa o .bak ⇒ senão perfil novo.
    /// </summary>
    public sealed class SaveManager
    {
        public const string FileName = "perfil.json";
        public const string BackupName = "perfil.json.bak";
        public const string TempName = "perfil.json.tmp";

        private readonly ISaveStorage _storage;
        public LoadOutcome LastOutcome { get; private set; }
        public string LastError { get; private set; }

        public SaveManager(ISaveStorage storage) { _storage = storage; }

        public SessionProfile Load()
        {
            LastError = null;
            if (TryLoad(FileName, out var p)) { LastOutcome = LoadOutcome.Loaded; return p; }
            if (TryLoad(BackupName, out p)) { LastOutcome = LoadOutcome.LoadedFromBackup; return p; }
            LastOutcome = LoadOutcome.NewProfile;
            return new SessionProfile();
        }

        public void Save(SessionProfile profile)
        {
            string json = ProfileSerializer.ToJson(profile);
            _storage.Write(TempName, json);
            if (_storage.Exists(FileName)) _storage.Move(FileName, BackupName);
            _storage.Move(TempName, FileName);
        }

        public void DeleteAll()
        {
            _storage.Delete(FileName);
            _storage.Delete(BackupName);
            _storage.Delete(TempName);
        }

        private bool TryLoad(string name, out SessionProfile profile)
        {
            profile = null;
            if (!_storage.Exists(name)) return false;
            try
            {
                profile = ProfileSerializer.FromJson(_storage.Read(name));
                return true;
            }
            catch (Exception e) when (e is FormatException || e is InvalidCastException || e is IOException)
            {
                LastError = $"{name}: {e.Message}";
                return false;
            }
        }
    }

    /// <summary>Conversão perfil ⇄ JSON, com migração de versões antigas.</summary>
    public static class ProfileSerializer
    {
        public static string ToJson(SessionProfile p)
        {
            var root = new Dictionary<string, object>
            {
                ["schemaVersion"] = SessionProfile.CurrentSchema,
                ["wallet"] = new Dictionary<string, object> { ["porcas"] = p.Coins, ["fichas"] = p.BankedTokens },
                ["bestDistance"] = p.BestDistance,
                ["pendingBoosts"] = new Dictionary<string, object> { ["headStart"] = p.PendingHeadStart, ["doubleCoins"] = p.PendingDoubleCoins },
                ["inventory"] = new Dictionary<string, object>
                {
                    ["owned"] = p.Owned.OrderBy(x => x, StringComparer.Ordinal).Cast<object>().ToList(),
                    ["consumables"] = p.Consumables.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
                    ["equipped"] = p.Equipped.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
                    ["gadgets"] = p.Gadgets.Cast<object>().ToList(),
                    ["gadgetSlots"] = p.GadgetSlots,
                },
                ["progress"] = new Dictionary<string, object>
                {
                    ["rank"] = p.Rank,
                    ["starsInRank"] = p.StarsInRank,
                    ["prestige"] = p.Prestige,
                    ["activeMissions"] = p.ActiveMissions.Select(m => (object)new Dictionary<string, object>
                    {
                        ["id"] = m.Id, ["progress"] = m.Progress, ["done"] = m.Done
                    }).ToList(),
                    ["completedMissions"] = p.CompletedMissions.OrderBy(x => x, StringComparer.Ordinal).Cast<object>().ToList(),
                },
                ["stats"] = p.Stats.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
                ["achievements"] = p.Achievements.OrderBy(x => x, StringComparer.Ordinal).Cast<object>().ToList(),
                ["settings"] = new Dictionary<string, object>
                {
                    ["music"] = p.MusicVolume, ["sfx"] = p.SfxVolume, ["vibration"] = p.Vibration
                },
            };
            return MiniJson.Serialize(root);
        }

        /// <exception cref="FormatException">JSON inválido ou versão futura.</exception>
        public static SessionProfile FromJson(string json)
        {
            if (!(MiniJson.Parse(json) is IDictionary<string, object> root)) throw new FormatException("Raiz não é objeto");
            int version = root.GetInt("schemaVersion", 0);
            if (version > SessionProfile.CurrentSchema) throw new FormatException($"Versão {version} é mais nova que este jogo");
            if (version == 0) root = MigrateV0(root);

            var p = new SessionProfile();
            var wallet = root.GetObject("wallet");
            p.Coins = Math.Max(0, wallet.GetInt("porcas"));
            p.BankedTokens = Math.Max(0, wallet.GetInt("fichas"));
            p.BestDistance = Math.Max(0, root.GetInt("bestDistance"));
            var boosts = root.GetObject("pendingBoosts");
            p.PendingHeadStart = boosts.GetBool("headStart");
            p.PendingDoubleCoins = boosts.GetBool("doubleCoins");

            var inv = root.GetObject("inventory");
            foreach (var o in inv.GetList("owned") ?? new List<object>()) if (o is string s) p.Owned.Add(s);
            var cons = inv.GetObject("consumables");
            if (cons != null) foreach (var kv in cons) if (kv.Value is double d) p.Consumables[kv.Key] = Math.Max(0, (int)d);
            var eq = inv.GetObject("equipped");
            if (eq != null) foreach (var kv in eq) if (kv.Value is string s) p.Equipped[kv.Key] = s;
            foreach (var o in inv.GetList("gadgets") ?? new List<object>()) if (o is string s) p.Gadgets.Add(s);
            p.GadgetSlots = Math.Max(2, Math.Min(3, inv.GetInt("gadgetSlots", 2)));

            var prog = root.GetObject("progress");
            p.Rank = Math.Max(1, Math.Min(15, prog.GetInt("rank", 1)));
            p.StarsInRank = Math.Max(0, prog.GetInt("starsInRank"));
            p.Prestige = Math.Max(0, prog.GetInt("prestige"));
            foreach (var o in prog.GetList("activeMissions") ?? new List<object>())
                if (o is IDictionary<string, object> m && m.GetString("id") != null)
                    p.ActiveMissions.Add(new MissionState { Id = m.GetString("id"), Progress = m.GetFloat("progress"), Done = m.GetBool("done") });
            foreach (var o in prog.GetList("completedMissions") ?? new List<object>()) if (o is string s) p.CompletedMissions.Add(s);

            var stats = root.GetObject("stats");
            if (stats != null) foreach (var kv in stats) if (kv.Value is double d) p.Stats[kv.Key] = d;
            foreach (var o in root.GetList("achievements") ?? new List<object>()) if (o is string s) p.Achievements.Add(s);

            var set = root.GetObject("settings");
            p.MusicVolume = Clamp01(set.GetFloat("music", 0.8f));
            p.SfxVolume = Clamp01(set.GetFloat("sfx", 1f));
            p.Vibration = set.GetBool("vibration", true);
            return p;
        }

        /// <summary>
        /// v0 (protótipo): campos planos { coins, best, tokens, secondChances }.
        /// </summary>
        private static IDictionary<string, object> MigrateV0(IDictionary<string, object> v0)
        {
            return new Dictionary<string, object>
            {
                ["schemaVersion"] = 1.0,
                ["wallet"] = new Dictionary<string, object> { ["porcas"] = v0.GetDouble("coins"), ["fichas"] = v0.GetDouble("tokens") },
                ["bestDistance"] = v0.GetDouble("best"),
                ["inventory"] = new Dictionary<string, object>
                {
                    ["consumables"] = new Dictionary<string, object> { [ItemIds.SecondChance] = v0.GetDouble("secondChances") }
                }
            };
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
