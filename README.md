# ZombieSharp
 
Zombie-Sharp is a plugin project for CS2 Zombie-Mode. By referencing the feature and the function from previous SourcePawn Zombie:Reloaded plugins. You can said this is the Zombie:Reloaded remake except is C#. Here is the list of feature that will be feature on Beta stage.

### Feature of Zombie-Sharp (list todo)
- [x] Basic Zombie Infection Initial with Timer.
- [x] Mother Zombie Cycle.
- [x] Infect and Human Command.
- [ ] Respawn Option
- [ ] Player Class Module
- [x] Weapon Module
- [x] Hitgroup Module
- [x] Knockback Module
- [x] ZTeleport Module
- [x] Configuratable for Infection setting (Previously: ConVar)


### We are now Pre-Alpha released!

To get knockback work properly you will need [MovementUnlocker](https://github.com/Source2ZE/MovementUnlocker) plugin. 

it's recommend to set these Convar before using the plugin to prevent crashed and issues that may occur.
```
mp_limitteams 0
mp_autoteambalance 0
mp_disconnect_kills_players 0 // set in gamemode_casual.cfg
```

You can figure a Weapon Config in weapons.json
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
Hitgroup configuration for knockback when get hit in specific of player body (hitgroups.json)
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
Game Settings for specific a thing you want. Default is default.json, but it will try to find that match with mapname (example: Map de_dust2 it will try to find de_dust2.json first. if doesn't found the file, it will use the default value instead)
```json
{
    "RespawnTimer": 5.0, // respawn timer when die (in progress, not done yet)
    "FirstInfectionTimer": 15.0, // First infection timer in seconds
    "MotherZombieRatio": 7.0, // Mother Zombie Spawn ratio (14 players / 7.0 ratio = 2 Mother zombie)
    "TeleportMotherZombie": true // Teleport mother zombie to spawn after get infected (Useful for Zombie Escape)
}
```
