using System;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZombieSharp
{
	public class ZombieSharp : BasePlugin
	{
		public override string ModuleName => "Zombie Sharp";
		public override string ModuleAuthor => "Oylsister";
		public override string ModuleVersion => "1.0";

		private EventModule _event;
		private ZombiePlayer _player; 

		public bool g_bZombieSpawned;
		public int g_iCountdown;

		private CounterStrikeSharp.API.Modules.Timers.Timer g_hCountdown = null;
		private CounterStrikeSharp.API.Modules.Timers.Timer g_hInfectMZ = null;

		public override void Load(bool HotReload)
		{
			_event.Initialize();
		}

		public void InfectOnRoundFreezeEnd()
		{
			g_iCountdown = 15;
			g_hCountdown = AddTimer(1.0f, Timer_Countdown, TimerFlags.REPEAT);
			g_hInfectMZ = AddTimer(15.0f, MotherZombieInfect);
		}

		public void Timer_Countdown()
		{
			if(g_iCountdown <= 0 && g_hCountdown != null)
			{
				g_hCountdown.Kill();
			}

			g_iCountdown--;
		}

		public void MotherZombieInfect()
		{
			g_bZombieSpawned = true;
		}

		public void InfectClient(CCSPlayerController client, CCSPlayerController attacker = null, bool motherzombie = false, bool force = false)
		{
			if(!g_bZombieSpawned)
				g_bZombieSpawned = true;

			if(g_bZombieSpawned)
				CheckGameStatus();

			if(_player.IsClientHuman(client))
			{
				_player.g_bZombie[client] = true;
			}

			if(attacker != null)
			{
				EventPlayerDeath _event = new EventPlayerDeath(true);

				_event.Set<CCSPlayerController>("attacker", attacker);
				_event.Set<CCSPlayerController>("userid", client);
				_event.Set<string>("weapon", "knife");
				_event.FireEvent(true);
			}
		}

		public void CheckGameStatus()
		{
			int human = 0;
			int zombie = 0;

			for(int i = 0; i < Server.MaxPlayers; i++)
			{
				var client = Utilities.GetPlayerFromUserid(i);

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
		public MotherZombieFlags g_MotherZombieStatus;

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
