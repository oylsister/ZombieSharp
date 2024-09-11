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

        private List<OnInfectClient> _infectPreHandler = new();
        private List<OnHumanizeClient> _humanizePreHandler = new();

        public void Hook_OnInfectClient(OnInfectClient handler)
        {
            _infectPreHandler.Add(handler);
        }

        public void Unhook_OnInfectClient(OnInfectClient handler)
        {
            _infectPreHandler.Remove(handler);
        }

        public int APIOnInfectClient(ref CCSPlayerController client, ref CCSPlayerController attacker, ref bool motherzombie, ref bool force, ref bool respawn)
        {
            foreach (var handler in _infectPreHandler)
            {
                var clientCopy = client;
                var attackerCopy = attacker;
                var mothercopy = motherzombie;
                var forcecopy = force;
                var respawncopy = respawn;

                HookResult hookResult = handler.Invoke(ref client, ref attacker, ref motherzombie, ref force, ref respawn);

                if (hookResult == HookResult.Stop || hookResult == HookResult.Handled)
                {
                    return -1;
                }

                else if (hookResult == HookResult.Continue)
                {
                    client = clientCopy;
                    attacker = attackerCopy;
                    motherzombie = mothercopy;
                    force = forcecopy;
                    respawn = respawncopy;

                    return 1;
                }
            }

            return 0;
        }

        public void Hook_OnHumanizeClient(OnHumanizeClient handler)
        {
            _humanizePreHandler.Add(handler);
        }

        public void Unhook_OnHumanizeClient(OnHumanizeClient handler)
        {
            _humanizePreHandler.Remove(handler);
        }

        public int APIOnHumanizeClient(ref CCSPlayerController client, ref bool force)
        {
            foreach (var handler in _humanizePreHandler)
            {
                var clientCopy = client;
                var forcecopy = force;

                HookResult hookResult = handler.Invoke(ref client, ref force);

                if (hookResult == HookResult.Stop || hookResult == HookResult.Handled)
                {
                    return -1;
                }

                else if (hookResult == HookResult.Continue)
                {
                    client = clientCopy;
                    force = forcecopy;

                    return 1;
                }
            }

            return 0;
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

        public string ZS_GetClientActiveClass(CCSPlayerController controller)
        {
            if(controller == null)
                return null;

            return _plugin.ClientPlayerClass[controller.Slot].ActiveClass;
        }

        public string ZS_GetClientZombieClass(CCSPlayerController controller)
        {
            if (controller == null)
                return null;

            return _plugin.ClientPlayerClass[controller.Slot].ZombieClass;
        }

        public string ZS_GetClientHumanClass(CCSPlayerController controller)
        {
            if (controller == null)
                return null;

            return _plugin.ClientPlayerClass[controller.Slot].HumanClass;
        }

        public PlayerClassData ZS_GetClassByString(string str)
        {
            if(str == null)
                return null;

            return _plugin.PlayerClassDatas.PlayerClasses[str];
        }

        public Dictionary<string, PlayerClassData> ZS_GetClassData()
        {
            if (_plugin.PlayerClassDatas.PlayerClasses == null)
                return null;

            return _plugin.PlayerClassDatas.PlayerClasses;
        }

        public void ZS_SetClientClass(CCSPlayerController controller, string playerClassData)
        {
            if (playerClassData == null)
                return;

            if (!_plugin.PlayerClassDatas.PlayerClasses.ContainsKey(playerClassData))
                return;

            if (_plugin.PlayerClassDatas.PlayerClasses[playerClassData].Team == 0)
                _plugin.ClientPlayerClass[controller.Slot].ZombieClass = playerClassData;

            else if (_plugin.PlayerClassDatas.PlayerClasses[playerClassData].Team == 1)
                _plugin.ClientPlayerClass[controller.Slot].HumanClass = playerClassData;
        }
    }
}
