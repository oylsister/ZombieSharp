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

    public static Dictionary<CCSPlayerController, float> RepeatKillerList = [];
    public static void OnPlayerDeath(CCSPlayerController? client, string weapon)
    {
        if(client == null)
            return;

        if(weapon != "trigger_hurt")
            return;

        var time = Server.CurrentTime;

        if(GameSettings.Settings == null)
            return;

        if(!GameSettings.Settings.RespawnEnable)
            return;

        if(!RepeatKillerList.ContainsKey(client))
            RepeatKillerList.Add(client, 0);

        if(time - RepeatKillerList[client] - GameSettings.Settings.RespawnDelay < 3.0)
        {
            Server.PrintToChatAll($" {_core?.Localizer["Prefix"]} {_core?.Localizer["Core.RepeatKiller"]}");
            _respawn?.ToggleRespawn(false);
        }

        RepeatKillerList[client] = time;
    }
}