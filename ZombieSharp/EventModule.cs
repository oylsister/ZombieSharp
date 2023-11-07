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
			_Core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);

			_Core.RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServerHandler);
			_Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnectHandler);
        }

		private void OnClientPutInServerHandler(int clientindex)
		{
			CCSPlayerController client = Utilities.GetPlayerFromUserid(clientindex);
			_Player.g_bZombie.Add(client, false);
		}

		private void OnClientDisconnectHandler(int clientindex)
		{
			CCSPlayerController client = Utilities.GetPlayerFromUserid(clientindex);
			_Player.g_bZombie.Remove(client);
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
                _Player.g_bZombie[Utilities.GetPlayerFromUserid(i)] = false;
            }

            return HookResult.Continue;
        }

		private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
		{
			CCSPlayerController client = @event.Userid;
			CCSPlayerController attacker = @event.Attacker;
			string weapon = @event.Weapon;

			if(_Player.IsClientInfect(attacker) && _Player.IsClientHuman(client) && string.Equals(weapon, "knife"))
				_Core.InfectClient(client, attacker);

			return HookResult.Continue;
		}
    }
}