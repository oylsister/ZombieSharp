using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using ZombieSharp.ZombieSharpAPI;

namespace ZombieTest
{
    public class ZombieTest : BasePlugin
    {
        public override string ModuleName => "Zombie Test";
        public override string ModuleVersion => "1.0";
        public override string ModuleAuthor => "Oylsister";

        public static PluginCapability<IZombiePlayer> ZombiePlayerAPI { get; } = new("zombiesharp:zombieplayer");

        public override void Load(bool hotReload)
        {
            AddCommand("css_ishuman", "Command Check Human", Command_HumanCheck);
        }

        private void Command_HumanCheck(CCSPlayerController? client, CommandInfo info)
        {
            var Zombie_Player = ZombiePlayerAPI.Get();

            if (info.ArgCount <= 1)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Usage: css_zs_infect [<playername>].");
                return;
            }

            var targets = info.GetArgTargetResult(1);

            if (targets.Players.Count <= 0)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Couldn't find any client contain with that name.");
                return;
            }

            foreach (CCSPlayerController target in targets.Players)
            {
                if (!target.IsValid)
                    continue;

                if(Zombie_Player!.IsClientHuman(client!))
                    info.ReplyToCommand($"{target.PlayerName} is human.");

                else 
                    info.ReplyToCommand($"{target.PlayerName} is not human.");
            }
        }
    }
}
