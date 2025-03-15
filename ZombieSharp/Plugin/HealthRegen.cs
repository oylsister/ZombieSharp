using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class HealthRegen
{
    private static ZombieSharp? _core;
    private static ILogger<ZombieSharp>? _logger;

    public HealthRegen(ZombieSharp core, ILogger<ZombieSharp> logger)
    {
        _core = core;
        _logger = logger;
    }

    public static void RegenOnClientDisconnect(CCSPlayerController client)
    {
        // if player data is null
        if (PlayerData.PlayerRegenData == null)
        {
            _logger?.LogError("[RegenOnClientDisconnect] PlayerRegenData is null");
            return;
        }

        // we stop the timer first.
        RegenKillTimer(client);

        // remove it.
        PlayerData.PlayerRegenData.Remove(client);
    }

    public static void RegenOnPlayerDeath(CCSPlayerController client)
    {
        // we stop the timer first.
        RegenKillTimer(client);
    }

    public static void RegenOnApplyClass(CCSPlayerController client, ClassAttribute classData)
    {
        // we stop the timer first.
        RegenKillTimer(client);

        // we start the timer.
        if(classData == null)
        {
            _logger?.LogError("[RegenOnPlayerSpawn] ClassAttribute is null");
            return;
        }

        if(classData.Regen_Interval <= 0 || classData.Regen_Amount <= 0)
        {
            return;
        }

        // if player data is null
        if (PlayerData.PlayerRegenData == null)
        {
            _logger?.LogError("[RegenKillTimer] PlayerRegenData is null");
            return;
        }

        // search for the player in the dictionary
        if (!PlayerData.PlayerRegenData.ContainsKey(client))
        {
            _logger?.LogInformation("[RegenKillTimer] Player {name} not found in PlayerRegenData, but add a new one anyway.", client.PlayerName);
            PlayerData.PlayerRegenData.Add(client, null);
        }

        PlayerData.PlayerRegenData[client] = _core?.AddTimer(classData.Regen_Interval, () =>
        {
            if(client == null)
            {
                _logger?.LogError("[RegenOnPlayerSpawn] CCSPlayerController is null");
                return;
            }

            if(!Utils.IsPlayerAlive(client))
            {
                RegenKillTimer(client);
                return;
            }   

            var playerPawn = client.PlayerPawn.Value;

            if(playerPawn == null)
            {
                _logger?.LogError("[RegenOnPlayerSpawn] PlayerPawn is null");
                RegenKillTimer(client);
                return;
            }

            // if client health is lower than max health and health plus with regen amount is greater than class health then we set the health to class health.
            if(playerPawn.Health < classData.Health && (playerPawn.Health + classData.Regen_Amount >= classData.Health))
            {
                Server.NextWorldUpdate(() => {
                    playerPawn.Health = classData.Health;
                    Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
                });
                return;
            }

            if(playerPawn.Health > classData.Health)
            {
                // just return if the health is greater than class health.
                return;
            }

            // else we add the regen amount to the health.
            Server.NextWorldUpdate(() => {
                playerPawn.Health += classData.Regen_Amount;
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
            });
            
        }, TimerFlags.REPEAT|TimerFlags.STOP_ON_MAPCHANGE);
    }

    public static void RegenKillTimer(CCSPlayerController client)
    {
        // if player data is null
        if (PlayerData.PlayerRegenData == null)
        {
            _logger?.LogError("[RegenKillTimer] PlayerRegenData is null");
            return;
        }

        // search for the player in the dictionary
        if (!PlayerData.PlayerRegenData.ContainsKey(client))
        {
            _logger?.LogError("[RegenKillTimer] Player not found in PlayerRegenData");
            return;
        }

        // we stop the timer first. 
        if(PlayerData.PlayerRegenData[client] != null)
        {
            PlayerData.PlayerRegenData[client]?.Kill();
            PlayerData.PlayerRegenData[client] = null;
        }
    }
}