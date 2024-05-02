# ZombieSharp
<b>This project is linux focusing as Windows has a lot issues with Virtual Functions. However, if Windows issue is solved, the plugin will mostly work fine.</b>

Zombie-Sharp is a Zombie Mode plugin for CS2 referencing the features and functions from the previous SourcePawn Zombie:Reloaded plugin. You can say this is the Zombie:Reloaded remake but in C#. Here is the list of features.

### Feature of Zombie-Sharp
- [x] Basic Zombie Infection Initial with Timer
- [x] Mother Zombie Cycle
- [x] Infect and Human Command
- [x] Respawn Toggle option
- [x] Player Class Module (Mostly work, except player speed.)
- [x] Weapon Module with purchase command.
- [x] Hitgroups Module
- [x] Knockback Module
- [x] ZTeleport Module
- [x] Configuration for Infection Settings (ConVar)
- [x] Repeat Killer Module (Obsolete now)
- [x] Top Defender
- [x] Cash on damage zombie

### Requirements
- [Metamode:Source](https://www.sourcemm.net/downloads.php/?branch=master) Dev build (2.x).
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) 
- [MovementUnlocker](https://github.com/Source2ZE/MovementUnlocker) plugin for knockback.
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/releases) (This is already included in Release)
- [Dual Mounting](https://github.com/Source2ZE/MultiAddonManager) for Custom Content for zombie mod.

### Recommend Plugin
- [NoBlock](https://github.com/ManifestManah/NoBlock) for Zombie Escape mode.

### How to Build
1. Install [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) and [Git](https://git-scm.com/downloads).
2. Open up Windows Powershell and follow these command
```shell
git clone https://github.com/Oylneechan/ZombieSharp
cd ZombieSharp
dotnet build
```

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
mp_disconnect_kills_players 1 // set in gamemode_casual.cfg
```

### Configuration
Here is a list of all the config files available and what they do.

weapons.json - Configure specific weapon settings. And purchase settings
```json
{
    "KnockbackMultiply": 1.0, // Knockback Multiply for all weapon
    "WeaponDatas":{
        "glock": { // The weaponname get from event when get fired.
            "WeaponName": "Glock", // weapon name you wish
            "WeaponEntity": "weapon_glock", // weapon entity
            "Knockback": 1.1, // knockback
            "WeaponSlot": 1, // weaponslot (0 = Primary, 1 = Secondary, 2 = knife, 3 = grenade)
            "Price": 200, // price you want
            "MaxPurchase": 0, // Allowing how many time client to purchase in one live.
            "Restrict": false, // Allow client to use or not.
            "PurchaseCommand": [ "css_glock", "css_gs" ] // Purchase command. Set whatever you want.
        }
    }
}
```
playerclasses.json - Player Classes configuration.
<b>Placing Custom model at</b> ``game/csgo`` <b>for both server and client (player)</b>

Example: ``game/csgo/characters/models/nozb1/2b_nier_automata_player_model/2b_nier_player_model.vmdl_c``
```json
{
    "PlayerClasses":{
        "human_default": { // Class unique name
            "Name": "Human Default", // class name
            "Description": "Default Class for human", // description
            "Enable": true, // enable it or not
            "Team": 1, // Team 0 = zombie, Team 1 = human
            "Model": "characters\\models\\nozb1\\2b_nier_automata_player_model\\2b_nier_player_model.vmdl", // Model path for this class change .vmdl_c to .vmdl in this config
            "MotherZombie": false, // Specify if this class is for mother zombie.
            "Health": 150, // class health
            "Regen_Interval": 0.0, // Specify how much second to regen health
            "Regen_Amount": 0, // Regen Health amount
            "Speed": 250.0, // class speed (not work yet)
            "Knockback": 0.0, // class knockback
            "Jump_Height": 3.0, // Jump height
            "Jump_Distance": 1.0, // Jump Distance
            "Fall_Damage": false // Disable fall damage or not
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