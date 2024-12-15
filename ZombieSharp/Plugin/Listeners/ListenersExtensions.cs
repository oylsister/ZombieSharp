namespace ZombieSharp.Plugin.Listeners;

using CounterStrikeSharp.API.Core;

public static class ListenersExtensions
{
    // how can we get all instances of ListenerBase?
    // reflection/source generator should be used
    // TODO: implement reflection/source generator
    private static readonly List<ListenerBase> _listeners = [
        new OnClientDisconnectListener(),
        new OnClientPutInServerListener(),
        new OnMapStartListener(),
    ];

    public static void RegisterAllListeners(this BasePlugin plugin)
    {
        foreach (var eventInstance in _listeners)
        {
            plugin.RegisterListener(eventInstance.DelegateMethod);
        }
    }

    public static void RemoveAllListeners(this BasePlugin plugin)
    {
        foreach (var eventInstance in _listeners)
        {
            plugin.RemoveListener(eventInstance.DelegateMethod);
        }
    }
}
