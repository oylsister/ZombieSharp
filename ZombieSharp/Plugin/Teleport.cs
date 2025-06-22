using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ZombieSharp.Extensions;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class Teleport(ZombieSharp core, ILogger<ZombieSharp> logger)
{
    private readonly ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;

    public void TeleportOnLoad()
    {
        _core.AddCommand("css_ztele", "ZTele Command", TeleportCommand);
    }

    public static List<SpawnPoint>? GetAllSpawnPoint()
    {
        List<SpawnPoint> SpawnPoints = [];

        var ctSpawn = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist");
        var tSpawn = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist");

        if(ctSpawn.Any())
        {
            foreach(var spawn in ctSpawn)
            {
                if(spawn == null || !spawn.IsValid)
                    continue;

                SpawnPoints.Add(spawn);
            }
        }

        if(tSpawn.Any())
        {
            foreach(var spawn in tSpawn)
            {
                if(spawn == null || !spawn.IsValid)
                    continue;
                    
                SpawnPoints.Add(spawn);
            }
        }

        if(SpawnPoints.Count <= 0)
            return null;

        return SpawnPoints;
    }

    public void TeleportOnPlayerSpawn(CCSPlayerController client)
    {
        if(client == null || !Utils.IsPlayerAlive(client))
            return;

        if(PlayerData.PlayerSpawnData == null)
        {
            _logger.LogCritical("[TeleportCommand] SpawnData is null!");
            return;
        }

        if(!PlayerData.PlayerSpawnData.TryGetValue(client, out var data))
        {
            _logger.LogCritical("[TeleportCommand] client {0} is not in PlayerSpawnData", client.PlayerName);
            return;
        }

        var pos = client.PlayerPawn.Value?.AbsOrigin ?? new(0, 0, 0);
        var angle = client.PlayerPawn.Value?.AbsRotation ?? new(0, 0, 0);

        // saving dev
        data.PlayerPosition = new(pos.X, pos.Y, pos.Z);
        data.PlayerAngle = new(angle.X, angle.Y, angle.Z);
    }

    [CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
    public void TeleportCommand(CCSPlayerController? client, CommandInfo info)
    {
        if(!GameSettings.Settings?.TeleportAllow ?? false)
        {
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.FeatureDisbled"]}");
            return;
        }

        if(client == null || !Utils.IsPlayerAlive(client))
        {
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.MustBeAlive"]}");
            return;
        }

        if(PlayerData.PlayerSpawnData == null)
        {
            _logger.LogCritical("[TeleportCommand] SpawnData is null!");
            return;
        }

        if(!PlayerData.PlayerSpawnData.ContainsKey(client))
        {
            _logger.LogCritical("[TeleportCommand] client {0} is not in PlayerSpawnData", client.PlayerName);
            return;
        }

        var pos = PlayerData.PlayerSpawnData[client].PlayerPosition;
        var angle = PlayerData.PlayerSpawnData[client].PlayerAngle;
        var timer = 5;

        info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Teleport.Countdown", timer]}");

        _core.AddTimer(timer, () => 
        {
            TeleportClientToSpawn(client, pos, angle);
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Teleport.Success", timer]}");
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    public static void TeleportClientToSpawn(CCSPlayerController client, Vector pos, QAngle angle)
    {
        if (client == null || !Utils.IsPlayerAlive(client))
            return;

        //client.PlayerPawn.Value?.Teleport(pos.ToVector_t(), angle);
        client.PlayerPawn.Value?.Teleport(position: pos.ToVector_t(), angles: angle.ToQAngle_t());
    }
}