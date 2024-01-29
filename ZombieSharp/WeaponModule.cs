using CounterStrikeSharp.API.Modules.Entities.Constants;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public WeaponConfig WeaponDatas { get; private set; }

        public Dictionary<int, PurchaseHistory> PlayersHistory = new();

        bool CommandInitialized = false;

        public void WeaponInitialize()
        {
            var configPath = Path.Combine(ModuleDirectory, "weapons.json");

            if (!File.Exists(configPath))
            {
                Logger.LogError("Cannot found weapons.json file!");
                return;
            }

            WeaponDatas = JsonConvert.DeserializeObject<WeaponConfig>(File.ReadAllText(configPath));

            if (WeaponDatas.EnableBuyModule)
                WeaponCommandInitialize();
        }

        public void WeaponOnClientPutInServer(int playerSlot)
        {
            PlayersHistory.Add(playerSlot, new PurchaseHistory());
        }

        public void WeaponOnClientDisconnect(int playerSlot)
        {
            PlayersHistory.Remove(playerSlot);
        }

        public void WeaponOnPlayerSpawn(int playerSlot)
        {
            if (PlayersHistory.ContainsKey(playerSlot))
                PlayersHistory[playerSlot].PlayerBuyHistory.Clear();
        }

        private void WeaponCommandInitialize()
        {
            if (CommandInitialized)
                return;

            if (WeaponDatas == null)
                return;

            foreach (var weapon in WeaponDatas.WeaponConfigs)
            {
                if (weapon.Value.PurchaseCommand != null)
                {
                    foreach (var command in weapon.Value.PurchaseCommand)
                    {
                        AddCommand(command, "Buy Weapon Command", PurchaseWeaponCommand);
                    }
                }
            }

            CommandInitialized = true;
        }

        public void PurchaseWeaponCommand(CCSPlayerController client, CommandInfo info)
        {
            if (client == null)
                return;

            var weaponCommand = info.GetArg(0);

            foreach (string weapon in WeaponDatas.WeaponConfigs.Keys)
            {
                if (WeaponDatas.WeaponConfigs[weapon].PurchaseCommand != null)
                {
                    foreach (var command in WeaponDatas.WeaponConfigs[weapon].PurchaseCommand)
                    {
                        if (weaponCommand == command)
                        {
                            PurchaseWeapon(client, weapon);
                            break;
                        }
                    }
                }
            }
        }

        public void PurchaseWeapon(CCSPlayerController client, string weapon)
        {
            var weaponConfig = WeaponDatas!.WeaponConfigs[weapon];

            if (weaponConfig == null)
            {
                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Invalid weapon!");
                return;
            }

            if (!IsPlayerAlive(client))
            {
                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} this feature need you to be alive!");
                return;
            }

            if (IsClientZombie(client))
            {
                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} this feature is for human only!");
                return;
            }

            if (weaponConfig.Restrict)
            {
                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Weapon {ChatColors.Lime}{weaponConfig.WeaponName}{ChatColors.Default} is restricted");
                return;
            }

            var clientMoney = client.InGameMoneyServices!.Account;

            if (clientMoney < weaponConfig.Price)
            {
                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} You don't have enough money to purchase this weapon!");
                return;
            }

            int weaponPurchased;
            bool weaponFound = PlayersHistory[client.Slot].PlayerBuyHistory.TryGetValue(weapon, out weaponPurchased);

            if (weaponConfig.MaxPurchase > 0)
            {
                if (weaponFound)
                {
                    if (weaponPurchased >= weaponConfig.MaxPurchase)
                    {
                        client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have reached maximum purchase for {ChatColors.Lime}{weaponConfig.WeaponName}{ChatColors.Default}, you can purchase again in next round");
                        return;
                    }
                    else
                    {
                        //client.PrintToChat($"{ChatColors.Lime}{weapon}{ChatColors.Default} Purchased: {weaponPurchased}");
                        PlayersHistory[client.Slot].PlayerBuyHistory[weapon] = weaponPurchased + 1;
                    }
                }
                else
                {
                    PlayersHistory[client.Slot].PlayerBuyHistory.Add(weapon, 1);
                }

                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have purchase {ChatColors.Lime}{weaponConfig.WeaponName}{ChatColors.Default}, Purchase Limit: {ChatColors.Green}{weaponConfig.MaxPurchase - weaponPurchased - 1}/{weaponConfig.MaxPurchase}");
            }

            else
            {
                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have purchase {ChatColors.Lime}{weaponConfig.WeaponName}{ChatColors.Default}.");
            }

            client.ExecuteClientCommand("slot3");
            client.ExecuteClientCommand($"slot{weaponConfig.WeaponSlot + 1}");

            var activeweapon = client.PlayerPawn.Value!.WeaponServices!.ActiveWeapon;

            if (activeweapon != null && activeweapon.Value!.DesignerName != "weapon_knife")
                client.DropActiveWeapon();

            client.GiveNamedItem(weaponConfig.WeaponEntity!);

            client.InGameMoneyServices.Account = clientMoney - weaponConfig.Price;
            Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");

            AddTimer(0.2f, () =>
            {
                client.ExecuteClientCommand($"slot{weaponConfig.WeaponSlot + 1}");
            });
        }

        public string FindWeaponItemDefinition(CHandle<CBasePlayerWeapon> weapon, string weaponstring)
        {
            var item = (ItemDefinition)weapon.Value.AttributeManager.Item.ItemDefinitionIndex;

            if (weaponstring == "m4a1")
            {
                switch (item)
                {
                    case ItemDefinition.M4A1_S: return "m4a1_silencer";
                    case ItemDefinition.M4A4: return "m4a1";
                }
            }

            else if (weaponstring == "hkp2000")
            {
                switch (item)
                {
                    case ItemDefinition.P2000: return "hkp2000";
                    case ItemDefinition.USP_S: return "usp_silencer";
                }
            }

            else if (weaponstring == "mp7")
            {
                switch (item)
                {
                    case ItemDefinition.MP7: return "mp7";
                    case ItemDefinition.MP5_SD: return "mp5sd";
                }
            }

            return weaponstring;
        }

        public bool WeaponIsRestricted(string weaponentity)
        {
            /*
            foreach (string weapon in WeaponDatas.WeaponConfigs.Keys)
            {
                return WeaponDatas.WeaponConfigs[weapon].Restrict;
            }
            */

            var key = GetKeyByWeaponEntity(weaponentity);

            if (key != null)
                return WeaponDatas.WeaponConfigs[key].Restrict;

            return false;
        }

        public string GetKeyByWeaponEntity(string weaponentity)
        {
            string result = null;

            foreach (string weapon in WeaponDatas.WeaponConfigs.Keys)
            {
                if (WeaponDatas.WeaponConfigs[weapon].WeaponEntity == weaponentity)
                {
                    result = weapon;
                    return result;
                }
            }

            return result;
        }
    }
}

public class PurchaseHistory
{
    public Dictionary<string, int> PlayerBuyHistory { get; set; } = new Dictionary<string, int>();
}

public class WeaponConfig
{
    public float KnockbackMultiply { get; set; } = 1.0f;
    public bool EnableBuyModule { get; set; } = false;

    public Dictionary<string, WeaponData> WeaponConfigs { get; set; } = new Dictionary<string, WeaponData>();

    public WeaponConfig()
    {
        WeaponConfigs = new Dictionary<string, WeaponData>(StringComparer.OrdinalIgnoreCase)
        {
            { "glock", new WeaponData("Glock", "weapon_glock", 1.0f) },
        };
    }
}

public class WeaponData
{
    public WeaponData(string weaponName, string weaponEntity, float knockback)
    {
        WeaponName = weaponName;
        WeaponEntity = weaponEntity;
        Knockback = knockback;
    }

    public string WeaponName { get; set; }
    public string WeaponEntity { get; set; }
    public float Knockback { get; set; }
    public int WeaponSlot { get; set; }
    public int Price { get; set; }
    public int MaxPurchase { get; set; }
    public bool Restrict { get; set; }
    public List<string> PurchaseCommand { get; set; }
}