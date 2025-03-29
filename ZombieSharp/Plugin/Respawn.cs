using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
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
        _core.AddCommand("zs_respawn", "Toggle Respawn Command", ToggleRespawnCommand);
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

    [RequiresPermissions("@css/slay")]
    [CommandHelper(1, "zs_respawn <0-1>")]
    public void ToggleRespawnCommand(CCSPlayerController? client, CommandInfo info)
    {
        var result = int.TryParse(info.GetArg(1), out var value);

        if(!result || value > 1 || value < 0)
        {
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} Usage: zs_respawn <0-1>");
            return;
        }

        // if allow respawn is false, then we force all player respawn.
        if(!GameSettings.Settings?.RespawnEnable ?? false)
        {
            _core.AddTimer(1.0f, () => {
                foreach(var player in Utilities.GetPlayers())
                {
                    if(player == null)
                        continue;

                    if(Utils.IsPlayerAlive(player))
                        continue;

                    if(player.Team == CsTeam.None || player.Team == CsTeam.Spectator)
                        continue;

                    RespawnClient(player);
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        ToggleRespawn(Convert.ToBoolean(value));
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

        if(RoundEnd.RoundEnded)
            return;

        client.Respawn();
    }

    public static void SpawnPlayer(CCSPlayerController client)
    {
        if(client == null || !client.IsValid)
            return;

        // no hltv spawn here.
        if(client.IsHLTV)
            return;

        Utils.ChangeTeam(client, Infect.InfectHasStarted() ? 2 : 3);
        var respawn = new CounterStrikeSharp.API.Modules.Timers.Timer(GameSettings.Settings?.RespawnDelay ?? 2.0f, () => RespawnClient(client), TimerFlags.STOP_ON_MAPCHANGE);
    }
}