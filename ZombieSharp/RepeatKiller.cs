﻿namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public bool RepeatKillerActivated { get; set; } = false;
        public bool RepeatKillerEnable { get; set; } = false;

        public Dictionary<int, float> PlayerDeathTime = new Dictionary<int, float>();

        public void RepeatKillerOnMapStart()
        {
            if (ConfigSettings.RepeatKillerThreshold > 0.0)
                RepeatKillerEnable = true;
        }

        public void RepeatKillerOnPlayerDeath(CCSPlayerController client, CCSPlayerController attacker, string weapon)
        {
            if (!RepeatKillerEnable)
                return;

            if (RepeatKillerActivated)
                return;

            if (client.IsValid && (attacker == null || !attacker.IsValid) && weapon == "trigger_hurt")
            {
                float GameTime = Server.CurrentTime;

                if ((GameTime - PlayerDeathTime[client.Slot] - ConfigSettings.RespawnTimer) < ConfigSettings.RepeatKillerThreshold)
                {
                    Server.PrintToChatAll($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Repeat Killer detected. Disabling respawn for this round");
                    RepeatKillerActivated = true;
                }

                PlayerDeathTime[client.Slot] = GameTime;
            }
        }

        public void ForceRespawnAllDeath()
        {
            foreach (var client in Utilities.GetPlayers())
            {
                if (client.IsValid && !client.PawnIsAlive && client.TeamNum > 1)
                    RespawnClient(client);
            }
        }
    }
}
