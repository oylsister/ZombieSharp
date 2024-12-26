using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Cvars.Validators;

namespace ZombieSharp;

public partial class ZombieSharp
{
    public FakeConVar<float> CVAR_FirstInfectionTimer = new("zs_infect_timer", "First Infection Countdown", 15f, default, new RangeValidator<float>(5.0f, 60.0f));
    public FakeConVar<float> CVAR_MotherZombieRatio = new("zs_infect_motherzombie_ratio", "Mother zombie ratio", 7f, default, new RangeValidator<float>(1.0f, 64.0f));
    public FakeConVar<bool> CVAR_MotherZombieTeleport = new("zs_infect_motherzombie_spawn", "Teleport Motherzombie back to spawn", false, default, new RangeValidator<bool>(false, true));
    public FakeConVar<bool> CVAR_CashOnDamage = new("zs_infect_cash_damage_enable", "Enable earning cash from damaging player", false, default, new RangeValidator<bool>(false, true));
    public FakeConVar<int> CVAR_TimeoutWinner = new("zs_infect_timeout_winner", "Winner team when timeout (0 = Zombie | 1 = Human)", 1, default, new RangeValidator<int>(0, 1));

    public FakeConVar<string> CVAR_DefaultHuman = new("zs_classes_default_human", "Default classes for human", "human_default");
    public FakeConVar<string> CVAR_DefaultZombie = new("zs_classes_default_zombie", "Default classes for human", "zombie_default");
    public FakeConVar<string> CVAR_MotherZombie = new("zs_classes_motherzombie", "Default classes for motherzombie", "motherzombie");
    public FakeConVar<bool> CVAR_RandomClassesOnConnect = new("zs_classes_random_connect", "Assign Random Classes on player connect", true, default, new RangeValidator<bool>(false, true));
    public FakeConVar<bool> CVAR_RandomClassesOnSpawn = new("zs_classes_random_spawn", "Assign Random Classes on player spawn", true, default, new RangeValidator<bool>(false, true));
    public FakeConVar<bool> CVAR_AllowSavingClass = new("zs_classes_allow_save", "Allowing player to save their classes", true, default, new RangeValidator<bool>(false, true));
    public FakeConVar<bool> CVAR_AllowChangeClass = new("zs_classes_allow_change", "Allowing player to change their classes", true, default, new RangeValidator<bool>(false, true));

    public FakeConVar<bool> CVAR_WeaponPurchaseEnable = new("zs_weapon_purchase_enable", "Enable Weapon purchase via command", true, default, new RangeValidator<bool>(false, true));
    public FakeConVar<bool> CVAR_WeaponRestrictEnable = new("zs_weapon_restrict_enable", "Enable Weapon restriction", true, default, new RangeValidator<bool>(false, true));
    public FakeConVar<bool> CVAR_WeaponBuyZoneOnly = new("zs_weapon_purchase_buyzone", "Only allowing weapon purchase in buyzone only", false, default, new RangeValidator<bool>(false, true));

    public FakeConVar<bool> CVAR_TeleportAllow = new("zs_ztele_enable", "Allowing player to use !ztele for teleport", true, default, new RangeValidator<bool>(false, true));

    public FakeConVar<bool> CVAR_RespawnEnable = new("zs_respawn_enable", "Allowing respawn after die", false, default, new RangeValidator<bool>(false, true));
    public FakeConVar<float> CVAR_RespawnDelay = new("zs_respawn_delay", "Respawn Delaying", 5f, default, new RangeValidator<float>(0.1f, 60.0f));
    public FakeConVar<bool> CVAR_AllowRespawnJoinLate = new("zs_respawn_allow_join_late", "Allowing player who join game late to spawn during the round", false, default, new RangeValidator<bool>(false, true));
    public FakeConVar<int> CVAR_RespawnTeam = new("zs_respawn_team", "Specify team to respawn with after death (0 = Zombie | 1 = Human | 2 = Player Team before death)", 0, default, new RangeValidator<int>(0, 2));
}