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

        MemoryFunctionVoid<CCSPlayerController, CCSPlayerPawn, bool, bool> CBasePlayerController_SetPawnFunc;
        MemoryFunctionVoid<CEntityIdentity, string> CEntityIdentity_SetEntityNameFunc;
        MemoryFunctionWithReturn<int, string, CCSWeaponBaseVData> GetCSWeaponDataFromKeyFunc;

        public void VirtualFunctionsInitialize()
        {
            CBasePlayerController_SetPawnFunc = new(GameData.GetSignature("CBasePlayerController_SetPawn"));
            CEntityIdentity_SetEntityNameFunc = new(GameData.GetSignature("CEntityIdentity_SetEntityName"));

            // VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Hook(OnWeaponCanUse, HookMode.Pre);

            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

            GetCSWeaponDataFromKeyFunc = new(GameData.GetSignature("GetCSWeaponDataFromKey"));

            MemoryFunctionWithReturn<CCSPlayer_ItemServices, CEconItemView, AcquireMethod, NativeObject, AcquireResult> CCSPlayer_CanAcquireFunc = new(GameData.GetSignature("CCSPlayer_CanAcquire"));
            CCSPlayer_CanAcquireFunc.Hook(OnWeaponAcquire, HookMode.Pre);

            MemoryFunctionVoid<CEntityIdentity, IntPtr, CEntityInstance, CEntityInstance, string, int> CEntityIdentity_AcceptInputFunc = new(GameData.GetSignature("CEntityIdentity_AcceptInput"));
            CEntityIdentity_AcceptInputFunc.Hook(OnEntityIdentityAcceptInput, HookMode.Pre);
        }

        private HookResult OnWeaponAcquire(DynamicHook hook)
        {
            var item = hook.GetParam<CCSPlayer_ItemServices>(0);
            var method = hook.GetParam<AcquireMethod>(2);
            //var vdata = GetCSWeaponDataFromKeyFunc.Invoke(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());
            var vdata = GetWeaponVData(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());

            var client = new CCSPlayerController(item.Pawn.Value.Controller.Value.Handle);

            if (ZombieSpawned)
            {
                if (IsClientZombie(client))
                {
                    if (vdata.Name != "weapon_knife")
                    {
                        hook.SetReturn(AcquireResult.NotAllowedByTeam);
                        return HookResult.Handled;
                    }
                }
                else
                {
                    if (WeaponIsRestricted(vdata.Name))
                    {
                        if (method == AcquireMethod.Buy)
                            hook.SetReturn(AcquireResult.NotAllowedForPurchase);

                        else
                            hook.SetReturn(AcquireResult.NotAllowedByMode);

                        return HookResult.Handled;
                    }
                }
            }

            return HookResult.Continue;
        }

        /*
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
                        hook.SetReturn(false);
                        return HookResult.Handled;
                    }
                }

                else
                {
                    if (WeaponIsRestricted(clientweapon.DesignerName))
                    {
                        hook.SetReturn(false);
                        return HookResult.Handled;
                    }
                }
            }

            return HookResult.Continue;
        }
        */

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

            bool warmup = GetGameRules().WarmupPeriod;

            if (warmup && !ConfigSettings.EnableOnWarmup)
            {
                if (client.DesignerName == "player" && attackInfo.Value.DesignerName == "player")
                {
                    damageInfo.Damage = 0;
                    return HookResult.Handled;
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

        private CCSWeaponBaseVData GetWeaponVData(int index, string definition)
        {
            return GetCSWeaponDataFromKeyFunc.Invoke(index, definition);
        }

        public void RespawnClient(CCSPlayerController client)
        {
            if (!client.IsValid || client.PawnIsAlive || client.TeamNum < 2)
                return;

            var clientPawn = client.PlayerPawn.Value;

            CBasePlayerController_SetPawnFunc.Invoke(client, clientPawn, true, false);
            VirtualFunction.CreateVoid<CCSPlayerController>(client.Handle, GameData.GetOffset("CCSPlayerController_Respawn"))(client);
        }

        public void CEntityIdentity_SetEntityName(CEntityIdentity entity, string name)
        {
            if (entity == null || string.IsNullOrEmpty(name))
                return;

            CEntityIdentity_SetEntityNameFunc.Invoke(entity, name);
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
