namespace ZombieSharp
{
    public partial class ZombieSharp
    { 
        public void EventInitialize()
        {
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterEventHandler<EventPlayerJump>(OnPlayerJump);

            RegisterListener<Listeners.OnClientConnected>(OnClientConnected);
            RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnected);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
        }

        private void OnClientConnected(int client)
        {
            var player = Utilities.GetPlayerFromSlot(client);

            int clientindex = player.UserId ?? 0;

            ClientSpawnDatas[clientindex] = new ClientSpawnData();

            IsZombie[clientindex] = false;
            MotherZombieStatus[clientindex] = MotherZombieFlags.NONE;
        }

        private void OnClientDisconnected(int client)
        {
            var player = Utilities.GetPlayerFromSlot(client);

            int clientindex = player.UserId ?? 0;

            ClientSpawnDatas[clientindex] = new ClientSpawnData();

            IsZombie[clientindex] = false;
            MotherZombieStatus[clientindex] = MotherZombieFlags.NONE;
        }

        private void OnMapStart(string mapname)
        {
            WeaponInitialize();
            bool load = SettingsIntialize(mapname);

            if (!load)
                ConfigSettings = new GameSettings();
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            RemoveRoundObjective();

            Server.PrintToChatAll($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} The current game mode is the Human vs. Zombie, the zombie goal is to infect all human before time is running out.");

            return HookResult.Continue;
        }

        private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
        {
            bool warmup = GetGameRules().WarmupPeriod;

            if (!warmup)
                InfectOnRoundFreezeEnd();

            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            // Reset Zombie Spawned here.
            ZombieSpawned = false;

            // Reset Client Status
            AddTimer(0.1f, () =>
            {
                // avoiding zombie status glitch on human class like in zombie:reloaded
                List<CCSPlayerController> clientlist = Utilities.GetPlayers();

                // Reset Client Status
                foreach (var client in clientlist)
                {
                    // Reset Client Status.
                    IsZombie[client.UserId ?? 0] = false;

                    // if they were chosen as motherzombie then let's make them not to get chosen again.
                    if (MotherZombieStatus[client.UserId ?? 0] == MotherZombieFlags.CHOSEN)
                        MotherZombieStatus[client.UserId ?? 0] = MotherZombieFlags.LAST;
                }
            });

            return HookResult.Continue;
        }

        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            if (ZombieSpawned)
            {
                var client = @event.Userid;
                var attacker = @event.Attacker;

                var weapon = @event.Weapon;
                var dmgHealth = @event.DmgHealth;

                if (IsZombie[attacker.UserId ?? 0] && !IsZombie[client.UserId ?? 0] && string.Equals(weapon, "knife"))
                    InfectClient(client, attacker);

                KnockbackClient(client, attacker, dmgHealth, weapon);
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (ZombieSpawned)
            {
                CheckGameStatus();

                if (ConfigSettings.RespawnTimer > 0.0f)
                {
                    AddTimer(5.0f, () =>
                    {
                        var clientPawn = @event.Userid.PlayerPawn.Value;

                        // Respawn the client.
                        if (!@event.Userid.PawnIsAlive)
                            clientPawn.Respawn();
                    });
                }
            }
            return HookResult.Continue;
        }

        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var client = @event.Userid;

            bool warmup = GetGameRules().WarmupPeriod;

            if (!warmup)
            {
                AddTimer(0.2f, () =>
                {
                    var clientPawn = client.PlayerPawn.Value;
                    var spawnPos = clientPawn.AbsOrigin!;
                    var spawnAngle = clientPawn.AbsRotation ?? new(0, 0, 0);

                    // if zombie already spawned then they become zombie.
                    if (ZombieSpawned)
                        InfectClient(client);

                    // else they're human!
                    else
                        HumanizeClient(client);

                    ZTele_GetClientSpawnPoint(client, spawnPos, spawnAngle);
                });
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerJump(EventPlayerJump @event, GameEventInfo info)
        {
            var client = @event.Userid;

            Vector velocity = client.PlayerPawn.Value.AbsVelocity;
            velocity.Y *= 1.07f; 

            client.Teleport(new(0f, 0f, 0f), new(0f, 0f, 0f), velocity);

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