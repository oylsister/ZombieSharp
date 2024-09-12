using Dapper;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        private StatsDatabase statsConfig;
        MySqlConnection connection = null;

        private void StatsOnLoad()
        {
            var statsConfigPath = Path.Combine(ModuleDirectory, $"../../configs/zombiesharp/database.json");
            statsConfig = JsonConvert.DeserializeObject<StatsDatabase>(File.ReadAllText(statsConfigPath));
            connection = StatsConnect();
        }

        private MySqlConnection StatsConnect()
        {
            var query = $"Server={statsConfig.hostname}; database={statsConfig.database}; UID={statsConfig.username}; password={statsConfig.password}";
            var conn = new MySqlConnection(query);

            if(conn == null)
            {
                Logger.LogError("[Stats] Connection is null.");
                return null;
            }

            conn.Open();

            return conn;
        }

        private async Task StatsSetData(CCSPlayerController client, int damage = 0, int kill = 0, int infect = 0)
        {
            if (client == null)
            {
                Logger.LogError("[Stats] client is null.");
                return;
            }

            if (connection == null)
            {
                return;
            }

            var clientData = GetClientStatsData(client);

            var query = $"INSERT INTO `zsharp_stats` (player_name, steam_auth, total_dmg, total_kill, total_infect, last_join) " +
                    $"VALUES (\"{client.PlayerName}\", \"{client.AuthorizedSteamID.SteamId3}\", {damage}, {kill}, {infect}, \"{DateTime.Now.ToString("yyyy'-'MM'-'dd")}\") ";

            if(clientData != null)
            {
                query += $"ON DUPLICATE KEY UPDATE player_name = \"{client.PlayerName}\", total_dmg = {damage + clientData.TotalDamage}, total_kill = {kill + clientData.TotalKill}, total_infect = {infect + clientData.TotalKill}, last_join = \"{DateTime.Now.ToString("yyyy'-'MM'-'dd")}\"";
            }

            // Logger.LogInformation($"[Stats] Query: {query};");

            await connection.ExecuteAsync($"{query};");
        }

        private ClientStatsData GetClientStatsData(CCSPlayerController client)
        {
            if (client == null)
                return null;

            var conn = StatsConnect();

            if (connection == null)
                return null;

            var data = new MySqlCommand($"SELECT * FROM `zsharp_stats` WHERE steam_auth = \"{client.AuthorizedSteamID.SteamId3}\";", conn);
            var reader = data.ExecuteReader();

            var clientData = new ClientStatsData();

            if (reader.HasRows)
            {
                reader.Read();
                clientData.PlayerName = reader.GetString(0);
                clientData.SteamAuth = reader.GetString(1);
                clientData.TotalDamage = reader.GetInt32(2);
                clientData.TotalKill = reader.GetInt32(3);
                clientData.TotalInfect = reader.GetInt32(4);
                clientData.LastJoin = reader.GetDateTime(5);
                Logger.LogInformation($"[Stats] Found the data of {client.PlayerName}.");

                conn.Close();

                return clientData;
            }

            Logger.LogInformation($"[Stats] {client.PlayerName} Data is not in table.");
            conn.Close();
            return null;
        }
    }

    public class StatsDatabase()
    {
        public string hostname { get; set; }
        public string database { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }

    public class ClientStatsData()
    {
        public string PlayerName { get; set; }
        public string SteamAuth { get; set; }
        public int TotalDamage { get; set; }
        public int TotalKill { get; set; }
        public int TotalInfect { get; set; }
        public DateTime LastJoin {  get; set; }
    }
}
