using CounterStrikeSharp.API.Core;

namespace ZombieSharpAPI;

public interface IZombieSharpAPI
{
    public event Func<CCSPlayerController, CCSPlayerController?, bool, bool, HookResult?>? OnClientInfect;
    public event Func<CCSPlayerController, bool, HookResult?>? OnClientHumanize;

    public bool ZS_IsClientHuman(CCSPlayerController client);
    public bool ZS_IsClientInfect(CCSPlayerController client);
    public void ZS_RespawnClient(CCSPlayerController client);
    public string? ZS_GetClientClassString(CCSPlayerController client, int team);
    public string? ZS_GetClassModel(string classname);
}
