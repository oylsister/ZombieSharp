using System.Diagnostics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;

namespace ZombieSharp.Plugin;

public class Hook(ZombieSharp core, Weapons weapons, Respawn respawn, ILogger<ZombieSharp> logger)
{
    private readonly ZombieSharp _core = core;
    private readonly Weapons _weapons = weapons;
    private readonly Respawn _respawn = respawn;
    private readonly ILogger<ZombieSharp> _logger = logger;

    public void HookOnLoad()
    {
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnCanAcquire, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

        _core.AddCommandListener("jointeam", OnClientJoinTeam, HookMode.Pre);
    }

    public void HookOnUnload()
    {
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnCanAcquire, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);

        _core.RemoveCommandListener("jointeam", OnClientJoinTeam, HookMode.Pre);
    }

    public HookResult OnCanAcquire(DynamicHook hook)
    {
        var itemService = hook.GetParam<CCSPlayer_ItemServices>(0);
        var weapon = VirtualFunctions.GetCSWeaponDataFromKey(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());
        var method = hook.GetParam<AcquireMethod>(2);
        var client = itemService.Pawn.Value.Controller.Value?.As<CCSPlayerController>();

        if(client == null)
            return HookResult.Continue;

        // if client is infect and weapon is not a knife.
        if(Infect.IsClientZombie(client) && !weapon.Name.Contains("knife"))
        {
            hook.SetReturn(AcquireResult.NotAllowedByProhibition);
            return HookResult.Handled;
        }

        var restirctEnable = GameSettings.Settings?.WeaponRestrictEnable ?? false;
        var purchaseEnable = GameSettings.Settings?.WeaponPurchaseEnable ?? false;

        // weapon restrict section.
        if(restirctEnable)
        {
            // if player buy from menu tell them they can't.
            if(Weapons.IsRestricted(weapon.Name))
            {
                var attribute = Weapons.GetWeaponAttributeByEntityName(weapon.Name);

                if(method == AcquireMethod.Buy)
                    client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Weapon.IsRestricted", attribute?.WeaponName!]}");

                hook.SetReturn(AcquireResult.NotAllowedByProhibition);
                return HookResult.Handled;
            }

            else
            {
                if(method == AcquireMethod.Buy && purchaseEnable)
                {
                    var attribute = Weapons.GetWeaponAttributeByEntityName(weapon.Name);

                    if(attribute != null)
                    {
                        _weapons.PurchaseWeapon(client, attribute);
                        hook.SetReturn(AcquireResult.NotAllowedByProhibition);
                        return HookResult.Handled;
                    }
                }
            }
        }

        else
        {
            if(method == AcquireMethod.Buy && purchaseEnable)
            {
                var attribute = Weapons.GetWeaponAttributeByEntityName(weapon.Name);

                if(attribute != null)
                {
                    _weapons.PurchaseWeapon(client, attribute);
                    hook.SetReturn(AcquireResult.NotAllowedByProhibition);
                    return HookResult.Handled;
                }
            }
        }

        return HookResult.Continue;
    }

    public HookResult OnTakeDamage(DynamicHook hook)
    {
        var victim = hook.GetParam<CEntityInstance>(0);
        var info = hook.GetParam<CTakeDamageInfo>(1);

        var client = Utils.GetCCSPlayerController(victim);
        var attacker = Utils.GetCCSPlayerController(info.Attacker.Value);

        if(client == null || attacker == null)
            return HookResult.Continue;

        if(info.Inflictor.Value?.DesignerName == "inferno")
        {
            // prevent self damage from molotov.
            var inferno = new CInferno(info.Inflictor.Value.Handle);
            if(client == inferno.OwnerEntity.Value)
                return HookResult.Handled;

            // if human step on it then we just stop here.
            if(Infect.IsClientHuman(client))
                return HookResult.Handled;

            // if zombie step on it then we make them walking slow.
            else if(Infect.IsClientZombie(client))
                Utils.SetStamina(client, 40.0f);
        }

        if(info.Inflictor.Value?.DesignerName == "hegrenade")
        {
            Knockback.KnockbackClientExplosion(client, info.Inflictor.Value, info.Damage);
        }

        // prevent death from backstabing.
        if (Infect.IsClientZombie(attacker) && Infect.IsClientHuman(client))
        {
            if (Infect.IsTestMode)
            {
                return HookResult.Handled;
            }
            var distance = Utils.GetPlayerDistance(client, attacker);

            // if distance is not null but player is too far for knife range, then we blocked it.
            if (distance != null && distance.Value > GameSettings.Settings?.MaxKnifeRange)
            {
                return HookResult.Handled; 
            }  
            info.Damage = 1;
        }
            
        return HookResult.Continue;
    }

    public HookResult OnClientJoinTeam(CCSPlayerController? client, CommandInfo info)
    {
        // check for client null again.
        if(client == null)
            return HookResult.Continue;

        var team = (CsTeam)int.Parse(info.GetArg(1));

        // for spectator case we allow this 
        if(team == CsTeam.Spectator || team == CsTeam.None)
        {
            
            if(Utils.IsPlayerAlive(client))
                client.CommitSuicide(false, true);

            client.SwitchTeam(CsTeam.Spectator);
            return HookResult.Handled;
        }
        else
        {
            // If infection has started, prevent manual team switching
            if(Infect.InfectHasStarted())
            {
                _logger.LogWarning("[OnClientJoinTeam] {0} attempting team switch during infection - enforcing zombie status", client.PlayerName);
                
                // Force the team assignment based on zombie status
                _core.AddTimer(0.05f, () => {
                    if(client == null || !client.IsValid)
                        return;
                        
                    bool isZombie = Infect.IsClientZombie(client);
                    CsTeam targetTeam = isZombie ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
                    
                    if(client.Team != targetTeam)
                        client.SwitchTeam(targetTeam);
                });
                return HookResult.Handled;
            }
            

            
            if((GameSettings.Settings?.RespawnEnable ?? true) && (GameSettings.Settings?.AllowRespawnJoinLate ?? true))
            {
                if(team == client.Team)
                {
                    //client.PrintToChat("You're choosing the same team!");
                    return HookResult.Continue;
                }

                if(Utils.IsPlayerAlive(client))
                    client.CommitSuicide(false, true);

                client.SwitchTeam(team);
            }
        }

        return HookResult.Continue;
    }
}