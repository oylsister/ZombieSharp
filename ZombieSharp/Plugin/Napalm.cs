using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class Napalm(ZombieSharp core, ILogger<ZombieSharp> logger)
{
    private readonly ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;

    public void NapalmOnLoad()
    {
        _core.AddCommand("css_burn", "Burn Test Command", Command_Burn);
    }

    [RequiresPermissions("@css/slay")]
    [CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
    public void Command_Burn(CCSPlayerController? client, CommandInfo info)
    {
        info.ReplyToCommand("Start Burn");
        IgnitePawn(client, null, 1, 5f);
    }

    public void NapalmOnHurt(CCSPlayerController? client, CCSPlayerController? attacker, string weapon, int damage)
    {
        if(!weapon.Contains("hegrenade"))
            return;

        if(client == null)
            return;

        var time = PlayerData.PlayerClassesData?[client].ActiveClass?.NapalmTime;

        if(time <= 0)
            return;
        
        IgnitePawn(client, attacker, damage, time ?? 1);
    }

    public bool IgnitePawn(CCSPlayerController? client, CCSPlayerController? attacker = null, int damage = 1, float duration = 1)
    {
        // if player is null why bother to do it.
        if(client == null || client.PlayerPawn.Value == null)
            return false;

        if(PlayerData.PlayerBurnData == null)
        {
            _logger.LogError("[IgnitePawn] PlayerBurnData is null!");
            return false;
        }

        var playerPawn = client.PlayerPawn.Value;

        if(!PlayerData.PlayerBurnData.ContainsKey(client))
        {
            PlayerData.PlayerBurnData.Add(client, null);
        }

        var particle = PlayerData.PlayerBurnData[client];

        if(particle != null)
        {
            particle.DissolveStartTime = Server.CurrentTime + duration;
            return true;
        }

        var position = playerPawn.AbsOrigin;

        position!.Z += 15;

        particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");

        particle!.StartActive = true;
        particle.EffectName = "particles\\oylsister\\env_fire_large.vpcf";
        particle.DataCP = 1;
        particle.DataCPValue.X = position.X;
        particle.DataCPValue.Y = position.Y;
        particle.DataCPValue.Z = position.Z;

        particle.DissolveStartTime = Server.CurrentTime + duration;

        particle.DispatchSpawn();

        particle.Teleport(position, null, null);
        particle.AcceptInput("SetParent", playerPawn, null, "!activator");

        PlayerData.PlayerBurnData[client] = particle;

        CounterStrikeSharp.API.Modules.Timers.Timer? timer = null;
            
        timer = _core.AddTimer(0.3f, () => 
        {
            //Server.PrintToChatAll($"Dissolve = {PlayerData.PlayerBurnData[client].DissolveStartTime} | Current = {Server.CurrentTime}");

            if(client == null)
            {
                timer?.Kill();
                return;
            }

            if(PlayerData.PlayerBurnData == null)
            {
                _logger.LogError("[IgnitePawn] PlayerBurnData is null!");
                timer?.Kill();
                return;
            }

            if(!PlayerData.PlayerBurnData.ContainsKey(client))
            {
                _logger.LogError("[IgnitePawn] PlayerBurnData doesn't has this client");
                timer?.Kill();
                return;
            }

            if(PlayerData.PlayerBurnData[client] == null)
            {
                timer?.Kill();
                return;
            }

            if(PlayerData.PlayerBurnData[client]?.DesignerName != "info_particle_system")
            {
                _logger.LogError("[IgnitePawn] Found unexpected entity type: {0}", particle.DesignerName);
                timer?.Kill();
                PlayerData.PlayerBurnData[client] = null;
                return;
            }

            if(PlayerData.PlayerBurnData[client]?.DissolveStartTime < Server.CurrentTime || !Utils.IsPlayerAlive(client))
            {
                PlayerData.PlayerBurnData[client]?.AcceptInput("Stop");
                PlayerData.PlayerBurnData[client]?.AddEntityIOEvent("Kill", PlayerData.PlayerBurnData[client], null, "", 0.1f);
                timer?.Kill();
                PlayerData.PlayerBurnData[client] = null;
                return;
            }
            
            if(damage > 0)
                Utils.TakeDamage(client, attacker, damage);

            Utils.SetStamina(client, 40.0f);

        }, TimerFlags.REPEAT|TimerFlags.STOP_ON_MAPCHANGE);

        return true;
    }
}