using CounterStrikeSharp.API.Core;

namespace ZombieSharpAPI
{
    public interface IZombieSharpAPI
    {
        // HookClientInfect
        delegate HookResult OnInfectClient(ref CCSPlayerController client, ref CCSPlayerController attacker, ref bool motherzombie, ref bool force, ref bool respawn);
        void Hook_OnInfectClient(OnInfectClient handler);
        void Unhook_OnInfectClient(OnInfectClient handler);

        delegate HookResult OnHumanizeClient(ref CCSPlayerController client, ref bool force);
        void Hook_OnHumanizeClient(OnHumanizeClient handler);
        void Unhook_OnHumanizeClient(OnHumanizeClient handler);

        public bool ZS_IsClientHuman(CCSPlayerController controller);
        public bool ZS_IsClientZombie(CCSPlayerController controller);
        public void ZS_InfectClient(CCSPlayerController controller);
        public void ZS_HumanizeClient(CCSPlayerController controller);
    }
}
