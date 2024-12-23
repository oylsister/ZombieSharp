using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;
using static CounterStrikeSharp.API.Core.Listeners;

namespace ZombieSharp.Plugin;

public class Events(ZombieSharp core, Infect infect, GameSettings settings, Classes classes, Weapons weapons, Teleport teleport, Respawn respawn, Napalm napalm, ConVars convar, ILogger<ZombieSharp> logger)
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

        _classes?.ClassesOnClientPutInServer(client);
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
    }

    // if reload a plugin this part won't be executed until you change map;
    public void OnMapStart(string mapname)
    {
        _settings.GameSettingsOnMapStart();
        _weapons.WeaponsOnMapStart();
        _classes.ClassesOnMapStart();
        _convar.ConVarOnLoad();
        _convar.ConVarExecuteOnMapStart(mapname);
        
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
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var client = @event.Userid;
        var attacker = @event.Attacker;
        var weapon = @event.Weapon;
        var dmgHealth = @event.DmgHealth;

        _infect.InfectOnPlayerHurt(client, attacker);
        Knockback.KnockbackClient(client, attacker, weapon, dmgHealth);
        _napalm.NapalmOnHurt(client, attacker, weapon, dmgHealth);
        _classes.ClassesOnPlayerHurt(client);

        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        // check the player count if there is any team that all dead.
        if(Infect.InfectHasStarted())
            Utils.CheckGameStatus();

        // play sound for zombie when killed by human.
        var client = @event.Userid;
        var attacker = @event.Attacker;

        if(client == null || attacker == null)
            return HookResult.Continue;

        if(Infect.IsClientHuman(attacker) && Infect.IsClientInfect(client))
            Utils.EmitSound(client, "zr.amb.zombie_die");

        _respawn.RespawnOnPlayerDeath(client);

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

        if(Infect.InfectHasStarted())
        {
            var team = GameSettings.Settings?.RespawTeam ?? 0;

            if(team == 0)
                _infect.InfectClient(client);

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
                    _infect.InfectClient(client);
            }
        }

        else
            _infect.HumanizeClient(client);

        // refresh purchase count here.
        Utils.RefreshPurchaseCount(client);
        _core.AddTimer(0.2f, () => _teleport.TeleportOnPlayerSpawn(client));

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
        return HookResult.Continue;
    }

    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Infect.InfectStarted = false;
        _infect.InfectKillInfectionTimer();
        return HookResult.Continue;
    }
}