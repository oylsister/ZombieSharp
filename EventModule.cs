using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZombieSharp
{
    public class EventModule
    {
        private ZombieSharp _Core;
        private ZombiePlayer _Player;

        public EventModule(ZombieSharp plugin)
        {
            _Core = plugin;
        }

        public void Initialize()
        {
            _Core.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            _Core.RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
            _Core.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            Server.PrintToChatAll(" [Z:Rev] The current game mode is the Human vs. Zombie, the zombie goal is to infect all human before time is running out.");
            return HookResult.Continue;
        }

        private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
        {
            _Core.InfectOnRoundFreezeEnd();
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            // Reset Zombie Spawned here.
            _Core.g_bZombieSpawned = false;

            // Reset Client Status
            for(int i = 0; i < Server.MaxPlayers; i++)
            {
                _Player.g_bZombie[i] = false;
            }

            return HookResult.Continue;
        }
    }
}