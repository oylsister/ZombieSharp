using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;
using static CounterStrikeSharp.API.Core.Listeners;

namespace ZombieSharp.Plugin;

public class Events(ZombieSharp core, Infect infect, GameSettings settings, Classes classes, Weapons weapons, Teleport teleport, Respawn respawn, Napalm napalm, ConVars convar, HitGroup hitgroup, ILogger<ZombieSharp> logger)
{
    private readonly ZombieSharp _core = core;
    private readonly Infect _infect = infect;
    private readonly Classes _classes = classes;
    private readonly GameSettings _settings = settings;
    private readonly Weapons _weapons = weapons;
    private readonly ILogger<ZombieSharp> _logger = logger;
    private readonly Teleport _teleport = teleport;
    private readonly Napalm _napalm = napalm;
    private readonly Respawn _respawn = respawn;
    private readonly ConVars _convar = convar;
    private readonly HitGroup _hitgroup = hitgroup;

    public void EventOnLoad()
    {
        _core.RegisterListener<OnClientPutInServer>(OnClientPutInServer);
        _core.RegisterListener<OnClientDisconnect>(OnClientDisconnect);
        _core.RegisterListener<OnMapStart>(OnMapStart);
        _core.RegisterListener<OnServerPrecacheResources>(OnPrecahceResources);

        _core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        _core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        _core.RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
        _core.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        _core.RegisterEventHandler<EventCsPreRestart>(OnPreRoundStart);
        _core.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        _core.RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
        _core.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        _core.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        _core.RegisterEventHandler<EventCsPreRestart>(OnPreRestart);
    }

    public void EventOnUnload()
    {
        _core.RemoveListener<OnClientPutInServer>(OnClientPutInServer);
        _core.RemoveListener<OnClientDisconnect>(OnClientDisconnect);
        _core.RemoveListener<OnMapStart>(OnMapStart);
        _core.RemoveListener<OnServerPrecacheResources>(OnPrecahceResources);

        _core.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        _core.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        _core.DeregisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
        _core.DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        _core.DeregisterEventHandler<EventCsPreRestart>(OnPreRoundStart);
        _core.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        _core.DeregisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
        _core.DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
        _core.DeregisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
    }

    public void OnClientPutInServer(int playerslot)
    {
        var client = Utilities.GetPlayerFromSlot(playerslot);

        if(client == null)
            return;

        PlayerData.ZombiePlayerData?.Add(client, new());
        PlayerData.PlayerClassesData?.Add(client, new());
        PlayerData.PlayerPurchaseCount?.Add(client, new());
        PlayerData.PlayerSpawnData?.Add(client, new());
        PlayerData.PlayerBurnData?.Add(client, null);
        PlayerData.PlayerRegenData?.Add(client, null);

        _classes?.ClassesOnClientPutInServer(client);
        PlayerSound.PlayerSoundOnClientPutInServer(client);

        // Respawn.SpawnPlayer(client);
    }

    public void OnClientDisconnect(int playerslot)
    {
        var client = Utilities.GetPlayerFromSlot(playerslot);

        if(client == null)
            return;

        PlayerData.ZombiePlayerData?.Remove(client);
        PlayerData.PlayerClassesData?.Remove(client);
        PlayerData.PlayerPurchaseCount?.Remove(client);
        PlayerData.PlayerSpawnData?.Remove(client);
        PlayerData.PlayerBurnData?.Remove(client);
        HealthRegen.RegenOnClientDisconnect(client);
        PlayerSound.PlayerSoundOnClientDisconnect(client);
    }

    // if reload a plugin this part won't be executed until you change map;
    public void OnMapStart(string mapname)
    {
        _settings.GameSettingsOnMapStart();
        _classes.ClassesOnMapStart();
        _hitgroup.HitGroupOnMapStart();
        _convar.ConVarOnLoad();
        _convar.ConVarExecuteOnMapStart(mapname);
        _weapons.WeaponsOnMapStart();

        PlayerData.ZombiePlayerData?.Clear();
        PlayerData.PlayerClassesData?.Clear();
        PlayerData.PlayerPurchaseCount?.Clear();
        PlayerData.PlayerSpawnData?.Clear();
        PlayerData.PlayerBurnData?.Clear();
        PlayerData.PlayerRegenData?.Clear();
        PlayerData.PlayerSoundData?.Clear();
        
        Server.ExecuteCommand("sv_predictable_damage_tag_ticks 0");
        Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
        Server.ExecuteCommand("mp_give_player_c4 0");
    }

    public void OnPrecahceResources(ResourceManifest manifest)
    {
        if(Classes.ClassesConfig == null)
        {
            _logger.LogCritical("[OnPrecahceResources] The player classes config is null or not loaded yet!");
            return;
        }
    
        foreach(var classes in Classes.ClassesConfig.Values)
        {
            if(!string.IsNullOrEmpty(classes.Model))
            {
                if(classes.Model != "default")
                    manifest.AddResource(classes.Model!);
            }
        }

        manifest.AddResource("particles\\oylsister\\env_fire_large.vpcf");
        manifest.AddResource(GameSettings.Settings!.HumanWinOverlayMaterial);
        manifest.AddResource(GameSettings.Settings.HumanWinOverlayParticle);
        manifest.AddResource(GameSettings.Settings.ZombieWinOverlayMaterial);
        manifest.AddResource(GameSettings.Settings.ZombieWinOverlayParticle);
        manifest.AddResource("soundevents\\soundevents_zsharp.vsndevts");
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var client = @event.Userid;
        var attacker = @event.Attacker;
        var weapon = @event.Weapon;
        var dmgHealth = @event.DmgHealth;
        var hitgroups = @event.Hitgroup;

        if(client == null || attacker == null)
            return HookResult.Continue;

        //Server.PrintToChatAll($"[OnPlayerHurt] {client?.PlayerName} hurt by {attacker?.PlayerName} with {weapon} for {dmgHealth} hitgroup {hitgroups}.");

        _infect.InfectOnPlayerHurt(client, attacker);
        Knockback.KnockbackClient(client, attacker, weapon, dmgHealth, hitgroups);
        Utils.UpdatedPlayerCash(attacker, dmgHealth);
        _napalm.NapalmOnHurt(client, attacker, weapon, dmgHealth);
        _classes.ClassesOnPlayerHurt(client);

        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        // check the player count if there is any team that all dead.
        if(Infect.InfectHasStarted())
            RoundEnd.CheckGameStatus();

        // play sound for zombie when killed by human.
        var client = @event.Userid;

        if(client == null)
            return HookResult.Continue;

        if(Infect.IsClientInfect(client))
            ZombieSound.ZombieEmitSound(client, "zr.amb.zombie_die");

        _respawn.RespawnOnPlayerDeath(client);
        HealthRegen.RegenOnPlayerDeath(client);
        // RepeatKiller.OnPlayerDeath(client, @event.Weapon);

        return HookResult.Continue;
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var client = @event.Userid;

        if(client == null)
            return HookResult.Continue;

        // when player join server this automatically trigger so we have to prevent this so they can switch team later.
        if(client.Team == CsTeam.None || client.Team == CsTeam.Spectator)
            return HookResult.Continue;

        _classes.ClassesOnPlayerSpawn(client);

        if(Infect.IsTestMode)
        {
            _core.AddTimer(0.05f, () => _infect.HumanizeClient(client));
            return HookResult.Continue;
        }

        if(Infect.InfectHasStarted())
        {
            var team = GameSettings.Settings?.RespawTeam ?? 0;

             _core.AddTimer(0.05f, () =>  {
                if(team == 0)
                {
                    _infect.InfectClient(client);
                    _infect.InfectRespawn(client);
                }

                else if(team == 1)
                    _infect.HumanizeClient(client);

                // get the player team
                else
                {
                    // not zombie
                    if(!PlayerData.ZombiePlayerData?[client].Zombie ?? false)
                        _infect.HumanizeClient(client);

                    // human
                    else
                    {
                        _infect.InfectClient(client);
                        _infect.InfectRespawn(client);
                    }
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        else
            _infect.HumanizeClient(client);

        // refresh purchase count here.
        Utils.RefreshPurchaseCount(client);
        _core.AddTimer(0.2f, () => _teleport.TeleportOnPlayerSpawn(client), TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var client = @event.Userid;
        var team = @event.Team;
        var isBot = @event.Isbot;

        if(isBot)
            return HookResult.Continue;

        //Server.PrintToChatAll($"{client?.PlayerName} join team {team}.");

        if(!GameSettings.Settings?.AllowRespawnJoinLate ?? false)
            return HookResult.Continue;

        if(team > 1)
        {
            _core.AddTimer(5.0f, () => {
                if(client == null)
                {
                    //Server.PrintToChatAll("Client is fucking null!");
                    return;
                }

                // client.PlayerPawn.Value!.TeamNum = (byte)team;
                Respawn.RespawnClient(client);
            });
        }

        return HookResult.Continue;
    }

    public HookResult OnPreRoundStart(EventCsPreRestart @event, GameEventInfo info)
    {
        _infect.InfectOnPreRoundStart();
        return HookResult.Continue;
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _infect.InfectKillInfectionTimer();
        _respawn.RespawnTogglerSetup();
        _respawn.ToggleRespawn(true);
        RepeatKiller.RepeatKillerList.Clear();
        Utils.RemoveRoundObjective();
        RoundEnd.RoundEndOnRoundStart();
        Server.PrintToChatAll($" {_core.Localizer["Prefix"]} {_core.Localizer["Infect.GameInfo"]}");
        return HookResult.Continue;
    }

    public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
    {
        Infect.InfectStarted = false;
        _infect.InfectKillInfectionTimer();
        return HookResult.Continue;
    }

    public HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
    {
        _infect.InfectOnRoundFreezeEnd();
        RoundEnd.RoundEndOnRoundFreezeEnd();
        return HookResult.Continue;
    }

    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Infect.InfectStarted = false;
        _infect.InfectKillInfectionTimer();
        _infect.InfectOnRoundEnd();
        RoundEnd.RoundEndOnRoundEnd();
        return HookResult.Continue;
    }

    public HookResult OnPreRestart(EventCsPreRestart @event, GameEventInfo info)
    {
        Infect.InfectStarted = false;
        _infect.InfectOnPreRoundStart(false);
        _infect.InfectKillInfectionTimer();
        RoundEnd.RoundEndOnRoundEnd();
        return HookResult.Continue;
    }
}