using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using Microsoft.Extensions.Logging;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public FakeConVar<float> CVAR_FirstInfectionTimer = new("zs_infect_spawntime", "Specify How long before first mother zombie will spawn after round freeze end.", 15.0f, ConVarFlags.FCVAR_NONE, new RangeValidator<float>(3.0f, 60.0f));
        public FakeConVar<float> CVAR_MotherZombieRatio = new("zs_infect_mzombie_ratio", "Mother Zombie Ratio", 7.0f, ConVarFlags.FCVAR_NONE, new RangeValidator<float>(1.0f, 64.0f));
        public FakeConVar<int> CVAR_MinimumMotherZombie = new("zs_infect_mzombie_min", "Minimum Mother Zombie to spawn", 1, ConVarFlags.FCVAR_NONE, new RangeValidator<int>(1, 64));
        public FakeConVar<bool> CVAR_TeleportMotherZombie = new("zs_infect_mzombie_respawn", "Teleport Mother Zombie Back to respawn.", true);
        public FakeConVar<bool> CVAR_EnableOnWarmup = new("zs_infect_enable_warmup", "Enable Infection during warmup, not recommend to enable as it possibly corrupt the memory", false);
        public FakeConVar<int> CVAR_ZombieDrop = new("zs_infect_drop_mode", "Weapon Drop Method for Zombie when get infected, [0 = Strip Weapon | 1 = Force Drop]", 0, ConVarFlags.FCVAR_NONE, new RangeValidator<int>(0, 1));
        public FakeConVar<bool> CVAR_CashOnDamage = new("zs_infect_cash_damage", "Allow player to earn money from damage zombie or not.", true);

        public FakeConVar<float> CVAR_RespawnTimer = new("zs_respawn_timer", "Respawn Delaying after player death, Set to 0 will disable it.", 5.0f, ConVarFlags.FCVAR_NONE, new RangeValidator<float>(0.0f, 60.0f));
        public FakeConVar<bool> CVAR_RespawnLate = new("zs_respawn_join_late", "Allow player to join game late", true);
        public FakeConVar<int> CVAR_RespawnTeam = new("zs_respawn_team", "Specify team to respawn after player death [0 = zombie | 1 = human]", 0, ConVarFlags.FCVAR_NONE, new RangeValidator<int>(0, 1));
        public FakeConVar<bool> CVAR_RespawnProtect = new("zs_respawn_protect", "Protect Human after respawn or not", false);
        public FakeConVar<float> CVAR_RespawnProtectTime = new("zs_respawn_protect_time", "Duration of human protection after they respawned.", 5.0f, ConVarFlags.FCVAR_NONE, new RangeValidator<float>(1.0f, 50.0f));
        public FakeConVar<float> CVAR_RespawnProtectSpeed = new("zs_respawn_protect_speed", "Human Speed during protection active.", 600.0f, ConVarFlags.FCVAR_NONE, new RangeValidator<float>(300.0f, 1200.0f));

        public FakeConVar<float> CVAR_RepeatKillerThreshold = new("zs_repeatkiller_threshold", "Death Ratio before disable spawning entirely in that round.", 0.0f, ConVarFlags.FCVAR_NONE, new RangeValidator<float>(0, 64));
        public FakeConVar<bool> CVAR_TopDefenderEnable = new("zs_topdefender_enable", "Enable Top defender or not", true);
        public FakeConVar<int> CVAR_TimeoutWinner = new("zs_timeout_winner", "Specify Which team should win if the round time is out.", 2, ConVarFlags.FCVAR_NONE, new RangeValidator<int>(2, 3));

        public void SettingsOnLoad()
        {
            RegisterFakeConVars(typeof(ConVar));
        }

        public bool SettingsIntialize(string mapname)
        {
            var configFolder = Path.Combine(Server.GameDirectory, "csgo/cfg/zombiesharp/");

            if (!Directory.Exists(configFolder))
            {
                Logger.LogError($"[Z:Sharp] Couldn't find directory {configFolder}");
                return false;
            }

            var configPath = Path.Combine(configFolder, "zombiesharp.cfg");

            if (!File.Exists(configPath))
            {
                CreateAutoExecCFG(configPath);
                Logger.LogInformation($"[Z:Sharp] Creating {configPath}");
            }

            Server.ExecuteCommand("exec zombiesharp/zombiesharp.cfg");

            var mapConfig = Path.Combine(configFolder, mapname + ".cfg");

            if (File.Exists(mapConfig))
            {
                Logger.LogInformation($"[Z:Sharp] Found Map cfg file loading {mapConfig}");
            }

            return true;
        }

        public void CreateAutoExecCFG(string path)
        {
            StreamWriter execCfg = File.CreateText(path);

            execCfg.WriteLine("zs_infect_spawntime \"15.0\"");
            execCfg.WriteLine("zs_infect_mzombie_ratio \"7.0\"");
            execCfg.WriteLine("zs_infect_mzombie_min \"1\"");
            execCfg.WriteLine("zs_infect_mzombie_respawn \"1\"");
            execCfg.WriteLine("zs_infect_enable_warmup \"0\"");
            execCfg.WriteLine("zs_infect_drop_mode \"0\"");
            execCfg.WriteLine("zs_infect_cash_damage \"1\"");

            execCfg.WriteLine("zs_respawn_timer \"5.0\"");
            execCfg.WriteLine("zs_respawn_join_late \"1\"");
            execCfg.WriteLine("zs_respawn_team \"0\"");
            execCfg.WriteLine("zs_respawn_protect \"0\"");
            execCfg.WriteLine("zs_respawn_protect_time \"5.0\"");
            execCfg.WriteLine("zs_respawn_protect_speed \"600.0\"");

            execCfg.WriteLine("zs_repeatkiller_threshold \"0.0\"");
            execCfg.WriteLine("zs_topdefender_enable \"1\"");
            execCfg.WriteLine("zs_timeout_winner \"3\"");

            execCfg.Close();
        }
    }
}
