namespace ZombieSharp.Models;

public class GameConfigs
{
    // Infection stuff.
    public float FirstInfectionTimer { get; set; } = 15f;
    public float MotherZombieRatio { get; set; } = 7f;
    public bool MotherZombieTeleport { get; set; } = false;
    public bool CashOnDamage { get; set; } = false;
    public int TimeoutWinner { get; set; } = 1;
    public float MaxKnifeRange { get; set; } = 75f;

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

    // win overlay stuff
    public string HumanWinOverlayParticle { get; set; } = string.Empty;
    public string HumanWinOverlayMaterial { get; set; } = string.Empty;
    public string ZombieWinOverlayParticle { get; set; } = string.Empty;
    public string ZombieWinOverlayMaterial { get; set; } = string.Empty;

    // config path
    public string WeaponPath { get; set; } = "weapons.jsonc";
}