using System.Collections;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Core;
using ZombieSharp.Helpers;
using CounterStrikeSharp.API.Modules.Entities;
using System.IO.Compression;
using static System.Formats.Asn1.AsnWriter;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace ZombieSharp
{
    public partial class ZombieSharp : BasePlugin
    {
        public override string ModuleName => "Zombie Sharp";
        public override string ModuleAuthor => "Oylsister, Kurumi, Sparky";
        public override string ModuleVersion => "1.0 Alpha";

        public bool ZombieSpawned;
        public int Countdown;

        [Flags]
        public enum MotherZombieFlags
        {
            NONE = (1 << 0),
            CHOSEN = (1 << 1),
            LAST = (1 << 2)
        }

        public bool[] IsZombie { get; set; } = new bool[Server.MaxPlayers];
        public MotherZombieFlags[] MotherZombieStatus { get; set; } = new MotherZombieFlags[Server.MaxPlayers];

        private CounterStrikeSharp.API.Modules.Timers.Timer g_hCountdown = null;
        private CounterStrikeSharp.API.Modules.Timers.Timer g_hInfectMZ = null;

        public override void Load(bool HotReload)
        {
            EventInitialize();
            CommandInitialize();

            MemoryFunctionVoid<CCSPlayer_WeaponServices, CBasePlayerWeapon> CCSPlayer_WeaponServices_CanUseFunc = new(GameData.GetSignature("CCSPlayer_WeaponServices_CanUse"));
            Action<CCSPlayer_WeaponServices, CBasePlayerWeapon> CCSPlayer_WeaponServices_CanUse = CCSPlayer_WeaponServices_CanUseFunc.Invoke;

            CCSPlayer_WeaponServices_CanUseFunc.Hook((h =>
            {
                var weaponservices = h.GetParam<CCSPlayer_WeaponServices>(0);
                var clientweapon = h.GetParam<CBasePlayerWeapon>(1);

                var client = new CCSPlayerController(weaponservices!.Pawn.Value.Controller.Value!.Handle);

                if (IsZombie[client.UserId ?? 0])
                {
                    if (clientweapon.DesignerName != "weapon_knife")
                    {
                        clientweapon.Remove();
                    }
                }

                return HookResult.Continue;

            }), HookMode.Pre);
        }

        public void InfectOnRoundFreezeEnd()
        {
            Countdown = 15;
            g_hCountdown = AddTimer(1.0f, Timer_Countdown, TimerFlags.REPEAT);
            g_hInfectMZ = AddTimer(15.0f, MotherZombieInfect);
        }

        public void Timer_Countdown()
        {
            if(Countdown < 0 && g_hCountdown != null)
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
            if(ZombieSpawned)
                return;

            //ArrayList candidate = new ArrayList();
            List<CCSPlayerController> candidate = new List<CCSPlayerController>();
            List<CCSPlayerController> targetlist = Utilities.GetPlayers();

            int allplayer = 0;

            foreach(var client in targetlist)
            {
                if(client.PawnIsAlive! && client.IsValid!)
                {
                    if (MotherZombieStatus[client.UserId ?? 0] == MotherZombieFlags.NONE)
                    {
                        // Server.PrintToChatAll($"Add {client.PlayerName} to mother zombie candidate.");
                        candidate.Add(client);
                    }

                    allplayer++;
                }
            }

            int alreadymade = 0;

            int maxmz = allplayer / 7;

            if(candidate.Count < maxmz)
            {
                Server.PrintToChatAll($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} Mother zombie cycle has been reset!");

                if(candidate.Count > 0)
                {
                    foreach (var client in candidate)
                    {
                        InfectClient(client, null, true);
                        alreadymade++;
                    }
                }

                candidate.Clear();

                foreach(var client in Utilities.GetPlayers())
                { 
                    if(MotherZombieStatus[client.UserId ?? 0] == MotherZombieFlags.LAST)
                    {
                        MotherZombieStatus[client.UserId ?? 0] = MotherZombieFlags.NONE;
                        candidate.Add(client);
                    }
                }

                foreach (var client in candidate)
                {
                    if(alreadymade >= maxmz)
                        break;

                    InfectClient(client, null, true);
                    alreadymade++;
                }
            }
            else
            {
                foreach (var client in candidate)
                {
                    if(alreadymade >= maxmz)
                        break;

                    InfectClient(client, null, true);
                    alreadymade++;
                }
            }
        }

        public void InfectClient(CCSPlayerController client, CCSPlayerController attacker = null, bool motherzombie = false, bool force = false)
        {
            // make zombie status be true.
            IsZombie[client.UserId ?? 0] = true;

            // if they from the motherzombie infection put status here to prevent being chosen for it again.
            if(motherzombie)
            {
                MotherZombieStatus[client.UserId ?? 0] = MotherZombieFlags.CHOSEN;
                ZTele_TeleportClientToSpawn(client);
            }

            // Remove all weapon.
            ForceDropAllWeapon(client);

            // swith to terrorist side.
            client.SwitchTeam(CsTeam.Terrorist);

            AddTimer(0.1f, () =>
            {
                client.PlayerPawn.Value!.SetModel(@"characters\models\tm_phoenix\tm_phoenix.vmdl");
            });

            // no armor
            CCSPlayerPawn clientpawn = client.PlayerPawn.Value;
            clientpawn.ArmorValue = 0;

            // will apply this in class system later
            clientpawn.Health = 10000;

            client.GiveNamedItem("weapon_knife");

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
            IsZombie[client.UserId ?? 0] = false;

            // switch client to CT
            client.SwitchTeam(CsTeam.CounterTerrorist);

            AddTimer(0.1f, () =>
            {
                client.PlayerPawn.Value!.SetModel("characters\\models\\ctm_sas\\ctm_sas.vmdl");
            });

            // if force tell them that they has been resurrected.
            if (force)
            {
                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have been resurrected by the god! (Knowing as Admin.) Find yourself a cover!");
            }
        }

        public void KnockbackClient(CCSPlayerController client, CCSPlayerController attacker, float damage, string weapon)
        {
            if(!IsClientHuman(attacker) || !IsClientZombie(client))
                return;

            var clientPawn = client.PlayerPawn.Value;
            var attackerPawn = attacker.PlayerPawn.Value;

            Vector clientpos = clientPawn.AbsOrigin ?? new(0f, 0f, 0f);
            Vector attackerpos = attackerPawn.AbsOrigin ?? new(0f, 0f, 0f);

            Vector direction = (clientpos - attackerpos).NormalizeVector();

            var clientVelocity = clientPawn.AbsVelocity;

            float weaponKnockback;

            // try to find the key then the knockback
            if(WeaponDatas.WeaponConfigs.ContainsKey(weapon)) 
            {
                weaponKnockback = WeaponDatas.WeaponConfigs[weapon].Knockback;
            }
            // if key isn't find then set the default one.
            else
            {
                weaponKnockback = 1f;
            }

            Vector pushVelocity = direction * damage * weaponKnockback * WeaponDatas.KnockbackMultiply;

            Vector velocity = clientVelocity + pushVelocity;

            client.Teleport(new(0f, 0f, 0f), new(0f, 0f, 0f), velocity);
        }

        public void CheckGameStatus()
        {
            if(!ZombieSpawned) return;

            int human = 0;
            int zombie = 0;

            List<CCSPlayerController> clientlist = Utilities.GetPlayers();
            foreach (var client in clientlist)
            {
                if(IsZombie[client.UserId ?? 0] && client.PawnIsAlive)
                    zombie++;
                
                else if(!IsZombie[client.UserId ?? 0] && client.PawnIsAlive)
                    human++;
            }

            if(human <= 0)
            {
                // round end.
                CCSGameRules gameRules = GetGameRules();
                gameRules.TerminateRound(5.0f, RoundEndReason.TerroristsWin);
            }
            else if(zombie <= 0)
            {
                // round end.
                CCSGameRules gameRules = GetGameRules();
                gameRules.TerminateRound(5.0f, RoundEndReason.CTsWin);
            }
        }

        public void ForceDropAllWeapon(CCSPlayerController client)
        {
            if (client == null)
                return;

            var weapons = client!.PlayerPawn.Value!.WeaponServices!.MyWeapons;

            for (int i = weapons.Count - 1; i >= 0; i--)
            {
                CCSWeaponBaseVData vdata = weapons[i].Value!.As<CCSWeaponBase>().GetVData<CCSWeaponBaseVData>()!;

                client.ExecuteClientCommand("slot3");
                client.ExecuteClientCommand($"slot{(int)vdata!.GearSlot + 1}");

                client.DropActiveWeapon();
            }
        }

        public CCSGameRules GetGameRules()
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }

        public bool IsClientZombie(CCSPlayerController controller)
        {
            return IsZombie[controller.UserId ?? 0];
        }

        public bool IsClientHuman(CCSPlayerController controller)
        {
            return !IsZombie[controller.UserId ?? 0];
        }
    }
}   
