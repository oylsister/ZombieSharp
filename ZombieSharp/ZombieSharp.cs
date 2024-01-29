using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using ZombieSharp.Helpers;

namespace ZombieSharp
{
    [MinimumApiVersion(159)]
    public partial class ZombieSharp : BasePlugin
    {
        public override string ModuleName => "Zombie Sharp";
        public override string ModuleAuthor => "Oylsister, Kurumi, Sparky";
        public override string ModuleVersion => "1.0.2";
        public override string ModuleDescription => "Infection/survival style gameplay for CS2 in C#";

        public bool ZombieSpawned;
        public int Countdown;

        [Flags]
        public enum MotherZombieFlags
        {
            NONE = (1 << 0),
            CHOSEN = (1 << 1),
            LAST = (1 << 2)
        }

        public class ZombiePlayer
        {
            public bool IsZombie { get; set; } = false;
            public MotherZombieFlags MotherZombieStatus { get; set; } = MotherZombieFlags.NONE;
        }

        public Dictionary<int, ZombiePlayer> ZombiePlayers { get; set; } = new Dictionary<int, ZombiePlayer>();

        private CounterStrikeSharp.API.Modules.Timers.Timer g_hCountdown = null;
        private CounterStrikeSharp.API.Modules.Timers.Timer g_hInfectMZ = null;

        public bool hitgroupLoad = false;

        public override void Load(bool HotReload)
        {
            EventInitialize();
            CommandInitialize();
            VirtualFunctionsInitialize();
            PlayerSettingsOnLoad().Wait();
        }

        public void InfectOnRoundFreezeEnd()
        {
            Countdown = (int)ConfigSettings.FirstInfectionTimer;
            g_hCountdown = AddTimer(1.0f, Timer_Countdown, TimerFlags.REPEAT);
            g_hInfectMZ = AddTimer(ConfigSettings.FirstInfectionTimer + 1.0f, MotherZombieInfect);
        }

        public void Timer_Countdown()
        {
            if (ZombieSpawned)
            {
                g_hCountdown.Kill();
                return;
            }

            if (Countdown < 0 && g_hCountdown != null)
            {
                g_hCountdown.Kill();
                return;
            }

            List<CCSPlayerController> targetlist = Utilities.GetPlayers();

            foreach (var client in targetlist)
            {
                if (client.IsValid)
                    client.PrintToCenter($" First Infection in {Countdown} seconds");
            }

            Countdown--;
        }

        public void MotherZombieInfect()
        {
            if (ZombieSpawned)
                return;

            //ArrayList candidate = new ArrayList();
            List<CCSPlayerController> candidate = new();
            List<CCSPlayerController> targetlist = Utilities.GetPlayers();

            int allplayer = 0;

            foreach (var client in targetlist)
            {
                if (client.PawnIsAlive && client.IsValid!)
                {
                    if (ZombiePlayers[client.Slot].MotherZombieStatus == MotherZombieFlags.NONE)
                    {
                        // Server.PrintToChatAll($"Add {client.PlayerName} to mother zombie candidate.");
                        candidate.Add(client);
                    }

                    allplayer++;
                }
            }

            int alreadymade = 0;

            int maxmz = (int)Math.Ceiling(allplayer / ConfigSettings.MotherZombieRatio);

            // if it is less than 1 then you need at least 1 mother zombie.
            if (maxmz < 1)
                maxmz = 1;

            // Server.PrintToChatAll($"Max Mother Zombie is: {maxmz}");

            if (candidate.Count < maxmz)
            {
                Server.PrintToChatAll($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Mother zombie cycle has been reset!");

                foreach (var client in Utilities.GetPlayers())
                {
                    if (!client.IsValid || !client.PawnIsAlive)
                        continue;

                    if (ZombiePlayers[client.Slot].MotherZombieStatus == MotherZombieFlags.LAST)
                    {
                        ZombiePlayers[client.Slot].MotherZombieStatus = MotherZombieFlags.NONE;
                        candidate.Add(client);
                    }
                }

                foreach (var client in candidate)
                {
                    if (alreadymade >= maxmz)
                        break;

                    if (!client.IsValid || !client.PawnIsAlive)
                        continue;

                    //Server.PrintToChatAll($"Infect {client.PlayerName} as Mother Zombie.");
                    InfectClient(client, null, true);
                    alreadymade++;
                }
            }
            else
            {
                foreach (var client in candidate)
                {
                    if (alreadymade >= maxmz)
                        break;

                    if (!client.IsValid || !client.PawnIsAlive)
                        continue;

                    //Server.PrintToChatAll($"Infect {client.PlayerName} as Mother Zombie.");
                    InfectClient(client, null, true);
                    alreadymade++;
                }
            }
        }

        public void InfectClient(CCSPlayerController client, CCSPlayerController attacker = null, bool motherzombie = false, bool force = false, bool respawn = false)
        {
            // make zombie status be true.
            ZombiePlayers[client.Slot].IsZombie = true;

            string ApplyClass;

            // if they from the motherzombie infection put status here to prevent being chosen for it again.
            if (motherzombie)
            {
                ZombiePlayers[client.Slot].MotherZombieStatus = MotherZombieFlags.CHOSEN;

                ApplyClass = ConfigSettings.Mother_Zombie;

                if (ConfigSettings.TeleportMotherZombie)
                    ZTele_TeleportClientToSpawn(client);
            }
            else
            {
                ApplyClass = ClientPlayerClass[client.Slot].ZombieClass;
            }

            // Create an event for killfeed
            if (attacker != null)
            {
                EventPlayerDeath eventDeath = new EventPlayerDeath(false);
                eventDeath.Userid = client;
                eventDeath.Attacker = attacker;
                eventDeath.Weapon = "knife";
                eventDeath.FireEvent(false);
            }

            // Remove all weapon.
            var dropmode = ConfigSettings.ZombieDrop;

            if (dropmode == 0)
                StripAllWeapon(client);

            else
                ForceDropAllWeapon(client);

            client.GiveNamedItem("weapon_knife");
            client!.PlayerPawn.Value!.WeaponServices!.AllowSwitchToNoWeapon = false;

            // swith to terrorist side.
            client.SwitchTeam(CsTeam.Terrorist);

            bool apply = ApplyClientPlayerClass(client, ApplyClass, 0);

            if (!apply)
            {
                AddTimer(0.1f, () =>
                {
                    client.PlayerPawn.Value!.SetModel(@"characters\models\tm_phoenix\tm_phoenix.vmdl");
                });

                // no armor
                var clientpawn = client.PlayerPawn.Value;
                clientpawn.ArmorValue = 0;

                // will apply this in class system later
                clientpawn.Health = 10000;

                ClientPlayerClass[client.Slot].ActiveClass = null;
            }

            // if all human died then let's end the round.
            if (ZombieSpawned)
                CheckGameStatus();

            // if zombie hasn't spawned yet, then make it true.
            if (!ZombieSpawned)
                ZombieSpawned = true;

            // if force then tell them that they has been punnished.
            if (force)
            {
                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have been punished by the god! (Knowing as Admin.) Now plauge all human!");
            }

            client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have been infected! Go pass it on to as many other players as you can.");
        }

        public void HumanizeClient(CCSPlayerController client, bool force = false)
        {
            // zombie status to false
            ZombiePlayers[client.Slot].IsZombie = false;

            // switch client to CT
            client.SwitchTeam(CsTeam.CounterTerrorist);

            bool apply = ApplyClientPlayerClass(client, ClientPlayerClass[client.Slot].HumanClass, 1);

            if (!apply)
            {
                AddTimer(0.1f, () =>
                {
                    client.PlayerPawn.Value!.SetModel(@"characters\models\ctm_sas\ctm_sas.vmdl");
                });

                var clientPawn = client.PlayerPawn.Value;

                clientPawn.Health = 100;
                clientPawn.ArmorValue = 100;

                ClientPlayerClass[client.Slot].ActiveClass = null;
            }

            // if force tell them that they has been resurrected.
            if (force)
            {
                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have been resurrected by the god! (Knowing as Admin.) Find yourself a cover!");
            }
        }

        public void KnockbackClient(CCSPlayerController client, CCSPlayerController attacker, float damage, string weapon, int hitgroup)
        {
            if (!IsClientHuman(attacker) || !IsClientZombie(client))
                return;

            var clientPawn = client.PlayerPawn.Value;
            var attackerPawn = attacker.PlayerPawn.Value;

            Vector clientpos = clientPawn.AbsOrigin ?? new(0f, 0f, 0f);
            Vector attackerpos = attackerPawn.AbsOrigin ?? new(0f, 0f, 0f);

            Vector direction = (clientpos - attackerpos).NormalizeVector();

            float weaponKnockback;
            var hitgroupknocback = HitGroupGetKnockback(hitgroup);


            // try to find the key then the knockback
            if (WeaponDatas.WeaponConfigs.ContainsKey(weapon))
            {
                weaponKnockback = WeaponDatas.WeaponConfigs[weapon].Knockback;
            }
            // if key isn't find then set the default one.
            else
            {
                weaponKnockback = 1f;
            }

            Vector pushVelocity = direction * damage * weaponKnockback * WeaponDatas.KnockbackMultiply * hitgroupknocback;

            clientPawn.AbsVelocity.Add(pushVelocity);
        }

        public void CheckGameStatus()
        {
            if (!ZombieSpawned) return;

            var teams = Utilities.FindAllEntitiesByDesignerName<CTeam>("cs_team_manager");

            int human = 0;
            int zombie = 0;

            CTeam CTTeam = null;
            CTeam TTeam = null;

            foreach (var team in teams)
            {
                if (team.TeamNum == 2)
                    TTeam = team;

                else if (team.TeamNum == 3)
                    CTTeam = team;
            }

            List<CCSPlayerController> clientlist = Utilities.GetPlayers();
            foreach (var client in clientlist)
            {
                if (!ZombiePlayers.ContainsKey(client.Slot))
                    continue;

                if (IsClientZombie(client) && IsPlayerAlive(client))
                    zombie++;

                else if (IsClientHuman(client) && IsPlayerAlive(client))
                    human++;
            }

            if (human <= 0)
            {
                TTeam.Score += 1;

                // round end.
                CCSGameRules gameRules = GetGameRules();
                gameRules.TerminateRound(5.0f, RoundEndReason.TerroristsWin);
            }
            else if (zombie <= 0)
            {
                CTTeam.Score += 1;

                // round end.
                CCSGameRules gameRules = GetGameRules();
                gameRules.TerminateRound(5.0f, RoundEndReason.CTsWin);
            }
        }

        public void StripAllWeapon(CCSPlayerController client)
        {
            if (client == null)
                return;

            var weapons = client!.PlayerPawn.Value!.WeaponServices!.MyWeapons;

            foreach (var weapon in weapons)
            {
                var vdata = new CCSWeaponBaseVData(weapon.Value!.VData.Handle);
                int weaponslot = (int)vdata!.GearSlot;

                if (!string.IsNullOrEmpty(weapon.Value.UniqueHammerID) && vdata.GearSlot != gear_slot_t.GEAR_SLOT_KNIFE)
                {
                    client.ExecuteClientCommand("slot3");
                    client.ExecuteClientCommand($"slot{weaponslot + 1}");
                    client.DropActiveWeapon();
                }
            }

            client!.PlayerPawn.Value!.WeaponServices!.AllowSwitchToNoWeapon = true;

            client.RemoveWeapons();
        }

        public void ForceDropAllWeapon(CCSPlayerController client)
        {
            if (client == null)
                return;

            var weapons = client!.PlayerPawn.Value!.WeaponServices!.MyWeapons;

            for (int i = weapons.Count - 1; i >= 0; i--)
            {
                CCSWeaponBaseVData vdata = weapons[i].Value!.As<CCSWeaponBase>().GetVData<CCSWeaponBaseVData>()!;

                if (vdata!.GearSlot != gear_slot_t.GEAR_SLOT_KNIFE)
                {
                    client.ExecuteClientCommand("slot3");
                    client.ExecuteClientCommand($"slot{(int)vdata!.GearSlot + 1}");
                    client.DropActiveWeapon();
                }
            }

            client!.PlayerPawn.Value!.WeaponServices.AllowSwitchToNoWeapon = true;
            client.ExecuteClientCommand("slot3");
            CBasePlayerWeapon activeweapon = new CBasePlayerWeapon(client!.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value.Handle);
            activeweapon.AcceptInput("Kill");
        }

        public CCSGameRules GetGameRules()
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }

        public bool IsClientZombie(CCSPlayerController controller)
        {
            if (controller.Slot == 32766)
                return false;

            return ZombiePlayers[controller.Slot].IsZombie;
        }

        public bool IsClientHuman(CCSPlayerController controller)
        {
            if (controller.Slot == 32766)
                return false;

            return !ZombiePlayers[controller.Slot].IsZombie;
        }

        public bool IsPlayerAlive(CCSPlayerController controller)
        {
            if (controller.Slot == 32766)
                return false;

            if (controller.LifeState == (byte)LifeState_t.LIFE_ALIVE || controller.PawnIsAlive)
                return true;

            else
                return false;
        }
    }
}
