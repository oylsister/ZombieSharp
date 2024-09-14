using System.Data.Common;
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
            if (!CVAR_EnableStats.Value)
                return;

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

            var query = """
                        INSERT INTO `zsharp_stats` 
                        (player_name, steam_auth, total_dmg, total_kill, total_infect, last_join) 
                        VALUES (@PlayerName, @SteamId, @Damage, @Kill, @Infect, @LastJoin)
                        ON DUPLICATE KEY UPDATE player_name = @PlayerName, total_dmg = @Damage, total_kill = @Kill, total_infect = @Infect, last_join = @LastJoin;
                        """;

            if (clientData == null)
            {
                var parametersNew = new
                {
                    PlayerName = client.PlayerName,
                    SteamId = client.AuthorizedSteamID.SteamId3,
                    Damage = damage,
                    Kill = kill,
                    Infect = infect,
                    LastJoin = DateTime.Now.ToString("yyyy'-'MM'-'dd")
                };

                // Logger.LogInformation("[Stats] Query: {query}", query);
                await connection.ExecuteAsync(query, parametersNew);
                return;
            }

            var parameters = new
            {
                PlayerName = client.PlayerName,
                SteamId = client.AuthorizedSteamID.SteamId3,
                Damage = damage + clientData.TotalDamage,
                Kill = kill + clientData.TotalKill,
                Infect = infect + clientData.TotalInfect,
                LastJoin = DateTime.Now.ToString("yyyy'-'MM'-'dd")
            };

            // Logger.LogInformation("[Stats] Query: {query}", query);
            await connection.ExecuteAsync(query, parameters);
        }

        private ClientStatsData GetClientStatsData(CCSPlayerController client)
        {
            if (client == null)
                return null;

            var conn = StatsConnect();

            if (conn == null)
                return null;

            using (var data = new MySqlCommand($"SELECT * FROM `zsharp_stats` WHERE steam_auth = \"{client.AuthorizedSteamID.SteamId3}\";", conn))
            {
                using (var reader = data.ExecuteReader())
                {
                    var clientData = new ClientStatsData();

                    if (!reader.HasRows)
                    {
                        Logger.LogInformation("[Stats] {client} Data is not in table.", client.PlayerName);
                        return null;
                    }

                    reader.Read();
                    clientData.PlayerName = (string)reader["player_name"];
                    clientData.SteamAuth = (string)reader["steam_auth"];
                    clientData.TotalDamage = (int)reader["total_dmg"];
                    clientData.TotalKill = (int)reader["total_kill"];
                    clientData.TotalInfect = (int)reader["total_infect"];
                    clientData.LastJoin = (DateTime)reader["last_join"];

                    Logger.LogInformation("[Stats] Found the data of {client}.", client.PlayerName);

                    return clientData;
                }
            }
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
