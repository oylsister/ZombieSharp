namespace ZombieSharp.Helpers
{
    public interface IZombiePlayer
    {
        public Dictionary<int, ZombiePlayer> ZombiePlayers { get; }
        public bool IsClientHuman(CCSPlayerController controller);
        public bool IsClientZombie(CCSPlayerController controller);
    }
}
