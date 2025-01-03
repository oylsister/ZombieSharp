using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace ZombieSharp.Plugin;

public class Respawn(ZombieSharp core, ILogger<ZombieSharp> logger)
{
    private readonly ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;

    public void RespawnOnLoad()
    {
        _core.AddCommand("css_zspawn", "Zspawn command obviously", ZSpawnCommand);
    }

    [CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
    public void ZSpawnCommand(CCSPlayerController? client, CommandInfo info)
    {
        if(client == null)
            return;

        if(client.Team != CsTeam.Terrorist && client.Team != CsTeam.CounterTerrorist)
        {
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.MustBeInTeam"]}");
            return;
        }

        if(Utils.IsPlayerAlive(client))
        {
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.MustBeDead"]}");
            return;
        }

        if(!GameSettings.Settings?.RespawnEnable ?? true)
        {
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Respawn.Disabled"]}");
            return;
        }

        RespawnClient(client);
    }

    public void RespawnOnPlayerDeath(CCSPlayerController? client)
    {
        if(client == null || !Utils.IsPlayerAlive(client))
        {
            _logger.LogCritical("[RespawnOnPlayerDeath] client {0} is null or not alive", client?.PlayerName ?? "Unnamed");
            return;
        }

        // if not enable then no respawn.
        if(!GameSettings.Settings?.RespawnEnable ?? true)
            return;
        
        _core.AddTimer(GameSettings.Settings?.RespawnDelay ?? 5.0f, () => RespawnClient(client));
    }

    public static void RespawnClient(CCSPlayerController? client)
    {
        if(client == null || client.Handle == IntPtr.Zero)
            return;

        if(Utils.IsPlayerAlive(client))
            return;

        if(client.Team == CsTeam.Spectator || client.Team == CsTeam.None)
            return;

        if(!GameSettings.Settings?.RespawnEnable ?? true)
            return;

        client.Respawn();
    }
}