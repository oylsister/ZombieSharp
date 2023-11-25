using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Core;
using ZombieSharp.Helpers;

namespace ZombieSharp
{
	public class ZombieSharp : BasePlugin
	{
		public override string ModuleName => "Zombie Sharp";
		public override string ModuleAuthor => "Oylsister, Kurumi, Sparky";
		public override string ModuleVersion => "1.0 Alpha";

		private IEventModule _event;
		private IZombiePlayer _player; 
		private ICommandModule _command;
		private IWeaponModule _weapon;
		private IZTeleModule _ztele;

		public ZombieSharp() : base()
		{
			PluginHost = Host.CreateDefaultBuilder().ConfigureServices(services => 
			{
				services
					.AddSingleton<ZombieSharp>()
					.AddSingleton<IEventModule, EventModule>()
					.AddSingleton<IWeaponModule, WeaponModule>()
					.AddSingleton<IZTeleModule, ZTeleModule>()
					.AddSingleton<ICommandModule, CommandModule>()
					.AddSingleton<IZombiePlayer, ZombiePlayer>();
			}).Build();

            _event = PluginHost.Services.GetRequiredService<IEventModule>();
            _weapon = PluginHost.Services.GetRequiredService<IWeaponModule>();
			_ztele = PluginHost.Services.GetRequiredService<IZTeleModule>();
			_command = PluginHost.Services.GetRequiredService<ICommandModule>();
			_player = PluginHost.Services.GetRequiredService<IZombiePlayer>();
		}

		public IHost PluginHost { get; init; }

		public bool ZombieSpawned;
		public int Countdown;

		private CounterStrikeSharp.API.Modules.Timers.Timer g_hCountdown = null;
		private CounterStrikeSharp.API.Modules.Timers.Timer g_hInfectMZ = null;

        public override void Load(bool HotReload)
		{
			_event.Initialize();
			_command.Initialize();
			_weapon.Initialize();
		}

		public void InfectOnRoundFreezeEnd()
		{
			Countdown = 15;
			g_hCountdown = AddTimer(1.0f, Timer_Countdown, TimerFlags.REPEAT);
			g_hInfectMZ = AddTimer(15.0f, MotherZombieInfect);

			for(int i = 1; i < Server.MaxPlayers; i++)
			{
				var client = Utilities.GetPlayerFromIndex(i);

				if(client.IsValid)
					client.PrintToCenter($" First Infection in {Countdown} seconds");
			}
		}

		public void Timer_Countdown()
		{
			if(Countdown <= 0 && g_hCountdown != null)
			{
				g_hCountdown.Kill();
			}

			Countdown--;

			for(int i = 1; i < Server.MaxPlayers; i++)
			{
				var client = Utilities.GetPlayerFromIndex(i);

				if(client.IsValid)
					client.PrintToCenter($" First Infection in {Countdown} seconds");
			}
		}

		public void MotherZombieInfect()
		{
			if(ZombieSpawned)
				return;

			//ArrayList candidate = new ArrayList();
			List<CCSPlayerController> candidate = new List<CCSPlayerController>();

			int allplayer = 0;

			for(int i = 1; i < Server.MaxPlayers; i++)
			{
				var client = Utilities.GetPlayerFromIndex(i);
				if(client.PawnIsAlive && client.IsValid)
				{
					if(_player.g_MotherZombieStatus[client] == ZombiePlayer.MotherZombieFlags.NONE)
						candidate.Add(client);

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
					for(int i = 0; i < candidate.Count; i++)
					{
						CCSPlayerController client = (CCSPlayerController)candidate[i];
						InfectClient(client, null, true);
						alreadymade++;
					}
				}

				candidate.Clear();

				for(int i = 1; i < Server.MaxPlayers;i++)
				{
					var client = Utilities.GetPlayerFromIndex(i);

					if(_player.g_MotherZombieStatus[client] == ZombiePlayer.MotherZombieFlags.LAST)
					{
						_player.g_MotherZombieStatus[client] = ZombiePlayer.MotherZombieFlags.NONE;
						candidate.Add(client);
					}
				}

				for(int i = 0; i < candidate.Count; i++)
				{
					if(alreadymade >= maxmz)
						break;

					CCSPlayerController client = (CCSPlayerController)candidate[i];
					InfectClient(client, null, true);
					alreadymade++;
				}
			}
			else
			{
				for(int i = 0; i < candidate.Count; i++)
				{
					if(alreadymade >= maxmz)
						break;

					CCSPlayerController client = (CCSPlayerController)candidate[i];
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
			if(_player.IsClientHuman(client))
			{
				_player.g_bZombie[client] = true;
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
				_player.g_MotherZombieStatus[client] = ZombiePlayer.MotherZombieFlags.CHOSEN;
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
			if(_player.IsClientInfect(client))
			{
				_player.g_bZombie[client] = false;
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
			if(!_player.IsClientInfect(client) || _player.IsClientHuman(attacker))
				return;

			var clientPawn = client.PlayerPawn.Value;
			var attackerPawn = attacker.PlayerPawn.Value;

            Vector clientpos = clientPawn.AbsOrigin ?? new(0f, 0f, 0f);
			Vector attackerpos = attackerPawn.AbsOrigin ?? new(0f, 0f, 0f);

			Vector direction = (clientpos - attackerpos).NormalizeVector();

			var clientVelocity = clientPawn.AbsVelocity;
			var weaponKnockback = _weapon.WeaponDatas.WeaponConfigs[weapon].Knockback * _weapon.WeaponDatas.KnockbackMultiply;
			Vector pushVelocity = direction * damage * weaponKnockback;

			Vector velocity = clientVelocity + pushVelocity;

			client.Teleport(new(0f, 0f, 0f), new(0f, 0f, 0f), velocity);
		}

		public void CheckGameStatus()
		{
			int human = 0;
			int zombie = 0;

			for(int i = 1; i < Server.MaxPlayers; i++)
			{
				var client = Utilities.GetPlayerFromIndex(i);

				if(_player.IsClientInfect(client) && client.PawnIsAlive)
					zombie++;
				
				else if(_player.IsClientHuman(client) && client.PawnIsAlive)
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

		public ArrayList FindTargetByName(string name)
		{
			ArrayList clientlist = new ArrayList();

			for(int i = 1; i <= Server.MaxPlayers; i++)
			{
				CCSPlayerController client = Utilities.GetPlayerFromIndex(i);

				if(string.Equals(name, "@all"))
				{
					clientlist.Add(client);
				}

				else if(string.Equals(name, "@ct"))
				{
					if((CsTeam)client.TeamNum == CsTeam.CounterTerrorist)
						clientlist.Add(client);
				}

				else if(string.Equals(name, "@t"))
				{
					if((CsTeam)client.TeamNum == CsTeam.Terrorist)
						clientlist.Add(client);
				}

				else
				{
					StringComparison compare = StringComparison.OrdinalIgnoreCase;

					if(client.PlayerName.Contains(name, compare))
					{
						clientlist.Add(client);
					}
				}
			}

			return clientlist;
		}

		public static CCSGameRules GetGameRules()
		{
			return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
		}
    }

    public interface IZombiePlayer
    {
        Dictionary<CCSPlayerController, bool> g_bZombie { get; set; }
        Dictionary<CCSPlayerController, ZombiePlayer.MotherZombieFlags> g_MotherZombieStatus { get; set; }

        bool IsClientHuman(CCSPlayerController player);
        bool IsClientInfect(CCSPlayerController player);
    }

    public class ZombiePlayer : IZombiePlayer
    {
        [Flags]
        public enum MotherZombieFlags
        {
            NONE = (1 << 0),
            CHOSEN = (1 << 1),
            LAST = (1 << 2)
        }

        //public bool[] g_bZombie = new bool[128];

        public Dictionary<CCSPlayerController, bool> g_bZombie { get; set; } = new Dictionary<CCSPlayerController, bool>();
        public Dictionary<CCSPlayerController, MotherZombieFlags> g_MotherZombieStatus { get; set; } = new Dictionary<CCSPlayerController, MotherZombieFlags>();

        public bool IsClientInfect(CCSPlayerController player)
        {
            return g_bZombie[player];
        }

        public bool IsClientHuman(CCSPlayerController player)
        {
            return !g_bZombie[player];
        }
    }
}   
