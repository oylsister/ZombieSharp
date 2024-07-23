using CounterStrikeSharp.API.Core;

namespace ZombieSharpAPI
{
    public interface IZombieSharpAPI
    {
        public event Action<CCSPlayerController, CCSPlayerController, bool, bool, bool> ZS_OnInfectClient;
        public event Action<CCSPlayerController, bool> ZS_OnHumanizeClient;

        public bool ZS_IsClientHuman(CCSPlayerController controller);
        public bool ZS_IsClientZombie(CCSPlayerController controller);
        public void ZS_InfectClient(CCSPlayerController controller);
        public void ZS_HumanizeClient(CCSPlayerController controller);
    }
}
