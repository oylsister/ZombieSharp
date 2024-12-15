namespace ZombieSharp.Plugin.Events;

using CounterStrikeSharp.API.Core;

public static class EventsExtensions
{
    private static readonly List<IEventBase> _events = [
        new PlayerHurtEvent(),
    ];

    public static void RegisterAllEventHandlers(this BasePlugin plugin)
    {
        foreach (var @event in _events)
        {
            @event.Register(plugin);
        }
    }

    public static void RemoveAllEventHandlers(this BasePlugin plugin)
    {
        foreach (var eventInstance in _events)
        {
            eventInstance.Deregister(plugin);
        }
    }
}
