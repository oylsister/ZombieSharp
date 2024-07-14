using ZombieSharp.Helpers;
using static ZombieSharp.ZombieSharp;

namespace ZombieSharp
{
    public class ZombiePlayer : IZombiePlayer
    {
        public bool IsZombie { get; set; } = false;
        public MotherZombieFlags MotherZombieStatus { get; set; } = MotherZombieFlags.NONE;

        public Dictionary<int, ZombiePlayer> ZombiePlayers { get; set; } = new Dictionary<int, ZombiePlayer>();

        public bool IsClientZombie(CCSPlayerController controller)
        {
            if (controller.Slot == 32766)
                return false;

            return ZombiePlayers[controller.Slot].IsZombie;
        }

        public bool IsClientHuman(CCSPlayerController controller)
        {
            if (controller.Slot == 32766)
                return false;

            return !ZombiePlayers[controller.Slot].IsZombie;
        }
    }
}
