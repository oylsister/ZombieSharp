namespace ZombieSharp.Plugin.Listeners;

using static CounterStrikeSharp.API.Core.Listeners;

public class OnClientPutInServerListener : ListenerBase<OnClientPutInServer>
{
    public override OnClientPutInServer DelegateMethod => OnClientPutInServerHandler;

    private void OnClientPutInServerHandler(int playerSlot)
    {
        Console.WriteLine($"Client put in server: Player Slot {playerSlot}");
    }
}