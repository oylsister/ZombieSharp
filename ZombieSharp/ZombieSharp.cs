using System.Collections;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Core;
using ZombieSharp.Helpers;
using CounterStrikeSharp.API.Modules.Entities;
using System.IO.Compression;
using static System.Formats.Asn1.AsnWriter;

namespace ZombieSharp
{
    public class ZombieSharp : BasePlugin
    {
        public override string ModuleName => "Zombie Sharp";
        public override string ModuleAuthor => "Oylsister, Kurumi, Sparky";
        public override string ModuleVersion => "1.0 Alpha";

        private IEventModule _event;
        private ICommandModule _command;
        private IWeaponModule _weapon;
        private IZTeleModule _ztele;

        public ZombieSharp()
        {
            _ztele = new ZTeleModule(this);
            _weapon = new WeaponModule(this);
            _event = new EventModule(this, _ztele, _weapon);
            _command = new CommandModule(this, _ztele);
        }

        public bool ZombieSpawned;
        public int Countdown;

        [Flags]
        public enum MotherZombieFlags
        {
            NONE = (1 << 0),
            CHOSEN = (1 << 1),
            LAST = (1 << 2)
        }

        public bool[] IsZombie = new bool[Server.MaxPlayers];
        public MotherZombieFlags[] MotherZombieStatus = new MotherZombieFlags[Server.MaxPlayers];

        private CounterStrikeSharp.API.Modules.Timers.Timer g_hCountdown = null;
        private CounterStrikeSharp.API.Modules.Timers.Timer g_hInfectMZ = null;

        public override void Load(bool HotReload)
        {
            _event.Initialize();
            _command.Initialize();
        }

        public void InfectOnRoundFreezeEnd()
        {
            Countdown = 15;
            g_hCountdown = AddTimer(1.0f, Timer_Countdown, TimerFlags.REPEAT);
            g_hInfectMZ = AddTimer(15.0f, MotherZombieInfect);

            List<CCSPlayerController> targetlist = Utilities.GetPlayers();

            foreach(var client in targetlist)
            {
                if(client.IsValid)
                    client.PrintToCenter($" First Infection in {Countdown} seconds");
            }
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
                    if (MotherZombieStatus[client.Slot] == MotherZombieFlags.NONE)
                    {
                        Server.PrintToChatAll($"Add {client.PlayerName} to mother zombie candidate.");
                        candidate.Add(client);
                    }

                    allplayer++;
                }
            }

            int alreadymade = 0;

            int maxmz = (int)Math.Round((float)allplayer / 7.0f);

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
                    if(MotherZombieStatus[client.Slot] == MotherZombieFlags.LAST)
                    {
                        MotherZombieStatus[client.Slot] = MotherZombieFlags.NONE;
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
            // if zombie hasn't spawned yet, then make it true.
            if (!ZombieSpawned)
                ZombieSpawned = true;

            // if all human died then let's end the round.
            if(ZombieSpawned)
                CheckGameStatus();

            // make zombie status be true.
            if(IsZombie[client.Slot])
            {
                IsZombie[client.Slot] = true;
            }

            // if has attacker then let's show them in kill feed.
            if(attacker != null)
            {
                EventPlayerDeath _event = new EventPlayerDeath(true);

                _event.Set<CCSPlayerController>("attacker", attacker);
                _event.Set<CCSPlayerController>("userid", client);
                _event.Set<string>("weapon", "knife");
                _event.FireEvent(true);
            }

            // if they from the motherzombie infection put status here to prevent being chosen for it again.
            if(motherzombie)
            {
                MotherZombieStatus[client.Slot] = MotherZombieFlags.CHOSEN;
                _ztele.ZTele_TeleportClientToSpawn(client);
            }

            // swith to terrorist side.
            client.SwitchTeam(CsTeam.Terrorist);

            // no armor
            CCSPlayerPawn clientpawn = client.PlayerPawn.Value;
            clientpawn.ArmorValue = 0;

            // will apply this in class system later
            clientpawn.Health = 10000;

            // Remove all weapon.
            var weapons = clientpawn.WeaponServices.MyWeapons;

            foreach(var weapon in weapons)
            {
                if(weapon == null)
                    continue;

                weapon.Value.Remove();
            }

            client.GiveNamedItem("weapon_knife");

            var winCondition = ConVar.Find("mp_ignore_round_win_conditions");

            if(winCondition.GetPrimitiveValue<bool>())
                Server.ExecuteCommand("mp_ignore_round_win_conditions 0");

            // if force then tell them that they has been punnished.
            if (force)
            {
                client.PrintToChat($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have been punished by the god! (Knowing as Admin.) Now plauge all human!");
            }

            client.PrintToChat($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have been infected! Go pass it on to as many other players as you can.");
        }

        public void HumanizeClient(CCSPlayerController client, bool force = false)
        {
            // zombie status to false
            if(IsZombie[client.Slot])
            {
                IsZombie[client.Slot] = false;
            }

            // switch client to CT
            client.SwitchTeam(CsTeam.CounterTerrorist);

            // if force tell them that they has been resurrected.
            if(force)
            {
                client.PrintToChat($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have been resurrected by the god! (Knowing as Admin.) Find yourself a cover!");
            }
        }

        public void KnockbackClient(CCSPlayerController client, CCSPlayerController attacker, float damage, string weapon)
        {
            if(IsZombie[client.Slot] || !IsZombie[attacker.Slot])
                return;

            var clientPawn = client.PlayerPawn.Value;
            var attackerPawn = attacker.PlayerPawn.Value;

            Vector clientpos = clientPawn.AbsOrigin ?? new(0f, 0f, 0f);
            Vector attackerpos = attackerPawn.AbsOrigin ?? new(0f, 0f, 0f);

            Vector direction = (clientpos - attackerpos).NormalizeVector();

            var clientVelocity = clientPawn.AbsVelocity;

            float weaponKnockback;

            // try to find the key then the knockback
            if(_weapon.WeaponDatas.WeaponConfigs.ContainsKey(weapon)) 
            {
                weaponKnockback = _weapon.WeaponDatas.WeaponConfigs[weapon].Knockback;
            }
            // if key isn't find then set the default one.
            else
            {
                weaponKnockback = 1f;
            }

            Vector pushVelocity = direction * damage * weaponKnockback * _weapon.WeaponDatas.KnockbackMultiply;

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
                if(IsZombie[client.Slot] && client.PawnIsAlive)
                    zombie++;
                
                else if(!IsZombie[client.Slot] && client.PawnIsAlive)
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

        public static CCSGameRules GetGameRules()
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }
    }
}   
