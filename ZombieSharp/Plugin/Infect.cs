﻿﻿﻿﻿﻿﻿﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
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
    public static bool IsTestMode = false;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _firstInfection = null;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _infectCountTimer = null;
    private int _infectCountNumber = 0;

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

            if(IsClientZombie(player))
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

        if (!mode)
        {
            info.ReplyToCommand($" {_core.Localizer["Prefix"]} invalid value.");
            return;
        }

        var result = Convert.ToBoolean(number);

        if (result)
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
        if (InfectHasStarted())
        {
            _logger.LogWarning("[InfectMotherZombie] Infection already started, aborting mother zombie selection");
            return;
        }

        var currentPlayer = Utilities.GetPlayers();

        // if player is alone then let them play alone as human.
        if (currentPlayer.Where(p => p.TeamNum > 1).Count() <= 1)
        {
            // set infect to true so when player die they restart the round.
            InfectStarted = true;
            return;
        }
        var ratio = GameSettings.Settings?.MotherZombieRatio ?? 7f;

        if (GameSettings.Settings == null)
        {
            _logger.LogCritical("[InfectMotherZombie] Game Settings is null!");
        }

        var requireZombie = (int)Math.Ceiling(currentPlayer.Count / ratio);
        
        List<CCSPlayerController> candidate = [];

        foreach (var player in currentPlayer)
        {
            if (player == null || !player.PawnIsAlive || !PlayerData.ZombiePlayerData!.ContainsKey(player))
            {
                continue;
            }

            if (PlayerData.ZombiePlayerData[player].MotherZombie == ZombiePlayer.MotherZombieStatus.NONE)
            {
                candidate.Add(player);
            }
        }
        


        if (candidate.Count < requireZombie)
        {
            Server.PrintToChatAll($" {_core.Localizer["Prefix"]} {_core.Localizer["Infect.MotherZombieReset"]}");
            
            foreach (var player in currentPlayer)
            {
                if (player == null || !player.PawnIsAlive || !PlayerData.ZombiePlayerData!.ContainsKey(player))
                    continue;

                if (PlayerData.ZombiePlayerData[player].MotherZombie == ZombiePlayer.MotherZombieStatus.LAST)
                {
                    candidate.Add(player);
                    PlayerData.ZombiePlayerData[player].MotherZombie = ZombiePlayer.MotherZombieStatus.NONE;
                }
            }
            

        }

        var n = candidate.Count;
        Random rng = new();
        
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var temp = candidate[k];
            candidate[k] = candidate[n];
            candidate[n] = temp;
        }
        


        var infected = 0;
        foreach (var player in candidate)
        {
            if (infected >= requireZombie)
            {
                break;
            }

            if (player == null || !player.IsValid || !player.PawnIsAlive)
            {
                continue;
            }


            
            InfectClient(player, null, true);
            infected++;
        }
        

    }

    public void InfectOnPlayerHurt(CCSPlayerController? client, CCSPlayerController? attacker)
    {
        if (client == null || attacker == null)
            return;
            
        bool clientIsHuman = IsClientHuman(client);
        bool attackerIsZombie = IsClientZombie(attacker);
            
        if (!clientIsHuman || !attackerIsZombie)
            return;

        InfectClient(client, attacker);
    }

    public void InfectOnPreRoundStart(bool switchTeam = true)
    {
        if (PlayerData.ZombiePlayerData == null)
        {
            _logger.LogCritical("[InfectOnPreRoundStart] ZombiePlayers data is null!");
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !PlayerData.ZombiePlayerData.ContainsKey(player))
            {
                if (player != null)
                    _logger.LogError("[InfectOnPreRoundStart] Player {name} is not in ZombiePlayersData!", player.PlayerName);
                continue;
            }

            PlayerData.ZombiePlayerData[player].Zombie = false;
            if (player.Team != CsTeam.Spectator && player.Team != CsTeam.None && switchTeam)
            {
                player.SwitchTeam(CsTeam.CounterTerrorist);
                
                // Force team switch with delay to ensure it takes effect
                _core.AddTimer(0.1f, () => {
                    if(player == null || !player.IsValid)
                        return;
                        
                    if(player.Team != CsTeam.CounterTerrorist && player.Team != CsTeam.Spectator && player.Team != CsTeam.None)
                    {
                        player.SwitchTeam(CsTeam.CounterTerrorist);
                        _logger.LogInformation("[InfectOnPreRoundStart] Forced team switch for {0} to CounterTerrorist", player.PlayerName);
                    }
                });
            }
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
        if (IsTestMode)
        {
            return;
        }

        if (client == null)
        {
            _logger.LogError("[InfectClient] Client is null!");
            return;
        }

        var result = _api.ZS_OnClientInfect(client, attacker, motherzombie, force);
        if (result == HookResult.Handled || result == HookResult.Stop)
        {
            _logger.LogInformation("[InfectClient] {name} Infection has been stopped by API", client.PlayerName);
            return;
        }

        if (!InfectHasStarted())
        {
            InfectKillInfectionTimer();
            InfectStarted = true;
        }

        if (PlayerData.ZombiePlayerData == null || !PlayerData.ZombiePlayerData.ContainsKey(client))
        {
            _logger.LogCritical("[InfectClient] ZombiePlayers data is null or client is not in ZombiePlayersData!");
            return;
        }

        PlayerData.ZombiePlayerData[client].Zombie = true;
        var applyClass = PlayerData.PlayerClassesData?[client].ZombieClass;

        if (motherzombie)
        {
            PlayerData.ZombiePlayerData[client].MotherZombie = ZombiePlayer.MotherZombieStatus.CHOSEN;
            if (Classes.MotherZombie != null)
                applyClass = Classes.MotherZombie;

            if (GameSettings.Settings?.MotherZombieTeleport ?? false)
            {
                Server.NextWorldUpdate(() =>
                {
                    var pos = PlayerData.PlayerSpawnData?[client].PlayerPosition;
                    var angle = PlayerData.PlayerSpawnData?[client].PlayerAngle;

                    if (pos == null || angle == null)
                    {
                        _logger.LogError("[InfectClient] Position of {name} is null!", client.PlayerName);
                        return;
                    }

                    Teleport.TeleportClientToSpawn(client, pos, angle);
                });
            }
        }
        
        // Step 1: Drop all weapons safely and give knife
        Utils.DropAllWeaponsExceptKnife(client);
        
        // Step 2: Switch team
        client.SwitchTeam(CsTeam.Terrorist);
        
        // Force team switch with delay to ensure it takes effect with more aggressive retries
        _core.AddTimer(0.05f, () => {
            if(client == null || !client.IsValid)
            {
                _logger.LogError("[InfectClient] Client became invalid during team switch delay");
                return;
            }
                
            if(client.Team != CsTeam.Terrorist)
            {
                client.SwitchTeam(CsTeam.Terrorist);
                
                // Additional retry if first attempt fails
                _core.AddTimer(0.1f, () => {
                    if(client == null || !client.IsValid)
                    {
                        _logger.LogError("[InfectClient] Client became invalid during second team switch delay");
                        return;
                    }
                        
                    if(client.Team != CsTeam.Terrorist)
                    {
                        client.SwitchTeam(CsTeam.Terrorist);
                        _logger.LogError("[InfectClient] Team switch failed after 2 attempts for {0} - Current team: {1}", client.PlayerName, client.Team);
                    }
                });
            }
            
            // Step 3: Apply zombie class after successful team switch with additional delay
            _core.AddTimer(0.05f, () => {
                if(client == null || !client.IsValid)
                {
                    _logger.LogError("[InfectClient] Client became invalid during class application delay");
                    return;
                }
                
                if (applyClass != null)
                {
                    _classes?.ClassesApplyToPlayer(client, applyClass);
                }
                else
                {
                    _logger.LogError("[InfectClient] No zombie class to apply for {0}!", client.PlayerName);
                }
                
                // Step 4: Play infection sound
                Utils.EmitSound(client, "zr.amb.scream");
            });
        });

        if (attacker != null)
        {
            EventPlayerDeath @event = new(false)
            {
                Userid = client,
                Attacker = attacker,
                Weapon = "knife"
            };
            @event.Assister = null;
            @event.FireEvent(false);

            attacker.ActionTrackingServices!.MatchStats.Kills += 1;
            client.ActionTrackingServices!.MatchStats.Deaths += 1;
        }

        if (force)
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

        // switch team to Counter-Terrorist
        client.SwitchTeam(CsTeam.CounterTerrorist);

        // if force then tell them that you have been force
        if(force)
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Infect.BecomeHuman"]}");

        // Apply human class attributes with delay to ensure proper application
        var humanClass = PlayerData.PlayerClassesData?[client].HumanClass;
        if(humanClass != null)
        {
            _core.AddTimer(0.1f, () => 
            {
                if(!Utils.IsPlayerAlive(client))
                    return;

                _logger.LogInformation("[HumanizeClient] Applying human class {className} to {playerName}", 
                    humanClass.Name ?? "Unknown", client.PlayerName);
                    
                _classes?.ClassesApplyToPlayer(client, humanClass);
                
                // Ensure human gets proper attributes
                var playerPawn = client.PlayerPawn.Value;
                if(playerPawn != null && playerPawn.IsValid)
                {
                    _core.AddTimer(0.1f, () => 
                    {
                        if(Utils.IsPlayerAlive(client) && playerPawn.IsValid)
                        {
                            // Set human health
                            playerPawn.Health = humanClass.Health;
                            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
                            
                            // Set human speed
                            var targetSpeed = humanClass.Speed / 250f;
                            playerPawn.VelocityModifier = targetSpeed;
                            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_flVelocityModifier");
                            
                            _logger.LogInformation("[HumanizeClient] Applied human attributes - Health: {health}, Speed: {speed}", 
                                humanClass.Health, humanClass.Speed);
                        }
                    });
                }
            });
        }
        else
        {
            _logger.LogError("[HumanizeClient] Human class is null for {playerName}!", client.PlayerName);
        }
    }

    public static bool InfectHasStarted()
    {
        return InfectStarted;
    }

    public static bool IsClientZombie(CCSPlayerController client)
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

    public void CleanupPlayerData(CCSPlayerController client)
    {
        if (client == null)
            return;
            
        _logger.LogInformation("[CleanupPlayerData] Cleaned up infection data for {0}", client.PlayerName);
    }
}
