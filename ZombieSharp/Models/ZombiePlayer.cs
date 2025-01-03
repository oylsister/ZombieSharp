namespace ZombieSharp.Models;

public class ZombiePlayer
{
    public enum MotherZombieStatus
    {
        NONE = 0,
        CHOSEN = 1,
        LAST = 2
    }

    public ZombiePlayer(MotherZombieStatus status = MotherZombieStatus.NONE, bool zombie = false)
    {
        MotherZombie = status;
        Zombie = zombie;
    }

    public MotherZombieStatus MotherZombie {  get; set; } = MotherZombieStatus.NONE;
    public bool Zombie { get; set; } = false;
}
