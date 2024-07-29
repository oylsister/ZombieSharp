using ZombieSharpAPI;
using static ZombieSharpAPI.IZombieSharpAPI;

namespace ZombieSharp
{
    public class ZombieSharpAPI : IZombieSharpAPI
    {
        ZombieSharp _plugin;
        public ZombieSharpAPI(ZombieSharp plugin)
        {
            _plugin = plugin;
        }

        private List<PreInfectClient> _infectPreHandler = new();

        public void AssignOnInfectClient(PreInfectClient handler)
        {
            _infectPreHandler.Add(handler);
        }

        public void ResignOnInfectClient(PreInfectClient handler)
        {
            _infectPreHandler.Remove(handler);
        }

        public void TriggerInfectPre(ref CCSPlayerController client, ref CCSPlayerController attacker, ref bool motherzombie, ref bool force, ref bool respawn)
        {
            foreach(var handler in _infectPreHandler)
            {
                var clientCopy = client;
                var attackerCopy = attacker;
                var mothercopy = motherzombie;
                var forcecopy = force;
                var respawncopy = respawn;

                HookResult hookResult = handler.Invoke(ref client, ref attacker, ref motherzombie, ref force, ref respawn);

                if(hookResult == HookResult.Stop)
                {
                    return;
                }

                else if(hookResult == HookResult.Continue)
                {
                    client = clientCopy;
                    attacker = attackerCopy;
                    motherzombie = mothercopy;
                    force = forcecopy;
                    respawn = respawncopy;
                }
            }
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
