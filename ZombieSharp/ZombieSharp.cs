using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ZombieSharp
{
	public class ZombieSharp : BasePlugin
	{
		public override string ModuleName => "Zombie Sharp";
		public override string ModuleAuthor => "Oylsister, Kurumi";
		public override string ModuleVersion => "1.0";

		private EventModule _event;
		private ZombiePlayer _player; 
		private CommandModule _command;
		private WeaponModule _weapon;
		private ZTeleModule _ztele;

		public ZombieSharp() : base()
		{
			PluginHost = Host.CreateDefaultBuilder().ConfigureServices(services => 
			{
				services.AddSingleton <IWeaponModule, WeaponModule>();
			}).Build();

			_weapon = PluginHost.Services.GetRequiredService<WeaponModule>();
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
				Server.PrintToChatAll("[Z:Sharp] Mother zombie cycle has been reset!");

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

			// if force then tell them that they has been punnished.
			if(force)
			{
				client.PrintToChat("[Z:Sharp] You have been punished by the god! (Knowing as Admin.) Now plauge all human!");
			}

			client.PrintToChat("[Z:Sharp] You have been infected! Go pass it on to as many other players as you can.");
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
				client.PrintToChat("[Z:Sharp] You have been resurrected by the god! (Knowing as Admin.) Find yourself a cover!");
			}
		}

		public void KnockbackClient(CCSPlayerController client, CCSPlayerController attacker, float damage)
		{
			if(!_player.IsClientInfect(client) || _player.IsClientHuman(attacker))
				return;

			// Get eye angle
			QAngle eyeangle = client.PlayerPawn.Value.EyeAngles;

			Vector clientpos = client.Pawn.Value.CBodyComponent!.SceneNode.AbsOrigin;
			Vector attackerpos = attacker.Pawn.Value.CBodyComponent!.SceneNode.AbsOrigin;
			Vector direction = attackerpos - clientpos;
			Vector velocity = direction * damage;

			client.Teleport(null, null, velocity);
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
				TerminateRound(CsTeam.Terrorist);
			}
			else if(zombie <= 0)
			{
				// round end.
				TerminateRound(CsTeam.CounterTerrorist);
			}
		}

		public void TerminateRound(CsTeam team)
		{
			EventRoundEnd _event = new EventRoundEnd(true);

			_event.Set<int>("winner", (int)team);
			_event.FireEvent(true);
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
	}

	public class ZombiePlayer
	{
		[Flags]
		public enum MotherZombieFlags
		{
			NONE = (1 << 0),
			CHOSEN = (1 << 1),
			LAST = (1 << 2)
		}

		//public bool[] g_bZombie = new bool[128];

		public Dictionary<CCSPlayerController, bool> g_bZombie = new Dictionary<CCSPlayerController, bool>();
		public Dictionary<CCSPlayerController, MotherZombieFlags> g_MotherZombieStatus = new Dictionary<CCSPlayerController, MotherZombieFlags>();

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
