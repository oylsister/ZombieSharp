using CounterStrikeSharp.API.Core;

namespace ZombieSharpAPI
{
    public interface IZombieSharpAPI
    {
        public delegate void ZS_OnInfectClient(CCSPlayerController controller, CCSPlayerController attacker, bool motherzombie, bool force, bool respawn);

        public void ZS_HookInfectClient(ZS_OnInfectClient hook);

        public bool ZS_IsClientHuman(CCSPlayerController controller);
        public bool ZS_IsClientZombie(CCSPlayerController controller);
        public void ZS_InfectClient(CCSPlayerController controller);
        public void ZS_HumanizeClient(CCSPlayerController controller);
    }
}
