using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ZombieSharp.Database;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class Classes(ZombieSharp core, DatabaseMain database, ILogger<ZombieSharp> logger)
{
    private readonly ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;
    private readonly DatabaseMain _database = database;
    public static Dictionary<string, ClassAttribute>? ClassesConfig = [];

    public static ClassAttribute? DefaultHuman = null;
    public static ClassAttribute? DefaultZombie = null;
    public static ClassAttribute? MotherZombie = null;

    public void ClassesOnLoad()
    {
        _core.AddCommand("css_zclass", "Select Class Menu Command", ClassesMainMenuCommand);
    }

    public void ClassesOnMapStart()
    {
        // make sure this one is null.
        ClassesConfig = null;

        // initial class config data.
        ClassesConfig = new Dictionary<string, ClassAttribute>();

        var configPath = Path.Combine(ZombieSharp.ConfigPath, "playerclasses.jsonc");

        if(!File.Exists(configPath))
        {
            _logger.LogCritical("[ClassesOnMapStart] Couldn't find a playerclasses.jsonc file!");
            return;
        }

        _logger.LogInformation("[ClassesOnMapStart] Loading Player Classes file.");

        // we get data from jsonc file.
        ClassesConfig = JsonConvert.DeserializeObject<Dictionary<string, ClassAttribute>>(File.ReadAllText(configPath));

        // settings is loaded before classes so we can get default human and zombie.
        if(GameSettings.Settings != null)
        {
            if(!string.IsNullOrEmpty(GameSettings.Settings.DefaultHumanBuffer))
            {
                var uniqueName = GameSettings.Settings.DefaultHumanBuffer;

                if(!ClassesConfig!.ContainsKey(uniqueName))
                {
                    _logger.LogCritical("[ClassesOnMapStart] Couldn't get classes \"{0}\" from playerclasses.jsonc", uniqueName);
                }

                DefaultHuman = ClassesConfig[uniqueName];

                _logger.LogInformation("[ClassesOnMapStart] Default human class: {0}", DefaultHuman.Name);
            }

            if(!string.IsNullOrEmpty(GameSettings.Settings.DefaultZombieBuffer))
            {
                var uniqueName = GameSettings.Settings.DefaultZombieBuffer;

                if(!ClassesConfig!.ContainsKey(uniqueName))
                {
                    _logger.LogCritical("[ClassesOnMapStart] Couldn't get classes \"{0}\" from playerclasses.jsonc", uniqueName);
                }

                DefaultZombie = ClassesConfig[uniqueName];

                _logger.LogInformation("[ClassesOnMapStart] Default zombie class: {0}", DefaultZombie.Name);
            }

            if(!string.IsNullOrEmpty(GameSettings.Settings.MotherZombieBuffer))
            {
                var uniqueName = GameSettings.Settings.MotherZombieBuffer;

                if(!ClassesConfig!.ContainsKey(uniqueName))
                {
                    _logger.LogCritical("[ClassesOnMapStart] Couldn't get classes \"{0}\" from playerclasses.jsonc", uniqueName);
                }

                MotherZombie = ClassesConfig[uniqueName];

                _logger.LogInformation("[ClassesOnMapStart] Mother zombie class: {0}", MotherZombie.Name);
            }

            if(DefaultHuman == null)
            {
                _logger.LogCritical("[ClassesOnMapStart] Human class from GameSettings is empty or null!");
            }

            if(DefaultZombie == null)
            {
                _logger.LogCritical("[ClassesOnMapStart] Zombie class from GameSettings is empty or null!");
            }

            if(MotherZombie == null)
            {
                _logger.LogInformation("[ClassesOnMapStart] Mother Zombie class not configured, will use selected zombie class for mother zombies.");
            }
        }
    } 

    public void ClassesOnClientPutInServer(CCSPlayerController client)
    {
        if(DefaultHuman == null || DefaultZombie == null)
        {
            _logger.LogError("[ClassesOnClientPutInServer] Default class is null!");
        }

        if(PlayerData.PlayerClassesData == null)
        {
            _logger.LogError("[ClassesOnClientPutInServer] PlayerClassesData is null!");
            return;
        }

        if(GameSettings.Settings?.RandomClassesOnConnect ?? false)
        {
            PlayerData.PlayerClassesData[client].HumanClass = Utils.GetRandomPlayerClasses(1);
            PlayerData.PlayerClassesData[client].ZombieClass = Utils.GetRandomPlayerClasses(0);
        }

        else
        {
            PlayerData.PlayerClassesData[client].HumanClass = DefaultHuman;
            PlayerData.PlayerClassesData[client].ZombieClass = DefaultZombie;

            if(!client.IsBot)
            {
                var steamid = client.AuthorizedSteamID?.SteamId64;
                //_logger.LogInformation("[ClassesOnClientPutInServer] client {0} join with steamid: {1}", client.PlayerName, steamid);

                if(steamid == null || !steamid.HasValue)
                {
                    _logger.LogError("[ClassesOnClientPutInServer] client {0} steam id is null!", client.PlayerName);
                    return;
                }

                Task.Run(async () => 
                { 
                    _logger.LogInformation("[ClassesOnClientPutInServer] Loading player data for SteamID {0}", steamid.Value);
                    var data = await _database.GetPlayerClassData(steamid.Value);

                    if(data == null)
                    {
                        await _database.InsertPlayerClassData(steamid.Value, PlayerData.PlayerClassesData[client]);
                        _logger.LogInformation("[ClassesOnClientPutInServer] Creating new data entry for SteamID {0}", steamid.Value);
                    }

                    else
                    {
                        if(data.HumanClass?.Enable ?? false)
                            PlayerData.PlayerClassesData[client].HumanClass = data.HumanClass;

                        if(data.ZombieClass?.Enable ?? false)
                            PlayerData.PlayerClassesData[client].ZombieClass = data.ZombieClass;
                    }
                });
            }
        }

        if(PlayerData.PlayerClassesData?[client].HumanClass == null)
            _logger.LogWarning("[ClassesOnClientPutInServer] Human class is null for {0}", client.PlayerName);

        if(PlayerData.PlayerClassesData?[client].ZombieClass == null)
            _logger.LogWarning("[ClassesOnClientPutInServer] Zombie class is null for {0}", client.PlayerName);
    }

    public void ClassesApplyToPlayer(CCSPlayerController client, ClassAttribute? data)
    {
        if(client == null)
        {
            _logger.LogError("[ClassesApplyToPlayer] Player is null!");
            return;
        }

        if(data == null)
        {
            _logger.LogError("[ClassesApplyToPlayer] Class data is null!");
            return;
        }

        if(!Utils.IsPlayerAlive(client))
        {
            _logger.LogError("[ClassesApplyToPlayer] Player {name} is not alive!", client.PlayerName);
            return;
        }

        var playerPawn = client.PlayerPawn.Value;

        if(playerPawn == null || !playerPawn.IsValid)
        {
            _logger.LogError("[ClassesApplyToPlayer] Player Pawn is null or invalid!");
            return;
        }

        // Apply model changes immediately
        Server.NextWorldUpdate(() => 
        {
            if(!Utils.IsPlayerAlive(client) || playerPawn == null || !playerPawn.IsValid)
                return;

            // Apply model if specified
            if(!string.IsNullOrEmpty(data.Model))
            {
                try
                {
                    if(data.Model != "default")
                    {
                        playerPawn.SetModel(data.Model);
                        _logger.LogDebug("[ClassesApplyToPlayer] Applied model {model} to {name}", data.Model, client.PlayerName);
                    }
                    else
                    {
                        // Use default CS2 models
                        if(data.Team == 0) // Zombie team
                            playerPawn.SetModel("characters/models/tm_phoenix/tm_phoenix.vmdl");
                        else // Human team
                            playerPawn.SetModel("characters/models/ctm_sas/ctm_sas.vmdl");
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError("[ClassesApplyToPlayer] Failed to set model for {name}: {error}", client.PlayerName, ex.Message);
                }
            }

            // Remove armor for zombies immediately
            if(data.Team == 0)
            {
                playerPawn.ArmorValue = 0;
                client.PawnHasHelmet = false;
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_ArmorValue");
            }
        });

        // Apply health, armor, and regeneration with a delay to ensure proper application
        _core.AddTimer(0.1f, () => 
        {
            if(!Utils.IsPlayerAlive(client) || playerPawn == null || !playerPawn.IsValid)
                return;

            try
            {
                // Set health
                playerPawn.Health = data.Health;
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
                
                // Double-check armor removal for zombies
                if(data.Team == 0)
                {
                    playerPawn.ArmorValue = 0;
                    client.PawnHasHelmet = false;
                    Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_ArmorValue");
                }

                // Apply regeneration settings
                HealthRegen.RegenOnApplyClass(client, data);
                
                _logger.LogDebug("[ClassesApplyToPlayer] Applied health ({health}) to {name}", data.Health, client.PlayerName);
            }
            catch(Exception ex)
            {
                _logger.LogError("[ClassesApplyToPlayer] Failed to apply health/armor to {name}: {error}", client.PlayerName, ex.Message);
            }
        });

        // Apply speed with multiple attempts to ensure it sticks
        var speedApplyAttempts = 0;
        var maxSpeedAttempts = 3;
        
        Action applySpeed = null!;
        applySpeed = () => 
        {
            if(!Utils.IsPlayerAlive(client) || playerPawn == null || !playerPawn.IsValid)
                return;

            try
            {
                var targetSpeed = data.Speed / 250f;
                playerPawn.VelocityModifier = targetSpeed;
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_flVelocityModifier");
                
                _logger.LogDebug("[ClassesApplyToPlayer] Applied speed ({speed}) to {name} (attempt {attempt})", 
                    data.Speed, client.PlayerName, speedApplyAttempts + 1);
                
                // Verify speed was applied correctly and retry if needed
                _core.AddTimer(0.1f, () => 
                {
                    if(Utils.IsPlayerAlive(client) && playerPawn != null && playerPawn.IsValid)
                    {
                        var currentSpeed = playerPawn.VelocityModifier;
                        if(Math.Abs(currentSpeed - targetSpeed) > 0.01f && speedApplyAttempts < maxSpeedAttempts - 1)
                        {
                            speedApplyAttempts++;
                            _logger.LogWarning("[ClassesApplyToPlayer] Speed application retry for {name} - expected: {expected}, actual: {actual}", 
                                client.PlayerName, targetSpeed, currentSpeed);
                            applySpeed();
                        }
                    }
                });
            }
            catch(Exception ex)
            {
                _logger.LogError("[ClassesApplyToPlayer] Failed to apply speed to {name}: {error}", client.PlayerName, ex.Message);
            }
        };

        // Initial speed application
        _core.AddTimer(0.05f, applySpeed);

        // Set active player data
        if(PlayerData.PlayerClassesData != null && PlayerData.PlayerClassesData.ContainsKey(client))
        {
            PlayerData.PlayerClassesData[client].ActiveClass = data;
        }

        _logger.LogInformation("[ClassesApplyToPlayer] Successfully applied class {className} to {playerName}", 
            data.Name ?? "Unknown", client.PlayerName);
    }

    public void ClassesOnPlayerSpawn(CCSPlayerController? client)
    {
        if(client == null)
            return;
            
        if(PlayerData.PlayerClassesData == null)
        {
            return;
        }

        if(!PlayerData.PlayerClassesData!.ContainsKey(client))
        {
            return;
        }

        if(GameSettings.Settings?.RandomClassesOnSpawn ?? false)
        {
            PlayerData.PlayerClassesData[client].HumanClass = Utils.GetRandomPlayerClasses(1);
            PlayerData.PlayerClassesData[client].ZombieClass = Utils.GetRandomPlayerClasses(0);
        }
    }

    public void ClassesOnPlayerHurt(CCSPlayerController? client)
    {
        _core?.AddTimer(0.5f, () => 
        {
            // need to be alive
            if(client == null || client.Handle == IntPtr.Zero)
                return;

            if(!Utils.IsPlayerAlive(client))
                return;

            // prevent error obviously.
            if(!PlayerData.PlayerClassesData!.ContainsKey(client))
            {
                return;
            }

            if(PlayerData.PlayerClassesData?[client].ActiveClass == null)
                return;

            // if speed is equal to 250 then we don't need this.
            if(PlayerData.PlayerClassesData?[client].ActiveClass?.Speed == 250f)
                return;

            // set speed back to velomodify
            client.PlayerPawn.Value!.VelocityModifier = PlayerData.PlayerClassesData![client].ActiveClass!.Speed / 250f;
        });
    }

    // CLASSES MENU
    [CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
    public void ClassesMainMenuCommand(CCSPlayerController? client, CommandInfo? info)
    {
        if(client == null)
            return;

        if(!GameSettings.Settings?.AllowChangeClass ?? true)
        {
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.FeatureDisbled"]}");
            return;
        }

        if(!PlayerData.PlayerClassesData?.ContainsKey(client) ?? false)
        {
            _logger.LogError("[ClassesMenuCommand] {0} is not in PlayerClassesData!", client.PlayerName);
            return;
        }

        var menu = new ChatMenu($" {_core.Localizer["Prefix"]} {_core.Localizer["Classes.MainMenu"]}");
        menu.AddMenuOption(_core.Localizer["Classes.MainMenu.Zombie"], (client, option) => ClassesSelectMenu(client, 0));
        menu.AddMenuOption(_core.Localizer["Classes.MainMenu.Human"], (client, option) => ClassesSelectMenu(client, 1));
        menu.ExitButton = true;
        MenuManager.OpenChatMenu(client, menu);
    }

    public void ClassesSelectMenu(CCSPlayerController client, int team)
    {
        if(ClassesConfig == null)
        {
            _logger.LogError("[ClassesSelectMenu] ClassesConfig is null!");
            return;
        }

        string title;

        if (team == 0)
            title = $" {_core.Localizer["Prefix"]} {_core.Localizer["Classes.Select.Zombie"]}";

        else
            title = $" {_core.Localizer["Prefix"]} {_core.Localizer["Classes.Select.Human"]}";

        var selectmenu = new ChatMenu(title);
        var menuhandle = (CCSPlayerController client, ChatMenuOption option) =>
        {
            if (option.Text == "Back")
            {
                ClassesMainMenuCommand(client, null);
                return;
            }

            if (team == 0)
                PlayerData.PlayerClassesData![client].ZombieClass = ClassesConfig.FirstOrDefault(x => x.Value.Name == option.Text).Value;

            else
                PlayerData.PlayerClassesData![client].HumanClass = ClassesConfig.FirstOrDefault(x => x.Value.Name == option.Text).Value;

            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Classes.SelectSuccess"]}");
            MenuManager.CloseActiveMenu(client);

            // update their player class into database
            if(GameSettings.Settings?.AllowSavingClass ?? true)
            {
                var steamid = client.AuthorizedSteamID?.SteamId64;

                if(steamid == null || !steamid.HasValue)
                {
                    _logger.LogError("[ClassesMenuCommand] SteamID of {0} is null!", client.PlayerName);
                    return;
                }

                Task.Run(async () => await _database.InsertPlayerClassData(steamid.Value, PlayerData.PlayerClassesData![client]));
            }
        };

        foreach (var playerclass in ClassesConfig)
        {
            if (playerclass.Value.Team == team)
            {
                bool alreadyselected = playerclass.Value == PlayerData.PlayerClassesData?[client].HumanClass || playerclass.Value == PlayerData.PlayerClassesData?[client].ZombieClass;
                bool motherzombie = playerclass.Value.MotherZombie;
                bool disable = !playerclass.Value.Enable;

                selectmenu.AddMenuOption(playerclass.Value.Name!, menuhandle, alreadyselected || motherzombie || disable);
            }
        }

        selectmenu.AddMenuOption("Back", menuhandle);
        selectmenu.ExitButton = true;
        MenuManager.OpenChatMenu(client, selectmenu);
    }
}