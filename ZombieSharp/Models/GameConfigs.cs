namespace ZombieSharp.Models;

public class GameConfigs
{
    // Infection stuff.
    public float FirstInfectionTimer { get; set; } = 15f;
    public float MotherZombieRatio { get; set; } = 7f;

    // human default class stuff.
    public string? DefaultHumanBuffer { get; set; }
    public string? DefaultZombieBuffer { get; set; }
    public string? MotherZombieBuffer { get; set; }

    // weapons section
    public bool WeaponPurchaseEnable { get; set; } = false;
    public bool WeaponRestrictEnable { get; set; } = true; 
    public bool WeaponBuyZoneOnly { get; set; } = false;
}