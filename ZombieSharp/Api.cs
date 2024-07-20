using ZombieSharpAPI;

namespace ZombieSharp
{
    public class ZombieSharpAPI : IZombieSharpAPI
    {
        ZombieSharp _plugin;
        public ZombieSharpAPI(ZombieSharp plugin)
        {
            _plugin = plugin;
        }

        public bool ZS_IsClientHuman(CCSPlayerController controller)
        {
            return _plugin.IsClientHuman(controller);
        }

        public bool ZS_IsClientZombie(CCSPlayerController controller)
        {
            return _plugin.IsClientZombie(controller);
        }
    }
}
