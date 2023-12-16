using System.Collections;
using CounterStrikeSharp.API.Modules.Commands.Targeting;

namespace ZombieSharp
{ 
    public partial class ZombieSharp 
    {
        public void CommandInitialize()
        {
            AddCommand("css_zs_infect", "Infect Client Command", InfectClientCommand);
            AddCommand("css_zs_human", "Humanize Client Command", HumanizeClientCommand);
            AddCommand("css_zs_ztele", "Teleport Client to spawn Command", ZTeleClientCommand);
            AddCommand("css_playerlist", "Player List Command", PlayerListCommand);
        }

        [RequiresPermissions(@"css/slay")]
        private void InfectClientCommand(CCSPlayerController client, CommandInfo info)
        {
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

                if (!target.PawnIsAlive)
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} target {target.PlayerName} is not alive.");

                    continue;
                }

                if (IsClientZombie(target))
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} target {target.PlayerName} is already zombie.");

                    continue;
                }

                InfectClient(target, null, false, true);

                if (targets.Players.Count < 2)
                    info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Successfully infected {target.PlayerName}");
            }

            info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Successfully infected group.");
        }

        [RequiresPermissions(@"css/slay")]
        private void HumanizeClientCommand(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount <= 1)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Usage: css_zs_human <playername>.");
                return;
            }

            var targets = info.GetArgTargetResult(1);

            if (targets.Players.Count <= 0)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Couldn't find any client contain with that name.");
                return;
            }

            foreach (var target in targets.Players)
            {
                if (!target.IsValid)
                    continue;

                if (!target.PawnIsAlive)
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} target {target.PlayerName} is not alive.");

                    continue;
                }

                if (IsClientHuman(target))
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} target {target.PlayerName} is already human.");

                    continue;
                }

                HumanizeClient(target, true);

                if (targets.Players.Count < 2)
                    info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Successfully humanized {target.PlayerName}");
            }
            info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Successfully humanized group.");
        }

        private void ZTeleClientCommand(CCSPlayerController client, CommandInfo info)
        {
            if (!client.IsValid)
                return;

            if (!client.PawnIsAlive)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} This feature requires that you are alive.");
                return;
            }

            client.PrintToCenter("You will be teleported back to spawn in 5 seconds.");

            AddTimer(5.0f, () => 
            {
                ZTele_TeleportClientToSpawn(client);
                info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Teleported back to spawn.");
                client.PrintToCenter("You have been teleported back to spawn.");
            });
        }

        private void PlayerListCommand(CCSPlayerController client, CommandInfo info)
        {
            foreach(var player in Utilities.GetPlayers())
            {
                info.ReplyToCommand($"{player.UserId}: {player.PlayerName}| Zombie: {ZombiePlayers[player.Slot].IsZombie}| MotherZombie: {ZombiePlayers[player.Slot].MotherZombieStatus} | Player Slot: {player.Slot}");
            }
        }
    }
}