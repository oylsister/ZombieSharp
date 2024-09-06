using System.Runtime.InteropServices;
using ZombieSharp.Helpers;

namespace ZombieSharp
{
    public class NapalmEffect
    {
        public NapalmEffect()
        {
            Particle = null;
            BurnEnd = 0f;
            Timer = null;
        }

        public CParticleSystem Particle = null;
        public float BurnEnd = 0f;
        public CounterStrikeSharp.API.Modules.Timers.Timer Timer = null;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CAttackerInfo
    {
        public CAttackerInfo(CEntityInstance attacker)
        {
            NeedInit = false;
            IsWorld = true;
            Attacker = attacker.EntityHandle.Raw;
            if (attacker.DesignerName != "cs_player_controller") return;

            var controller = attacker.As<CCSPlayerController>();
            IsWorld = false;
            IsPawn = true;
            AttackerUserId = (ushort)(controller.UserId ?? 0xFFFF);
            TeamNum = controller.TeamNum;
            TeamChecked = controller.TeamNum;
        }

        [FieldOffset(0x0)] public bool NeedInit = true;
        [FieldOffset(0x1)] public bool IsPawn = false;
        [FieldOffset(0x2)] public bool IsWorld = false;

        [FieldOffset(0x4)]
        public UInt32 Attacker;

        [FieldOffset(0x8)]
        public ushort AttackerUserId;

        [FieldOffset(0x0C)] public int TeamChecked = -1;
        [FieldOffset(0x10)] public int TeamNum = -1;
    }

    public partial class ZombieSharp
    {
        public Dictionary<CCSPlayerController, NapalmEffect> NapalmClient = new Dictionary<CCSPlayerController, NapalmEffect> ();

        public void GrenadeEffectOnClientPutInServer(CCSPlayerController client)
        {
            if (client == null)
                return;

            NapalmClient.Add(client, new());
        }

        public void GrenadeEffectOnClientDisconnect(CCSPlayerController client)
        {
            if (client == null) return;

            if (NapalmClient.ContainsKey(client))
            {
                if (NapalmClient[client].Particle != null)
                {
                    NapalmClient[client].Particle.AcceptInput("Stop");
                    NapalmClient[client].Particle.AcceptInput("Kill");
                    NapalmClient[client].Particle = null;
                }

                if (NapalmClient[client].Timer != null)
                {
                    NapalmClient[client].Timer.Kill();
                    NapalmClient[client].Timer = null;
                }

                NapalmClient.Remove(client);
            }
        }

        public void KnockbackExplosion(CCSPlayerController client, CBaseEntity entity, float damage)
        {
            if (client == null)
                return;

            if (!client.PawnIsAlive)
                return;

            var grenadePos = entity.AbsOrigin;
            var clientPos = client.PlayerPawn.Value.AbsOrigin;

            var direction = (clientPos - grenadePos).NormalizeVector();
            var weaponKnockback = 1f;

            if(WeaponDatas.WeaponConfigs.ContainsKey(entity.DesignerName))
            {
                weaponKnockback = WeaponDatas.WeaponConfigs[entity.DesignerName].Knockback;
            }

            var totalkb = damage * WeaponDatas.KnockbackMultiply * weaponKnockback;

            var pushVelocity = direction * totalkb;

            client.PlayerPawn.Value.AbsVelocity.Add(pushVelocity);
        }

        public void IgniteClient(CCSPlayerController client, float duration = 5, int damage = 24)
        {
            if (client == null)
                return;

            if (!client.PawnIsAlive)
                return;

            if (duration <= 0)
                return;

            var pawn = client.PlayerPawn.Value;

            var particle = NapalmClient[client].Particle;

            if (particle != null)
            {
                NapalmClient[client].BurnEnd = Server.CurrentTime + duration;
                return;
            }

            if (particle == null)
            {
                Server.PrintToChatAll("Particle is null!");
                return;
            }

            if (!particle.IsValid)
            {
                Server.PrintToChatAll("Particle is not valid!");
                return;
            }

            particle.EffectName = "particles/burning_fx/env_fire_medium.vpcf";
            particle.StartActive = true;
            particle.DispatchSpawn();
            particle.Teleport(pawn.AbsOrigin, new(), new());
            particle.AcceptInput("SetParent", client.PlayerPawn.Value, null, "!activator");
            particle.AcceptInput("Start");

            NapalmClient[client].Particle = particle;
            NapalmClient[client].BurnEnd = Server.CurrentTime + duration;

            var clientSpeed = pawn.MovementServices!.Maxspeed;

            NapalmClient[client].Timer = AddTimer(1.0f, () =>
            {
                if (client == null)
                {
                    NapalmClient[client].Timer.Kill();
                    return;
                }

                if (!client.PawnIsAlive)
                {
                    NapalmClient[client].Timer.Kill();
                    return;
                }

                if (NapalmClient[client].BurnEnd > Server.CurrentTime)
                {
                    pawn.MovementServices!.Maxspeed = 180f;

                    var size = Schema.GetClassSize("CTakeDamageInfo");
                    var ptr = Marshal.AllocHGlobal(size);

                    for (var i = 0; i < size; i++)
                        Marshal.WriteByte(ptr, i, 0);

                    var damageInfo = new CTakeDamageInfo(ptr);
                    var attackerInfo = new CAttackerInfo(client);

                    Marshal.StructureToPtr(attackerInfo, new IntPtr(ptr.ToInt64() + 0x80), false);

                    Schema.SetSchemaValue(damageInfo.Handle, "CTakeDamageInfo", "m_hInflictor", client.Pawn.Raw);
                    Schema.SetSchemaValue(damageInfo.Handle, "CTakeDamageInfo", "m_hAttacker", client.Pawn.Raw);

                    damageInfo.Damage = damage;

                    VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Invoke(client.Pawn.Value, damageInfo);
                    Marshal.FreeHGlobal(ptr);
                }

                else
                {
                    pawn.MovementServices!.Maxspeed = clientSpeed;
                    NapalmClient[client].Particle.AcceptInput("Stop");
                    NapalmClient[client].Particle.AcceptInput("Kill");
                    NapalmClient[client].Timer.Kill();
                    return;
                }
            });
        }
    }
}
