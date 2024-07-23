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

        public event Action<CCSPlayerController, CCSPlayerController, bool, bool, bool> ZS_OnInfectClient;
        public event Action<CCSPlayerController, bool> ZS_OnHumanizeClient;

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

        public void OnInfectClient(CCSPlayerController client, CCSPlayerController attacker, bool motherzombie, bool force, bool respawn)
        {
            ZS_OnInfectClient?.Invoke(client, attacker, motherzombie, force, respawn);
        }

        public void OnHumanizeClient(CCSPlayerController client, bool force)
        {
            ZS_OnHumanizeClient?.Invoke(client, force);
        }
    }
}
