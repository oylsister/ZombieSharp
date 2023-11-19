using System.Collections;

namespace ZombieSharp
{
	public class CommandModule
	{
		private readonly ZombieSharp _core;
		private ZombiePlayer _player;
		public CommandModule(ZombieSharp plugin)
		{
			_core = plugin;
		}

		public void Initialize()
		{
			_core.AddCommand("css_zs_infect", "Infect Client Command", InfectClientCommand);
			_core.AddCommand("css_zs_human", "Humanize Client Command", HumanizeClientCommand);
		}

		private void InfectClientCommand(CCSPlayerController client, CommandInfo info)
		{
			if(info == null)
			{
				Utilities.ReplyToCommand(client, "[Z:Sharp] Usage: css_zs_infect <playername>.");
				return;
			}

			var targets = _core.FindTargetByName(info.ArgString);

			if(targets.Count == 0)
			{
				Utilities.ReplyToCommand(client, "[Z:Sharp] Couldn't find any client contain with that name.");
				return;
			}

			foreach (CCSPlayerController target in targets)
			{
				if(!target.IsValid)
					continue;

				if(!target.PawnIsAlive)
				{
					Utilities.ReplyToCommand(client, $"[Z:Sharp] target {target.PlayerName} is not alive.");
					continue;
				}

				if(_player.IsClientInfect(target))
				{
					Utilities.ReplyToCommand(client, $"[Z:Sharp] target {target.PlayerName} is already zombie.");
					continue;
				}

				_core.InfectClient(target, null, false, true);
				Utilities.ReplyToCommand(client, $"[Z:Sharp] Successfully infected {target.PlayerName}");
			}
		}

		private void HumanizeClientCommand(CCSPlayerController client, CommandInfo info)
		{
			if(info == null)
			{
				Utilities.ReplyToCommand(client, "[Z:Sharp] Usage: css_zs_human <playername>.");
				return;
			}

			var targets = _core.FindTargetByName(info.ArgString);

			if(targets.Count == 0)
			{
				Utilities.ReplyToCommand(client, "[Z:Sharp] Couldn't find any client contain with that name.");
				return;
			}

			foreach (CCSPlayerController target in targets)
			{
				if(!target.IsValid)
					continue;

				if(!target.PawnIsAlive)
				{
					Utilities.ReplyToCommand(client, $"[Z:Sharp] target {target.PlayerName} is not alive.");
					continue;
				}

				if(_player.IsClientHuman(target))
				{
					Utilities.ReplyToCommand(client, $"[Z:Sharp] target {target.PlayerName} is already human.");
					continue;
				}

				_core.HumanizeClient(target, true);
				Utilities.ReplyToCommand(client, $"[Z:Sharp] Successfully humanized {target.PlayerName}");
			}
		}
	}
}