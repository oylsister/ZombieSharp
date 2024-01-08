using Dapper;
using Microsoft.Data.Sqlite;

namespace ZombieSharp
{
    public interface IPlayerClassDB
    {
        Task Create(PlayerClassDB classDB);
        Task<PlayerClassDB> Get(string steamid);
        Task Update(PlayerClassDB classDB);
    }
    public partial class ZombieSharp : IPlayerClassDB
    {
        private SqliteConnection PlayerDB = null!;
        public void PlayerSettingsOnLoad()
        {
            PlayerDB = new SqliteConnection($"Data Source={Path.Join(ModuleDirectory, "zombiesharp.db")}");
            PlayerDB.Open();

            Task.Run(async () =>
            {
                await PlayerDB.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS `player_class` (`SteamID` UNSIGNED BIG INT NOT NULL, `ZClass` VARCHAR(64), `HClass` VARCHAR(64), PRIMARY KEY (`SteamID`));");
            });
        }

        public async Task Create(PlayerClassDB classDB)
        {
            await PlayerDB.ExecuteAsync("INSERT INTO player_class (SteamID, ZClass, HClass) VALUES(@SteamID, @ZClass, @HClass)", classDB);
        }

        public async Task<PlayerClassDB> Get(string steamid)
        {
            return await PlayerDB.QueryFirstAsync<PlayerClassDB>("SELECT * From player_class WHERE SteamID = @steamid",
                new
                {
                    steamid
                });
        }

        public async Task Update(PlayerClassDB classDB)
        {
            await PlayerDB.ExecuteAsync("Update player_class SET ZClass = @ZClass, HClass = @HClass WHERE SteamID = @SteamID", classDB);
        }

        public void PlayerSettingsAuthorized(CCSPlayerController client)
        {
            var steamId = client.AuthorizedSteamID.SteamId64;
        }
    }
}

public class PlayerClassDB
{
    public string SteamID { get; set; }
    public string ZClass { get; set; }
    public string HClass { get; set; }
}