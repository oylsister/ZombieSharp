# ZombieSharp
 
Zombie-Sharp is a Zombie Mode plugin for CS2 referencing the features and functions from the previous SourcePawn Zombie:Reloaded plugin. You can say this is the Zombie:Reloaded remake but in C#. Here is the list of features. <b>We're now in BETA.</b>

### Feature of Zombie-Sharp
- [x] Basic Zombie Infection Initial with Timer
- [x] Mother Zombie Cycle
- [x] Infect and Human Command
- [x] Respawn Option
- [x] Player Class Module (Mostly work, except player speed.)
- [x] Weapon Module
- [x] Hitgroups Module
- [x] Knockback Module
- [x] ZTeleport Module
- [x] Configuration for Infection Settings (Previously: ConVar)
- [x] Repeat Killer Module (NEW!)

### Requirements
- [Metamode:Source](https://www.sourcemm.net/downloads.php/?branch=master) Dev build (2.x).
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) 
- [MovementUnlocker](https://github.com/Source2ZE/MovementUnlocker) plugin for knockback.
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/releases) (This is already included in Release)
- [PrecaheResource](https://github.com/KillStr3aK/ResourcePrecacher/) for Zombie and Player model etc.

### Installation
1. Install a Metamod and CounterStrikeSharp with Runtime build.
2. Drag All files in zip to ``csgo/addons/counterstrikesharp/``.
3. Doing command setting and file configuration.
4. Start server and enjoy.

### Command Setting
It's recommend to set these Convar before using the plugin to prevent crashed and issues that may occur.
```
mp_limitteams 0 // set in server.cfg
mp_autoteambalance 0 // set in server.cfg
mp_disconnect_kills_players 0 // set in gamemode_casual.cfg
```

### Configuration
Here is a list of all the config files available and what they do.

weapons.json - Configure specific weapon settings.
```json
{
    "KnockbackMultiply": 1.0, // Knockback Multiply for all weapon
    "WeaponDatas":{
        "glock": { // The weaponname get from event when get fired.
            "WeaponName": "Glock", // weapon name you wish
            "WeaponEntity": "weapon_glock", // weapon entity
            "Knockback": 1.1 // knockback
        }
    }
}
```
playerclasses.json - Player Classes configuration.
```json
{
    "PlayerClasses":{
        "human_default": { // Class unique name
            "Name": "Human Default", // class name
            "Description": "Default Class for human", // description
            "Enable": true, // enable it or not
            "Team": 1, // Team 0 = zombie, Team 1 = human
            "Model": "", // Model path for this class
            "MotherZombie": false, // Specify if this class is for mother zombie.
            "Health": 150, // class health
            "Regen_Interval": 0.0, // Specify how much second to regen health
            "Regen_Amount": 0, // Regen Health amount
            "Speed": 250.0, // class speed (not work yet)
            "Knockback": 0.0, // class knockback
            "Jump_Height": 3.0, // Jump height
            "Jump_Distance": 1.0 // Jump Distance
        }
    }
}
```
hitgroups.json - Hitgroup configuration for knockback.
```json
{
    "HitGroupDatas": {
        "Generic": { // name of the part, doesn't affect anything
            "HitgroupIndex": 0, // hitgroup index DO NOT CHANGE THIS
            "HitgroupKnockback": 1.0 // knockback multiply when get hit to this hitgroup
        },
        "Head": {
            "HitgroupIndex": 1,
            "HitgroupKnockback": 1.2
        }
    }
}
```
default.json - Custom Settings. These can be set for specific maps too. Example: de_dust2.json. If it doesn't find de_dust2.json first it will use the default.json file instead.
```json
{
    "RespawnTimer": 5.0, // respawn timer when die (in progress, not done yet)
    "FirstInfectionTimer": 15.0, // First infection timer in seconds
    "MotherZombieRatio": 7.0, // Mother Zombie Spawn ratio (14 players / 7.0 ratio = 2 Mother zombie)
    "TeleportMotherZombie": true, // Teleport mother zombie to spawn after get infected (Useful for Zombie Escape)
    "EnableOnWarmup": false, // Enable Infection in warmup round or not?, this is not recommend to enable as it has potential memory corrupt to the server.
    "RepeatKillerThreshold": 3.0, // Repeat Killer Threshould to prevent zombie dying from respawn over and over again.
    "Human_Default": "human_default", // Default Human Class
    "Zombie_Default": "zombie_default", // Default Zombie Class
    "Mother_Zombie": "motherzombie" // Default Mother Zombie Class
}
```
