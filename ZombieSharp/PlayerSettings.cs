using Dapper;
using Microsoft.Data.Sqlite;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        private SqliteConnection PlayerDB = null!;
        public async Task PlayerSettingsOnLoad()
        {
            PlayerDB = new SqliteConnection($"Data Source={Path.Join(ModuleDirectory, "zombiesharp.db")}");
            PlayerDB.Open();

            await PlayerDB.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS `player_class` (`SteamID` VARCHAR(64), `ZClass` VARCHAR(64), `HClass` VARCHAR(64), PRIMARY KEY (`SteamID`));");
        }

        public async Task CreatePlayerSettings(PlayerClassDB classDB)
        {
            await PlayerDB.ExecuteAsync("INSERT INTO `player_class` (`SteamID`, `ZClass`, `HClass`) VALUES(@SteamID, @ZClass, @HClass) ON CONFLICT(`SteamID`) DO UPDATE SET `ZClass` = @ZClass, `HClass` = @HClass;", classDB);
        }

        public async Task<PlayerClassDB> GetPlayerSettings(string steamid)
        {
            PlayerClassDB db = await PlayerDB.QueryFirstOrDefaultAsync<PlayerClassDB>(@"SELECT * From `player_class` WHERE `SteamID` = @steamid",
                new
                {
                    steamid
                });

            return db;
        }

        public async Task UpdatePlayerSettings(PlayerClassDB classDB)
        {
            await PlayerDB.ExecuteAsync("Update player_class SET ZClass = @ZClass, HClass = @HClass WHERE SteamID = @SteamID", classDB);
        }

        public void PlayerSettingsOnPutInServer(CCSPlayerController client)
        {
            var clientindex = client.Slot;

            ClientPlayerClass.Add(clientindex, new PlayerClientClass());

            ClientPlayerClass[clientindex].HumanClass = CVAR_Human_Default.Value;
            ClientPlayerClass[clientindex].ZombieClass = CVAR_Zombie_Default.Value;
            ClientPlayerClass[clientindex].ActiveClass = null;
        }

        public async Task PlayerSettingsAuthorized(CCSPlayerController client)
        {
            if (client.IsBot)
                return;

            var clientindex = client.Slot;
            var result = await GetPlayerSettings(client.AuthorizedSteamID.SteamId3);

            if (result == null)
            {
                PlayerClassDB db = new();

                db.SteamID = client.AuthorizedSteamID.SteamId3;
                db.HClass = ClientPlayerClass[clientindex].HumanClass;
                db.ZClass = ClientPlayerClass[clientindex].ZombieClass;

                await CreatePlayerSettings(db);

                return;
            }
            else
            {
                if (result.HClass == null || result.ZClass == null)
                {
                    PlayerClassDB db = new();

                    db.SteamID = client.AuthorizedSteamID.SteamId3;
                    db.HClass = ClientPlayerClass[clientindex].HumanClass;
                    db.ZClass = ClientPlayerClass[clientindex].ZombieClass;

                    await CreatePlayerSettings(db);
                }

                else
                {
                    ClientPlayerClass[clientindex].HumanClass = result.HClass;
                    ClientPlayerClass[clientindex].ZombieClass = result.ZClass;
                }

                return;
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