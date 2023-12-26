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
            AddCommand("css_classlist", "Class List Command", CommandClassList);
            AddCommand("css_weaponlist", "Weapon List Command", WeaponListCommand);
            AddCommand("css_hitgrouplist", "Hitgroup List Command", HiggroupsListCommand);
            AddCommand("css_rk", "Toggle Repeat Killer Command", ToggleRepeatKillerCommand);
            AddCommand("css_myclass", "Client PlayerClass Command", ClientPlayerClassCommand);
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

        [RequiresPermissions(@"css/slay")]
        private void PlayerListCommand(CCSPlayerController client, CommandInfo info)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                info.ReplyToCommand($"{player.UserId}: {player.PlayerName}| Zombie: {ZombiePlayers[player.Slot].IsZombie}| MotherZombie: {ZombiePlayers[player.Slot].MotherZombieStatus} | Player Slot: {player.Slot}");
            }
        }

        [RequiresPermissions(@"css/slay")]
        private void CommandClassList(CCSPlayerController client, CommandInfo info)
        {
            foreach (var classData in PlayerClassDatas.PlayerClasses)
            {
                info.ReplyToCommand($"Class Name: {classData.Value.Name}");
            }
        }

        [RequiresPermissions(@"css/slay")]
        private void WeaponListCommand(CCSPlayerController client, CommandInfo info)
        {
            foreach (var weaponData in WeaponDatas.WeaponConfigs)
            {
                info.ReplyToCommand($"Class Name: {weaponData.Value.WeaponName}");
            }
        }

        [RequiresPermissions(@"css/slay")]
        private void HiggroupsListCommand(CCSPlayerController client, CommandInfo info)
        {
            foreach (var hitgroupData in HitGroupDatas.HitGroupConfigs)
            {
                info.ReplyToCommand($"Found: {hitgroupData.Key}");
            }
        }

        private void ToggleRepeatKillerCommand(CCSPlayerController client, CommandInfo info)
        {
            if (!RepeatKillerEnable)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} This feature is currently disabled.");
                return;
            }

            if (RepeatKillerActivated)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Repeat killer detector force toggled off. Re-enabling respawn for this round.");
                RepeatKillerActivated = false;
                ForceRespawnAllDeath();
                return;
            }

            else
            {
                info.ReplyToCommand($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Repeat killer is already turned off!");
                return;
            }
        }

        private void ClientPlayerClassCommand(CCSPlayerController client, CommandInfo info)
        {
            info.ReplyToCommand($"Human Class: {ClientPlayerClass[client.Slot].HumanClass}");
            info.ReplyToCommand($"Zombie Class: {ClientPlayerClass[client.Slot].ZombieClass}");
            info.ReplyToCommand($"Active Class: {ClientPlayerClass[client.Slot].HumanClass}");
        }
    }
}