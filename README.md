# ZombieSharp
 
Zombie-Sharp is a Zombie Mode plugin for CS2 referencing the features and functions from the previous SourcePawn Zombie:Reloaded plugin. You can say this is the Zombie:Reloaded remake but in C#. Here is the list of features that will be featured in the Beta.

### Feature of Zombie-Sharp
- [x] Basic Zombie Infection Initial with Timer
- [x] Mother Zombie Cycle
- [x] Infect and Human Command
- [x] Respawn Option
- [ ] Player Class Module
- [x] Weapon Module
- [x] Hitgroups Module
- [x] Knockback Module
- [x] ZTeleport Module
- [x] Configuration for Infection Settings (Previously: ConVar)


### We are now in Pre-Alpha!
To get knockback working correctly you will need [MovementUnlocker](https://github.com/Source2ZE/MovementUnlocker) plugin. 
It's recommend to set these Convar before using the plugin to prevent crashed and issues that may occur. Since the respawn function still have an issue, it's recommend to enable ``mp_respawn_on_death_ct`` and ``mp_respawn_on_death_t`` to 1. 
```
mp_limitteams 0
mp_autoteambalance 0
mp_disconnect_kills_players 0 // set in gamemode_casual.cfg
mp_respawn_on_death_ct 1
mp_respawn_on_death_t 1
```

### Other config files
Here is a list of all the config files available and what they do.

weapons.json - Configure specific weapon settings.
```json
{
    "KnockbackMultiply": 1.0, // Knockback Multiply for all weapon
    "glock": { // The weaponname get from event when get fired.
        "WeaponName": "Glock", // weapon name you wish
        "WeaponEntity": "weapon_glock", // weapon entity
        "Knockback": 1.1 // knockback
    },
}
```
hitgroups.json - Hitgroup configuration for knockback.
```json
{
    "Generic": { // name of the part, doesn't affect anything
        "HitgroupIndex": 0, // hitgroup index DO NOT CHANGE THIS
        "HitgroupKnockback": 1.0 // knockback multiply when get hit to this hitgroup
    },
    "Head": {
        "HitgroupIndex": 1,
        "HitgroupKnockback": 1.2
    }
}
```
default.json - Custom Settings. These can be set for specific maps too. Example: de_dust2.json. If it doesn't find de_dust2.json first it will use the default.json file instead.
```json
{
    "RespawnTimer": 5.0, // respawn timer when die (in progress, not done yet)
    "FirstInfectionTimer": 15.0, // First infection timer in seconds
    "MotherZombieRatio": 7.0, // Mother Zombie Spawn ratio (14 players / 7.0 ratio = 2 Mother zombie)
    "TeleportMotherZombie": true // Teleport mother zombie to spawn after get infected (Useful for Zombie Escape)
}
```
