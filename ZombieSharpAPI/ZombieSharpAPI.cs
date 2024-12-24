using CounterStrikeSharp.API.Core;

namespace ZombieSharpAPI;

public interface IZombieSharpAPI
{
    public event Func<CCSPlayerController, CCSPlayerController?, bool, bool, HookResult?>? OnClientInfect;
    public event Func<CCSPlayerController, bool, HookResult?>? OnClientHumanize;

    public HookResult? ZS_OnClientInfect(CCSPlayerController client, CCSPlayerController attacker, bool motherzombie, bool force);
    public HookResult? ZS_OnClientHumanize(CCSPlayerController client, bool force);
    public bool ZS_IsClientHuman(CCSPlayerController client);
    public bool ZS_IsClientInfect(CCSPlayerController client);
}
