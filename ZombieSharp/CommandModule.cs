using System.Collections;
using CounterStrikeSharp.API.Modules.Commands.Targeting;

namespace ZombieSharp
{
    public interface ICommandModule
    {
        void Initialize();
    }

    public class CommandModule : ICommandModule
    {
        private readonly ZombieSharp _core;
        private IZTeleModule _zTeleModule;

        public CommandModule(ZombieSharp plugin, IZTeleModule zTeleModule)
        {
            _core = plugin;
            _zTeleModule = zTeleModule;
        }

        public void Initialize()
        {
            _core.AddCommand("css_zs_infect", "Infect Client Command", InfectClientCommand);
            _core.AddCommand("css_zs_human", "Humanize Client Command", HumanizeClientCommand);
            _core.AddCommand("css_zs_ztele", "Teleport Client to spawn Command", ZTeleClientCommand);
        }

        private void InfectClientCommand(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount <= 1)
            {
                info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} Usage: css_zs_infect [<playername>].");
                return;
            }

            var targets = info.GetArgTargetResult(1);

            if (targets.Players.Count <= 0)
            {
                info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} Couldn't find any client contain with that name.");
                return;
            }

            foreach (CCSPlayerController target in targets.Players)
            {
                if (!target.IsValid)
                    continue;

                if (!target.PawnIsAlive)
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} target {target.PlayerName} is not alive.");

                    continue;
                }

                if (_core.IsZombie[client.Slot])
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} target {target.PlayerName} is already zombie.");

                    continue;
                }

                _core.InfectClient(target, null, false, true);

                if (targets.Players.Count < 2)
                    info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} Successfully infected {target.PlayerName}");
            }

            info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} Successfully infected group.");
        }

        private void HumanizeClientCommand(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount <= 1)
            {
                info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} Usage: css_zs_human <playername>.");
                return;
            }

            var targets = info.GetArgTargetResult(1);

            if (targets.Players.Count <= 0)
            {
                info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} Couldn't find any client contain with that name.");
                return;
            }

            foreach (var target in targets.Players)
            {
                if (!target.IsValid)
                    continue;

                if (!target.PawnIsAlive)
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} target {target.PlayerName} is not alive.");

                    continue;
                }

                if (!_core.IsZombie[client.Slot])
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} target {target.PlayerName} is already human.");

                    continue;
                }

                _core.HumanizeClient(target, true);

                if (targets.Players.Count < 2)
                    info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} Successfully humanized {target.PlayerName}");
            }
            info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} Successfully humanized group.");
        }

        private void ZTeleClientCommand(CCSPlayerController client, CommandInfo info)
        {
            if (!client.IsValid)
                return;

            if (!client.PawnIsAlive)
            {
                info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} This feature requires that you are alive.");
                return;
            }

            _zTeleModule.ZTele_TeleportClientToSpawn(client);
            info.ReplyToCommand($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} Teleported back to spawn.");
        }
    }
}