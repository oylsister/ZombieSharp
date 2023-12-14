using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public GameSettings ConfigSettings { get; private set; }
        
        public bool SettingsIntialize(string mapname)
        {
            string configPath = Path.Combine(ModuleDirectory, $"settings/{mapname}.json");
            string defaultconfig = Path.Combine(ModuleDirectory, $"settings/default.json");

            if(File.Exists(configPath))
            {
                ConfigSettings = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(configPath));
                Logger.LogInformation($"[Z:Sharp] Found config file for {mapname}.json.");
                return true;
            }

            if(File.Exists(defaultconfig))
            {
                ConfigSettings = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(defaultconfig));
                Logger.LogInformation($"[Z:Sharp] There is no config file for {mapname}.json, Default file is used.");
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

}
