using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public GameSettings ConfigSettings { get; private set; }

        public bool SettingsIntialize(string mapname)
        {
            string configPath = Path.Combine(ModuleDirectory, $"settings/{mapname}.json");
            string defaultconfig = Path.Combine(ModuleDirectory, $"settings/default.json");

            if (File.Exists(configPath))
            {
                ConfigSettings = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(configPath));
                Logger.LogInformation($"[Z:Sharp] Found config file for {mapname}.json.");
                Logger.LogInformation($"Respawn Timer = {ConfigSettings.RespawnTimer}, Infect Timer = {ConfigSettings.FirstInfectionTimer}, MTZ Ratio = {ConfigSettings.MotherZombieRatio}");
                return true;
            }

            if (File.Exists(defaultconfig))
            {
                ConfigSettings = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(defaultconfig));
                Logger.LogInformation($"[Z:Sharp] There is no config file for {mapname}.json, Default file is used.");
                Logger.LogInformation($"Respawn Timer = {ConfigSettings.RespawnTimer}, Infect Timer = {ConfigSettings.FirstInfectionTimer}, MTZ Ratio = {ConfigSettings.MotherZombieRatio}");
                return true;
            }

            Logger.LogError("[Z:Sharp] There is no any configs seetings existed in the folder!");
            return false;
        }
    }
}

public class GameSettings
{
    public float RespawnTimer { get; set; } = 5.0f;
    public float FirstInfectionTimer { get; set; } = 15.0f;
    public float MotherZombieRatio { get; set; } = 7.0f;
    public bool TeleportMotherZombie { get; set; } = true;
    public bool EnableOnWarmup { get; set; } = false;
    public float RepeatKillerThreshold { get; set; } = 3.0f;
    public int ZombieDrop { get; set; } = 0; // 0 = stip , 1 = force drop

    // Default Class
    public string Human_Default { get; set; } = "human_default";
    public string Zombie_Default { get; set; } = "zombie_default";
    public string Mother_Zombie { get; set; } = "motherzombie";
}
