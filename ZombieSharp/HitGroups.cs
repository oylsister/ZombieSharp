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
        public HitGroupConfig HitGroupDatas { get; private set; }

        public bool HitGroupIntialize()
        {
            string configPath = Path.Combine(ModuleDirectory, "hitgroups.json");

            if (File.Exists(configPath))
            {
                HitGroupDatas = JsonSerializer.Deserialize<HitGroupConfig>(File.ReadAllText(configPath));
                return true;
            }

            Logger.LogError("[Z:Sharp] Couldn't find hitgroup configs file.");
            return false;
        }

        public float HitGroupGetKnockback(int hitgroup)
        {
            if(!hitgroupLoad)
                return 1.0f;

            foreach (var data in HitGroupDatas.HitGroupConfigs)
            {
                if(data.Value.HitgroupIndex == hitgroup)
                    return data.Value.HitgroupKnockback;
            }

            return 1.0f;
        }
    }
}

public class HitGroupConfig
{
    public Dictionary<string, HitGroupData> HitGroupConfigs { get; set; } = new Dictionary<string, HitGroupData>(StringComparer.OrdinalIgnoreCase);
}

public class HitGroupData
{
    public int HitgroupIndex { get; set; }
    public float HitgroupKnockback { get; set; }
}
