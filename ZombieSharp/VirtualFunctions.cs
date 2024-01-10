using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        MemoryFunctionVoid<CCSPlayerController, CCSPlayerPawn, bool, bool> CBasePlayerController_SetPawnFunc;

        public CLogicRelay RespawnRelay;

        public void VirtualFunctionsInitialize()
        {
            CBasePlayerController_SetPawnFunc = new(GameData.GetSignature("CBasePlayerController_SetPawn"));

            MemoryFunctionWithReturn<CCSPlayer_WeaponServices, CBasePlayerWeapon, bool> CCSPlayer_WeaponServices_CanUseFunc = new(GameData.GetSignature("CCSPlayer_WeaponServices_CanUse"));
            CCSPlayer_WeaponServices_CanUseFunc.Hook(OnWeaponCanUse, HookMode.Pre);

            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

            Hook_CEntityIdentity();
        }

        private HookResult OnWeaponCanUse(DynamicHook hook)
        {
            var weaponservices = hook.GetParam<CCSPlayer_WeaponServices>(0);
            var clientweapon = hook.GetParam<CBasePlayerWeapon>(1);

            var client = new CCSPlayerController(weaponservices!.Pawn.Value.Controller.Value!.Handle);

            //Server.PrintToChatAll($"{client.PlayerName}: {CCSPlayer_WeaponServices_CanUseFunc.Invoke(weaponservices, clientweapon)}");

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
        }

        private HookResult OnTakeDamage(DynamicHook hook)
        {
            var client = hook.GetParam<CEntityInstance>(0);
            var damageInfo = hook.GetParam<CTakeDamageInfo>(1);

            var attackInfo = damageInfo.Attacker;

            var controller = new CCSPlayerController(client.Handle);
            //var attacker = new CCSPlayerController(damageInfo.Attacker.Value.Handle);

            // 32 for fall damage
            /*
            if (client.IsValid)
                Server.PrintToChatAll($"{client.DesignerName} damaged by type: {attackInfo.Value.DesignerName}");
            */

            //int damagetype = 0;
            bool falldamage = Int32.TryParse(attackInfo.Value.DesignerName, out int damagetype) && damagetype == 32;

            bool warmup = GetGameRules().WarmupPeriod;

            if (warmup && !ConfigSettings.EnableOnWarmup)
            {
                if (client.DesignerName == "player" && attackInfo.Value.DesignerName == "player")
                    damageInfo.Damage = 0;
            }

            if (controller != null && !PlayerClassDatas.PlayerClasses[ClientPlayerClass[controller.Slot].ActiveClass].Fall_Damage && falldamage)
            {
                damageInfo.Damage = 0;
            }

            return HookResult.Continue;
        }

        private void Hook_CEntityIdentity()
        {
            //MemoryFunctionVoid<CEntityIdentity, CUtlStringToken, CEntityInstance, CEntityInstance, string, int> CEntityIdentity_AcceptInputFunc = new(GameData.GetSignature("CEntityIdentity_AcceptInput"));

            //CEntityIdentity_AcceptInputFunc.Hook((h =>
            VirtualFunctions.AcceptInputFunc.Hook((h =>
            {
                var identity = h.GetParam<CEntityInstance>(0).Entity;
                var input = h.GetParam<string>(1);

                // Server.PrintToChatAll($"Found the entity {identity.Name} with {input}");

                if (identity != RespawnRelay.Entity)
                {
                    return HookResult.Stop;
                }

                if (input == "Trigger")
                    ToggleRespawn();

                else if (input == "Enable" && !RespawnEnable)
                    ToggleRespawn(true, true);

                else if (input == "Disable" && RespawnEnable)
                    ToggleRespawn(true, false);

                return HookResult.Continue;
            }), HookMode.Post);
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
