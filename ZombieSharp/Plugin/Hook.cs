using System.Diagnostics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

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
        if(Infect.IsClientInfect(client) && !weapon.Name.Contains("knife"))
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

        // prevent death from backstabing.
        if(Infect.IsClientInfect(attacker) && Infect.IsClientHuman(client))
            info.Damage = 1;

        return HookResult.Continue;
    }

    public HookResult OnClientJoinTeam(CCSPlayerController? client, CommandInfo info)
    {
        // check for client null again.
        if(client == null)
            return HookResult.Continue;

        //Server.PrintToChatAll($"{client.PlayerName} is doing {info.GetArg(0)} {info.GetArg(1)}");

        var team = (CsTeam)int.Parse(info.GetArg(1));

        // for spectator case we allow this 
        if(team == CsTeam.Spectator || team == CsTeam.None)
        {
            if(client.PawnIsAlive)
                client.CommitSuicide(false, true);

            client.SwitchTeam(CsTeam.Spectator);
        }

        else
        {
            if((GameSettings.Settings?.RespawnEnable ?? true) && (GameSettings.Settings?.AllowRespawnJoinLate ?? true))
            {
                if(team == client.Team)
                {
                    client.PrintToChat("You're choosing the same team!");
                    return HookResult.Continue;
                }

                if(client.PawnIsAlive)
                    client.CommitSuicide(false, true);

                client.SwitchTeam(team);
            }
        }

        return HookResult.Continue;
    }
}