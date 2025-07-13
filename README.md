# ZombieSharp

### Notice 07/13/2025

I decided to discontinue this project due to serveral reason.
1. CounterStrikeSharp's capability lacking a feature that I want to expand the feature.
2. Performanace issues, I take a measure after testing with 64 players comparing with CS2Fixes zombie reborn, ZombieSharp is worse.
3. In real life stuff, I'm hunting for a job right now lol.
4. Incoming of ModSharp (even it could take to 2035).

### I TESTED RUNNING THIS PLUGIN WITH CS2FIXES. AS LONG AS YOU DON'T ENABLE ZOMBIE:REBORN, YOU CAN USE CS2FIXES ALONG WITH ZOMBIESHARP PLUGIN.

Zombie-Sharp is a Zombie Mode plugin for CS2 referencing the features and functions from the previous SourcePawn Zombie:Reloaded plugin. You can say this is the Zombie:Reloaded remake but in C#. Here is the list of features.

### Feature of Zombie-Sharp
- [x] Basic Zombie Infection Initial with Timer
- [x] Mother Zombie Cycle
- [x] Infect and Human Command
- [x] Respawn Toggle option
- [x] Player Class Module
- [x] Weapon Module with purchase command.
- [x] Hitgroups Module (Will add later)
- [x] Knockback Module
- [x] ZTeleport Module
- [x] Configuration for Infection Settings (ConVar)
- [x] Cash on damage zombie
- [x] API for external plugin
- [x] Grenade Napalm Effect

### Requirements
- [Metamode:Source](https://www.sourcemm.net/downloads.php/?branch=master) Dev build (2.x).
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) 
- [CSSharpFixes](https://github.com/CharlesBarone/CSSharp-Fixes) or [MovementUnlocker](https://github.com/Source2ZE/MovementUnlocker) or [CS2-Sigpatcher](https://github.com/oylsister/CS2-SigPatcher) plugin for knockback.
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/releases) (This is already included in Release)
- [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager) for Custom Content for zombie mod.

### Recommend Plugin
- [NoBlock](https://github.com/ManifestManah/NoBlock) for Zombie Escape mode.

### How to Build
1. Install [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) and [Git](https://git-scm.com/downloads).
2. Open up Windows Powershell and follow these command
```shell
git clone https://github.com/oylsister/ZombieSharp
cd ZombieSharp
dotnet build
```

### Installation
1. Install a Metamod and CounterStrikeSharp with Runtime build.
2. Drag All files in zip to ``game/csgo``.
3. Doing command setting and file configuration.
4. Start server and enjoy.

### Command Setting
It's recommend to set these Convar before using the plugin to prevent crashed and issues that may occur.
```
// set in server.cfg
mp_limitteams 0
mp_autoteambalance 0

// set in gamemode_casual_server.cfg if file is not existed copy gamemode_casual.cfg and rename it.
mp_disconnect_kills_players 1
mp_roundtime 3 // set it to round time that you want.
mp_roundtime_hostage 0 // this will override mp_roundtime in the map "cs_" if value is more than 0.
mp_roundtime_defuse 0 // this will override mp_roundtime in the map "de_" if value is more than 0.
```

### API Example 
Check out [ZombieTest](https://github.com/oylsister/ZombieSharp/blob/main/ZombieTest/ZombieTest.cs) for other API usages example. 
```cs
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using ZombieSharpAPI;

namespace ZombieTest
{
    public class ZombieTest : BasePlugin    
    {
        public override string ModuleName => "Zombie Test";
        public override string ModuleVersion => "1.0";

        // Declare Capability First.
        public static PluginCapability<IZombieSharpAPI> ZombieCapability { get; } = new("zombiesharp:core");

        // Declare API class
        IZombieSharpAPI? API;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            // Get Capability.
            API = ZombieCapability.Get()!;

            // Excute Hook function 
            API.OnClientInfect += ZS_OnInfectClient;
        }

        // Hook function is here.
        public HookResult ZS_OnInfectClient(CCSPlayerController client, CCSPlayerController attacker, bool motherzombie, bool force)
        {
            // check which client is infect.
            Server.PrintToChatAll($"{client.PlayerName} is infected");

            // if client name is Oylsister
            if (client.PlayerName == "Oylsister")
            {
                Server.PrintToChatAll("Oylsister is immunity");

                // Blocking infected
                return HookResult.Handled;
            }
            
            if (force)
                Server.PrintToChatAll($"by forcing.");

            // Always use HookResult.Continue to allowing other player get infect as usual.
            return HookResult.Continue;
        }
    }
}
```

### Configuration
Here is a list of all the config files available and what they do.

gamesettings.jsonc - Basic Infection timer and more settings you can set. This file existed as to apply game settings in case of ConVar failed to do.

```jsonc
{
    "FirstInfectionTimer": 15.0, // First infection delaying in seconds
    "MotherZombieRatio": 7.0, // First mother zombie ratio (21 players / 7 ratio = 3 motherzombie will spawned.)
    "MotherZombieTeleport": false, // Teleport mother zombie back to spawn after infection (Zombie Escape stuff)
    "CashOnDamage": false, // Turn damage into cash for human.
    "TimeoutWinner": 1, // When round time end specific team will win (0 = zombie | 1 = human.)

    "DefaultHumanBuffer": "human_default", // human class unique name for default class
    "DefaultZombieBuffer": "zombie_default", // zombie class unique name for default class
    "MotherZombieBuffer": "motherzombie", // mother zombie class unique name for default class
    "RandomClassesOnConnect": false, // random class on join or not.
    "RandomClassesOnSpawn": true, // random class on every player respawn.

    "WeaponPurchaseEnable": true, // Enable purchase via command or not. This also will toggle weapon purchase limit too.
    "WeaponRestrictEnable": true, // Restrict specific weapon or not.
    "WeaponBuyZoneOnly": false, // Only allow player to purchase weapon in buyzone.

    "TeleportAllow": true, // allow using !ztele to teleport back to spawn point.

    "RespawnEnable": true, // allow respawn after death or not
    "RespawnDelay": 5.0, // respawn delay obviously
    "AllowRespawnJoinLate": false, // allow player to join during the round and respawn into the game or not.
    "RespawnTeam": 0 // 1 for human team | 0 for zombie team | 2 for player's they were before death.
}
```

weapons.jsonc - Configure specific weapon settings. And purchase settings
```jsonc
{
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
```
playerclasses.jsonc - Player Classes configuration.
<b>Placing Custom model at</b> ``game/csgo`` <b>for both server and client (player)</b>

Example: ``game/csgo/characters/models/nozb1/2b_nier_automata_player_model/2b_nier_player_model.vmdl_c``
```jsonc
{
    "human_default": { // Class unique name
        "Name": "Human Default", // class name
        "Enable": true, // enable it or not
        "Team": 1, // Team 0 = zombie, Team 1 = human
        "Model": "characters\\models\\nozb1\\2b_nier_automata_player_model\\2b_nier_player_model.vmdl", // Model path for this class change .vmdl_c to .vmdl in this config
        "MotherZombie": false, // Specify if this class is for mother zombie.
        "Health": 150, // class health
        "Regen_Interval": 1.0, // Regen_Interval is the time in seconds between each regen tick
        "Regen_Amount": 3, // Regen_Amount is the amount of health to regen each tick
        "Napalm_Time": 0, // Duration of Napalm grenade, set to 0 meaning no burn.
        "Speed": 250.0, // class speed (not work yet)
        "Knockback": 0.0, // class knockback
    }
}
```
hitgroups.jsonc - Hitgroup configuration for knockback.
```jsonc
{
    "Generic": { // name doesn't effect anything
        "Index": 0, // do not edit this part.
        "Knockback": 1.0 // you can change knockback to whatever you want.
    },
    "Head": {
        "Index": 1,
        "Knockback": 1.2
    },
    "Chest": {
        "Index": 2,
        "Knockback": 1.0
    },
    "Stomach": {
        "Index": 3,
        "Knockback": 1.0
    }
}
```
