namespace ZombieSharp.Plugin.Listeners;

using static CounterStrikeSharp.API.Core.Listeners;

public class OnClientDisconnectListener : ListenerBase<OnClientDisconnect>
{
    public override OnClientDisconnect DelegateMethod => OnClientDisconnectHandler;

    private void OnClientDisconnectHandler(int playerSlot)
    {
        Console.WriteLine($"Client disconnected: Player Slot {playerSlot}");
    }
}