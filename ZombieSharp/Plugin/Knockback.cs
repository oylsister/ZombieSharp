using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class Knockback
{
    private static ILogger<ZombieSharp>? _logger;

    public Knockback(ILogger<ZombieSharp> logger)
    {
        _logger = logger;
    }

    public static void KnockbackClient(CCSPlayerController? client, CCSPlayerController? attacker, string weaponname, float dmgHealth)
    {
        if (client == null || attacker == null)
            return;

        // ent world also trigger hurt event
        if (attacker.DesignerName != "cs_player_controller")
            return;

        // knockback is for zombie only.
        if (!Infect.IsClientHuman(attacker) || !Infect.IsClientInfect(client))
            return;

        var clientPos = client.PlayerPawn.Value?.AbsOrigin;
        var attackerPos = attacker.PlayerPawn.Value?.AbsOrigin;

        if (clientPos == null || attackerPos == null)
            return;

        var direction = clientPos - attackerPos;
        var normalizedDir = NormalizeVector(direction);

        // Class Data section.
        if (PlayerData.PlayerClassesData == null)
        {
            _logger?.LogError("[KnockbackClient] PlayerClassesData is null!");
            return;
        }

        if (!PlayerData.PlayerClassesData.ContainsKey(client))
        {
            _logger?.LogError("[KnockbackClient] client {0} is not in playerdata!", client.PlayerName);
            return;
        }

        if (PlayerData.PlayerClassesData[client].ActiveClass == null)
        {
            _logger?.LogError("[KnockbackClient] client {0} active class is null!", client.PlayerName);
            return;
        }

        float weaponknockback = 1.0f;

        // weapon knockback section
        if (Weapons.WeaponsConfig == null)
            _logger?.LogError("[KnockbackClient] Weapon Data is null!");

        if (weaponname.Contains("knife") || weaponname.Contains("hegrenade"))
        {
            var weapon = Weapons.GetWeaponAttributeByEntityName(weaponname);

            if (weapon != null)
                weaponknockback = weapon.Knockback;
        }

        else
        {
            var activeWeapon = attacker.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
            var weaponString = Utils.FindWeaponItemDefinition(activeWeapon);

            WeaponAttribute? weapon = null;

            if (weaponString != null)
                weapon = Weapons.GetWeaponAttributeByEntityName(weaponString!);

            if (weapon != null)
                weaponknockback = weapon.Knockback;
        }

        var pushVelocity = normalizedDir * dmgHealth * PlayerData.PlayerClassesData[client].ActiveClass!.Knockback * weaponknockback;
        client.PlayerPawn.Value?.AbsVelocity.Add(pushVelocity);
    }

    private static Vector NormalizeVector(Vector vector)
    {
        var x = vector.X;
        var y = vector.Y;
        var z = vector.Z;

        var magnitude = MathF.Sqrt(x * x + y * y + z * z);

        if (magnitude != 0.0)
        {
            x /= magnitude;
            y /= magnitude;
            z /= magnitude;
        }

        return new Vector(x, y, z);
    }
}}
}