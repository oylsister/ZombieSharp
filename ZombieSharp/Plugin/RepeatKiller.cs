using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace ZombieSharp.Plugin;

public class RepeatKiller
{
    private static ZombieSharp? _core;
    private static Respawn? _respawn;

    public RepeatKiller(ZombieSharp core, Respawn respawn)
    {
        _core = core;
        _respawn = respawn;
    }

    private static Dictionary<CCSPlayerController, float> _repeatKiller = [];
    public static void OnPlayerDeath(CCSPlayerController client, string weapon)
    {
        if(weapon != "trigger_hurt")
            return;

        var time = Server.CurrentTime;

        if(GameSettings.Settings == null)
            return;

        if(!_repeatKiller.ContainsKey(client))
            _repeatKiller.Add(client, 0);

        if(time - _repeatKiller[client] - GameSettings.Settings.RespawnDelay < 3.0)
        {
            Server.PrintToChatAll($" {_core?.Localizer["Prefix"]} {_core?.Localizer["Core.RepeatKiller"]}");
            _respawn?.ToggleRespawn(false);
        }

        _repeatKiller[client] = time;
    }
}