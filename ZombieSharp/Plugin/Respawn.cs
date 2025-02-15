using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace ZombieSharp.Plugin;

public class Respawn(ZombieSharp core, ILogger<ZombieSharp> logger)
{
    private readonly ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;
    public static CHandle<CLogicRelay>? RespawnRelay = null;

    public void RespawnOnLoad()
    {
        _core.AddCommand("css_zspawn", "Zspawn command obviously", ZSpawnCommand);
    }

    public void ToggleRespawn(bool value = true)
    {
        if(GameSettings.Settings == null)
        {
            _logger.LogError("[ToggleRespawn] GameSettings is null!");
            return;
        }

        GameSettings.Settings.RespawnEnable = value;
    }

    public void RespawnTogglerSetup()
    {
        var relay = Utilities.CreateEntityByName<CLogicRelay>("logic_relay");

        if(relay == null || relay.Entity == null)
        {
            _logger.LogInformation("[RespawnTogglerSetup] Respawn Relay is null!");
            return;
        }

        relay.Entity.Name = "zr_toggle_respawn";
        Utils.SetEntityName(relay, "zr_toggle_respawn");
        relay.DispatchSpawn();

        RespawnRelay = new CHandle<CLogicRelay>(relay.Handle);
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
        if(client == null)
        {
            _logger.LogCritical("[RespawnOnPlayerDeath] client {0} is null or alive", client?.PlayerName ?? "Unnamed");
            return;
        }

        // if not enable then no respawn.
        if(!GameSettings.Settings?.RespawnEnable ?? true)
            return;
        
        _core.AddTimer(GameSettings.Settings?.RespawnDelay ?? 5.0f, () => RespawnClient(client), TimerFlags.STOP_ON_MAPCHANGE);
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