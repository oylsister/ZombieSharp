using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ZombieSharp.Api;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class Infect(ZombieSharp core, ILogger<ZombieSharp> logger, Classes classes, ZombieSharpInterface api)
{
    private ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;
    private Classes? _classes = classes;
    private readonly ZombieSharpInterface _api = api;
    public static bool InfectStarted = false;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _firstInfection = null;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _infectCountTimer = null;
    private int _infectCountNumber = 0;
    public static bool IsTestMode = false;

    public void InfectOnLoad()
    {
        _core.AddCommand("zs_infect", "Infection Command", InfectClientCommand);
        _core.AddCommand("zs_human", "Humanize Command", HumanizeClientCommand);
        _core.AddCommand("zs_testmode", "TestMode Command", TestModeCommand);
    }

    [CommandHelper(1, "zs_infect <targetname>")]
    [RequiresPermissions("@css/slay")]
    public void InfectClientCommand(CCSPlayerController? client, CommandInfo info)
    {
        var target = info.GetArgTargetResult(1);

        if(target == null || target.Count() <= 0)
        {
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.TargetInvalid"]}");
            return;
        }

        var mother = false;

        if(!InfectHasStarted())
            mother = true;

        foreach(var player in target)
        {
            if(player == null) continue;
            if(!Utils.IsPlayerAlive(player))
            {
                if(target.Count() < 2)
                    info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.MustBeAlive"]}");

                continue;
            }

            if(IsClientInfect(player))
            {
                if(target.Count() < 2)
                    info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Infect.AlreadyZombie"]}");

                continue;
            }

            InfectClient(player, null, mother, true);
        }

        if(target.Count() > 1)
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Infect.SuccessAll"]}");

        else if(target.Count() < 2 && target.FirstOrDefault() != null)
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Infect.Success", target.FirstOrDefault()!.PlayerName]}");
    }

    [CommandHelper(1, "zs_human <targetname>")]
    [RequiresPermissions("@css/slay")]
    public void HumanizeClientCommand(CCSPlayerController? client, CommandInfo info)
    {
        var target = info.GetArgTargetResult(1);

        if(target == null || target.Count() <= 0)
        {
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.TargetInvalid"]}");
            return;
        }

        foreach(var player in target)
        {
            if(player == null) continue;
            if(!Utils.IsPlayerAlive(player))
            {
                if(target.Count() < 2)
                    info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.MustBeAlive"]}");

                continue;
            }

            if(IsClientHuman(player))
            {
                if(target.Count() < 2)
                    info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Human.AlreadyHuman"]}");

                continue;
            }

            HumanizeClient(player, true);
        }

        if(target.Count() > 1)
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Human.SuccessAll"]}");

        else if(target.Count() < 2 && target.FirstOrDefault() != null)
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Human.Success", target.FirstOrDefault()!.PlayerName]}");
    }

    [CommandHelper(1, "zs_testmode <0-1>")]
    [RequiresPermissions("@css/slay")]
    public void TestModeCommand(CCSPlayerController? client, CommandInfo info)
    {
        var mode = int.TryParse(info.ArgByIndex(1), out var number);

        if(!mode)
        {
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} invalid value.");
            return;
        }

        var result = Convert.ToBoolean(number);

        if(result)
        {
            IsTestMode = true;
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} Test mode has been activated.");
            return;
        }

        else
        {
            IsTestMode = false;
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} Test mode has been deactivated.");
            return;
        }
    }

    public void InfectOnRoundFreezeEnd()
    {
        // kill timer just in case.
        InfectKillInfectionTimer();

        // we disable this for warmup so player can chill.
        if(Utils.IsWarmup())
            return;

        if(GameSettings.Settings == null)
        {
            _logger.LogCritical("[InfectOnRoundFreezeEnd] Game Settings is null!");
            _infectCountNumber = 15;
        }

        else
            _infectCountNumber = (int)GameSettings.Settings.FirstInfectionTimer;

        _firstInfection = _core.AddTimer(_infectCountNumber + 1, InfectMotherZombie, TimerFlags.STOP_ON_MAPCHANGE);

        _infectCountTimer = _core.AddTimer(1f, () => 
        {
            if(_infectCountNumber < 0)
            {
                InfectKillInfectionTimer();
                return;
            }

            Utils.PrintToCenterAll($" {_core.Localizer["Infect.Countdown", _infectCountNumber]}");
            _infectCountNumber--;

        }, TimerFlags.REPEAT|TimerFlags.STOP_ON_MAPCHANGE);
    }

    public void InfectKillInfectionTimer()
    {
        if(_firstInfection != null)
        {
            _firstInfection.Kill();
            _firstInfection = null;
        }

        if(_infectCountTimer != null)
        {
            _infectCountTimer.Kill();
            _infectCountTimer = null;
        }
    }

    public void InfectMotherZombie()
    {
        if(IsTestMode)
            return;

        // if infection already started then stop it.
        if(InfectHasStarted())
            return;

        // we get how much zombie we need.
        var currentPlayer = Utilities.GetPlayers();
        var ratio = 7f;

        if(GameSettings.Settings == null)
        {
            _logger.LogCritical("[InfectMotherZombie] Game Settings is null!");
            ratio = 7f;
        }

        else
            ratio = GameSettings.Settings.MotherZombieRatio;

        var requireZombie = (int)Math.Ceiling(currentPlayer.Count / ratio);

        // get the list of candidate
        List<CCSPlayerController> candidate = new();

        foreach(var player in currentPlayer)
        {
            // null or not alive player.
            if(player == null || !player.PawnIsAlive)
                continue;

            // not in the data list.
            if(!PlayerData.ZombiePlayerData!.ContainsKey(player))
                continue;

            if(PlayerData.ZombiePlayerData[player].MotherZombie == ZombiePlayer.MotherZombieStatus.NONE)
                candidate.Add(player);
        }

        // if candidate is not enough
        if(candidate.Count < requireZombie)
        {
            // tell player that mother zombie cycle has been reset
            Server.PrintToChatAll($" {_core.Localizer["Prefix"]} {_core.Localizer["Infect.MotherZombieReset"]}");

            foreach(var player in currentPlayer)
            {
                // null or not alive player.
                if(player == null || !player.PawnIsAlive)
                    continue;

                // not in the data list.
                if(!PlayerData.ZombiePlayerData!.ContainsKey(player))
                    continue;

                if(PlayerData.ZombiePlayerData[player].MotherZombie == ZombiePlayer.MotherZombieStatus.LAST)
                {
                    // add to candidate.
                    candidate.Add(player);

                    // Mother zombie status to none.
                    PlayerData.ZombiePlayerData[player].MotherZombie = ZombiePlayer.MotherZombieStatus.NONE;
                }
            }
        }

        // we will infect motherzombie here.
        // we have to loop all candidate in order to ensure we get all required zombie as we want.
        // variable check total zombie made.
        var infected = 0;

        foreach(var player in candidate)
        {
            // we stop here if all zombie is made.
            if(infected >= requireZombie)
                return;

            // check every time.
            if(player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            // infect them with motherzombie true
            InfectClient(player, null, true);

            // motherzombie made +
            infected++;
        }
    }

    public void InfectOnPlayerHurt(CCSPlayerController? client, CCSPlayerController? attacker)
    {
        if(client == null || attacker == null)
            return;

        if(IsClientHuman(client) && IsClientInfect(attacker))
            InfectClient(client, attacker);
    }

    public void InfectOnPreRoundStart(bool switchTeam = true)
    {
        if(PlayerData.ZombiePlayerData == null)
        {
            _logger.LogCritical("[InfectOnPreRoundStart] ZombiePlayers is invalid!");
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if(player == null || !player.IsValid)
            {
                _logger.LogError("[InfectOnPreRoundStart] Player key is not invalid!");
                continue;
            }

            // if player is not in the dictionary then skip it.
            if(!PlayerData.ZombiePlayerData.ContainsKey(player))
            {
                _logger.LogError("[InfectOnPreRoundStart] Player {name} is not in ZombiePlayersData!", player.PlayerName);
                continue;
            }

            // set to false.
            PlayerData.ZombiePlayerData[player].Zombie = false;

            // switch their team to CT. and make sure they are not spectator or else they will get spawned with another.
            if(player.Team != CsTeam.Spectator && player.Team != CsTeam.None && switchTeam)
                player.SwitchTeam(CsTeam.CounterTerrorist);
        }
    }

    // we need to set mother zombie to last if they are chosen. so when mother zombie candidate run out we bring them to motherzombie cycle again.
    public void InfectOnRoundEnd()
    {
        if(PlayerData.ZombiePlayerData == null)
        {
            _logger.LogCritical("[InfectOnRoundEnd] ZombiePlayers data is null!");
            return;
        }

        foreach(var player in Utilities.GetPlayers())
        {
            if(player == null || !player.IsValid)
            {
                _logger.LogError("[InfectOnRoundEnd] Player is not invalid!");
                continue;
            }

            // check if player is in dictionary and motherzombie is chosen then we have to set them to last.
            if(!PlayerData.ZombiePlayerData.ContainsKey(player))
            {
                _logger.LogError("[InfectOnRoundEnd] Player {name} is not in ZombiePlayersData!", player.PlayerName);
                continue;
            }

            // set mother zombie status to Last.
            if(PlayerData.ZombiePlayerData[player].MotherZombie == ZombiePlayer.MotherZombieStatus.CHOSEN)
                PlayerData.ZombiePlayerData[player].MotherZombie = ZombiePlayer.MotherZombieStatus.LAST;
        }
    }

    public void InfectClient(CCSPlayerController client, CCSPlayerController? attacker = null, bool motherzombie = false, bool force = false)
    {
        if(IsTestMode)
            return;
            
        if(client == null)
        {
            _logger.LogError("[InfectClient] Client is null!");
            return;
        }

        var result = _api.ZS_OnClientInfect(client, attacker, motherzombie, force);
        
        if(result == HookResult.Handled || result == HookResult.Stop)
        {
            _logger.LogInformation("[InfectClient] {name} Infection has been stopped by API", client.PlayerName);
            return;
        }

        // if infect is not started yet then tell them yeah.
        if(!InfectHasStarted())
        {
            InfectKillInfectionTimer();
            InfectStarted = true;
        }

        if(PlayerData.ZombiePlayerData == null)
        {
            _logger.LogCritical("[InfectClient] ZombiePlayers data is null!");
            return;
        }

        if(!PlayerData.ZombiePlayerData.ContainsKey(client))
        {
            _logger.LogCritical("[InfectClient] client is not in ZombiePlayersData!");
            return;
        }

        // set player zombie to true.
        PlayerData.ZombiePlayerData[client].Zombie = true;

        // we get player class for applying attribute here.
        var applyClass = PlayerData.PlayerClassesData?[client].ZombieClass;

        // set motherzombie status to chosen
        if(motherzombie)
        {
            PlayerData.ZombiePlayerData[client].MotherZombie = ZombiePlayer.MotherZombieStatus.CHOSEN;
            
            // if mother zombie class is specificed then change it, or else we just use player setting class from above.
            if(Classes.MotherZombie != null)
                applyClass = Classes.MotherZombie;

            // if teleport zombie back to spawn is enabled then we teleport them back to spawn.
            if(GameSettings.Settings?.MotherZombieTeleport ?? false)
            {
                Server.NextWorldUpdate(() => 
                {
                    var pos = PlayerData.PlayerSpawnData?[client].PlayerPosition;
                    var angle = PlayerData.PlayerSpawnData?[client].PlayerAngle;

                    if(pos == null || angle == null)
                    {
                        _logger.LogError("[InfectClient] Position of {name} is null!", client.PlayerName);
                        return;
                    }

                    Teleport.TeleportClientToSpawn(client, pos, angle);
                });
            }
        }
        // switch team to terrorist
        client.SwitchTeam(CsTeam.Terrorist);

        // remove all player weapon
        Server.NextWorldUpdate(() => Utils.DropAllWeapon(client));

        //scream sound.
        ZombieSound.ZombieEmitSound(client, "zr.amb.scream");

        // apply class attribute.
        _classes?.ClassesApplyToPlayer(client, applyClass);

        // create fake killfeed
        if(attacker != null)
        {
            // fire event.
            EventPlayerDeath @event = new EventPlayerDeath(false);

            @event.Userid = client;
            @event.Attacker = attacker;
            @event.Weapon = "knife";
            @event.FireEvent(false);

            // update player score
            attacker.ActionTrackingServices!.MatchStats.Kills += 1;
            client.ActionTrackingServices!.MatchStats.Deaths += 1;
        }

        // tell them you have been infect
        client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Infect.BecomeZombie"]}");
    }

    public void HumanizeClient(CCSPlayerController client, bool force = false)
    {
        if(client == null)
        {
            _logger.LogError("[HumanizeClient] Client is null!");
            return;
        }

        var result = _api.ZS_OnClientHumanize(client, force);
        
        if(result == HookResult.Handled || result == HookResult.Stop)
        {
            _logger.LogInformation("[HumanizeClient] {name} Humanize has been stopped by API", client.PlayerName);
            return;
        }

        if(PlayerData.ZombiePlayerData == null)
        {
            _logger.LogCritical("[HumanizeClient] ZombiePlayers data is null!");
            return;
        }

        if(!PlayerData.ZombiePlayerData.ContainsKey(client))
        {
            _logger.LogCritical("[HumanizeClient] client is not in ZombiePlayersData!");
            return;
        }
        
        // set player zombie to false
        PlayerData.ZombiePlayerData[client].Zombie = false;

        // switch team to terrorist
        client.SwitchTeam(CsTeam.CounterTerrorist);

        // if force then tell them that you have been force
        if(force)
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Infect.BecomeHuman"]}");

        // apply class attribute.
        _classes?.ClassesApplyToPlayer(client, PlayerData.PlayerClassesData?[client].HumanClass!);
    }

    public static void SpawnPlayer(CCSPlayerController client)
    {
        if(client == null || !client.IsValid)
            return;

        client.ChangeTeam(InfectHasStarted() ? CsTeam.Terrorist : CsTeam.CounterTerrorist);

        var ct = Utilities.GetPlayers().Where(player => player.TeamNum == 3 && player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE).Count();
        var t = Utilities.GetPlayers().Where(player => player.TeamNum == 2 && player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE).Count();

        // if server is empty
        if(ct == 0 && t == 0 && !RoundEnd.RoundEnded)   
        {
            RoundEnd.TerminateRound(CsTeam.None, 3f);
        }

        Timer timer = new(2.0f, () => Respawn.RespawnClient(client), TimerFlags.STOP_ON_MAPCHANGE);
    }

    public static bool InfectHasStarted()
    {
        return InfectStarted;
    }

    public static bool IsClientInfect(CCSPlayerController client)
    {
        if(!PlayerData.ZombiePlayerData!.ContainsKey(client))
            return false;

        return PlayerData.ZombiePlayerData[client].Zombie;
    }

    public static bool IsClientHuman(CCSPlayerController client)
    {
        if(!PlayerData.ZombiePlayerData!.ContainsKey(client))
            return false;

        return !PlayerData.ZombiePlayerData[client].Zombie;
    }
}
