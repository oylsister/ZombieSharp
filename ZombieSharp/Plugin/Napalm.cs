using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZombieSharp.Plugin;

public class Napalm(ZombieSharp core)
{
    private readonly ZombieSharp _core = core;

    public void NapalmOnLoad()
    {
        _core.AddCommand("css_burn", "Burn Test Command", Command_Burn);
    }

    public void Command_Burn(CCSPlayerController? client, CommandInfo info)
    {
        info.ReplyToCommand("Start Burn");
        IgnitePawn(client, null, 5f);
    }

    public bool IgnitePawn(CCSPlayerController? client, CBaseEntity? attacker, float duration)
    {
        // if player is null why bother to do it.
        if(client == null || client.PlayerPawn.Value == null)
            return false;

        var playerPawn = client.PlayerPawn.Value;

        var particle = playerPawn.EffectEntity.Value?.As<CParticleSystem>();

        if(particle != null)
        {
            particle.DissolveStartTime = Server.CurrentTime + duration;
            return true;
        }

        var position = playerPawn.AbsOrigin;
        var pawn = client.PlayerPawn;

        position!.Z += 15;

        particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");

        particle!.StartActive = true;
        particle.EffectName = "particles\\oylsister\\env_fire_large.vpcf";
        //particle.RenderMode = RenderMode_t.kRenderGlow;
        //particle.EffectName = "particles\\burning_fx\\barrel_burning_engine_fire.vpcf";

        particle.DissolveStartTime = Server.CurrentTime + duration;
        particle.Teleport(position, null, null);

        particle.DispatchSpawn();
        particle.AcceptInput("SetParent", playerPawn, null, "!activator");

        CHandle<CBaseEntity> rawParticle = new CHandle<CBaseEntity>(particle.Handle);
        playerPawn.EffectEntity.Raw = rawParticle;

        return true;
    }
}