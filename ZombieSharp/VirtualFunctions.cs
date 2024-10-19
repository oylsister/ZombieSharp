using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        // Possible results for CSPlayer::CanAcquire
        enum AcquireResult : int
        {
            Allowed = 0,
            InvalidItem,
            AlreadyOwned,
            AlreadyPurchased,
            ReachedGrenadeTypeLimit,
            ReachedGrenadeTotalLimit,
            NotAllowedByTeam,
            NotAllowedByMap,
            NotAllowedByMode,
            NotAllowedForPurchase,
            NotAllowedByProhibition,
        };

        // Possible method for CSPlayer::CanAcquire
        enum AcquireMethod : int
        {
            PickUp = 0,
            Buy,
        };

        MemoryFunctionVoid<CEntityIdentity, string> CEntityIdentity_SetEntityNameFunc;
        MemoryFunctionVoid<CBaseEntity, string, int, float, float> CBaseEntity_EmitSoundParamsFunc;

        public void VirtualFunctionsInitialize()
        {
            CEntityIdentity_SetEntityNameFunc = new(GameData.GetSignature("CEntityIdentity_SetEntityName"));
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
            CBaseEntity_EmitSoundParamsFunc = new(GameData.GetSignature("CBaseEntity_EmitSoundParams"));
            VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnWeaponAcquire, HookMode.Pre);

            MemoryFunctionVoid<CEntityIdentity, IntPtr, CEntityInstance, CEntityInstance, string, int> CEntityIdentity_AcceptInputFunc = new(GameData.GetSignature("CEntityIdentity_AcceptInput"));
            CEntityIdentity_AcceptInputFunc.Hook(OnEntityIdentityAcceptInput, HookMode.Pre);
        }
        private HookResult OnWeaponAcquire(DynamicHook hook)
        {
            var item = hook.GetParam<CCSPlayer_ItemServices>(0);
            var method = hook.GetParam<AcquireMethod>(2);
            var vdata = VirtualFunctions.GetCSWeaponDataFromKey(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());

            if (vdata == null)
                return HookResult.Continue;

            if (item == null)
                return HookResult.Continue;

            var client = new CCSPlayerController(item.Pawn.Value.Controller.Value.Handle);

            if (client == null)
                return HookResult.Continue;

            if (method == AcquireMethod.PickUp)
            {
                // Server.PrintToChatAll($"Try pick up {vdata.Name}");
                if (WeaponIsRestricted(vdata.Name))
                {
                    hook.SetReturn(AcquireResult.NotAllowedByProhibition);
                    return HookResult.Handled;
                }
            }
            else
            {
                var weapon = GetKeyByWeaponEntity(vdata.Name);

                if (weapon != null && WeaponDatas.WeaponConfigs[weapon].Price > 0)
                {
                    PurchaseWeapon(client, weapon);
                    hook.SetReturn(AcquireResult.NotAllowedByProhibition);
                    return HookResult.Handled;
                }
            }

            if (ZombieSpawned)
            {
                if (IsClientZombie(client))
                {
                    if (vdata.GearSlot != gear_slot_t.GEAR_SLOT_KNIFE)
                    {
                        hook.SetReturn(AcquireResult.NotAllowedByProhibition);
                        return HookResult.Handled;
                    }
                }
            }

            return HookResult.Continue;
        }

        private HookResult OnTakeDamage(DynamicHook hook)
        {
            var client = hook.GetParam<CEntityInstance>(0);
            var damageInfo = hook.GetParam<CTakeDamageInfo>(1);

            var attackInfo = damageInfo.Attacker;

            // var controller = player(client);

            // blocking inferno damage because Nori is a fucking idiot want to burn himself
            if (damageInfo.Inflictor.Value.DesignerName == "inferno")
            {
                var inferno = new CInferno(damageInfo.Inflictor.Value.Handle);
                if (client == inferno.OwnerEntity.Value)
                {
                    damageInfo.Damage = 0;
                }
            }

            if(damageInfo.Inflictor.Value.DesignerName == "hegrenade")
            {
                var victim = player(client);

                if(victim == null)
                    return HookResult.Continue;

                if (IsClientZombie(victim))
                    KnockbackExplosion(victim, damageInfo.Inflictor.Value, damageInfo.Damage);
            }

            var controller = player(client);

            if (CVAR_RespawnProtect.Value && ClientProtected[controller.Slot].Protected && controller.IsValid)
            {
                damageInfo.Damage = 0;
            }

            if (enableWarmupOnline)
            {
                bool warmup = GetGameRules().WarmupPeriod;

                if (warmup && !CVAR_EnableOnWarmup.Value)
                {
                    if (client.DesignerName == "player" && attackInfo.Value.DesignerName == "player")
                    {
                        damageInfo.Damage = 0;
                        return HookResult.Handled;
                    }
                }
            }

            // Server.PrintToChatAll($"{controller.PlayerName} take damaged");
            return HookResult.Continue;
        }

        private HookResult OnEntityIdentityAcceptInput(DynamicHook hook)
        {
            var identity = hook.GetParam<CEntityIdentity>(0);
            var input = hook.GetParam<IntPtr>(1);

            var stringinput = Utilities.ReadStringUtf8(input);

            //Server.PrintToChatAll($"Found: {identity.Name}, input: {stringinput}");

            if (RespawnRelay != null && identity != null)
            {
                if (identity.Name == "zr_toggle_respawn")
                {
                    if (stringinput.Equals("Trigger", StringComparison.OrdinalIgnoreCase))
                        ToggleRespawn();

                    else if (stringinput.Equals("Enable", StringComparison.OrdinalIgnoreCase) && !RespawnEnable)
                        ToggleRespawn(true, true);

                    else if (stringinput.Equals("Disable", StringComparison.OrdinalIgnoreCase) && RespawnEnable)
                        ToggleRespawn(true, false);
                }
            }

            return HookResult.Continue;
        }

        public void CEntityIdentity_SetEntityName(CEntityIdentity entity, string name)
        {
            if (entity == null || string.IsNullOrEmpty(name))
                return;

            CEntityIdentity_SetEntityNameFunc.Invoke(entity, name);
        }

        public void EmitSound(CBaseEntity entity, string sound, int pitch = 100, float volume = 1.0f, float delay = 0.0f)
        {
            if (entity == null || string.IsNullOrEmpty(sound))
                return;

            CBaseEntity_EmitSoundParamsFunc.Invoke(entity, sound, pitch, volume, delay);
        }

        public static CCSPlayerController player(CEntityInstance instance)
        {
            if (instance == null)
            {
                return null;
            }

            if (instance.DesignerName != "player")
            {
                return null;
            }

            // grab the pawn index
            int player_index = (int)instance.Index;

            // grab player controller from pawn
            CCSPlayerPawn player_pawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>(player_index);

            // pawn valid
            if (player_pawn == null || !player_pawn.IsValid)
            {
                return null;
            }

            // controller valid
            if (player_pawn.OriginalController == null || !player_pawn.OriginalController.IsValid)
            {
                return null;
            }

            // any further validity is up to the caller
            return player_pawn.OriginalController.Value;
        }
    }
}
