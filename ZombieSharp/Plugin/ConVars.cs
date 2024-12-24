using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class ConVars(ZombieSharp core, Weapons weapons, ILogger<ZombieSharp> logger)
{
    private ZombieSharp _core = core;
    private readonly Weapons _weapon = weapons;
    private readonly ILogger<ZombieSharp> _logger = logger;

    public void ConVarOnLoad()
    {
        if(GameSettings.Settings == null)
        {
            _logger.LogError("[ConVarOnLoad] Game Settings is null! ConVar will not proceed any longer!");
            return;
        }

        // we hook convar changed and apply it to our GameSettings.
        _core.CVAR_FirstInfectionTimer.ValueChanged += (sender, value) => {
            GameSettings.Settings.FirstInfectionTimer = value;
        };

        _core.CVAR_MotherZombieRatio.ValueChanged += (sender, value) => {
            GameSettings.Settings.MotherZombieRatio = value;
        };

        _core.CVAR_MotherZombieTeleport.ValueChanged += (sender, value) => {
            GameSettings.Settings.MotherZombieTeleport = value;
        };

        _core.CVAR_CashOnDamage.ValueChanged += (sender, value) => {
            GameSettings.Settings.CashOnDamage = value;
        };

        // class section
        _core.CVAR_DefaultHuman.ValueChanged += (sender, value) => {
            GameSettings.Settings.DefaultHumanBuffer = value;

            if(!Classes.ClassesConfig!.ContainsKey(value))
            {
                _logger.LogCritical("[ConVarOnLoad] Couldn't get classes \"{0}\" from playerclasses.jsonc", value);
                return;
            }

            Classes.DefaultHuman = Classes.ClassesConfig[value];
        };

        _core.CVAR_DefaultZombie.ValueChanged += (sender, value) => {
            GameSettings.Settings.DefaultZombieBuffer = value;

            if(!Classes.ClassesConfig!.ContainsKey(value))
            {
                _logger.LogCritical("[ConVarOnLoad] Couldn't get classes \"{0}\" from playerclasses.jsonc", value);
                return;
            }

            Classes.DefaultZombie = Classes.ClassesConfig[value];
        };

        _core.CVAR_MotherZombie.ValueChanged += (sender, value) => {
            GameSettings.Settings.MotherZombieBuffer = value;

            if(!Classes.ClassesConfig!.ContainsKey(value))
            {
                _logger.LogCritical("[ConVarOnLoad] Couldn't get classes \"{0}\" from playerclasses.jsonc", value);
                return;
            }

            Classes.MotherZombie = Classes.ClassesConfig[value];
        };

        _core.CVAR_RandomClassesOnConnect.ValueChanged += (sender, value) => {
            GameSettings.Settings.RandomClassesOnConnect = value;
        };

        _core.CVAR_RandomClassesOnSpawn.ValueChanged += (sender, value) => {
            GameSettings.Settings.RandomClassesOnSpawn = value;
        };

        _core.CVAR_AllowSavingClass.ValueChanged += (sender, value) => {
            GameSettings.Settings.AllowSavingClass = value;
        };

        _core.CVAR_AllowChangeClass.ValueChanged += (sender, value) => {
            GameSettings.Settings.AllowChangeClass = value;
        };

        // weapon section
        _core.CVAR_WeaponPurchaseEnable.ValueChanged += (sender, value) => {
            GameSettings.Settings.WeaponPurchaseEnable = value;
            _logger.LogInformation("[ConVarChanged] zs_weapon_purchase_enable changed to {0}", value);
            _weapon.IntialWeaponPurchaseCommand();
        };

        _core.CVAR_WeaponRestrictEnable.ValueChanged += (sender, value) => {
            GameSettings.Settings.WeaponRestrictEnable = value;
        };

        _core.CVAR_WeaponBuyZoneOnly.ValueChanged += (sender, value) => {
            GameSettings.Settings.WeaponBuyZoneOnly = value;
        };

        // Teleport
        _core.CVAR_TeleportAllow.ValueChanged += (sender, value) => {
            GameSettings.Settings.TeleportAllow = value;
        };

        // respawn
        _core.CVAR_RespawnEnable.ValueChanged += (sender, value) => {
            GameSettings.Settings.RespawnEnable = value;

            _logger.LogInformation("[ConVarChanged] zs_respawn_enable changed to {0}", value);
            Server.PrintToChatAll($" {_core.Localizer["Prefix"]} zs_respawn_enable changed to {value}");

            _core.AddTimer(1.0f, () => {
                foreach(var player in Utilities.GetPlayers())
                {
                    if(player == null)
                        continue;

                    if(Utils.IsPlayerAlive(player))
                        continue;

                    if(player.Team == CsTeam.None || player.Team == CsTeam.Spectator)
                        continue;

                    Respawn.RespawnClient(player);
                }
            });

        };
        _core.CVAR_RespawnDelay.ValueChanged += (sender, value) => {
            GameSettings.Settings.RespawnDelay = value;
        };
        _core.CVAR_AllowRespawnJoinLate.ValueChanged += (sender, value) => {
            GameSettings.Settings.AllowRespawnJoinLate = value;
        };
        _core.CVAR_RespawnTeam.ValueChanged += (sender, value) => {
            GameSettings.Settings.RespawTeam = value;
        };

        // create convar first.
        _core.RegisterFakeConVars(typeof(ConVar));
    }

    public void ConVarExecuteOnMapStart(string mapname)
    {
        CreateExecuteFile();

        Server.ExecuteCommand("exec zombiesharp/zombiesharp.cfg");

        var configFolder = Path.Combine(Server.GameDirectory, "csgo/cfg/zombiesharp/");
        var mapConfig = Path.Combine(configFolder, mapname + ".cfg");

        if (File.Exists(mapConfig))
        {
            _logger.LogInformation("[ConVarOnMapStart] Found Map cfg file loading {0}", mapConfig);
            Server.ExecuteCommand($"exec zombiesharp/{mapname}.cfg");
        }
    }

    public void CreateExecuteFile()
    {
        var configFolder = Path.Combine(Server.GameDirectory, "csgo/cfg/zombiesharp/");

        if (!Directory.Exists(configFolder))
        {
            _logger.LogInformation("[CreateExecuteFile] Couldn't find directory, so proceed creating {0}", configFolder);
            Directory.CreateDirectory(configFolder);
        }

        var configPath = Path.Combine(configFolder, "zombiesharp.cfg");

        if (File.Exists(configPath))
            return;

        
        _logger.LogInformation("[CreateExecuteFile] Creating {0}", configPath);

        var configFile = File.CreateText(configPath);

        configFile.WriteLine($"// This file is generated by ZombieSharp.dll at {DateTime.Today}");
        configFile.WriteLine();

        // hard code but try creating the list of <FakeConVar<T>> is not possible with me, so I leave that method to my ruskie friend.
        CreateConVarLine(configFile, _core.CVAR_FirstInfectionTimer);
        CreateConVarLine(configFile, _core.CVAR_MotherZombieRatio);
        CreateConVarLine(configFile, _core.CVAR_MotherZombieTeleport);
        CreateConVarLine(configFile, _core.CVAR_CashOnDamage);

        CreateConVarLine(configFile, _core.CVAR_DefaultHuman);
        CreateConVarLine(configFile, _core.CVAR_DefaultZombie);
        CreateConVarLine(configFile, _core.CVAR_MotherZombie);
        CreateConVarLine(configFile, _core.CVAR_RandomClassesOnConnect);
        CreateConVarLine(configFile, _core.CVAR_RandomClassesOnSpawn);
        CreateConVarLine(configFile, _core.CVAR_AllowSavingClass);
        CreateConVarLine(configFile, _core.CVAR_AllowChangeClass);

        CreateConVarLine(configFile, _core.CVAR_WeaponPurchaseEnable);
        CreateConVarLine(configFile, _core.CVAR_WeaponRestrictEnable);
        CreateConVarLine(configFile, _core.CVAR_WeaponBuyZoneOnly);

        CreateConVarLine(configFile, _core.CVAR_TeleportAllow);

        CreateConVarLine(configFile, _core.CVAR_RespawnEnable);
        CreateConVarLine(configFile, _core.CVAR_RespawnDelay);
        CreateConVarLine(configFile, _core.CVAR_AllowRespawnJoinLate);
        CreateConVarLine(configFile, _core.CVAR_RespawnTeam);

        configFile.Close();
    }
    
    public void CreateConVarLine<T>(StreamWriter configFile, FakeConVar<T> fakeConVar) where T : IComparable<T>
    {
        if(configFile == null)
        {
            _logger.LogCritical("[CreateConVarLine] Config File is null");
            return;
        }

        var command = fakeConVar.Name;
        var value = fakeConVar.Value;
        var description = fakeConVar.Description;

        configFile.WriteLine($"// {description}");
        configFile.WriteLine($"// -");
        configFile.WriteLine($"// Default: {value}");
        configFile.WriteLine($"{command} {value}");
        // empty file.
        configFile.WriteLine();
    }
}