using static ZombieSharp.ZombieSharp;

namespace ZombieSharp
{
    public class ZombiePlayer
    {
        public ZombiePlayer()
        {
            IsZombie = false;
            MotherZombieStatus = MotherZombieFlags.NONE;
        }

        public bool IsZombie { get; set; } = false;
        public MotherZombieFlags MotherZombieStatus { get; set; } = MotherZombieFlags.NONE;
    }
}
