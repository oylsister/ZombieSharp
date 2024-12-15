namespace ZombieSharp.Plugin.Events;

using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.BasePlugin;

public class PlayerHurtEvent : EventBase<EventPlayerHurt>
{
    public override GameEventHandler<EventPlayerHurt> Handler => OnPlayerHurt;

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        Console.WriteLine($"Player {@event.Userid} hurt with {@event.DmgHealth} damage.");
        return HookResult.Continue;
    }
}
