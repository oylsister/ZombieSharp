namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public Dictionary<int, ClientDamage> ClientsDamage = new();

        public void TopDenfederOnRoundEnd()
        {
            foreach (var client in Utilities.GetPlayers())
            {
                if (!client.IsValid)
                    continue;

                if (!ClientsDamage.ContainsKey(client.Slot))
                    continue;
            }
        }
    }
}

public class ClientDamage
{
    public int Damage { get; set; }
    public int Infected { get; set; }
}