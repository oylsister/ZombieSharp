namespace ZombieSharp.Plugin.Listeners;

using static CounterStrikeSharp.API.Core.Listeners;

public class OnMapStartListener : ListenerBase<OnMapStart>
{
    public override OnMapStart DelegateMethod => OnMapStartHandler;

    private void OnMapStartHandler(string mapName)
    {
        Console.WriteLine($"Map started {mapName}");
    }
}
