using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        MemoryFunctionVoid<CCSPlayerController, CCSPlayerPawn, bool, bool> CBasePlayerController_SetPawnFunc;

        public void VirtualFunctionsInitialize()
        {
            CBasePlayerController_SetPawnFunc = new(GameData.GetSignature("CBasePlayerController_SetPawn"));
            Hook_OnPlayerCanUse();
            Hook_OnTakeDamageOld();
        }

        private void Hook_OnPlayerCanUse()
        {
            MemoryFunctionVoid<CCSPlayer_WeaponServices, CBasePlayerWeapon> CCSPlayer_WeaponServices_CanUseFunc = new(GameData.GetSignature("CCSPlayer_WeaponServices_CanUse"));
            Action<CCSPlayer_WeaponServices, CBasePlayerWeapon> CCSPlayer_WeaponServices_CanUse = CCSPlayer_WeaponServices_CanUseFunc.Invoke;

            CCSPlayer_WeaponServices_CanUseFunc.Hook((h =>
            {
                var weaponservices = h.GetParam<CCSPlayer_WeaponServices>(0);
                var clientweapon = h.GetParam<CBasePlayerWeapon>(1);

                var client = new CCSPlayerController(weaponservices!.Pawn.Value.Controller.Value!.Handle);

                if (ZombieSpawned)
                {
                    if (IsClientZombie(client))
                    {
                        if (clientweapon.DesignerName != "weapon_knife")
                        {
                            if (!weaponservices.PreventWeaponPickup)
                            {
                                weaponservices.PreventWeaponPickup = true;
                                clientweapon.Remove();
                            }
                        }
                        else
                        {
                            weaponservices.PreventWeaponPickup = false;
                        }
                    }
                    else
                    {
                        weaponservices.PreventWeaponPickup = false;
                    }
                }
                else
                {
                    weaponservices.PreventWeaponPickup = false;
                }

                return HookResult.Continue;

            }), HookMode.Pre);
        }

        private void Hook_OnTakeDamageOld()
        {
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook((h =>
            {
                var client = h.GetParam<CEntityInstance>(0);
                var damageInfo = h.GetParam<CTakeDamageInfo>(1);

                var attackInfo = damageInfo.Attacker;

                /*
                var controller = new CCSPlayerController(client.Handle);
                var attacker = new CCSPlayerController(damageInfo.Attacker.Value.Handle);

                // 32 for fall damage
                if (client.IsValid)
                    Server.PrintToChatAll($"{client.DesignerName} damaged by type: {attackInfo.Value.DesignerName}");
                */

                bool warmup = GetGameRules().WarmupPeriod;

                if (warmup && !ConfigSettings.EnableOnWarmup)
                {
                    if (client.DesignerName == "player" && attackInfo.Value.DesignerName == "player")
                        damageInfo.Damage = 0;
                }
                return HookResult.Continue;
            }), HookMode.Pre);
        }

        public void RespawnClient(CCSPlayerController client)
        {
            if (!client.IsValid || client.PawnIsAlive || client.TeamNum < 2)
                return;

            var clientPawn = client.PlayerPawn.Value;

            CBasePlayerController_SetPawnFunc.Invoke(client, clientPawn, true, false);
            VirtualFunction.CreateVoid<CCSPlayerController>(client.Handle, GameData.GetOffset("CCSPlayerController_Respawn"))(client);
        }
    }
}
