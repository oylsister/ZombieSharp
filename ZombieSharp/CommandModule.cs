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
            AddCommand("css_classlist", "Class List Command", CommandClassList);
            AddCommand("css_findclass", "Find Class", CommandClassFinder);
            AddCommand("css_weaponlist", "Weapon List Command", WeaponListCommand);
            AddCommand("css_hitgrouplist", "Hitgroup List Command", HiggroupsListCommand);
            AddCommand("css_togglerespawn", "Toggle Respawn Command", ToggleRespawnCommand);
            AddCommand("css_myclass", "Client PlayerClass Command", ClientPlayerClassCommand);
            AddCommand("css_logiclist", "Logic Relay List", LogicRelayListCommand);
            AddCommand("css_zclass", "Player Class Command", PlayerClassCommand);
            AddCommand("css_dropme", "Test Force All Drop Weapon Command", ForceDropCommand);
            AddCommand("css_myweapon", "Get Client Weapon VData List", MyWeaponCommand);
            AddCommand("css_zspawn", "ZSpawn Command", ZSpawnCommand);
            // AddCommand("css_rr", "Restart Round Command", RestartRoundCommand);
            AddCommand("css_scream", "Scream Command", ScreamCommand);
        }

        [RequiresPermissions(@"css/slay")]
        private void InfectClientCommand(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount <= 1)
            {
                info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.InfectUsage"]}");
                return;
            }

            var targets = info.GetArgTargetResult(1);

            if (targets.Players.Count <= 0)
            {
                info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.NoTarget"]}");
                return;
            }

            foreach (CCSPlayerController target in targets.Players)
            {
                if (!target.IsValid)
                    continue;

                if (!IsPlayerAlive(target))
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.TargetNotAlive", target.PlayerName]}");

                    continue;
                }

                if (IsClientZombie(target))
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.AlreadyZombie", target.PlayerName]}");

                    continue;
                }

                InfectClient(target, null, false, true);

                if (targets.Players.Count < 2)
                    info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.SuccessInfect", target.PlayerName]}");
            }

            info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.SuccessInfectGroup"]}");
        }

        [RequiresPermissions(@"css/slay")]
        private void HumanizeClientCommand(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount <= 1)
            {
                info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.HumanUsage"]}");
                return;
            }

            var targets = info.GetArgTargetResult(1);

            if (targets.Players.Count <= 0)
            {
                info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.NoTarget"]}");
                return;
            }

            foreach (var target in targets.Players)
            {
                if (!target.IsValid)
                    continue;

                if (!IsPlayerAlive(target))
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.TargetNotAlive", target.PlayerName]}");

                    continue;
                }

                if (IsClientHuman(target))
                {
                    if (targets.Players.Count < 2)
                        info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.AlreadyHuman", target.PlayerName]}");

                    continue;
                }

                HumanizeClient(target, true);

                if (targets.Players.Count < 2)
                    info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.SuccessHuman", target.PlayerName]}");
            }

            info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.SuccessHumanGroup"]}");
        }

        private void ZTeleClientCommand(CCSPlayerController client, CommandInfo info)
        {
            if (!client.IsValid)
                return;

            if (!IsPlayerAlive(client))
            {
                info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Feature.RequireAlive"]}");
                return;
            }

            client.PrintToCenter(Localizer["ZTele.Delay"]);

            AddTimer(5.0f, () =>
            {
                ZTele_TeleportClientToSpawn(client);
                info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.ZTeleSuccess"]}");
                client.PrintToCenter(Localizer["ZTele.Success"]);
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
                info.ReplyToCommand($"Class Name: {classData.Key}");
            }
        }

        [RequiresPermissions(@"css/slay")]
        private void CommandClassFinder(CCSPlayerController client, CommandInfo info)
        {
            var playerclass = info.GetArg(1);

            if (!PlayerClassDatas.PlayerClasses.ContainsKey(playerclass))
            {
                info.ReplyToCommand($"Can't find {playerclass}");
                return;
            }

            info.ReplyToCommand($"Found {playerclass}");
            info.ReplyToCommand($"Class Name: {PlayerClassDatas.PlayerClasses[playerclass].Name}");
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

        [RequiresPermissions(@"css/slay")]
        private void ToggleRespawnCommand(CCSPlayerController client, CommandInfo info)
        {
            if (info.ArgCount <= 0)
            {
                if (!RespawnEnable)
                {
                    ToggleRespawn(true, true);
                    ForceRespawnAllDeath();
                    return;
                }

                else
                {
                    ToggleRespawn(true, false);
                    return;
                }
            }
            var arg = int.Parse(info.GetArg(1));

            if (arg <= 0)
            {
                ToggleRespawn(true, false);
                return;
            }
            else
            {
                ToggleRespawn(true, true);
                ForceRespawnAllDeath();
                return;
            }
        }

        [RequiresPermissions(@"css/slay")]
        private void ClientPlayerClassCommand(CCSPlayerController client, CommandInfo info)
        {
            info.ReplyToCommand($"Human Class: {ClientPlayerClass[client.Slot].HumanClass}");
            info.ReplyToCommand($"Zombie Class: {ClientPlayerClass[client.Slot].ZombieClass}");
            info.ReplyToCommand($"Active Class: {ClientPlayerClass[client.Slot].ActiveClass}");
        }

        [RequiresPermissions("@css/slay")]
        private void LogicRelayListCommand(CCSPlayerController client, CommandInfo info)
        {
            var entities = Utilities.FindAllEntitiesByDesignerName<CLogicRelay>("logic_relay");

            foreach (var entity in entities)
            {
                info.ReplyToCommand($"Found: {entity.Entity.Name}");
            }
        }

        /*
        [RequiresPermissions("@css/slay")]
        private void RestartRoundCommand(CCSPlayerController client, CommandInfo info)
        {
            var entity = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

            info.ReplyToCommand("Termianted Round");

            entity.TerminateRound(3f, RoundEndReason.RoundDraw);
        }
        */

        private void PlayerClassCommand(CCSPlayerController client, CommandInfo info)
        {
            // if you're not actual player you can't
            if (client == null)
                return;

            PlayerClassMainMenu(client);
        }

        [RequiresPermissions(@"css/slay")]
        private void ForceDropCommand(CCSPlayerController client, CommandInfo info)
        {
            if (client == null)
                return;

            if (!IsPlayerAlive(client))
                return;

            ForceDropAllWeapon(client);
            client.GiveNamedItem("weapon_knife");
        }

        [RequiresPermissions(@"css/slay")]
        private void MyWeaponCommand(CCSPlayerController client, CommandInfo info)
        {
            if (client == null) return;

            if (!client.PawnIsAlive) return;

            var weapons = client.PlayerPawn.Value.WeaponServices.MyWeapons;

            foreach (var weapon in weapons)
            {
                var vdata = new CCSWeaponBaseVData(weapon.Value.VData.Handle);
                info.ReplyToCommand($"Slot: {vdata.Slot} GearSlot {(int)vdata.GearSlot}: {weapon.Value.DesignerName}");
            }
        }

        [CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
        private void ZSpawnCommand(CCSPlayerController client, CommandInfo info)
        {
            if (!client.IsValid)
                return;

            if (CVAR_RespawnTimer.Value <= 0.0 || !RespawnEnable)
            {
                info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Zspawn.Decline"]}");
                return;
            }

            if (client.PawnIsAlive)
            {
                info.ReplyToCommand($" {Localizer["Prefix"]} {Localizer["Command.RequireDead"]}");
                return;
            }

            RespawnClient(client);
        }

        private void ScreamCommand(CCSPlayerController client, CommandInfo info)
        {
            if(client == null) return;

            if (!client.PawnIsAlive)
                return;

            if(!IsClientZombie(client))
                return;

            ZombieScream(client);
        }
    }
}