using System.Text.Json;

namespace ZombieSharp
{
    public class WeaponModule : IWeaponModule
    {
        private ZombieSharp _Core;

        public WeaponModule(ZombieSharp plugin)
        {
            _Core = plugin;
        }

        public WeaponConfig WeaponDatas { get; private set; }

        public void Initialize()
        {
            var configPath = Path.Combine(_Core.ModulePath, "zs/weapons.json");

            if(!File.Exists(configPath))
            {
                CreateConfigFile(configPath);
            }

            WeaponDatas = JsonSerializer.Deserialize<WeaponConfig>(File.ReadAllText(configPath));
        }

        private void CreateConfigFile(string configPath) 
        {
            WeaponDatas = new WeaponConfig();

            File.WriteAllText(configPath, 
                JsonSerializer.Serialize(WeaponDatas, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}

public class WeaponConfig
{
    public Dictionary<string, WeaponData>WeaponConfigs = new Dictionary<string, WeaponData>();

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