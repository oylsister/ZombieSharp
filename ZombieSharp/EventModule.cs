using CounterStrikeSharp.API.Modules.Cvars;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public CounterStrikeSharp.API.Modules.Timers.Timer RoundTimer = null;

        bool ClassIsLoaded = false;

        public void EventInitialize()
        {
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            RegisterEventHandler<EventPlayerJump>(OnPlayerJump);
            RegisterEventHandler<EventCsPreRestart>(OnPreRestart);

            RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
            RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnected);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnServerPrecacheResources>(OnPrecacheResources);
        }

        // bot can only be initial here only
        private void OnClientPutInServer(int client)
        {
            var player = Utilities.GetPlayerFromSlot(client);

            if (!player.IsBot)
                return;

            InitialClientData(player);
        }

        private void InitialClientData(CCSPlayerController player)
        {
            int clientindex = player.Slot;

            ClientSpawnDatas.Add(clientindex, new ClientSpawnData());

            ZombiePlayerClass.ZombiePlayers.Add(clientindex, new ZombiePlayer());

            ZombiePlayerClass.ZombiePlayers[clientindex].IsZombie = false;
            ZombiePlayerClass.ZombiePlayers[clientindex].MotherZombieStatus = MotherZombieFlags.NONE;

            PlayerDeathTime.Add(clientindex, 0.0f);

            RegenTimer.Add(clientindex, null);

            PlayerSettingsOnPutInServer(player);

            WeaponOnClientPutInServer(clientindex);

            ClientProtected.Add(clientindex, new());

            TopDefenderOnPutInServer(player);
        }

        // Normal player will be hook here.
        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var client = @event.Userid;

            if (!client.IsBot)
                InitialClientData(client);

            PlayerSettingsAuthorized(client).Wait();
            return HookResult.Continue;
        }

        private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            if (!CVAR_RespawnLate.Value)
                return HookResult.Continue;

            var client = @event.Userid;
            var team = @event.Team;

            if (client.IsBot || !client.IsValid)
                return HookResult.Continue;

            if (team > (int)CsTeam.Spectator)
                AddTimer(1.0f, () => { RespawnClient(client); });

            return HookResult.Continue;
        }

        private void OnClientDisconnected(int client)
        {
            var player = Utilities.GetPlayerFromSlot(client);

            int clientindex = player.Slot;

            ClientSpawnDatas.Remove(clientindex);
            ZombiePlayerClass.ZombiePlayers.Remove(clientindex);
            ClientPlayerClass.Remove(clientindex);
            PlayerDeathTime.Remove(clientindex);

            RegenTimerStop(player);
            RegenTimer.Remove(clientindex);

            WeaponOnClientDisconnect(clientindex);

            ClientProtected.Remove(clientindex);

            TopDefenderOnDisconnect(player);
        }

        private void OnMapStart(string mapname)
        {
            WeaponInitialize();
            SettingsIntialize(mapname);
            ClassIsLoaded = PlayerClassIntialize();

            hitgroupLoad = HitGroupIntialize();
            RepeatKillerOnMapStart();

            Server.ExecuteCommand("mp_ignore_round_win_conditions 0");
        }

        private void OnPrecacheResources(ResourceManifest manifest)
        {
            if (ClassIsLoaded)
                PrecachePlayerModel(manifest);
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            RemoveRoundObjective();
            RespawnTogglerSetup();

            Server.PrintToChatAll($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} The current game mode is the Human vs. Zombie, the zombie goal is to infect all human before time is running out.");

            bool warmup = GetGameRules().WarmupPeriod;

            if (!warmup)
                Server.ExecuteCommand("mp_ignore_round_win_conditions 1");

            else
                Server.ExecuteCommand("mp_ignore_round_win_conditions 0");

            return HookResult.Continue;
        }

        private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
        {
            bool warmup = GetGameRules().WarmupPeriod;

            if (warmup && !CVAR_EnableOnWarmup.Value)
                Server.PrintToChatAll($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} The current server has disabled infection in warmup round.");

            if (!warmup || CVAR_EnableOnWarmup.Value)
            {
                if (!warmup)
                {
                    var roundtimeCvar = ConVar.Find("mp_roundtime");
                    RoundTimer = AddTimer(roundtimeCvar.GetPrimitiveValue<float>() * 60f, TerminateRoundTimeOut);
                }

                InfectOnRoundFreezeEnd();
            }

            return HookResult.Continue;
        }

        private HookResult OnPreRestart(EventCsPreRestart @event, GameEventInfo info)
        {
            bool warmup = GetGameRules().WarmupPeriod;

            if (!warmup || CVAR_EnableOnWarmup.Value)
            {
                ToggleRespawn(true, true);

                AddTimer(0.1f, () =>
                {
                    List<CCSPlayerController> clientlist = Utilities.GetPlayers();

                    foreach (var client in clientlist)
                    {
                        if (client.IsValid && IsPlayerAlive(client))
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
            bool warmup = GetGameRules().WarmupPeriod;

            if (RoundTimer != null)
                RoundTimer.Kill();

            TopDenfederOnRoundEnd();

            if (!warmup || CVAR_EnableOnWarmup.Value)
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
                        if (!client.IsValid)
                            continue;

                        // Reset Client Status.
                        ZombiePlayerClass.ZombiePlayers[client.Slot].IsZombie = false;

                        // if they were chosen as motherzombie then let's make them not to get chosen again.
                        if (ZombiePlayerClass.ZombiePlayers[client.Slot].MotherZombieStatus == MotherZombieFlags.CHOSEN)
                            ZombiePlayerClass.ZombiePlayers[client.Slot].MotherZombieStatus = MotherZombieFlags.LAST;
                    }
                });
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            if(@event.Userid.Slot == 32766)
                return HookResult.Continue;

            if (ZombieSpawned)
            {
                var client = @event.Userid;
                var attacker = @event.Attacker;

                var weapon = @event.Weapon;
                var dmgHealth = @event.DmgHealth;
                var hitgroup = @event.Hitgroup;

                if (!attacker.IsValid || !client.IsValid)
                    return HookResult.Continue;

                if (ZombiePlayerClass.IsClientZombie(attacker) && ZombiePlayerClass.IsClientHuman(client) && string.Equals(weapon, "knife") && !ClientProtected[client.Slot].Protected)
                {
                    // Server.PrintToChatAll($"{client.PlayerName} Infected by {attacker.PlayerName}");
                    InfectClient(client, attacker);
                }

                if (ZombiePlayerClass.IsClientZombie(client))
                {
                    if (CVAR_CashOnDamage.Value)
                        DamageCash(attacker, dmgHealth);

                    FindWeaponItemDefinition(attacker.PlayerPawn.Value.WeaponServices.ActiveWeapon, weapon);

                    KnockbackClient(client, attacker, dmgHealth, weapon, hitgroup);
                    TopDefenderOnPlayerHurt(attacker, dmgHealth);
                }
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (@event.Userid.Slot == 32766)
                return HookResult.Continue;

            var client = @event.Userid;
            var attacker = @event.Attacker;
            var weapon = @event.Weapon;

            client.PawnIsAlive = false;

            if (ZombieSpawned)
            {
                CheckGameStatus();

                if (RespawnEnable)
                {
                    RespawnPlayer(client);
                    RepeatKillerOnPlayerDeath(client, attacker, weapon);
                }

                RegenTimerStop(client);
            }

            return HookResult.Continue;
        }

        public void RespawnPlayer(CCSPlayerController client)
        {
            if (CVAR_RespawnTimer.Value > 0.0f)
            {
                AddTimer(CVAR_RespawnTimer.Value, () =>
                {
                    if (CVAR_RespawnProtect.Value && CVAR_RespawnTeam.Value == 1)
                        ClientProtected[client.Slot].Protected = true;

                    // Server.PrintToChatAll($"Player {client.PlayerName} should be respawn here.");
                    RespawnClient(client);
                });
            }
        }

        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var client = @event.Userid;

            bool warmup = GetGameRules().WarmupPeriod;

            if (!warmup || CVAR_EnableOnWarmup.Value)
            {
                AddTimer(0.2f, () =>
                {
                    WeaponOnPlayerSpawn(client.Slot);

                    // if zombie already spawned then they become zombie.
                    if (ZombieSpawned)
                    {
                        // Server.PrintToChatAll($"Infect {client.PlayerName} on Spawn.");
                        if (CVAR_RespawnTeam.Value == 0)
                            InfectClient(client, null, false, false, true);

                        else if (CVAR_RespawnTeam.Value == 1)
                            HumanizeClient(client);

                        if (ClientProtected[client.Slot].Protected)
                        {
                            AddTimer(CVAR_RespawnProtectTime.Value, () => { ResetProtectedClient(client); });
                            RespawnProtectClient(client);
                        }
                    }

                    // else they're human!
                    else
                        HumanizeClient(client);

                    var clientPawn = client.PlayerPawn.Value;
                    var spawnPos = clientPawn.AbsOrigin!;
                    var spawnAngle = clientPawn.AbsRotation!;

                    ZTele_GetClientSpawnPoint(client, spawnPos, spawnAngle);
                });
            }

            return HookResult.Continue;
        }

        public HookResult OnPlayerJump(EventPlayerJump @event, GameEventInfo info)
        {
            var client = @event.Userid;

            var warmup = GetGameRules().WarmupPeriod;

            if (!warmup || CVAR_EnableOnWarmup.Value)
                JumpBoost(client);

            return HookResult.Continue;
        }

        public void JumpBoost(CCSPlayerController client)
        {
            var classData = PlayerClassDatas.PlayerClasses;
            var activeclass = ClientPlayerClass[client.Slot].ActiveClass;

            if (!GetGameRules().WarmupPeriod || CVAR_EnableOnWarmup.Value)
            {
                // if jump boost can apply after client is already jump.
                AddTimer(0.0f, () =>
                {
                    if (activeclass == null)
                    {
                        if (ZombiePlayerClass.IsClientHuman(client))
                            activeclass = CVAR_Human_Default.Value;

                        else
                            activeclass = CVAR_Zombie_Default.Value;
                    }

                    if (classData.ContainsKey(activeclass))
                    {
                        client.PlayerPawn.Value.AbsVelocity.X *= classData[activeclass].Jump_Distance;
                        client.PlayerPawn.Value.AbsVelocity.Y *= classData[activeclass].Jump_Distance;
                        client.PlayerPawn.Value.AbsVelocity.Z *= classData[activeclass].Jump_Height;
                    }
                });
            }
        }

        private void DamageCash(CCSPlayerController client, int dmgHealth)
        {
            var money = client.InGameMoneyServices.Account;
            client.InGameMoneyServices.Account = money + dmgHealth;
            Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
        }

        private void ResetProtectedClient(CCSPlayerController client)
        {
            if (!client.IsValid)
                return;

            ClientProtected[client.Slot].Protected = false;
            RespawnProtectClient(client, true);
        }

        private void TerminateRoundTimeOut()
        {
            int team = CVAR_TimeoutWinner.Value;

            CCSGameRules gameRules = GetGameRules();

            if (team == 2)
            {
                gameRules.TerminateRound(5f, RoundEndReason.TerroristsWin);
            }

            else if (team == 3)
            {
                gameRules.TerminateRound(5f, RoundEndReason.CTsWin);
            }

            else
            {
                gameRules.TerminateRound(5f, RoundEndReason.RoundDraw);
            }
        }

        private void RemoveRoundObjective()
        {
            var objectivelist = new List<string>() { "func_bomb_target", "func_hostage_rescue", "hostage_entity", "c4" };

            foreach (string objectivename in objectivelist)
            {
                var entityIndex = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>(objectivename);

                foreach (var entity in entityIndex)
                {
                    entity.Remove();
                }
            }
        }
    }
}