﻿namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public bool RespawnEnable { get; set; } = true;

        public CHandle<CLogicRelay> RespawnRelay = null;

        public Dictionary<int, RespawnProtectData> ClientProtected = new();

        public void RespawnClient(CCSPlayerController client)
        {
            if (!client.IsValid || client.PawnIsAlive || client.TeamNum < 2)
                return;

            client.Respawn();
        }

        public void RespawnTogglerSetup()
        {
            CLogicRelay relay = Utilities.CreateEntityByName<CLogicRelay>("logic_relay");

            relay.Entity.Name = "zr_toggle_respawn";
            CEntityIdentity_SetEntityName(relay.Entity, "zr_toggle_respawn");
            relay.DispatchSpawn();

            RespawnRelay = new CHandle<CLogicRelay>(relay.Handle);
        }

        public void RespawnProtectClient(CCSPlayerController client, bool reset = false)
        {
            if (!client.IsValid || !client.PawnIsAlive)
                return;

            if (!reset)
            {
                ClientProtected[client.Slot].Velocity = client.PlayerPawn.Value.VelocityModifier;
                client.PlayerPawn.Value.VelocityModifier = CVAR_RespawnProtectSpeed.Value / 250.0f;
                client.PlayerPawn.Value.GravityScale = CVAR_RespawnProtectSpeed.Value / 250.0f;
            }

            else
            {
                client.PrintToChat($" {Localizer["Prefix"]} {Localizer["Respawn.EndProtect"]}");
                client.PlayerPawn.Value.VelocityModifier = ClientProtected[client.Slot].Velocity;
                client.PlayerPawn.Value.GravityScale = 1.0f;
            }
        }

        public void ToggleRespawn(bool force = false, bool value = false)
        {
            if ((!force && !RespawnEnable) || (force && value))
            {
                //ForceRespawnAllDeath();
                Server.PrintToChatAll($" {Localizer["Prefix"]} {Localizer["Respawn.Enabled"]}");
                RespawnEnable = true;
            }
            else
            {
                RespawnEnable = false;
                Server.PrintToChatAll($" {Localizer["Prefix"]} {Localizer["Respawn.Disabled"]}");
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

public class RespawnProtectData
{
    public bool Protected { get; set; } = false;
    public float Velocity { get; set; } = 1.0f;
}
