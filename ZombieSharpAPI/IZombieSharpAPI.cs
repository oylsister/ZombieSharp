using CounterStrikeSharp.API.Core;

namespace ZombieSharpAPI
{
    public interface IZombieSharpAPI
    {
        // HookClientInfect
        delegate HookResult OnInfectClient(ref CCSPlayerController client, ref CCSPlayerController attacker, ref bool motherzombie, ref bool force, ref bool respawn);
        void Hook_OnInfectClient(OnInfectClient handler);
        void Unhook_OnInfectClient(OnInfectClient handler);

        delegate HookResult OnHumanizeClient(ref CCSPlayerController client, ref bool force);
        void Hook_OnHumanizeClient(OnHumanizeClient handler);
        void Unhook_OnHumanizeClient(OnHumanizeClient handler);

        public bool ZS_IsClientHuman(CCSPlayerController controller);
        public bool ZS_IsClientZombie(CCSPlayerController controller);
        public void ZS_InfectClient(CCSPlayerController controller);
        public void ZS_HumanizeClient(CCSPlayerController controller);

        public string ZS_GetClientActiveClass(CCSPlayerController controller);
        public string ZS_GetClientZombieClass(CCSPlayerController controller);
        public string ZS_GetClientHumanClass(CCSPlayerController controller);
        public PlayerClassData ZS_GetClassByString(string str);
        public Dictionary<string, PlayerClassData> ZS_GetClassData();
        public void ZS_SetClientClass(CCSPlayerController controller, string playerClassData);

        public class PlayerClassData
        {
            public PlayerClassData(string name, string desc, bool enable, bool default_class, int team, string model, bool motherzombie, int hp, float regen_interval, int regen_amount, float napalm_time, float speed, float knockback, float jump_height, float jump_distance)
            {
                Name = name;
                Description = desc;
                Enable = enable;
                Default_Class = default_class;
                Team = team;
                Model = model;
                MotherZombie = motherzombie;
                Health = hp;
                Regen_Interval = regen_interval;
                Regen_Amount = regen_amount;
                Napalm_Time = napalm_time;
                Speed = speed;
                Knockback = knockback;
                Jump_Height = jump_height;
                Jump_Distance = jump_distance;
            }

            public string Name { get; set; }
            public string Description { get; set; }
            public bool Enable { get; set; }
            public bool Default_Class { get; set; }
            public int Team { get; set; }
            public string Model { get; set; }

            public bool MotherZombie { get; set; }

            public int Health { get; set; }
            public float Regen_Interval { get; set; }
            public int Regen_Amount { get; set; }
            public float Napalm_Time { get; set; } = 0f;

            public float Speed { get; set; }
            public float Knockback { get; set; }
            public float Jump_Height { get; set; }
            public float Jump_Distance { get; set; }
        }
    }
}
