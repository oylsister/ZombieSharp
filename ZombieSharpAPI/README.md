# ZombieSharpAPI

This API is provided for using with [ZombieSharp](https://github.com/oylsister/ZombieSharp)

### API Example 
Check out [ZombieTest](https://github.com/oylsister/ZombieSharp/blob/main/ZombieTest/ZombieTest.cs) for other API usages example. 
```cs
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using ZombieSharpAPI;

namespace ZombieTest
{
    public class ZombieTest : BasePlugin    
    {
        public override string ModuleName => "Zombie Test";
        public override string ModuleVersion => "1.0";

        // Declare Capability First.
        public static PluginCapability<IZombieSharpAPI> ZombieCapability { get; } = new("zombiesharp");

        // Declare API class
        IZombieSharpAPI? API;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            // Get Capability.
            API = ZombieCapability.Get()!;

            // Excute Hook function 
            API.Hook_OnInfectClient(ZS_OnInfectClient);
        }

        // Hook function is here.
        public HookResult ZS_OnInfectClient(ref CCSPlayerController client, ref CCSPlayerController attacker, ref bool motherzombie, ref bool force, ref bool respawn)
        {
            // check which client is infect.
            Server.PrintToChatAll($"{client.PlayerName} is infected");

            // if client name is Oylsister
            if (client.PlayerName == "Oylsister")
            {
                Server.PrintToChatAll("Oylsister is immunity");

                // Blocking infected
                return HookResult.Handled;
            }
            
            if (force)
                Server.PrintToChatAll($"by forcing.");

            // Always use HookResult.Continue to allowing other player get infect as usual.
            return HookResult.Continue;
        }
    }
}
```
