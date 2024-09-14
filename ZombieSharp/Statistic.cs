using CounterStrikeSharp.API.Core.Hosting;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        private readonly MySqlConnection _statsConnection;

        public ZombieSharp(MySqlConnection statsConnection)
        {
            _statsConnection = statsConnection;
        }

        private async Task StatsSetData(CCSPlayerController client, int damage = 0, int kill = 0, int infect = 0)
        {
            if (client == null)
            {
                Logger.LogError("[Stats] client is null.");
                return;
            }

            if (_statsConnection == null)
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

            await _statsConnection.ExecuteAsync($"{query};");
        }

        private ClientStatsData GetClientStatsData(CCSPlayerController client)
        {
            if (client == null)
                return null;

            if (_statsConnection == null)
                return null;

            var data = new MySqlCommand($"SELECT * FROM `zsharp_stats` WHERE steam_auth = \"{client.AuthorizedSteamID.SteamId3}\";", _statsConnection);
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
                Logger.LogInformation("[Stats] Found the data of {client}.", client.PlayerName);

                return clientData;
            }

            Logger.LogInformation("[Stats] {client} Data is not in table.", client.PlayerName);
            return null;
        }
    }

    public class StatsDatabase
    {
        public string hostname { get; set; }
        public string database { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }

    public class StatsDatabaseCollection : IPluginServiceCollection<ZombieSharp>
    {
        public void ConfigureServices(IServiceCollection service)
        {
            service.AddScoped(service =>
            {
                var plugin = service.GetRequiredService<ZombieSharp>();
                var moduleDirectory = plugin.ModuleDirectory;
                var statsConfigPath = Path.Combine(moduleDirectory, $"../../configs/zombiesharp/database.json");

                return JsonConvert.DeserializeObject<StatsDatabase>(File.ReadAllText(statsConfigPath));
            });

            service.AddScoped(service =>
            {
                var config = service.GetRequiredService<StatsDatabase>();
                return new MySqlConnection($"Server={config.hostname}; database={config.database}; UID={config.username}; password={config.password}");
            });
        }
    }

    public class ClientStatsData
    {
        public string PlayerName { get; set; }
        public string SteamAuth { get; set; }
        public int TotalDamage { get; set; }
        public int TotalKill { get; set; }
        public int TotalInfect { get; set; }
        public DateTime LastJoin {  get; set; }
    }
}
