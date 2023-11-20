namespace ZombieSharp
{
	public class EventModule
	{
		private readonly ZombieSharp _core;
		private ZombiePlayer _player;
		private ZTeleModule _zTeleModule;

		public EventModule(ZombieSharp plugin)
		{
			_core = plugin;
		}

		public void Initialize()
		{
			_core.RegisterEventHandler<EventRoundStart>(OnRoundStart);
			_core.RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
			_core.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
			_core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
			_core.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
			_core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
			_core.RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Pre);

			_core.RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServerHandler);
			_core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnectHandler);
		}

		private void OnClientPutInServerHandler(int clientindex)
		{
			var client = Utilities.GetPlayerFromSlot(clientindex);
			_player.g_bZombie.Add(client, false);
		}

		private void OnClientDisconnectHandler(int clientindex)
		{
			var client = Utilities.GetPlayerFromSlot(clientindex);
			_player.g_bZombie.Remove(client);
			_zTeleModule.ClientSpawnDatas.Remove(client);
		}

		private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
		{
			Server.PrintToChatAll($"{ChatColors.Green}[Z:Sharp]{ChatColors.Default} The current game mode is the Human vs. Zombie, the zombie goal is to infect all human before time is running out.");
			return HookResult.Continue;
		}

		private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
		{
			_core.InfectOnRoundFreezeEnd();
			return HookResult.Continue;
		}

		private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
		{
			// Reset Zombie Spawned here.
			_core.ZombieSpawned = false;

			// Reset Client Status
			_core.AddTimer(0.3f, Timer_ResetZombieStatus);

			return HookResult.Continue;
		}

		// avoiding zombie status glitch on human class like in zombie:reloaded
		private void Timer_ResetZombieStatus()
		{
			// Reset Client Status
			for(var i = 1; i < Server.MaxPlayers; i++)
			{
				var client = Utilities.GetPlayerFromIndex(i);
				
				// Reset Client Status.
				_player.g_bZombie[client] = false;

				// if they were chosen as motherzombie then let's make them not to get chosen again.
				if(_player.g_MotherZombieStatus[client] == ZombiePlayer.MotherZombieFlags.CHOSEN)
					_player.g_MotherZombieStatus[client] = ZombiePlayer.MotherZombieFlags.LAST;
			}
		}

		private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
		{
			var client = @event.Userid;
			var attacker = @event.Attacker;
			
			var weapon = @event.Weapon;
			var dmgHealth = @event.DmgHealth;

			if(_player.IsClientInfect(attacker) && _player.IsClientHuman(client) && string.Equals(weapon, "knife"))
				_core.InfectClient(client, attacker);

			_core.KnockbackClient(client, attacker, dmgHealth);

			return HookResult.Continue;
		}

		private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
		{
			_core.CheckGameStatus();
			return HookResult.Continue;
		}

		private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
		{
			var client = @event.Userid;

			var spawnPos = @event.Userid.PlayerPawn.Value.AbsOrigin;
			var spawnAngle = @event.Userid.PlayerPawn.Value.AbsRotation;

			_zTeleModule.ZTele_GetClientSpawnPoint(client, spawnPos, spawnAngle);

            // if zombie already spawned then they become zombie.
            if (_core.ZombieSpawned)
				_core.InfectClient(client);

			// else they're human!
			_core.HumanizeClient(client);

			return HookResult.Continue;
		}

		private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
		{
			var client = @event.Userid;
			
			var weapon = @event.Item;

			// if client is zombie and it's not a knife, then no pickup
			if(_player.IsClientInfect(client) && !string.Equals(weapon, "knife"))
				return HookResult.Handled;

			return HookResult.Continue;
		}
	}
}