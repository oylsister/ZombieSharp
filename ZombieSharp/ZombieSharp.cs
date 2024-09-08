using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Cvars;
using ZombieSharp.Helpers;
using ZombieSharpAPI;

namespace ZombieSharp
{
    [MinimumApiVersion(179)]
    public partial class ZombieSharp : BasePlugin
    {
        public override string ModuleName => "Zombie Sharp";
        public override string ModuleAuthor => "Oylsister, Kurumi, Sparky";
        public override string ModuleVersion => "1.2.3";
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

        private CounterStrikeSharp.API.Modules.Timers.Timer g_hCountdown = null;
        private CounterStrikeSharp.API.Modules.Timers.Timer g_hInfectMZ = null;

        public bool hitgroupLoad = false;

        public Dictionary<int, ZombiePlayer> ZombiePlayers { get; set; } = new Dictionary<int, ZombiePlayer>();

        ZombieSharpAPI API { get; set; }

        public static PluginCapability<IZombieSharpAPI> APICapability = new("zombiesharp");

        bool enableWarmupOnline = true;

        public override void Load(bool HotReload)
        {
            API = new ZombieSharpAPI(this);

            Capabilities.RegisterPluginCapability(APICapability, () => API);

            EventInitialize();
            CommandInitialize();
            VirtualFunctionsInitialize();
            PlayerSettingsOnLoad().Wait();

            if(HotReload)
            {
                WeaponInitialize();
                SettingsIntialize(Server.MapName);
                ClassIsLoaded = PlayerClassIntialize();

                enableWarmupOnline = ConVar.Find("mp_warmup_online_enabled").GetPrimitiveValue<bool>(); 

                hitgroupLoad = HitGroupIntialize();
                RepeatKillerOnMapStart();

                Server.ExecuteCommand("mp_ignore_round_win_conditions 0");
            }
        }

        public void InfectOnRoundFreezeEnd()
        {
            if(g_hCountdown != null)
                g_hCountdown.Kill();

            if(g_hInfectMZ != null)
                g_hInfectMZ.Kill();

            Countdown = (int)CVAR_FirstInfectionTimer.Value;
            g_hCountdown = AddTimer(1.0f, Timer_Countdown, TimerFlags.REPEAT);
            g_hInfectMZ = AddTimer(CVAR_FirstInfectionTimer.Value + 1.0f, MotherZombieInfect);
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
                    client.PrintToCenter($" {Localizer["Core.Countdown", Countdown]}");
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

            int maxmz = (int)Math.Ceiling(allplayer / CVAR_MotherZombieRatio.Value);

            if (CVAR_MinimumMotherZombie.Value > 0 && maxmz < CVAR_MinimumMotherZombie.Value)
                maxmz = CVAR_MinimumMotherZombie.Value;

            else if (CVAR_MinimumMotherZombie.Value <= 0 && maxmz <= 0)
                maxmz = 1;

            // if it is less than 1 then you need at least 1 mother zombie.
            if (maxmz < 1)
                maxmz = 1;

            // Server.PrintToChatAll($"Max Mother Zombie is: {maxmz}");

            if (candidate.Count < maxmz)
            {
                Server.PrintToChatAll($" {Localizer["Prefix"]} {Localizer["Core.MotherZombieReset"]}");

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
            // Action prevent infect client API
            var result = API.APIOnInfectClient(ref client, ref attacker, ref motherzombie, ref force, ref respawn);

            if (result == -1)
            {
                // we have to apply a human attribute when you're not allow them to get infected.
                HumanizeClient(client, false);
                return;
            }

            // make zombie status be true.
            ZombiePlayers[client.Slot].IsZombie = true;

            // if zombie hasn't spawned yet, then make it true.
            if (!ZombieSpawned)
            { 
                ZombieSpawned = true;
                motherzombie = true;
            }

            string ApplyClass;

            // if they from the motherzombie infection put status here to prevent being chosen for it again.
            if (motherzombie)
            {
                ZombiePlayers[client.Slot].MotherZombieStatus = MotherZombieFlags.CHOSEN;

                ApplyClass = Default_MotherZombie;

                if (CVAR_TeleportMotherZombie.Value)
                    ZTele_TeleportClientToSpawn(client);
            }
            else
            {
                ApplyClass = ClientPlayerClass[client.Slot].ZombieClass;
            }

            // Create an event for killfeed
            if (attacker != null && attacker.IsValid)
            {
                EventPlayerDeath eventDeath = new EventPlayerDeath(false);
                eventDeath.Userid = client;
                eventDeath.Attacker = attacker;
                eventDeath.Weapon = "knife";
                eventDeath.FireEvent(false);

                attacker.ActionTrackingServices.MatchStats.Kills += 1;
                client.ActionTrackingServices.MatchStats.Deaths += 1;

                TopDefenderOnInfect(attacker);
            }

            // Remove all weapon.
            var dropmode = CVAR_ZombieDrop.Value;

            if (dropmode == 0)
                StripAllWeapon(client);

            else
                ForceDropAllWeapon(client);

            client.GiveNamedItem("weapon_knife");

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

            // play sound here
            AddTimer(0.1f, () => ZombieScream(client));

            // if all human died then let's end the round.
            /*
            if (ZombieSpawned)
                CheckGameStatus();
            */

            // if force then tell them that they has been punnished.
            if (force)
            {
                client.PrintToChat($" {Localizer["Prefix"]} {Localizer["Core.GetInfect.Force"]}");
            }

            client.PrintToChat($" {Localizer["Prefix"]} {Localizer["Core.GetInfect"]}");
        }

        public void HumanizeClient(CCSPlayerController client, bool force = false)
        {
            // Action prevent humanize client API
            var result = API.APIOnHumanizeClient(ref client, ref force);

            if (result == -1)
            {
                // we have to apply a zombie attribute when you're not allow them to get humanized.
                InfectClient(client, null, false, false, false);
                return;
            }

            // zombie status to false
            ZombiePlayers[client.Slot].IsZombie = false;

            // switch client to CT
            client.SwitchTeam(CsTeam.CounterTerrorist);

            bool apply = ApplyClientPlayerClass(client, ClientPlayerClass[client.Slot].HumanClass, 1);

            if (!apply)
            {
                AddTimer(0.1f, () =>
                {
                    if(force)
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
                client.PrintToChat($" {Localizer["Prefix"]} {Localizer["Core.GetHumanized"]}");
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

            // attacker.PrintToChat($"{weaponKnockback} | {WeaponDatas.KnockbackMultiply} | {damage} | {hitgroupknocback}");

            var totalkb = damage * weaponKnockback * WeaponDatas.KnockbackMultiply * hitgroupknocback;
            // attacker.PrintToChat($"Total {totalkb}");

            Vector pushVelocity = direction * damage * weaponKnockback * WeaponDatas.KnockbackMultiply * hitgroupknocback;

            clientPawn.AbsVelocity.Add(pushVelocity);
        }

        public void CheckGameStatus()
        {
            //Server.PrintToChatAll("Terminate is activated here.");

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

                //Server.PrintToChatAll($"{client.PlayerName} Life state is {client.LifeState} and Pawn is {client.PawnIsAlive}");

                if (IsClientZombie(client) && client.PawnIsAlive)
                    zombie++;

                else if (IsClientHuman(client) && client.PawnIsAlive)
                    human++;
            }

            //Server.PrintToChatAll($"Human = {human}, Zombie = {zombie}");

            if (human <= 0 && zombie > 0)
            {
                TTeam.Score += 1;

                // round end.
                Z_TerminateRound(5f, RoundEndReason.TerroristsWin);
            }
            else if (zombie <= 0 && human > 0)
            {
                CTTeam.Score += 1;

                // round end.
                Z_TerminateRound(5f, RoundEndReason.CTsWin);
            }

            else if (zombie <= 0 && human <= 0)
            {
                Z_TerminateRound(5f, RoundEndReason.TerroristsWin);
            }
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

        public void StripAllWeapon(CCSPlayerController client)
        {
            if (client == null || !client.IsValid)
                return;

            var weapons = client!.PlayerPawn.Value!.WeaponServices!.MyWeapons;

            foreach (var weapon in weapons)
            {
                if (weapon == null || !weapon.IsValid)
                    continue;

                var vdata = new CCSWeaponBaseVData(weapon.Value!.VData.Handle);
                int weaponslot = (int)vdata!.GearSlot;

                if (!string.IsNullOrEmpty(weapon.Value.UniqueHammerID) && vdata.GearSlot != gear_slot_t.GEAR_SLOT_KNIFE)
                {
                    DropWeaponByDesignerName(client, weapon.Value.DesignerName);
                }
            }

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
                    DropWeaponByDesignerName(client, weapons[i].Value.DesignerName);
                }
            }
        }

        public void DropWeaponByDesignerName(CCSPlayerController player, string weaponName)
        {
            var matchedWeapon = player.PlayerPawn.Value.WeaponServices.MyWeapons
                .Where(x => x.Value.DesignerName == weaponName).FirstOrDefault();

            if (matchedWeapon != null && matchedWeapon.IsValid)
            {
                player.PlayerPawn.Value.WeaponServices.ActiveWeapon.Raw = matchedWeapon.Raw;
                player.DropActiveWeapon();
            }
        }

        public CCSGameRules GetGameRules()
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }

        public void Z_TerminateRound(float delay, RoundEndReason reason)
        {
            CCSGameRules gameRules = GetGameRules();
            gameRules.TerminateRound(delay, reason);
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