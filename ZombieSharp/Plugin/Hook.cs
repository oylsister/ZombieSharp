using System.Diagnostics;
using System.Runtime.InteropServices;
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

    //public static MemoryFunctionVoid<CEntityIdentity, CUtlSymbolLarge, CEntityInstance, CEntityInstance, CVariant, int> CEntityIdentity_AcceptInputFunc = new(GameData.GetSignature("CEntityIdentity_AcceptInput"));

    public void HookOnLoad()
    {
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnCanAcquire, HookMode.Pre);
        VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Hook(OnCanUse, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        //CEntityIdentity_AcceptInputFunc.Hook(OnEntityAcceptInput, HookMode.Post);

        _core.AddCommandListener("jointeam", OnClientJoinTeam, HookMode.Pre);
        _core.AddCommandListener("say", OnPlayerSay, HookMode.Post);
        _core.AddCommandListener("say_team", OnPlayerSayTeam, HookMode.Post);

        _core.RegisterListener<OnEntityInput>(OnEntityInput);
    }

    public void HookOnUnload()
    {
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnCanAcquire, HookMode.Pre);
        VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Unhook(OnCanUse, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        //CEntityIdentity_AcceptInputFunc.Unhook(OnEntityAcceptInput, HookMode.Post);

        _core.RemoveCommandListener("jointeam", OnClientJoinTeam, HookMode.Pre);
        _core.AddCommandListener("say", OnPlayerSay, HookMode.Post);
        _core.AddCommandListener("say_team", OnPlayerSayTeam, HookMode.Post);
    }

    public HookResult OnCanUse(DynamicHook hook)
    {
        var itemService = hook.GetParam<CCSPlayer_WeaponServices>(0);
        var weapon = hook.GetParam<CBasePlayerWeapon>(1);

        var client = itemService.Pawn.Value.Controller.Value?.As<CCSPlayerController>();

        if(client == null)
            return HookResult.Continue;

        // if client is infect and weapon is not a knife.
        if(Infect.IsClientInfect(client) && !weapon.DesignerName.Contains("knife"))
        {
            hook.SetReturn(false);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    public HookResult OnCanAcquire(DynamicHook hook)
    {
        var itemService = hook.GetParam<CCSPlayer_ItemServices>(0);
        var weapon = VirtualFunctions.GetCSWeaponDataFromKey(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());
        var method = hook.GetParam<AcquireMethod>(2);
        var client = itemService.Pawn.Value.Controller.Value?.As<CCSPlayerController>();

        if(client == null)
            return HookResult.Continue;

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

        if(victim.DesignerName != "player" || info.Attacker.Value?.DesignerName != "player")
        {
            //Server.PrintToChatAll($"[OnTakeDamage] Victim: {victim.DesignerName}, Attacker: {info.Attacker.Value?.DesignerName}");
            return HookResult.Continue;
        }

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
            else if(Infect.IsClientInfect(client))
                Utils.SetStamina(client, 40.0f);
        }

        if(info.Inflictor.Value?.DesignerName == "hegrenade")
        {
            Knockback.KnockbackClientExplosion(client, info.Inflictor.Value, info.Damage);
        }

        // prevent death from backstabing.
        if(Infect.IsClientInfect(attacker) && Infect.IsClientHuman(client))
        {
            if(Infect.IsTestMode)
                return HookResult.Handled;

            var distance = Utils.GetPlayerDistance(client, attacker);

            // if distance is not null but player is too far for knife range, then we blocked it.
            if(distance != null && distance.Value > GameSettings.Settings?.MaxKnifeRange)
                return HookResult.Handled;
                
            info.Damage = 1;
        }

        return HookResult.Continue;
    }

    private HookResult OnEntityAcceptInput(DynamicHook hook)
    {
        var identity = hook.GetParam<CEntityIdentity>(0);

        if (identity.Name != "zr_toggle_respawn")
            return HookResult.Continue;

        var input = hook.GetParam<CUtlSymbolLarge>(1);

        //Server.PrintToChatAll($"Found: {identity.Name}, input: {stringinput}");

        if(GameSettings.Settings == null)
        {
            _logger.LogError("[OnEntityAcceptInput] GameSettings is null!");
            return HookResult.Continue;
        }

        if (Respawn.RespawnRelay != null && identity != null)
        {
            if (identity.Name == "zr_toggle_respawn")
            {
                if (input.KeyValue?.Equals("Trigger", StringComparison.OrdinalIgnoreCase) ?? false)
                    _respawn.ToggleRespawn(false);

                else if (input.KeyValue?.Equals("Enable", StringComparison.OrdinalIgnoreCase) ?? false && !GameSettings.Settings.RespawnEnable)
                    _respawn.ToggleRespawn(true);

                else if (input.KeyValue?.Equals("Disable", StringComparison.OrdinalIgnoreCase) ?? false && GameSettings.Settings.RespawnEnable)
                    _respawn.ToggleRespawn(false);
            }
        }

        return HookResult.Continue;
    }

    public void OnEntityInput(CEntityIdentity entity, string input, CEntityInstance activator, CEntityInstance caller, int output)
    {
        /*
        if(input != null)
            Server.PrintToChatAll($"[OnEntityInput] Found: {entity.Name}, input: {input}");
        */

        if (entity.Name != "zr_toggle_respawn")
            return;

        //Server.PrintToChatAll($"Found: {identity.Name}, input: {stringinput}");

        if(GameSettings.Settings == null)
        {
            _logger.LogError("[OnEntityAcceptInput] GameSettings is null!");
            return;
        }

        if (Respawn.RespawnRelay != null && entity != null && input != null)
        {
            if (entity.Name == "zr_toggle_respawn")
            {
                if (input.Equals("Trigger", StringComparison.OrdinalIgnoreCase))
                    _respawn.ToggleRespawn(false);

                else if (input.Equals("Enable", StringComparison.OrdinalIgnoreCase) && !GameSettings.Settings.RespawnEnable)
                    _respawn.ToggleRespawn(true);

                else if (input.Equals("Disable", StringComparison.OrdinalIgnoreCase) && GameSettings.Settings.RespawnEnable)
                    _respawn.ToggleRespawn(false);
            }
        }
    }

    public HookResult OnClientJoinTeam(CCSPlayerController? client, CommandInfo info)
    {
        // check for client null again.
        if(client == null)
            return HookResult.Continue;

        //Server.PrintToChatAll($"{client.PlayerName} is doing {info.GetArg(0)} {info.GetArg(1)}");

        var team = (CsTeam)int.Parse(info.GetArg(1));

        // stable
        // for spectator case we allow this 
        if(team == CsTeam.Spectator || team == CsTeam.None)
        {
            /*
            if(Utils.IsPlayerAlive(client))
                client.CommitSuicide(false, true);

            Utils.ChangeTeam(client, 1);
            */
            info.ReplyToCommand($"Joining spectator team has been blocked!");
            return HookResult.Handled;
        }

        else
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
        
        /*
        if(Utils.IsPlayerAlive(client))
            client.CommitSuicide(false, true);

        if(info.ArgCount >= 2 && (CsTeam)int.Parse(info.GetArg(1)) == CsTeam.Spectator || (CsTeam)int.Parse(info.GetArg(1)) == CsTeam.None)
            Utils.ChangeTeam(client, 1);

        else if(info.ArgCount >= 2 && client.TeamNum <= 1 && (CsTeam)int.Parse(info.GetArg(1)) == CsTeam.CounterTerrorist || (CsTeam)int.Parse(info.GetArg(1)) == CsTeam.Terrorist)
            Respawn.SpawnPlayer(client);
        */

        return HookResult.Continue;
    }

    public HookResult OnPlayerSay(CCSPlayerController? client, CommandInfo info)
    {
        // check for client null again.
        if(client == null)
            return HookResult.Continue;

        _weapons.WeaponPurchaseChat(client, info.ArgString);
        return HookResult.Continue;
    }

    public HookResult OnPlayerSayTeam(CCSPlayerController? client, CommandInfo info)
    {
        // check for client null again.
        if(client == null)
            return HookResult.Continue;

        _weapons.WeaponPurchaseChat(client, info.ArgString);
        return HookResult.Continue;
    }
}

public class CUtlSymbolLarge : NativeObject
{
    public CUtlSymbolLarge(IntPtr pointer) : base(pointer)
    {
        IntPtr ptr = Marshal.ReadIntPtr(pointer);
        //KeyValue = ptr.ToString();
        if (ptr == IntPtr.Zero || ptr < 200000000000) return;
        KeyValue = Marshal.PtrToStringUTF8(ptr);
    }
    public string? KeyValue;
}