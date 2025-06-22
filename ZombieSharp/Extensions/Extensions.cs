using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using ZombieSharp.Struct;

namespace ZombieSharp.Extensions;

unsafe static class Extensions
{
    public static void Teleport(this CBaseEntity entity, Vector_t? position = null, QAngle_t? angles = null, Vector_t? velocity = null)
    {
        Guard.IsValidEntity(entity);

        void* pPos = null, pAng = null, pVel = null;

        // Structs are stored on the stack, GC should not break pointers.

        if (position.HasValue)
        {
            var pos = position.Value; // Remove nullable wrapper
            pPos = &pos;
        }

        if (angles.HasValue)
        {
            var ang = angles.Value;
            pAng = &ang;
        }

        if (velocity.HasValue)
        {
            var vel = velocity.Value;
            pVel = &vel;
        }

        VirtualFunction.CreateVoid<IntPtr, IntPtr, IntPtr, IntPtr>(entity.Handle, GameData.GetOffset("CBaseEntity_Teleport"))(entity.Handle, (nint)pPos,
            (nint)pAng, (nint)pVel);
    }

    public static (Vector_t fwd, Vector_t right, Vector_t up) AngleVectors(this QAngle vec) => vec.ToQAngle_t().AngleVectors();
    public static void AngleVectors(this QAngle vec, out Vector_t fwd, out Vector_t right, out Vector_t up) => vec.ToQAngle_t().AngleVectors(out fwd, out right, out up);

    public static Vector_t ToVector_t(this Vector vec) => new(vec.Handle);
    public static QAngle_t ToQAngle_t(this QAngle vec) => new(vec.Handle);
}