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

        public void ZS_InfectClient(CCSPlayerController controller)
        {
            _plugin.InfectClient(controller, null, false, true);
        }

        public void ZS_HumanizeClient(CCSPlayerController controller)
        {
            _plugin.HumanizeClient(controller, true);
        }
    }
}
