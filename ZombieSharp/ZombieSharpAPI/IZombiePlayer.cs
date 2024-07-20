using CounterStrikeSharp.API.Core;

namespace ZombieSharp.ZombieSharpAPI
{
    public interface IZombiePlayer
    {
        public bool IsClientHuman(CCSPlayerController controller);
        public bool IsClientZombie(CCSPlayerController controller);
    }
}
