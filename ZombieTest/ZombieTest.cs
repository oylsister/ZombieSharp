using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using ZombieSharpAPI;

namespace ZombieTest
{
    public class ZombieTest : BasePlugin    
    {
        public override string ModuleName => "Zombie Test";
        public override string ModuleVersion => "1.0";
        public static PluginCapability<IZombieSharpAPI> ZombieCapability { get; } = new("zombiesharp");

        IZombieSharpAPI? API;

        public override void Load(bool hotReload)
        {
            AddCommand("css_ishuman", "", Command_CheckHuman);
            AddCommand("css_imzombie", "", Command_Zombie);
            AddCommand("css_imhuman", "", Command_Human);
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            API = ZombieCapability.Get()!;

            API.Hook_OnInfectClient(ZS_OnInfectClient);
        }

        public void ZS_OnInfectClient(CCSPlayerController client, CCSPlayerController attacker, bool motherzombie, bool force, bool respawn)
        {
            Server.PrintToChatAll($"{client.PlayerName} is infected");

            if(attacker != null)
                Server.PrintToChatAll($"by {attacker.PlayerName}");

            if(motherzombie)
                Server.PrintToChatAll($"by MotherZombie Cycle");

            if (force)
                Server.PrintToChatAll($"by forcing.");
        }

        public HookResult ZS_OnInfectClient(ref CCSPlayerController client, ref CCSPlayerController attacker, ref bool motherzombie, ref bool force, ref bool respawn)
        {
            Server.PrintToChatAll($"{client.PlayerName} is infected");

            if (client.PlayerName == "Oylsister")
            {
                Server.PrintToChatAll("Oylsister is immunity");
                return HookResult.Handled;
            }
            
            if (force)
                Server.PrintToChatAll($"by forcing.");

            return HookResult.Continue;
        }

        private void Command_CheckHuman(CCSPlayerController? controller, CommandInfo info)
        {
            if (API == null)
            {
                info.ReplyToCommand("API is null gfys");
                return;
            }

            foreach (var player in Utilities.GetPlayers())
            {
                if (API.ZS_IsClientZombie(player))
                    info.ReplyToCommand($"[ZSharp] {player.PlayerName} is Zombie");

                else if(API.ZS_IsClientHuman(player))
                    info.ReplyToCommand($"[ZSharp] {player.PlayerName} is Human");
            }
        }

        private void Command_Human(CCSPlayerController? controller, CommandInfo info)
        {
            if (controller == null || !controller.PawnIsAlive)
                return;

            if (API == null)
            {
                info.ReplyToCommand("API is null gfys");
                return;
            }

            if (API.ZS_IsClientHuman(controller))
            {
                info.ReplyToCommand("You're already human");
                return;
            }

            API.ZS_HumanizeClient(controller);
        }

        private void Command_Zombie(CCSPlayerController? controller, CommandInfo info)
        {
            if (controller == null || !controller.PawnIsAlive)
                return;

            if (API == null)
            {
                info.ReplyToCommand("API is null gfys");
                return;
            }

            if (API.ZS_IsClientHuman(controller))
            {
                info.ReplyToCommand("You're already zombie");
                return;
            }

            API.ZS_InfectClient(controller);
        }
    }
}
