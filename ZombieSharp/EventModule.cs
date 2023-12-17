using System.Reflection.Metadata;

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
            RegisterEventHandler<EventCsPreRestart>(OnPreRestart);

            RegisterListener<Listeners.OnClientConnected>(OnClientConnected);
            RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnected);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
        }

        private void OnClientConnected(int client)
        {
            var player = Utilities.GetPlayerFromSlot(client);

            int clientindex = player.Slot;

            ClientSpawnDatas.Add(clientindex, new ClientSpawnData());

            ZombiePlayers.Add(clientindex, new ZombiePlayer());

            ZombiePlayers[clientindex].IsZombie = false;
            ZombiePlayers[clientindex].MotherZombieStatus = MotherZombieFlags.NONE;
        }

        private void OnClientDisconnected(int client)
        {
            var player = Utilities.GetPlayerFromSlot(client);

            int clientindex = player.Slot;

            ClientSpawnDatas.Remove(clientindex);
            ZombiePlayers.Remove(clientindex);
        }

        private void OnMapStart(string mapname)
        {
            WeaponInitialize();
            bool load = SettingsIntialize(mapname);

            if (!load)
                ConfigSettings = new GameSettings();

            hitgroupLoad = HitGroupIntialize();
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

        private HookResult OnPreRestart(EventCsPreRestart @event, GameEventInfo info)
        {
            bool warmup = GetGameRules().WarmupPeriod;

            if (!warmup)
            {
                AddTimer(0.1f, () =>
                {
                    List<CCSPlayerController> clientlist = Utilities.GetPlayers();

                    foreach (var client in clientlist)
                    {
                        if (client.IsValid && client.PawnIsAlive)
                        {
                            HumanizeClient(client);
                        }
                    }
                });
            }
            return HookResult.Continue; 
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            // Reset Client Status
            AddTimer(0.2f, () =>
            {
                // Reset Zombie Spawned here.
                ZombieSpawned = false;

                // avoiding zombie status glitch on human class like in zombie:reloaded
                List<CCSPlayerController> clientlist = Utilities.GetPlayers();

                // Reset Client Status
                foreach (var client in clientlist)
                {
                    // Reset Client Status.
                    ZombiePlayers[client.Slot].IsZombie = false;

                    // if they were chosen as motherzombie then let's make them not to get chosen again.
                    if (ZombiePlayers[client.Slot].MotherZombieStatus == MotherZombieFlags.CHOSEN)
                        ZombiePlayers[client.Slot].MotherZombieStatus = MotherZombieFlags.LAST;
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
                var hitgroup = @event.Hitgroup;

                if (IsClientZombie(attacker) && IsClientHuman(client) && string.Equals(weapon, "knife"))
                {
                    // Server.PrintToChatAll($"{client.PlayerName} Infected by {attacker.PlayerName}");
                    InfectClient(client, attacker);
                }

                KnockbackClient(client, attacker, dmgHealth, weapon, hitgroup);
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (ZombieSpawned)
            {
                var client = @event.Userid!;
                CheckGameStatus();
                RespawnPlayer(client); 
            }
            return HookResult.Continue;
        }

        public void RespawnPlayer(CCSPlayerController client)
        {
            if (ConfigSettings.RespawnTimer > 0.0f)
            {
                AddTimer(ConfigSettings.RespawnTimer, () =>
                {
                    // Respawn the client.
                    if (!client.PawnIsAlive)
                    {
                        var clientPawn = client.PlayerPawn.Value;
                        client.Respawn();
                        clientPawn.Respawn();
                    }
                });
            }
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
                    var spawnAngle = clientPawn.AbsRotation!;

                    // if zombie already spawned then they become zombie.
                    if (ZombieSpawned)
                    {
                        // Server.PrintToChatAll($"Infect {client.PlayerName} on Spawn.");
                        InfectClient(client);
                    }

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