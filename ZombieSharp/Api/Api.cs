using CounterStrikeSharp.API.Core;
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

    public void Hook_OnInfectClient(Func<CCSPlayerController, CCSPlayerController?, bool, bool, bool, HookResult> handler)
    {
        OnClientInfect += (client, attacker, motherzombie, force) =>
        {
            return handler(client, attacker, motherzombie, force, false);
        };
    }

    public HookResult? ZS_OnClientHumanize(CCSPlayerController client, bool force)
    {
        return OnClientHumanize?.Invoke(client, force);
    }

    public bool ZS_IsClientHuman(CCSPlayerController client)
    {
        return Infect.IsClientHuman(client);
    }

    public bool ZS_IsClientZombie(CCSPlayerController controller)
    {
        return Infect.IsClientZombie(controller);
    }

    public void ZS_RespawnClient(CCSPlayerController client)
    {
        Respawn.RespawnClient(client);
    }
}