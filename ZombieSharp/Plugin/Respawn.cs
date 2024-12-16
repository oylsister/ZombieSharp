using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace ZombieSharp.Plugin;

public class Respawn(ZombieSharp core, ILogger<ZombieSharp> logger)
{
    private readonly ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;

    public void RespawnOnPlayerDeath(CCSPlayerController? client)
    {
        if(client == null || !client.PawnIsAlive)
        {
            _logger.LogCritical("[RespawnOnPlayerDeath] client {0} is null or not alive", client?.PlayerName ?? "Unnamed");
            return;
        }

        // if not enable then no respawn.
        if(!GameSettings.Settings?.RespawnEnable ?? true)
            return;
        
        _core.AddTimer(GameSettings.Settings?.RespawnDelay ?? 5.0f, () => RespawnClient(client));
    }

    public void RespawnClient(CCSPlayerController? client)
    {
        if(client == null || client.PawnIsAlive)
            return;

        if(!GameSettings.Settings?.RespawnEnable ?? true)
            return;

        client.Respawn();
    }
}