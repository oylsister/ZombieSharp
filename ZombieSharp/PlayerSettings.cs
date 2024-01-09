using Dapper;
using Microsoft.Data.Sqlite;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        private SqliteConnection PlayerDB = null!;
        public void PlayerSettingsOnLoad()
        {
            PlayerDB = new SqliteConnection($"Data Source={Path.Join(ModuleDirectory, "zombiesharp.db")}");
            PlayerDB.Open();

            Task.Run(async () =>
            {
                await PlayerDB.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS `player_class` (`SteamID` VARCHAR(64), `ZClass` VARCHAR(64), `HClass` VARCHAR(64), PRIMARY KEY (`SteamID`));");
            });
        }

        public void CreatePlayerSettings(PlayerClassDB classDB)
        {
            Task.Run(async () =>
            {
                await PlayerDB.ExecuteAsync("INSERT INTO player_class (SteamID, ZClass, HClass) VALUES(@SteamID, @ZClass, @HClass)", classDB);
            });
        }

        public PlayerClassDB GetPlayerSettings(string steamid)
        {
            PlayerClassDB db = null;

            Task.Run(async () =>
            {
                db = await PlayerDB.QueryFirstAsync<PlayerClassDB>(@"SELECT * From `player_class` WHERE `SteamID` = @steamid",
                new
                {
                    steamid
                });
            });

            return db;
        }

        public void UpdatePlayerSettings(PlayerClassDB classDB)
        {
            Task.Run(async () =>
            {
                await PlayerDB.ExecuteAsync("Update player_class SET ZClass = @ZClass, HClass = @HClass WHERE SteamID = @SteamID", classDB);
            });
        }

        public void PlayerSettingsAuthorized(CCSPlayerController client)
        {
            var clientindex = client.Slot;

            if (client.IsBot)
            {
                ClientPlayerClass.Add(clientindex, new PlayerClientClass());

                ClientPlayerClass[clientindex].HumanClass = ConfigSettings.Human_Default;
                ClientPlayerClass[clientindex].ZombieClass = ConfigSettings.Zombie_Default;
                ClientPlayerClass[clientindex].ActiveClass = null;

                return;
            }

            var result = GetPlayerSettings(client.AuthorizedSteamID.SteamId3);

            if (result == null)
            {
                ClientPlayerClass.Add(clientindex, new PlayerClientClass());

                ClientPlayerClass[clientindex].HumanClass = ConfigSettings.Human_Default;
                ClientPlayerClass[clientindex].ZombieClass = ConfigSettings.Zombie_Default;
                ClientPlayerClass[clientindex].ActiveClass = null;

                PlayerClassDB db = new PlayerClassDB();

                db.SteamID = client.AuthorizedSteamID.SteamId3;
                db.HClass = ClientPlayerClass[clientindex].HumanClass;
                db.ZClass = ClientPlayerClass[clientindex].ZombieClass;

                CreatePlayerSettings(db);

                return;
            }
            else
            {
                ClientPlayerClass.Add(clientindex, new PlayerClientClass());

                ClientPlayerClass[clientindex].HumanClass = result.HClass;
                ClientPlayerClass[clientindex].ZombieClass = result.ZClass;
                ClientPlayerClass[clientindex].ActiveClass = null;
            }
        }
    }
}

public class PlayerClassDB
{
    public string SteamID { get; set; }
    public string ZClass { get; set; }
    public string HClass { get; set; }
}