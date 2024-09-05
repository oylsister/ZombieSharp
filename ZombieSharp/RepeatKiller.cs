namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public bool RepeatKillerEnable { get; set; } = false;

        public Dictionary<int, float> PlayerDeathTime = new Dictionary<int, float>();

        public void RepeatKillerOnMapStart()
        {
            if (CVAR_RepeatKillerThreshold.Value > 0.0)
                RepeatKillerEnable = true;
        }

        public void RepeatKillerOnPlayerDeath(CCSPlayerController client, CCSPlayerController attacker, string weapon)
        {
            if (!RepeatKillerEnable)
                return;

            if (!RespawnEnable)
                return;

            if (client.IsValid && (attacker == null || !attacker.IsValid) && weapon == "trigger_hurt")
            {
                float GameTime = Server.CurrentTime;

                if ((GameTime - PlayerDeathTime[client.Slot] - CVAR_RespawnTimer.Value) < CVAR_RepeatKillerThreshold.Value)
                {
                    Server.PrintToChatAll($" {Localizer["Prefix"]} {Localizer["RepeatKiller"]}");
                    ToggleRespawn(true, false);
                }

                PlayerDeathTime[client.Slot] = GameTime;
            }
        }
    }
}
