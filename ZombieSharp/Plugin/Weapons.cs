using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class Weapons(ZombieSharp core, ILogger<ZombieSharp> logger)
{
    private readonly ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;
    public static Dictionary<string, WeaponAttribute>? WeaponsConfig = null;
    bool weaponCommandInitialized = false;

    public void WeaponOnLoad()
    {
        _core.AddCommand("zs_restrict", "Restrict Weapon Command", WeaponRestrictCommand);
        _core.AddCommand("zs_unrestrict", "Unrestrict Weapon Command", WeaponUnrestrictCommand);
    }

    public void WeaponsOnMapStart()
    {
        WeaponsConfig?.Clear();
        
        // make sure this one is null.
        WeaponsConfig = null;

        // initial weapon config data
        WeaponsConfig = new Dictionary<string, WeaponAttribute>();

        var configPath = Path.Combine(ZombieSharp.ConfigPath, "weapons.jsonc");

        if(!File.Exists(configPath))
        {
            _logger.LogCritical("[WeaponsOnMapStart] Couldn't find a weapons.jsonc file!");
            return;
        }

        _logger.LogInformation("[WeaponsOnMapStart] Load Weapon Config file.");

        // we get data from jsonc file.
        WeaponsConfig = JsonConvert.DeserializeObject<Dictionary<string, WeaponAttribute>>(File.ReadAllText(configPath));

        // we create weapon purchase command here.
        IntialWeaponPurchaseCommand();
    }

    public void IntialWeaponPurchaseCommand()
    {
        // if it's not enabled or null then we don't have to.
        if(!GameSettings.Settings?.WeaponPurchaseEnable ?? false)
        {
            // at least tell them that this is disable.
            _logger.LogInformation("[IntialWeaponPurchaseCommand] Purchasing is disabled");
            return;
        }

        // if this part has been done before don't do it again.
        if(weaponCommandInitialized)
            return;

        // safety first.
        if(WeaponsConfig == null)
        {
            _logger.LogError("[IntialWeaponPurchaseCommand] Weapon Configs is null!");
            return;
        }

        foreach(var weapon in WeaponsConfig.Values)
        {
            if(weapon.PurchaseCommand == null || weapon.PurchaseCommand.Count <= 0)
                continue;

            foreach(var command in weapon.PurchaseCommand)
            {
                if(string.IsNullOrEmpty(command))
                    continue;

                _core.AddCommand(command, $"Weapon {weapon.WeaponName} Purchase Command", WeaponPurchaseCommand);
            }
        }

        weaponCommandInitialized = true;
    }

    [RequiresPermissions("@css/slay")]
    public void WeaponRestrictCommand(CCSPlayerController? client, CommandInfo info)
    {
        if(WeaponsConfig == null)
        {
            _logger.LogError("[WeaponRestrictCommand] WeaponsConfig is null!");
            return;
        }

        if(info.ArgCount > 1)
        {
            var weaponname = info.GetArg(1);
            var weapon = GetWeaponAttributeByName(weaponname);

            if(weapon == null)
            {
                info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Weapon.NotFound", weaponname]}");
                return;
            }

            Server.PrintToChatAll($" {_core.Localizer["Prefix"]} {_core.Localizer["Restrict.Weapon", weapon.WeaponName!]}");
            weapon.Restrict = true;
            return;
        }

        if(client == null)
            return;

        var menu = new ChatMenu($" {_core.Localizer["Prefix"]} {_core.Localizer["Restrict.MainMenu"]}");
        
        foreach(var weapon in WeaponsConfig)
        {
            menu.AddMenuOption(weapon.Value.WeaponName!, (client, option) => 
            {
                weapon.Value.Restrict = true;
                Server.PrintToChatAll($" {_core.Localizer["Prefix"]} {_core.Localizer["Restrict.Weapon", weapon.Value.WeaponName!]}");
                MenuManager.CloseActiveMenu(client);
            }, 
            weapon.Value.Restrict);
        }
        menu.ExitButton = true;
        MenuManager.OpenChatMenu(client, menu);
    }

    [RequiresPermissions("@css/slay")]
    public void WeaponUnrestrictCommand(CCSPlayerController? client, CommandInfo info)
    {
        if(WeaponsConfig == null)
        {
            _logger.LogError("[WeaponRestrictCommand] WeaponsConfig is null!");
            return;
        }

        if(info.ArgCount > 1)
        {
            var weaponname = info.GetArg(1);
            var weapon = GetWeaponAttributeByName(weaponname);

            if(weapon == null)
            {
                info.ReplyToCommand($" {_core.Localizer["Prefix"]} {_core.Localizer["Weapon.NotFound"]}");
                return;
            }

            Server.PrintToChatAll($" {_core.Localizer["Prefix"]} {_core.Localizer["Unrestrict.Weapon", weapon.WeaponName!]}");
            weapon.Restrict = false;
            return;
        }

        if(client == null)
            return;

        var menu = new ChatMenu($" {_core.Localizer["Prefix"]} {_core.Localizer["Unrestrict.MainMenu"]}");
        
        foreach(var weapon in WeaponsConfig)
        {
            menu.AddMenuOption(weapon.Value.WeaponName!, (client, option) => 
            {
                weapon.Value.Restrict = false;
                Server.PrintToChatAll($" {_core.Localizer["Prefix"]} {_core.Localizer["Unrestrict.Weapon", weapon.Value.WeaponName!]}");
                MenuManager.CloseActiveMenu(client);
            }, 
            !weapon.Value.Restrict);
        }
        menu.ExitButton = true;
        MenuManager.OpenChatMenu(client, menu);
    }

    [CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
    public void WeaponPurchaseCommand(CCSPlayerController? client, CommandInfo info)
    {
        // args 0 is basically command string.
        var command = info.GetArg(0);
        var weaponAttribute = WeaponsConfig?.Where(weapon => weapon.Value.PurchaseCommand!.Contains(command)).FirstOrDefault().Value;

        if(weaponAttribute != null && client != null)
            PurchaseWeapon(client, weaponAttribute);
    }

    public void WeaponPurchaseChat(CCSPlayerController client, string message)
    {
        if(message.StartsWith("!"))
            message = string.Concat("css_", message.AsSpan(1));

        var weaponAttribute = WeaponsConfig?.Where(weapon => weapon.Value.PurchaseCommand?.Contains(message) ?? false).FirstOrDefault().Value;

        if(weaponAttribute != null && client != null)
            PurchaseWeapon(client, weaponAttribute);
    }

    public void PurchaseWeapon(CCSPlayerController client, WeaponAttribute attribute)
    {
        // if not enable then we don't have to proceed any further.
        if(!GameSettings.Settings?.WeaponPurchaseEnable ?? false)
            return;

        // double check for possible
        if(attribute == null || client == null)
            return;

        // if not alive.
        if(!Utils.IsPlayerAlive(client))
        {
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.MustBeAlive"]}");
            return;
        }

        // if is zombie
        if(Infect.IsClientInfect(client))
        {
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Core.MustBeHuman"]}");
            return;
        }

        // this check for buyzone if enable.
        var buyzone = GameSettings.Settings?.WeaponBuyZoneOnly ?? false;

        if(buyzone && !Utils.IsClientInBuyZone(client))
        {
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Weapon.BuyZoneOnly"]}");
            return;
        }

        // if it's restricted
        if(IsRestricted(attribute.WeaponEntity!))
        {
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Weapon.IsRestricted", attribute.WeaponName!]}");
            return; 
        }

        // check their cash in account
        if(client.InGameMoneyServices?.Account < attribute.Price)
        {
            client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Weapon.NotEnoughCash"]}");
            return;
        }

        // Max Purchase section, will be added later.
        if(attribute.MaxPurchase != 0)
        {
            var count = 0;

            if(PlayerData.PlayerPurchaseCount == null)
            {
                _logger.LogError("[PurchaseWeapon] Player Purchase count is null!");
                return;
            }

            if(!PlayerData.PlayerPurchaseCount.ContainsKey(client))
            {
                _logger.LogError("[PurchaseWeapon] Player {name} is not in purchase count data, so create a new one", client.PlayerName);
                PlayerData.PlayerPurchaseCount.Add(client, new());
            }

            if(PlayerData.PlayerPurchaseCount[client].WeaponCount == null)
            {
                _logger.LogError("[PurchaseWeapon] Player {name} Purchase data is null", client.PlayerName);
                return;
            }

            if(PlayerData.PlayerPurchaseCount[client].WeaponCount!.ContainsKey(attribute.WeaponEntity!))
                count = PlayerData.PlayerPurchaseCount[client].WeaponCount![attribute.WeaponEntity!];

            if(count >= attribute.MaxPurchase)
            {
                client.PrintToChat($" {_core.Localizer["Prefix"]} {_core.Localizer["Weapon.ReachMaxPurchase", attribute.MaxPurchase]}");
                return;
            }
        }
        
        // we need to force drop weapon first before make an purchase
        var weapons = client.PlayerPawn.Value?.WeaponServices?.MyWeapons;

        if(weapons == null)
        {
            _logger.LogError("[PurchaseWeapon] {0} Weapon service is somehow null", client.PlayerName);
            return;
        }

        foreach(var weapon in weapons)
        {
            var slot = (int)weapon.Value!.GetVData<CCSWeaponBaseVData>()!.GearSlot;

            // for primary and secondary only.
            if(slot > 2)
                continue;

            if(slot == attribute.WeaponSlot)
            {
                // drop this weapon then break
                Utils.DropWeaponByDesignName(client, weapon.Value.DesignerName);
                break;
            }
        }

        Server.NextWorldUpdate(() => 
        {
            // we give weapon to them this part can't be null unless server manager fucked it up.
            if(attribute.WeaponEntity == "item_kevlar")
            {
                client.PlayerPawn.Value!.ArmorValue = 100;
                Utilities.SetStateChanged(client.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
            }
            
            else
                client.GiveNamedItem(attribute.WeaponEntity!);

            // update purchase history
            if(PlayerData.PlayerPurchaseCount![client].WeaponCount!.ContainsKey(attribute.WeaponEntity!))
                PlayerData.PlayerPurchaseCount![client].WeaponCount![attribute.WeaponEntity!]++;

            else
                PlayerData.PlayerPurchaseCount?[client].WeaponCount?.Add(attribute.WeaponEntity!, 1);

            var purchaseCount = PlayerData.PlayerPurchaseCount![client].WeaponCount![attribute.WeaponEntity!];

            var message = $" {_core.Localizer["Prefix"]} {_core.Localizer["Weapon.PurchaseSuccess", attribute.WeaponName!]}";

            if(attribute.MaxPurchase > 0)
                message += $" {_core.Localizer["Weapon.PurchaseCount", attribute.MaxPurchase - purchaseCount, attribute.MaxPurchase]}";

            client.PrintToChat($"{message}");

            // updated their cash.
            client.InGameMoneyServices!.Account -= attribute.Price;
            Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
        });
    }

    public static bool IsRestricted(string weaponentity)
    {
        if(WeaponsConfig == null)
            return false;

        var weapon = GetWeaponAttributeByEntityName(weaponentity);

        if(weapon == null)
            return false;

        return weapon.Restrict;
    }

    public static WeaponAttribute? GetWeaponAttributeByEntityName(string? weaponentity)
    {
        if(WeaponsConfig == null)
            return null;

        return WeaponsConfig.Where(data => data.Value.WeaponEntity == weaponentity).FirstOrDefault().Value;
    }

    public static WeaponAttribute? GetWeaponAttributeByName(string? weapon)
    {
        if(WeaponsConfig == null)
            return null;

        return WeaponsConfig.Where(data => string.Equals(data.Value.WeaponName, weapon, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
    }
}