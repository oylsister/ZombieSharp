using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class HitGroup(ILogger<ZombieSharp> logger)
{
    private readonly ILogger<ZombieSharp> _logger = logger;
    public static Dictionary<string, HitGroupData>? HitGroupConfigs = [];

    public void HitGroupOnMapStart()
    {
        // make sure this one is null.
        HitGroupConfigs = null;

        // initial class config data.
        HitGroupConfigs = new Dictionary<string, HitGroupData>();

        var configPath = Path.Combine(ZombieSharp.ConfigPath, "hitgroups.jsonc");

        if(!File.Exists(configPath))
        {
            _logger.LogCritical("[HitGroupOnMapStart] Couldn't find a hitgroups.jsonc file!");
            return;
        }

        _logger.LogInformation("[HitGroupOnMapStart] Load Player Classes file.");

        // we get data from jsonc file.
        HitGroupConfigs = JsonConvert.DeserializeObject<Dictionary<string, HitGroupData>>(File.ReadAllText(configPath));
    }

    public static float GetHitGroupKnockback(int hitgroups)
    {
        if(HitGroupConfigs == null)
            return 1.0f;

        var index = GetHitGroupDataByIndex(hitgroups);

        if(index == null)
            return 1.0f;

        return index.Knockback;
    }

    public static HitGroupData? GetHitGroupDataByIndex(int hitgroups)
    {
        if(HitGroupConfigs == null)
            return null;

        return HitGroupConfigs.Where(p => p.Value.Index == hitgroups).FirstOrDefault().Value;
    }
}