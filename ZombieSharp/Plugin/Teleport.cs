using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
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

    public void TeleportOnPlayerSpawn(CCSPlayerController client)
    {
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

        // saving dev
        data.PlayerPosition = client.PlayerPawn.Value?.AbsOrigin ?? Vector.Zero;
        data.PlayerAngle = client.PlayerPawn.Value?.AbsRotation ?? QAngle.Zero;
    }

    [CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
    public void TeleportCommand(CCSPlayerController? client, CommandInfo info)
    {
        if(client == null || !client.PawnIsAlive)
        {
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.MustBeAlive"]}");
            return;
        }

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

        var pos = data.PlayerPosition;
        var angle = data.PlayerAngle;
        var timer = 5;

        info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Teleport.Countdown", timer]}");

        _core.AddTimer(timer, () => 
        {
            client.PlayerPawn.Value?.Teleport(pos, angle);
        });
    }
}