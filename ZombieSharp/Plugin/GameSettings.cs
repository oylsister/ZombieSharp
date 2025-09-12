using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class GameSettings(ILogger<ZombieSharp> logger)
{
    private readonly ILogger<ZombieSharp> _logger = logger;
    public static GameConfigs? Settings = null;

    public void GameSettingsOnMapStart()
    {
        Settings = null;

        // initial class config data.
        Settings = new();

        var configPath = Path.Combine(ZombieSharp.ConfigPath, "gamesettings.jsonc");

        if(!File.Exists(configPath))
        {
            _logger.LogCritical("[GameSettingsOnMapStart] Couldn't find a gamesettings.jsonc file!");
            return;
        }

        _logger.LogInformation("[GameSettingsOnMapStart] Loading Game settings file.");
        Settings = JsonConvert.DeserializeObject<GameConfigs>(File.ReadAllText(configPath));
    }
}