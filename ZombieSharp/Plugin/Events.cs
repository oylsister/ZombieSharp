using System.Xml.Schema;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;
using static CounterStrikeSharp.API.Core.Listeners;

namespace ZombieSharp.Plugin;

public class Events
{
    private readonly ZombieSharp _core;
    private readonly Infect _infect;
    private readonly Classes _classes;
    private readonly GameSettings _settings;
    private readonly Weapons _weapons;
    private readonly ILogger<ZombieSharp> _logger;

    public Events(ZombieSharp core, Infect infect, GameSettings settings, Classes classes, Weapons weapons, ILogger<ZombieSharp> logger)
    {
        _core = core;
        _infect = infect;
        _settings = settings;
        _classes = classes;
        _weapons = weapons;
        _logger = logger;
    }

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

        _classes?.ClassesOnClientPutInServer(client);
    }

    public void OnClientDisconnect(int playerslot)
    {
        var client = Utilities.GetPlayerFromSlot(playerslot);

        if(client == null)
            return;

        PlayerData.ZombiePlayerData?.Remove(client);
        PlayerData.PlayerClassesData?.Remove(client);
    }

    // if reload a plugin this part won't be executed until you change map;
    public void OnMapStart(string mapname)
    {
        _settings.GameSettingsOnMapStart();
        _classes.ClassesOnMapStart();
        _weapons.WeaponsOnMapStart();
        Server.ExecuteCommand("sv_predictable_damage_tag_ticks 0");
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
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var client = @event.Userid;
        var attacker = @event.Attacker;
        var weapon = @event.Weapon;
        var dmgHealth = @event.DmgHealth;

        _infect.InfectOnPlayerHurt(client, attacker);
        Knockback.KnockbackClient(client, attacker, weapon, dmgHealth);
        _classes.ClassesOnPlayerHurt(client);

        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        // check the player count if there is any team that all dead.
        Utils.CheckGameStatus();

        // play sound for zombie when killed by human.
        var client = @event.Userid;
        var attacker = @event.Attacker;

        if(client == null || attacker == null)
            return HookResult.Continue;

        if(Infect.IsClientHuman(attacker) && Infect.IsClientInfect(client))
            Utils.EmitSound(client, "zr.amb.zombie_die");

        return HookResult.Continue;
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var client = @event.Userid;

        if(client == null)
            return HookResult.Continue;

        if(Infect.InfectHasStarted())
            _infect.InfectClient(client);

        else
            _infect.HumanizeClient(client);

        return HookResult.Continue;
    }

    public HookResult OnPreRoundStart(EventCsPreRestart @event, GameEventInfo info)
    {
        _infect.InfectOnPreRoundStart();
        return HookResult.Continue;
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
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
        return HookResult.Continue;
    }
}