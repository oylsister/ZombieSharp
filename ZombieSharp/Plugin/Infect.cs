using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class Infect(ZombieSharp core, ILogger<ZombieSharp> logger, Classes classes)
{
    private ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;
    private Classes? _classes = classes;
    public static bool InfectStarted = false;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _firstInfection = null;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _infectCountTimer = null;
    private int _infectCountNumber = 0;

    public void InfectOnRoundFreezeEnd()
    {
        // kill timer just in case.
        InfectKillInfectionTimer();

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

    public void InfectOnPreRoundStart()
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

            // set to false.
            PlayerData.ZombiePlayerData[player].Zombie = false;

            // switch their team to CT.
            player.SwitchTeam(CsTeam.CounterTerrorist);
        }
    }

    public void InfectClient(CCSPlayerController client, CCSPlayerController? attacker = null, bool motherzombie = false, bool force = false)
    {
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

        // set player zombie to true.
        PlayerData.ZombiePlayerData[client].Zombie = true;

        // set motherzombie status to chosen
        if(motherzombie)
        {
            PlayerData.ZombiePlayerData[client].MotherZombie = ZombiePlayer.MotherZombieStatus.CHOSEN;

            // if teleport zombie back to spawn is enabled then we teleport them back to spawn.
            if(GameSettings.Settings?.MotherZombieTeleport ?? false)
            {
                Server.NextFrame(() => 
                {
                    var pos = PlayerData.PlayerSpawnData?[client].PlayerPosition;
                    var angle = PlayerData.PlayerSpawnData?[client].PlayerAngle;

                    if(pos == null || angle == null)
                    {
                        _logger.LogError("[InfectClient] Position of {0} is null!", client.PlayerName);
                        return;
                    }

                    Teleport.TeleportClientToSpawn(client, pos, angle);
                });
            }
        }
        // switch team to terrorist
        client.SwitchTeam(CsTeam.Terrorist);

        // remove all player weapon
        Utils.DropAllWeapon(client);

        //scream sound.
        Utils.EmitSound(client, "zr.amb.scream");

        // apply class attribute.
        Server.NextFrame(() => _classes?.ClassesApplyToPlayer(client, PlayerData.PlayerClassesData?[client].ZombieClass!));

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
        if(PlayerData.ZombiePlayerData == null)
        {
            _logger.LogCritical("[InfectClient] ZombiePlayers data is null!");
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
        Server.NextFrame(() => _classes?.ClassesApplyToPlayer(client, PlayerData.PlayerClassesData?[client].HumanClass!));
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
