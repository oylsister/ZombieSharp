﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;
using ZombieSharp.Plugin;
using ZombieSharp.Plugin.Events;
using ZombieSharp.Plugin.Listeners;

namespace ZombieSharp;

public class ZombieSharp : BasePlugin
{
    public override string ModuleName => "ZombieSharp";
    public override string ModuleVersion => "2.0.0";
    public override string ModuleAuthor => "Oylsister";
    public override string ModuleDescription => "Infection/survival style gameplay for CS2 in C#";

    private Infect? _infect;
    private Utils? _utils;
    private Hook? _hook;
    private Classes? _classes;
    private GameSettings? _settings;
    private Weapons? _weapons;
    private Knockback? _knockback;
    private readonly ILogger<ZombieSharp> _logger;

    public ZombieSharp(ILogger<ZombieSharp> logger)
    {
        _logger = logger;
    }

    public override void Load(bool hotReload)
    {
        PlayerData.ZombiePlayerData = new();
        PlayerData.PlayerClassesData = new();
        PlayerData.PlayerPurchaseCount = new();

        _classes = new Classes(this, _logger);
        _infect = new Infect(this, _logger, _classes);
        _utils = new Utils(this, _logger);
        _settings = new GameSettings(_logger);
        _weapons = new Weapons(this, _logger);
        _hook = new Hook(this, _weapons, _logger);

        this.RegisterAllListeners();
        this.RegisterAllEventHandlers();

        _knockback = new Knockback(_logger);

        if (hotReload)
        {
            _logger.LogWarning("[Load] The plugin is hotReloaded! This might cause instability to your server.");
            _settings?.GameSettingsOnMapStart();
            _classes?.ClassesOnMapStart();
            _weapons?.WeaponsOnMapStart();
        }

        Server.ExecuteCommand("sv_predictable_damage_tag_ticks 0");

        // initial
        _hook.HookOnLoad();
    }

    public override void Unload(bool hotReload)
    {
        PlayerData.ZombiePlayerData = null;
        PlayerData.PlayerClassesData = null;
        PlayerData.PlayerPurchaseCount = null;

        this.RemoveAllListeners();
        this.RegisterAllEventHandlers();
        _hook?.HookOnUnload();
    }

    public static string ConfigPath = Path.Combine(Application.RootDirectory, "configs/zombiesharp/");
}
