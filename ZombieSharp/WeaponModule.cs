using CounterStrikeSharp.API.Core.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        private ILogger _logger = CoreLogging.Factory.CreateLogger("WeaponConfgLog");

        public WeaponConfig WeaponDatas { get; private set; }

        public void WeaponInitialize()
        {
            var configPath = Path.Combine(ModuleDirectory, "weapons.json");

            if (!File.Exists(configPath))
            {
                _logger.LogWarning("Cannot found weapons.json file!");
                return;
            }

            WeaponDatas = JsonConvert.DeserializeObject<WeaponConfig>(File.ReadAllText(configPath));
        }
    }
}

public class WeaponConfig
{
    public float KnockbackMultiply { get; set; } = 1.0f;

    public Dictionary<string, WeaponData> WeaponConfigs { get; set; } = new Dictionary<string, WeaponData>();

    public WeaponConfig()
    {
        WeaponConfigs = new Dictionary<string, WeaponData>(StringComparer.OrdinalIgnoreCase)
        {
            { "glock", new WeaponData("Glock", "weapon_glock", 1.0f) },
        };
    }
}

public class WeaponData
{
    public WeaponData(string weaponName, string weaponEntity, float knockback)
    {
        WeaponName = weaponName;
        WeaponEntity = weaponEntity;
        Knockback = knockback;
    }

    public string WeaponName { get; set; }
    public string WeaponEntity { get; set; }
    public float Knockback { get; set; }
}