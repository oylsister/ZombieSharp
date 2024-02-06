﻿using Microsoft.Extensions.Logging;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public GameSettings ConfigSettings { get; private set; }

        public bool SettingsIntialize(string mapname)
        {
            ConVarInitial();

            ConfigSettings = new GameSettings();

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
