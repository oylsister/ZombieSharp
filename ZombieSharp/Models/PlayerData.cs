using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using ZombieSharp.Plugin;

namespace ZombieSharp.Models;

public class PlayerData
{
    public static Dictionary<CCSPlayerController, ZombiePlayer>? ZombiePlayerData { get; set; } = [];
    public static Dictionary<CCSPlayerController, PlayerClasses>? PlayerClassesData { get; set; } = [];
    public static Dictionary<CCSPlayerController, PurchaseCount>? PlayerPurchaseCount { get; set; } = [];
    public static Dictionary<CCSPlayerController, SpawnData>? PlayerSpawnData { get; set; } = [];
    public static Dictionary<CCSPlayerController, CParticleSystem?>? PlayerBurnData { get; set; } = [];
    public static Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer?>? PlayerRegenData { get; set; } = [];
    public static Dictionary<CCSPlayerController, ZombieSound> PlayerSoundData { get; set; } = [];
}
