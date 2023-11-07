using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZombieSharp
{
	public class EventModule
	{
		private ZombieSharp _Core;
		private ZombiePlayer _Player;

		public EventModule(ZombieSharp plugin)
		{
			_Core = plugin;
		}

		public void Initialize()
		{
			_Core.RegisterEventHandler<EventRoundStart>(OnRoundStart);
			_Core.RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
			_Core.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
			_Core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
			_Core.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);

			_Core.RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServerHandler);
			_Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnectHandler);
		}

		private void OnClientPutInServerHandler(int clientindex)
		{
			CCSPlayerController client = Utilities.GetPlayerFromUserid(clientindex);
			_Player.g_bZombie.Add(client, false);
		}

		private void OnClientDisconnectHandler(int clientindex)
		{
			CCSPlayerController client = Utilities.GetPlayerFromUserid(clientindex);
			_Player.g_bZombie.Remove(client);
		}

		private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
		{
			Server.PrintToChatAll("[Z:Sharp] The current game mode is the Human vs. Zombie, the zombie goal is to infect all human before time is running out.");
			return HookResult.Continue;
		}

		private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
		{
			_Core.InfectOnRoundFreezeEnd();
			return HookResult.Continue;
		}

		private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
		{
			// Reset Zombie Spawned here.
			_Core.g_bZombieSpawned = false;

			// Reset Client Status
			_Core.AddTimer(0.3f, Timer_ResetZombieStatus);

			return HookResult.Continue;
		}

		// avoiding zombie status glitch on human class like in zombie:reloaded
		private void Timer_ResetZombieStatus()
		{
			// Reset Client Status
			for(int i = 0; i < Server.MaxPlayers; i++)
			{
				// Reset Client Status.
				_Player.g_bZombie[Utilities.GetPlayerFromUserid(i)] = false;

				// if they were chosen as motherzombie then let's make them not to get chosen again.
				if(_Player.g_MotherZombieStatus[Utilities.GetPlayerFromUserid(i)] == ZombiePlayer.MotherZombieFlags.CHOSEN)
					_Player.g_MotherZombieStatus[Utilities.GetPlayerFromUserid(i)] = ZombiePlayer.MotherZombieFlags.LAST;
			}
		}

		private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
		{
			CCSPlayerController client = @event.Userid;
			CCSPlayerController attacker = @event.Attacker;
			string weapon = @event.Weapon;

			if(_Player.IsClientInfect(attacker) && _Player.IsClientHuman(client) && string.Equals(weapon, "knife"))
				_Core.InfectClient(client, attacker);

			return HookResult.Continue;
		}

		private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
		{
			_Core.CheckGameStatus();
			return HookResult.Continue;
		}
	}
}