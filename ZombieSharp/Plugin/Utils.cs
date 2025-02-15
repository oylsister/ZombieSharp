using System.Drawing;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class Utils
{
    private static ZombieSharp? _core;
    private static ILogger<ZombieSharp>? _logger;

    public Utils(ZombieSharp core, ILogger<ZombieSharp> logger)
    {
        _core = core;
        _logger = logger;
    }

    public static MemoryFunctionVoid<CBaseEntity, string, int, float, float> CBaseEntity_EmitSoundParamsFunc = new(GameData.GetSignature("CBaseEntity_EmitSoundParams"));
    public static MemoryFunctionVoid<CEntityIdentity, string> CEntityIdentity_SetEntityNameFunc = new(GameData.GetSignature("CEntityIdentity_SetEntityName"));

    public static void PrintToCenterAll(string message)
    {
        if(string.IsNullOrEmpty(message))
            return;

        foreach(var player in Utilities.GetPlayers())
        {
            player.PrintToCenter(message);
        }
    }

    public static void EmitSound(CBaseEntity entity, string soundPath, int pitch = 100, float volume = 1.0f, float deley = 0.0f)
    {
        if(entity == null || string.IsNullOrEmpty(soundPath))
            return;

        CBaseEntity_EmitSoundParamsFunc.Invoke(entity, soundPath, pitch, volume, deley);
    }

    public static void SetEntityName(CEntityInstance entity, string name)
    {
        if(entity == null || entity.Entity == null || string.IsNullOrEmpty(name))
            return;

        CEntityIdentity_SetEntityNameFunc.Invoke(entity.Entity, name);
    }

    public static CCSPlayerController? GetCCSPlayerController(CEntityInstance? instance)
    {
        if (instance == null)
            return null;

        if (instance.DesignerName != "player")
            return null;

        // grab the pawn index
        int index = (int)instance.Index;

        // grab player controller from pawn
        var pawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>(index);

        // pawn valid
        if (pawn == null || !pawn.IsValid)
            return null;

        // controller valid
        if (pawn.OriginalController == null || !pawn.OriginalController.IsValid)
            return null;

        // any further validity is up to the caller
        return pawn.OriginalController.Value;
    }

    public static string? FindWeaponItemDefinition(CBasePlayerWeapon? weapon)
    {
        if(weapon == null)
            return null;

        var item = (ItemDefinition)weapon.AttributeManager.Item.ItemDefinitionIndex;

        if (weapon.DesignerName == "weapon_m4a1")
        {
            switch (item)
            {
                case ItemDefinition.M4A1_S: return "weapon_m4a1_silencer";
                case ItemDefinition.M4A4: return "weapon_m4a1";
            }
        }

        else if (weapon.DesignerName == "weapon_hkp2000")
        {
            switch (item)
            {
                case ItemDefinition.P2000: return "weapon_hkp2000";
                case ItemDefinition.USP_S: return "weapon_usp_silencer";
            }
        }

        else if (weapon.DesignerName == "weapon_mp7")
        {
            switch (item)
            {
                case ItemDefinition.MP7: return "weapon_mp7";
                case ItemDefinition.MP5_SD: return "weapon_mp5sd";
            }
        }

        return weapon.DesignerName;
    }

    public static void DropAllWeapon(CCSPlayerController client, bool remove = false)
    {
        if(client == null)
            return;

        var service = client.PlayerPawn.Value?.WeaponServices;
        var weapons = client.PlayerPawn.Value?.WeaponServices?.MyWeapons;

        if(weapons == null || service == null)
        {
            _logger?.LogError("[DropAllWeapn] Client weapon service is null!");
            return;
        }

        foreach(var weapon in WeaponList)
        {
            DropWeaponByDesignName(client, weapon);
        }
    }

    public static void DropWeapon(CCSPlayer_WeaponServices services ,CBasePlayerWeapon weapon)
    {
        Guard.IsValidEntity(weapon);
        VirtualFunction.CreateVoid<nint, CBasePlayerWeapon, Vector?, Vector?>(services.Handle, GameData.GetOffset("CCSPlayer_WeaponServices_DropWeapon"))(services.Handle, weapon, null, null);
    }

    public static void DropWeaponByDesignName(CCSPlayerController client, string weaponName, bool remove = false)
    {
        if(client == null)
            return;
            
        var service = client.PlayerPawn.Value?.WeaponServices;
        var matchedWeapon = client.PlayerPawn.Value?.WeaponServices?.MyWeapons.Where(x => x.Value?.DesignerName == weaponName).FirstOrDefault();

        if (matchedWeapon != null && matchedWeapon.IsValid && service != null)
        {
            if(matchedWeapon.Value == null)
                return;

            DropWeapon(service.As<CCSPlayer_WeaponServices>(), matchedWeapon.Value);

            // set timer to remove if remove is true.
            if(remove)
            {
                _core?.AddTimer(1f, () => {
                    matchedWeapon.Value?.AddEntityIOEvent("Kill", matchedWeapon.Value, null, "", 0.1f);
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }
    }

    public static void RefreshPurchaseCount(CCSPlayerController client)
    {
        if(PlayerData.PlayerPurchaseCount == null)
        {
            _logger?.LogError("[RefreshPurchaseCount] Player Purchase count is null");
            return;
        }

        if(!PlayerData.PlayerPurchaseCount.ContainsKey(client))
        {
            _logger?.LogError("[RefreshPurchaseCount] {name} is not in array data, so create a new one for player.", client.PlayerName);
            PlayerData.PlayerPurchaseCount.Add(client, new());
        }

        // clear them all.
        PlayerData.PlayerPurchaseCount[client].WeaponCount?.Clear();
    }

    public static bool IsClientInBuyZone(CCSPlayerController client)
    {
        if(client == null)
            return false;

        return client.PlayerPawn.Value?.InBuyZone ?? false;
    }

    public static bool IsPlayerAlive(CCSPlayerController client)
    {
        if(client == null)
            return false;

        if(!client.IsValid)
        {
            _logger?.LogError("[IsPlayerAlive] Client is invalid pointer!");
            return false;
        }

        var clientPawn = client.PlayerPawn.Value;
        
        if(clientPawn == null || !clientPawn.IsValid)
            return false;

        return (LifeState_t)clientPawn.LifeState == LifeState_t.LIFE_ALIVE;
    }

    public static ClassAttribute? GetRandomPlayerClasses(int team)
    {
        if(Classes.ClassesConfig == null)
        {
            _logger?.LogError("[GetRandomPlayerClasses] ClassesConfig is null!");
            return null;
        }

        List<ClassAttribute> classes = [];

        foreach(var playerClasses in Classes.ClassesConfig)
        {
            if(playerClasses.Value.Team == team && playerClasses.Value.Enable)
                classes.Add(playerClasses.Value);
        }

        if(classes.Count <= 0)
        {
            _logger?.LogError("[GetRandomPlayerClasses] Can't get one from it since it's empty!");
            return null;
        }

        return GetRandomItem(classes);
    }

    public static void CreateOverlay(string particlePath, string materialPath, float alpha, float radius, float lifeTime)
    {
        var particle = Utilities.CreateEntityByName<CEnvParticleGlow>("env_particle_glow");

        if (particle == null)
        {
            _logger?.LogError("[CreateOverlay] Particle is null");
            return;
        }

        var kv = new CEntityKeyValues();

        kv.SetString("effect_name", particlePath);
        kv.SetFloat("alphascale", alpha);
        kv.SetFloat("scale", radius);
        kv.SetFloat("selfillumscale", lifeTime);
        kv.SetColor("colortint", Color.White);

        kv.SetString("effect_textureOverride", materialPath);

        particle.DispatchSpawn(kv);
        particle.AcceptInput("Start");

        particle.AddEntityIOEvent("Kill", particle, null, "", lifeTime + 1f);
    }

    public static T GetRandomItem<T>(List<T> list) 
    { 
        // Create a new Random instance 
        Random random = new Random(); 
        // Generate a random index 
        int index = random.Next(list.Count); 
        // Return the item at the random index 
        return list[index];
    }

    public static void TakeDamage(CCSPlayerController? client, CCSPlayerController? attacker = null, int damage = 1)
    {
        if(client == null)
        {
            _logger?.LogError("[TakeDamage] Client is null!");
            return;
        }

        if(!IsPlayerAlive(client))
        {
            return;
        }

        var size = Schema.GetClassSize("CTakeDamageInfo");
        var ptr = Marshal.AllocHGlobal(size);

        for (var i = 0; i < size; i++)
            Marshal.WriteByte(ptr, i, 0);

        var damageInfo = new CTakeDamageInfo(ptr);

        CAttackerInfo attackerInfo;

        if(attacker == null)
            attackerInfo = new CAttackerInfo(client);

        else
            attackerInfo = new CAttackerInfo(attacker);

        Marshal.StructureToPtr(attackerInfo, new IntPtr(ptr.ToInt64() + 0x80), false);

        if(attacker == null)
        {
            Schema.SetSchemaValue(damageInfo.Handle, "CTakeDamageInfo", "m_hInflictor", client.Pawn.Raw);
            Schema.SetSchemaValue(damageInfo.Handle, "CTakeDamageInfo", "m_hAttacker", client.Pawn.Raw);
        }

        else
        {
            Schema.SetSchemaValue(damageInfo.Handle, "CTakeDamageInfo", "m_hInflictor", attacker.Pawn.Raw);
            Schema.SetSchemaValue(damageInfo.Handle, "CTakeDamageInfo", "m_hAttacker", attacker.Pawn.Raw);
        }

        damageInfo.Damage = damage;

        if(client.Pawn.Value == null)
        {
            _logger?.LogError("[TakeDamage] Client Pawn is null!");
            return;
        }

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Invoke(client.Pawn.Value, damageInfo);
        Marshal.FreeHGlobal(ptr);
    }

    public static void UpdatedPlayerCash(CCSPlayerController? client, int damage)
    {
        if(client == null)
            return;

        if(client.InGameMoneyServices == null)
            return;

        client.InGameMoneyServices.Account += damage;
        Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
    }

    public static void RemoveRoundObjective()
    {
        var objectivelist = new List<string>() { "func_bomb_target", "func_hostage_rescue", "hostage_entity", "c4" };

        foreach (string objectivename in objectivelist)
        {
            var entityIndex = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>(objectivename);

            foreach (var entity in entityIndex)
            {
                _logger?.LogInformation("[RemoveRoundObjective]: Removed {entityname}", entity.DesignerName);
                entity.AddEntityIOEvent("Kill", entity, null, "", 0.1f);
            }
        }
    }

    public static void SetStamina(CCSPlayerController? client, float stamina)
    {
        if(client == null)
            return;

        if(client.PlayerPawn.Value == null)
            return;

        client.PlayerPawn.Value.VelocityModifier = stamina / 100f;
    }

    public static bool IsWarmup()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules?.WarmupPeriod ?? false;
    }

    public static List<string> WeaponList = new List<string> 
    { 
        "weapon_deagle", 
        "weapon_elite", 
        "weapon_fiveseven", 
        "weapon_glock", 
        "weapon_ak47", 
        "weapon_aug", 
        "weapon_awp", 
        "weapon_famas", 
        "weapon_g3sg1", 
        "weapon_galilar", 
        "weapon_m249", 
        "weapon_m4a1", 
        "weapon_mac10", 
        "weapon_p90", 
        "weapon_mp5sd", 
        "weapon_ump45", 
        "weapon_xm1014", 
        "weapon_bizon", 
        "weapon_mag7", 
        "weapon_negev", 
        "weapon_sawedoff", 
        "weapon_tec9", 
        "weapon_hkp2000", 
        "weapon_mp7", 
        "weapon_mp9", 
        "weapon_nova", 
        "weapon_p250", 
        "weapon_scar20", 
        "weapon_sg556", 
        "weapon_ssg08", 
        "weapon_m4a1_silencer", 
        "weapon_usp_silencer", 
        "weapon_cz75a", 
        "weapon_revolver", 
        "weapon_hegrenade", 
        "weapon_incgrenade", 
        "weapon_decoy", 
        "weapon_molotov", 
        "weapon_flashbang", 
        "weapon_smokegrenade" 
    };
}