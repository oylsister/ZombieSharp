using CounterStrikeSharp.API.Core;

namespace ZombieSharpAPI
{
    public interface IZombieSharpAPI
    {
        delegate HookResult PreInfectClient(ref CCSPlayerController client, ref CCSPlayerController attacker, ref bool motherzombie, ref bool force, ref bool respawn);

        void AssignOnInfectClient(PreInfectClient handler);

        void ResignOnInfectClient(PreInfectClient handler);

        public bool ZS_IsClientHuman(CCSPlayerController controller);
        public bool ZS_IsClientZombie(CCSPlayerController controller);
        public void ZS_InfectClient(CCSPlayerController controller);
        public void ZS_HumanizeClient(CCSPlayerController controller);
    }
}
