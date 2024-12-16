using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class Classes(ZombieSharp core, ILogger<ZombieSharp> logger)
{
    private readonly ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;
    public static Dictionary<string, ClassAttribute>? ClassesConfig = null;

    public static ClassAttribute? DefaultHuman = null;
    public static ClassAttribute? DefaultZombie = null;
    public static ClassAttribute? MotherZombie = null;

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

        _logger.LogInformation("[ClassesOnMapStart] Load Player Classes file.");

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

                _logger.LogInformation("[ClassesOnMapStart] Classes {0} is default class for human", DefaultHuman.Name);
            }

            if(!string.IsNullOrEmpty(GameSettings.Settings.DefaultZombieBuffer))
            {
                var uniqueName = GameSettings.Settings.DefaultZombieBuffer;

                if(!ClassesConfig!.ContainsKey(uniqueName))
                {
                    _logger.LogCritical("[ClassesOnMapStart] Couldn't get classes \"{0}\" from playerclasses.jsonc", uniqueName);
                }

                DefaultZombie = ClassesConfig[uniqueName];

                _logger.LogInformation("[ClassesOnMapStart] Classes {0} is default class for human", DefaultZombie.Name);
            }

            if(!string.IsNullOrEmpty(GameSettings.Settings.MotherZombieBuffer))
            {
                var uniqueName = GameSettings.Settings.MotherZombieBuffer;

                if(!ClassesConfig!.ContainsKey(uniqueName))
                {
                    _logger.LogCritical("[ClassesOnMapStart] Couldn't get classes \"{0}\" from playerclasses.jsonc", uniqueName);
                }

                MotherZombie = ClassesConfig[uniqueName];

                _logger.LogInformation("[ClassesOnMapStart] Classes {0} is mother zombie classes.", MotherZombie.Name);
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
                _logger.LogInformation("[ClassesOnMapStart] Mother Zombie class from GameSettings is empty or null, when player infect will use their selected zombie class instead.");
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

        PlayerData.PlayerClassesData[client].HumanClass = DefaultHuman;
        PlayerData.PlayerClassesData[client].ZombieClass = DefaultZombie;

        if(PlayerData.PlayerClassesData?[client].HumanClass == null)
            _logger.LogInformation("[ClassesOnClientPutInServer] {0} is not null, but client got null anyway.", DefaultHuman?.Name);

        if(PlayerData.PlayerClassesData?[client].ZombieClass == null)
            _logger.LogInformation("[ClassesOnClientPutInServer] {0} is not null, but client got null anyway.", DefaultZombie?.Name);
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

        var playerPawn = client.PlayerPawn.Value;

        if(playerPawn == null)
        {
            _logger.LogError("[ClassesApplyToPlayer] Player Pawn is null!");
            return;
        }

        // if the model is not empty string and model path is not same as current model we have.
        if(!string.IsNullOrEmpty(data.Model))
        {
            // set it.
            if(data.Model != "default")
                playerPawn.SetModel(data.Model);

            // change to cs2 default model blyat
            else
            {
                if(data.Team == 0)
                    playerPawn.SetModel("characters/models/tm_phoenix/tm_phoenix.vmdl");
                
                else
                    playerPawn.SetModel("characters/models/ctm_sas/ctm_sas.vmdl");
            }
        }

        // set player health
        playerPawn.Health = data.Health;
        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");

        // if zombie then remove their kevlar
        if(data.Team == 0)
        {
            playerPawn.ArmorValue = 0;
            client.PawnHasHelmet = false;
        }

        // set speed 
        playerPawn.VelocityModifier = data.Speed / 250f;

        Server.PrintToChatAll($"{client.PlayerName} Classes Apply reach here!");

        // set active player data.
        PlayerData.PlayerClassesData![client].ActiveClass = data;
    }

    public void ClassesOnPlayerHurt(CCSPlayerController? client)
    {
        _core?.AddTimer(0.3f, () => 
        {
            // need to be alive
            if(client == null || client.Handle == IntPtr.Zero)
                return;

            if(!client.PawnIsAlive)
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
}