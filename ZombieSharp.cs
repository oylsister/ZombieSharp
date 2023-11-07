using System;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
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

		public bool g_bZombieSpawned;
		public int g_iCountdown;

		private CounterStrikeSharp.API.Modules.Timers.Timer g_hCountdown = null;
		private CounterStrikeSharp.API.Modules.Timers.Timer g_hInfectMZ = null;

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

		public Dictionary<int, bool> g_bZombie = new Dictionary<int, bool>();

		private int g_iClientIndex;

		public int Index => g_iClientIndex;

		public MotherZombieFlags g_MotherZombieStatus;

		public CCSPlayerController Player { get; init; }
		public CCSPlayerController GetPlayer() => Player;
	}
}   
