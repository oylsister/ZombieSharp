using CounterStrikeSharp.API.Core;

namespace ZombieSharp.Models;

public class PlayerData
{
    public static Dictionary<CCSPlayerController, ZombiePlayer>? ZombiePlayerData { get; set; } = [];
    public static Dictionary<CCSPlayerController, PlayerClasses>? PlayerClassesData { get; set; } = [];
    public static Dictionary<CCSPlayerController, PurchaseCount>? PlayerPurchaseCount { get; set; } = [];
    public static Dictionary<CCSPlayerController, SpawnData>? PlayerSpawnData { get; set; } = [];
}
