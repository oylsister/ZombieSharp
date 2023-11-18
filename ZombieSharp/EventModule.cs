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
			_Core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
			_Core.RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Pre);

			_Core.RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServerHandler);
			_Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnectHandler);
		}

		private void OnClientPutInServerHandler(int clientindex)
		{
			CCSPlayerController client = Utilities.GetPlayerFromSlot(clientindex);
			_Player.g_bZombie.Add(client, false);
		}

		private void OnClientDisconnectHandler(int clientindex)
		{
			CCSPlayerController client = Utilities.GetPlayerFromSlot(clientindex);
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
			_Core.ZombieSpawned = false;

			// Reset Client Status
			_Core.AddTimer(0.3f, Timer_ResetZombieStatus);

			return HookResult.Continue;
		}

		// avoiding zombie status glitch on human class like in zombie:reloaded
		private void Timer_ResetZombieStatus()
		{
			// Reset Client Status
			for(int i = 1; i < Server.MaxPlayers; i++)
			{
				// Reset Client Status.
				_Player.g_bZombie[Utilities.GetPlayerFromIndex(i)] = false;

				// if they were chosen as motherzombie then let's make them not to get chosen again.
				if(_Player.g_MotherZombieStatus[Utilities.GetPlayerFromIndex(i)] == ZombiePlayer.MotherZombieFlags.CHOSEN)
					_Player.g_MotherZombieStatus[Utilities.GetPlayerFromIndex(i)] = ZombiePlayer.MotherZombieFlags.LAST;
			}
		}

		private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
		{
			CCSPlayerController client = @event.Userid;
			CCSPlayerController attacker = @event.Attacker;
			string weapon = @event.Weapon;
			int dmg_health = @event.DmgHealth;

			if(_Player.IsClientInfect(attacker) && _Player.IsClientHuman(client) && string.Equals(weapon, "knife"))
				_Core.InfectClient(client, attacker);

			_Core.KnockbackClient(client, attacker, (float)dmg_health);

			return HookResult.Continue;
		}

		private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
		{
			_Core.CheckGameStatus();
			return HookResult.Continue;
		}

		private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
		{
			CCSPlayerController client = @event.Userid;

			// if zombie already spawned then they become zombie.
			if(_Core.ZombieSpawned)
				_Core.InfectClient(client);

			// else they're human!
			_Core.HumanizeClient(client);

			return HookResult.Continue;
		}

		private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
		{
			CCSPlayerController client = @event.Userid;
			string weapon = @event.Item;

			// if client is zombie and it's not a knife, then no pickup
			if(_Player.IsClientInfect(client) && !string.Equals(weapon, "knife"))
				return HookResult.Handled;

			return HookResult.Continue;
		}
	}
}