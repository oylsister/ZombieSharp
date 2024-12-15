namespace ZombieSharp.Plugin.Events;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using static CounterStrikeSharp.API.Core.BasePlugin;

public abstract class EventBase<T> : IEventBase where T : GameEvent
{
    public abstract GameEventHandler<T> Handler { get; }

    public void Register(BasePlugin plugin)
    {
        plugin.RegisterEventHandler(Handler);
    }

    public void Deregister(BasePlugin plugin)
    {
        plugin.DeregisterEventHandler(Handler);
    }
}

public interface IEventBase
{
    void Register(BasePlugin plugin);
    void Deregister(BasePlugin plugin);
}