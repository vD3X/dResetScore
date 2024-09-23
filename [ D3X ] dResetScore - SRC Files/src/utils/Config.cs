using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static dResetScore.dResetScore;

namespace dResetScore
{
    public static class Config
    {
        private static readonly string configPath = Path.Combine(Instance.ModuleDirectory, "Config.json");
        public static ConfigModel config;
        private static FileSystemWatcher fileWatcher;

        public static ConfigModel LoadedConfig => config;

        public static void Initialize()
        {
            config = LoadConfig();
            SetupFileWatcher();
        }

        private static ConfigModel LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                Instance.Logger.LogInformation("Plik konfiguracyjny nie istnieje. Tworzenie nowego pliku konfiguracyjnego...");
                var defaultConfig = new ConfigModel();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<ConfigModel>(json) ?? new ConfigModel();
            }
            catch (Exception ex)
            {
                Instance.Logger.LogError($"Błąd podczas wczytywania pliku konfiguracyjnego.");
                return new ConfigModel();
            }
        }

        public static void SaveConfig(ConfigModel config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Instance.Logger.LogError($"Błąd podczas zapisywania pliku konfiguracyjnego: {ex.Message}");
            }
        }

        private static void SetupFileWatcher()
        {
            fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(configPath))
            {
                Filter = Path.GetFileName(configPath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            fileWatcher.Changed += (sender, e) => config = LoadConfig();
            fileWatcher.EnableRaisingEvents = true;
        }

        public class ConfigModel
        {
            public Settings Settings { get; set; } = new Settings();
            public Commands Commands { get; set; } = new Commands();
        }

        public class Settings
        {
            public string AdminFlag { get; set; } = "@css/ban";
            public string ResetScoreFlag { get; set; } = "";
            public int ResetScorePayment { get; set; } = 0;
            public int ResetScoreCooldown { get; set; } = 30;
        }

        public class Commands
        {
            public string ResetScore_Commands { get; set; } = "resetscore, rs";
            public string ResetTeamScore_Commands { get; set; } = "resetteamscore, rts";
            public string SetScore_Commands { get; set; } = "setscore";
            public string SetTeamScore_Commands { get; set; } = "setteamscore";
        }
    }
}