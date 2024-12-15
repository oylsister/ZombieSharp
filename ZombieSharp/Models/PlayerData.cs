using CounterStrikeSharp.API.Core;

namespace ZombieSharp.Models;

public class PlayerData
{
    public static Dictionary<CCSPlayerController, ZombiePlayer>? ZombiePlayerData { get; set; } = null;
    public static Dictionary<CCSPlayerController, PlayerClasses>? PlayerClassesData { get; set; } = null;
    public static Dictionary<CCSPlayerController, PurchaseCount>? PlayerPurchaseCount { get; set; } = null;
}
