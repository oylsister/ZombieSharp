namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public bool RespawnEnable { get; set; } = true;

        public CHandle<CLogicRelay> RespawnRelay = null;

        public Dictionary<int, bool> ClientProtected = new();

        public void RespawnTogglerSetup()
        {
            if (RespawnRelay != null)
                RespawnRelay = null;

            CLogicRelay relay = Utilities.CreateEntityByName<CLogicRelay>("logic_relay");

            relay.Entity.Name = "zr_toggle_respawn";
            CEntityIdentity_SetEntityName(relay.Entity, "zr_toggle_respawn");
            relay.DispatchSpawn();

            RespawnRelay = new CHandle<CLogicRelay>(relay.Handle);
        }

        public void ToggleRespawn(bool force = false, bool value = false)
        {
            if ((!force && !RespawnEnable) || (force && value))
            {
                //ForceRespawnAllDeath();
                Server.PrintToChatAll($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Respawn has been enabled");
                RespawnEnable = true;
            }
            else
            {
                RespawnEnable = false;
                Server.PrintToChatAll($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Respawn has been disabled");
                //CheckGameStatus();
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
