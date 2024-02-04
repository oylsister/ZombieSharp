namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public Dictionary<CCSPlayerController, ClientDamage> ClientsDamage = new();

        public void TopDefenderOnPutInServer(CCSPlayerController client)
        {
            ClientsDamage.Add(client, new());
        }

        public void TopDefenderOnDisconnect(CCSPlayerController client)
        {
            ClientsDamage.Remove(client);
        }

        public void TopDefenderOnPlayerHurt(CCSPlayerController client, int damage)
        {
            if (!ConfigSettings.EnableTopDefender)
                return;

            if (client.IsValid && ClientsDamage.ContainsKey(client))
                ClientsDamage[client].Damage += damage;
        }

        public void TopDefenderOnInfect(CCSPlayerController client)
        {
            if (!ConfigSettings.EnableTopDefender)
                return;

            if (client.IsValid && ClientsDamage.ContainsKey(client))
                ClientsDamage[client].Infected++;
        }

        public void TopDenfederOnRoundEnd()
        {
            if (!ConfigSettings.EnableTopDefender)
                return;

            int item;

            if (ClientsDamage.Count <= 0)
                return;

            else if (ClientsDamage.Count < 3)
                item = ClientsDamage.Count;

            else
                item = 3;

            var topdamage = ClientsDamage.OrderByDescending(entry => entry.Value.Damage).Take(item).ToDictionary(pair => pair.Key, pair => pair.Value.Damage);
            int rank = 1;

            Server.PrintToChatAll($" {ChatColors.Lime}============== TOP DEFENDER ==============");

            foreach (var entry in topdamage)
            {
                if (!entry.Key.IsValid)
                    continue;

                Server.PrintToChatAll($" {rank}.{entry.Key.PlayerName} - {ChatColors.Lime}{entry.Value} Damage");
                rank++;
            }

            var topinfect = ClientsDamage.OrderByDescending(entry => entry.Value.Infected).Take(item).ToDictionary(pair => pair.Key, pair => pair.Value.Infected);
            rank = 1;

            Server.PrintToChatAll($" {ChatColors.LightRed}============== TOP INFECTER ==============");

            foreach (var entry in topinfect)
            {
                if (!entry.Key.IsValid)
                    continue;

                Server.PrintToChatAll($" {rank}.{entry.Key.PlayerName} - {ChatColors.LightRed}{entry.Value} Infected");
                rank++;
            }

            ResetTopDefender();
        }

        private void ResetTopDefender()
        {
            if (!ConfigSettings.EnableTopDefender)
                return;

            foreach (var client in Utilities.GetPlayers())
            {
                if (!client.IsValid)
                    continue;

                if (!ClientsDamage.ContainsKey(client))
                    continue;

                ClientsDamage[client].Damage = 0;
                ClientsDamage[client].Infected = 0;
            }
        }
    }
}

public class ClientDamage
{
    public int Damage { get; set; }
    public int Infected { get; set; }
}