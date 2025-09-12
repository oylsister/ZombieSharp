using CounterStrikeSharp.API.Core;

namespace ZombieSharpAPI;

public interface IZombieSharpAPI
{
    public event Func<CCSPlayerController, CCSPlayerController?, bool, bool, HookResult?>? OnClientInfect;
    public event Func<CCSPlayerController, bool, HookResult?>? OnClientHumanize;

    public void Hook_OnInfectClient(Func<CCSPlayerController, CCSPlayerController?, bool, bool, bool, HookResult> handler);

    public bool ZS_IsClientHuman(CCSPlayerController client);
    public bool ZS_IsClientZombie(CCSPlayerController controller);
    public void ZS_RespawnClient(CCSPlayerController client);
}
