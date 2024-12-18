namespace ZombieSharp.Models;

public class GameConfigs
{
    // Infection stuff.
    public float FirstInfectionTimer { get; set; } = 15f;
    public float MotherZombieRatio { get; set; } = 7f;
    public bool MotherZombieTeleport { get; set; } = false;

    // human default class stuff.
    public string? DefaultHumanBuffer { get; set; }
    public string? DefaultZombieBuffer { get; set; }
    public string? MotherZombieBuffer { get; set; }
    public bool RandomClassesOnConnect { get; set; } = true;
    public bool RandomClassesOnSpawn { get; set; } = false;
    public bool AllowSavingClass { get; set; } = true;
    public bool AllowChangeClass { get; set; } = true;

    // weapons section
    public bool WeaponPurchaseEnable { get; set; } = false;
    public bool WeaponRestrictEnable { get; set; } = true; 
    public bool WeaponBuyZoneOnly { get; set; } = false;

    // teleport section
    public bool TeleportAllow { get; set; } = true;

    // respawn section
    public bool RespawnEnable { get; set; } = false;
    public float RespawnDelay { get; set; } = 5.0f;
    public bool AllowRespawnJoinLate { get; set; } = false;
    public int RespawTeam { get; set; } = 0;
}