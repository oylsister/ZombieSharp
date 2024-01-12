using CounterStrikeSharp.API.Core.Logging;
using CounterStrikeSharp.API.Modules.Entities.Constants;
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

        public string FindWeaponItemDefinition(CHandle<CBasePlayerWeapon> weapon, string weaponstring)
        {
            var item = (ItemDefinition)weapon.Value.AttributeManager.Item.ItemDefinitionIndex;

            if (weaponstring == "m4a1")
            {
                switch (item)
                {
                    case ItemDefinition.M4A1_S: return "m4a1_silencer";
                    case ItemDefinition.M4A4: return "m4a1";
                }
            }

            else if (weaponstring == "hkp2000")
            {
                switch (item)
                {
                    case ItemDefinition.P2000: return "hkp2000";
                    case ItemDefinition.USP_S: return "usp_silencer";
                }
            }

            else if (weaponstring == "mp7")
            {
                switch (item)
                {
                    case ItemDefinition.MP7: return "mp7";
                    case ItemDefinition.MP5_SD: return "mp5sd";
                }
            }

            return weaponstring;
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