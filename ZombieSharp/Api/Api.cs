using CounterStrikeSharp.API.Core;
using ZombieSharp.Models;
using ZombieSharp.Plugin;
using ZombieSharpAPI;

namespace ZombieSharp.Api;

public class ZombieSharpInterface : IZombieSharpAPI
{
    public event Func<CCSPlayerController, CCSPlayerController?, bool, bool, HookResult?>? OnClientInfect;
    public event Func<CCSPlayerController, bool, HookResult?>? OnClientHumanize;

    public HookResult? ZS_OnClientInfect(CCSPlayerController client, CCSPlayerController? attacker, bool motherzombie, bool force)
    {
        return OnClientInfect?.Invoke(client, attacker, motherzombie, force);
    }

    public HookResult? ZS_OnClientHumanize(CCSPlayerController client, bool force)
    {
        return OnClientHumanize?.Invoke(client, force);
    }

    public bool ZS_IsClientHuman(CCSPlayerController client)
    {
        return Infect.IsClientHuman(client);
    }

    public bool ZS_IsClientInfect(CCSPlayerController client)
    {
        return Infect.IsClientInfect(client);
    }

    public void ZS_RespawnClient(CCSPlayerController client)
    {
        Respawn.RespawnClient(client);
    }

    public string? ZS_GetClientClassString(CCSPlayerController client, int team)
    {
        return Classes.ClassesConfig?.Where(p => p.Value == PlayerData.PlayerClassesData?[client].HumanClass).FirstOrDefault().Key;
    }

    public string? ZS_GetClassModel(string className)
    {
        return Classes.ClassesConfig?.Where(p => p.Key == className).FirstOrDefault().Value.Model;
    }
}