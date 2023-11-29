namespace ZombieSharp
{
    public class EventModule : IEventModule
    {
        private readonly ZombieSharp _core;
        private IZombiePlayer _player;
        private IZTeleModule _zTeleModule;

        public EventModule(ZombieSharp plugin, IZombiePlayer player, IZTeleModule zTeleModule)
        {
            _core = plugin;
            _player = player;
            _zTeleModule = zTeleModule;
        }

        public void Initialize()
        {
            _core.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            _core.RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
            _core.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            _core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            _core.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            _core.RegisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
            _core.RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Pre);

            _core.RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServerHandler);
            _core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnectHandler);
        }

        private void OnClientPutInServerHandler(int clientindex)
        {
            var client = Utilities.GetPlayerFromSlot(clientindex);
            _player.g_bZombie.Add(client, false);
            _player.g_MotherZombieStatus.Add(client, ZombiePlayer.MotherZombieFlags.NONE);
        }

        private void OnClientDisconnectHandler(int clientindex)
        {
            var client = Utilities.GetPlayerFromSlot(clientindex);
            _player.g_bZombie.Remove(client);
            _zTeleModule.ClientSpawnDatas.Remove(client);
            _player.g_MotherZombieStatus.Remove(client);
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            RemoveRoundObjective();

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
            List<CCSPlayerController> clientlist = Utilities.GetPlayers();

            // Reset Client Status
            foreach (var client in clientlist)
            {
                // Reset Client Status.
                _player.g_bZombie[client] = false;

                // if they were chosen as motherzombie then let's make them not to get chosen again.
                if (_player.g_MotherZombieStatus[client] == ZombiePlayer.MotherZombieFlags.CHOSEN)
                    _player.g_MotherZombieStatus[client] = ZombiePlayer.MotherZombieFlags.LAST;
            }
        }

        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            var client = @event.Userid;
            var attacker = @event.Attacker;

            var weapon = @event.Weapon;
            var dmgHealth = @event.DmgHealth;

            if (_player.IsClientInfect(attacker) && _player.IsClientHuman(client) && string.Equals(weapon, "knife"))
                _core.InfectClient(client, attacker);

            _core.KnockbackClient(client, attacker, dmgHealth, weapon);

            return HookResult.Continue;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            _core.CheckGameStatus();

            _core.AddTimer(5.0f, () =>
            {
                var clientPawn = @event.Userid.PlayerPawn.Value;

                // Respawn the client.
                clientPawn.Respawn();
            });

            return HookResult.Continue;
        }

        private HookResult OnPlayerSpawned(EventPlayerSpawned @event, GameEventInfo info)
        {
            var client = @event.Userid;
            var clientPawn = client.PlayerPawn.Value;
            var spawnPos = clientPawn.AbsOrigin!;
            var spawnAngle = clientPawn.AbsRotation!;

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
            if (_player.IsClientInfect(client) && !string.Equals(weapon, "knife"))
                return HookResult.Handled;

            return HookResult.Continue;
        }

        private void RemoveRoundObjective()
        {
            var objectivelist = new List<string>() {"func_bomb_target", "func_hostage_rescue", "hostage_entity", "c4"};

            foreach (string objectivename in objectivelist)
            {
                var entityIndex = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>(objectivename);

                foreach(var entity in entityIndex)
                {
                    entity.Remove();
                }
            }
        }
    }
}