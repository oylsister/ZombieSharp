using Microsoft.Extensions.Logging;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public GameSettings ConfigSettings { get; private set; }

        public bool SettingsIntialize(string mapname)
        {
            // initial value first
            ConfigSettings = new GameSettings();

            // then convar command.
            ConVarInitial();

            var cfgPath = Path.Combine(ModuleDirectory, @"../../../../cfg");
            if (!Directory.Exists(cfgPath))
            {
                Logger.LogError($"Couldn't find {cfgPath} directory.");
                return false;
            }

            var zsharpDir = Path.Combine(cfgPath, "zombiesharp");
            Directory.CreateDirectory(zsharpDir);

            var zsharpCfg = Path.Combine(zsharpDir, "zombiesharp.cfg");

            if (!File.Exists(zsharpCfg))
            {
                CreateExecConfigFile(zsharpCfg);
                Server.ExecuteCommand("exec \"zombiesharp/zombiesharp.cfg\"");
            }

            return true;
        }

        void ConVarInitial()
        {
            // Infection section.
            AddCommand("zs_infect_spawntime", "First Infection Countdown", CVAR_InfectSpawnTime);
            AddCommand("zs_infect_mzombie_ratio", "MotherZombie Ratio", CVAR_InfectMotherZombieRatio);
            AddCommand("zs_infect_mzombie_min", "MotherZombie Minimum", CVAR_InfectMotherZombieMinimum);
            AddCommand("zs_infect_mzombie_respawn", "Teleport Mother Zombie Back", CVAR_InfectMotherZombieRespawn);
            AddCommand("zs_infect_enable_warmup", "Enable Gamemode in warmup", CVAR_InfectEnableWarmup);
            AddCommand("zs_infect_drop_mode", "Teleport Mother Zombie Back", CVAR_InfectWeaponDropMode);
            AddCommand("zs_infect_cash_damage", "Cash on damage", CVAR_InfectCashDamage);

            // Respawn Section
        }

        // Infection Section.
        [RequiresPermissions("@css/cvar")]
        private void CVAR_InfectSpawnTime(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_infect_spawntime Current Value: {ConfigSettings.FirstInfectionTimer}");
                return;
            }

            var value = float.Parse(info.GetArg(1));

            if (value < 0.0)
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_infect_spawntime <float>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_infect_spawntime changed value from \"{ConfigSettings.FirstInfectionTimer}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_infect_spawntime changed value from \"{ConfigSettings.FirstInfectionTimer}\" to \"{value}\".");
            ConfigSettings.FirstInfectionTimer = value;
        }

        [RequiresPermissions("@css/cvar")]
        private void CVAR_InfectMotherZombieRatio(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_infect_mzombie_ratio Current Value: {ConfigSettings.MotherZombieRatio}");
                return;
            }

            var value = float.Parse(info.GetArg(1));

            if (value < 0.0)
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_infect_mzombie_ratio <int>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_infect_mzombie_ratio changed value from \"{ConfigSettings.MotherZombieRatio}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_infect_mzombie_ratio changed value from \"{ConfigSettings.MotherZombieRatio}\" to \"{value}\".");
            ConfigSettings.MotherZombieRatio = value;
        }

        [RequiresPermissions("@css/cvar")]
        private void CVAR_InfectMotherZombieMinimum(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_infect_mzombie_min Current Value: {ConfigSettings.MotherZombieMinimum}");
                return;
            }

            var value = int.Parse(info.GetArg(1));

            if (value < 0)
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_infect_mzombie_min <int>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_infect_mzombie_min changed value from \"{ConfigSettings.MotherZombieMinimum}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_infect_mzombie_min changed value from \"{ConfigSettings.MotherZombieMinimum}\" to \"{value}\".");
            ConfigSettings.MotherZombieMinimum = value;
        }

        [RequiresPermissions("@css/cvar")]
        private void CVAR_InfectMotherZombieRespawn(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_infect_mzombie_respawn Current Value: {ConfigSettings.TeleportMotherZombie}");
                return;
            }

            var value = Convert.ToBoolean(int.Parse(info.GetArg(1)));

            if (typeof(bool) != value.GetType())
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_infect_mzombie_respawn <bool>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_infect_mzombie_respawn changed value from \"{ConfigSettings.TeleportMotherZombie}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_infect_mzombie_respawn changed value from \"{ConfigSettings.TeleportMotherZombie}\" to \"{value}\".");
            ConfigSettings.TeleportMotherZombie = value;
        }

        [RequiresPermissions("@css/cvar")]
        private void CVAR_InfectEnableWarmup(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_infect_enable_warmup Current Value: {ConfigSettings.EnableOnWarmup}");
                return;
            }

            var value = Convert.ToBoolean(int.Parse(info.GetArg(1)));

            if (typeof(bool) != value.GetType())
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_infect_enable_warmup <0-1>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_infect_enable_warmup changed value from \"{ConfigSettings.EnableOnWarmup}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_infect_enable_warmup changed value from \"{ConfigSettings.EnableOnWarmup}\" to \"{value}\".");
            ConfigSettings.EnableOnWarmup = value;
        }

        [RequiresPermissions("@css/cvar")]
        private void CVAR_InfectWeaponDropMode(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_infect_drop_mode Current Value: {ConfigSettings.ZombieDrop}");
                return;
            }

            var value = int.Parse(info.GetArg(1));

            if (value is < 0 or > 1)
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_infect_drop_mode <0-1>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_infect_drop_mode changed value from \"{ConfigSettings.ZombieDrop}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_infect_drop_mode changed value from \"{ConfigSettings.ZombieDrop}\" to \"{value}\".");
            ConfigSettings.ZombieDrop = value;
        }

        [RequiresPermissions("@css/cvar")]
        private void CVAR_InfectCashDamage(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_infect_cash_damage Current Value: {ConfigSettings.CashOnDamage}");
                return;
            }

            var value = Convert.ToBoolean(int.Parse(info.GetArg(1)));

            if (typeof(bool) != value.GetType())
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_infect_cash_damage <0-1>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_infect_cash_damage changed value from \"{ConfigSettings.CashOnDamage}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_infect_cash_damage changed value from \"{ConfigSettings.CashOnDamage}\" to \"{value}\".");
            ConfigSettings.CashOnDamage = value;
        }

        // Respawn Section
        [RequiresPermissions("@css/cvar")]
        private void CVAR_RespawnTime(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_respawn_timer Current Value: {ConfigSettings.RespawnTimer}");
                return;
            }

            var value = float.Parse(info.GetArg(1));

            if (value < 0.0)
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_respawn_timer <float>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_respawn_timer changed value from \"{ConfigSettings.RespawnTimer}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_respawn_timer changed value from \"{ConfigSettings.RespawnTimer}\" to \"{value}\".");
            ConfigSettings.RespawnTimer = value;
        }

        [RequiresPermissions("@css/cvar")]
        private void CVAR_RespawnAllowJoinLate(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_respawn_join_late Current Value: {ConfigSettings.Respawn_Late}");
                return;
            }

            var value = Convert.ToBoolean(int.Parse(info.GetArg(1)));

            if (typeof(bool) != value.GetType())
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_respawn_join_late <0-1>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_respawn_join_late changed value from \"{ConfigSettings.Respawn_Late}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_respawn_join_late changed value from \"{ConfigSettings.Respawn_Late}\" to \"{value}\".");
            ConfigSettings.Respawn_Late = value;
        }

        [RequiresPermissions("@css/cvar")]
        private void CVAR_RespawnTeam(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_respawn_team Current Value: {ConfigSettings.Respawn_Team}");
                return;
            }

            var value = int.Parse(info.GetArg(1));

            if (value is < 0 or > 1)
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_respawn_team <0-1>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_respawn_team changed value from \"{ConfigSettings.Respawn_Team}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_respawn_team changed value from \"{ConfigSettings.Respawn_Team}\" to \"{value}\".");
            ConfigSettings.Respawn_Team = value;
        }

        [RequiresPermissions("@css/cvar")]
        private void CVAR_RespawnProtect(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount < 1)
            {
                info.ReplyToCommand($"[Z:Sharp] zs_respawn_protect Current Value: {ConfigSettings.Respawn_ProtectHuman}");
                return;
            }

            var value = Convert.ToBoolean(int.Parse(info.GetArg(1)));

            if (typeof(bool) != value.GetType())
            {
                info.ReplyToCommand($"[Z:Sharp] Usage: zs_respawn_protect <0-1>");
                return;
            }

            Server.PrintToChatAll($"[Z:Sharp] ConVar zs_respawn_protect changed value from \"{ConfigSettings.Respawn_ProtectHuman}\" to \"{value}\".");
            Logger.LogInformation($"[Z:Sharp] ConVar zs_respawn_protect changed value from \"{ConfigSettings.Respawn_ProtectHuman}\" to \"{value}\".");
            ConfigSettings.Respawn_ProtectHuman = value;
        }

        void CreateExecConfigFile(string path)
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

            execCfg.WriteLine("zs_classes_human_default \"human_default\"");
            execCfg.WriteLine("zs_classes_zombie_default \"zombie_default\"");
            execCfg.WriteLine("zs_classes_mother_default \"motherzombie\"");

            execCfg.WriteLine("zs_repeatkiller_threshold \"0.0\"");
            execCfg.WriteLine("zs_topdefender_enable \"1\"");
            execCfg.WriteLine("zs_timeout_winner \"3\"");
        }
    }
}

public class GameSettings
{
    public float RespawnTimer { get; set; } = 5.0f;
    public float FirstInfectionTimer { get; set; } = 15.0f;
    public float MotherZombieRatio { get; set; } = 7.0f;
    public int MotherZombieMinimum { get; set; } = 0;
    public bool TeleportMotherZombie { get; set; } = true;
    public bool EnableOnWarmup { get; set; } = false;
    public float RepeatKillerThreshold { get; set; } = 3.0f;
    public int ZombieDrop { get; set; } = 0; // 0 = stip , 1 = force drop
    public bool CashOnDamage { get; set; } = true;

    // Default Class
    public string Human_Default { get; set; } = "human_default";
    public string Zombie_Default { get; set; } = "zombie_default";
    public string Mother_Zombie { get; set; } = "motherzombie";

    // Respawn Protection
    public bool Respawn_Late { get; set; } = true;
    public int Respawn_Team { get; set; } = 0;
    public bool Respawn_ProtectHuman { get; set; } = false;
    public float Respawn_ProtectTime { get; set; } = 5.0f;
    public float Respawn_Speed { get; set; } = 600.0f;

    // top defender
    public bool EnableTopDefender { get; set; } = false;

    // Terminate Round Winner
    public int TimeoutWinner { get; set; } = 3;
}
