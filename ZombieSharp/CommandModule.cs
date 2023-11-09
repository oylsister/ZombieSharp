using System;
using System.Collections;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZombieSharp
{
	public class CommandModule
	{
		private ZombieSharp _Core;
		private ZombiePlayer _Player;
		public CommandModule(ZombieSharp plugin)
		{
			_Core = plugin;
		}

		public void Initialize()
		{
			_Core.AddCommand("css_zs_infect", "Infect Client Command", InfectClientCommand);
			_Core.AddCommand("css_zs_human", "Humanize Client Command", HumanizeClientCommand);
		}

		private void InfectClientCommand(CCSPlayerController client, CommandInfo info)
		{
			if(info == null)
			{
				Utilities.ReplyToCommand(client, "[Z:Sharp] Usage: css_zs_infect <playername>.");
				return;
			}

			ArrayList target = _Core.FindTargetByName(info.ArgString);

			if(target.Count == 0)
			{
				Utilities.ReplyToCommand(client, "[Z:Sharp] Couldn't find any client contain with that name.");
				return;
			}

			for(int i = 0; i < target.Count; i++)
			{
				CCSPlayerController targetindex = (CCSPlayerController)target[i];

				if(!targetindex.IsValid)
					continue;

				if(!targetindex.PawnIsAlive)
				{
					Utilities.ReplyToCommand(client, $"[Z:Sharp] target {targetindex.PlayerName} is not alive.");
					continue;
				}

				if(_Player.IsClientInfect(targetindex))
				{
					Utilities.ReplyToCommand(client, $"[Z:Sharp] target {targetindex.PlayerName} is already zombie.");
					continue;
				}

				_Core.InfectClient(targetindex, null, false, true);
				Utilities.ReplyToCommand(client, $"[Z:Sharp] Successfully infected {targetindex.PlayerName}");
			}
		}

		private void HumanizeClientCommand(CCSPlayerController client, CommandInfo info)
		{
			if(info == null)
			{
				Utilities.ReplyToCommand(client, "[Z:Sharp] Usage: css_zs_human <playername>.");
				return;
			}

			ArrayList target = _Core.FindTargetByName(info.ArgString);

			if(target.Count == 0)
			{
				Utilities.ReplyToCommand(client, "[Z:Sharp] Couldn't find any client contain with that name.");
				return;
			}

			for(int i = 0; i < target.Count; i++)
			{
				CCSPlayerController targetindex = (CCSPlayerController)target[i];

				if(!targetindex.IsValid)
					continue;

				if(!targetindex.PawnIsAlive)
				{
					Utilities.ReplyToCommand(client, $"[Z:Sharp] target {targetindex.PlayerName} is not alive.");
					continue;
				}

				if(_Player.IsClientHuman(targetindex))
				{
					Utilities.ReplyToCommand(client, $"[Z:Sharp] target {targetindex.PlayerName} is already human.");
					continue;
				}

				_Core.HumanizeClient(targetindex, true);
				Utilities.ReplyToCommand(client, $"[Z:Sharp] Successfully humanized {targetindex.PlayerName}");
			}
		}
	}
}