using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ZombieSharp.Models;
using ZombieSharp.Plugin;

namespace ZombieSharp.Database;

public class DatabaseMain(ZombieSharp core, ILogger<ZombieSharp> logger)
{
    private SqliteConnection? _connection;
    private readonly ZombieSharp _core = core;
    private readonly ILogger<ZombieSharp> _logger = logger;

    public async Task DatabaseOnLoad()
    {
        var path = Path.Join(_core.ModuleDirectory, "zsharpdatabase.db");
        var found = File.Exists(path);

        _connection = new SqliteConnection($"Data Source={path}");
        _connection.Open();

        if(!found)
            _logger.LogInformation("[DatabaseOnLoad] Database has been created. to {0}", path);

        else
            _logger.LogInformation("[DatabaseOnLoad] Database is Loaded");

        await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS player_classes (player_auth TEXT PRIMARY KEY, zombie_class VARCHAR(64), human_class VARCHAR(64));");
        _logger.LogInformation("[DatabaseOnLoad] Initialize Player Class settings Table.");

        await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS player_sound (player_auth TEXT PRIMARY KEY, zombie_voice INT, zombie_countdown INT);");
        _logger.LogInformation("[DatabaseOnLoad] Initialize Player Sound settings Table.");
    }

    public void DatabaseOnUnload()
    {
        _connection?.Close();
    }

    public async Task<PlayerClasses?> GetPlayerClassData(ulong steamid)
    {
        if(_connection == null)
        {
            _logger.LogError("[GetPlayerClassData] SqlConnection is null!");
            return null;
        }

        if(Classes.ClassesConfig == null)
        {
            _logger.LogCritical("[GetPlayerClassData] ClassesConfig is null!");
            return null;
        }

        var query = @"SELECT * FROM player_classes WHERE player_auth = @Auth;";
        var reader = await _connection.ExecuteReaderAsync(query, new {
            Auth = steamid.ToString()
        });

        //_logger.LogInformation("[GetPlayerClassData] Getting Player Data start here.");

        if(await reader.ReadAsync())
        {
            var humanClass = reader["human_class"].ToString();
            var zombieClass = reader["zombie_class"].ToString();

            PlayerClasses classes = new();

            // obviously has to be null on connected.
            classes.ActiveClass = null;
            classes.HumanClass = Classes.ClassesConfig.Where(attribute => attribute.Key == humanClass).FirstOrDefault().Value;
            classes.ZombieClass = Classes.ClassesConfig.Where(attribute => attribute.Key == zombieClass).FirstOrDefault().Value;

            if(classes.HumanClass == null)
                _logger.LogError("[GetPlayerClassData] HumanClass: \"{0}\" is null in classes config!", humanClass);

            if(classes.ZombieClass == null)
                _logger.LogError("[GetPlayerClassData] ZombieClass: \"{0}\" is null in classes config!", zombieClass);

            //_logger.LogInformation("[GetPlayerClassData] We done here.");
            return classes;
        }

        // _logger.LogInformation("[GetPlayerClassData] It's null");
        return null;
    } 

    public async Task InsertPlayerClassData(ulong steamid, PlayerClasses playerClasses)
    {
        if(_connection == null)
        {
            _logger.LogError("[GetPlayerClassData] SqlConnection is null!");
            return;
        }

        if(Classes.ClassesConfig == null)
        {
            _logger.LogCritical("[InsertPlayerClassData] ClassesConfig is null!");
            return;
        }

        var humanClass = Classes.ClassesConfig.Where(attribute => attribute.Value == playerClasses.HumanClass).FirstOrDefault().Key;
        var zombieClass = Classes.ClassesConfig.Where(attribute => attribute.Value == playerClasses.ZombieClass).FirstOrDefault().Key;

        if(humanClass == null)
        {
            _logger.LogError("[InsertPlayerClassData] HumanClass: \"{0}\" is null in classes config!", humanClass);
            return;
        }

        if(zombieClass == null)
        {
            _logger.LogError("[InsertPlayerClassData] ZombieClass: \"{0}\" is null in classes config!", zombieClass);
            return;
        }

        var query = @"INSERT INTO player_classes (player_auth, zombie_class, human_class) VALUES(@Auth, @ZombieClass, @HumanClass) ON CONFLICT(player_auth) DO UPDATE SET zombie_class = @ZombieClass, human_class = @HumanClass";

        await _connection.ExecuteAsync(query, new {
            Auth = steamid.ToString(),
            ZombieClass = zombieClass,
            HumanClass = humanClass
        });
    }

    public async Task<ZombieSound?> GetPlayerSoundData(ulong steamid)
    {
        if(_connection == null)
        {
            _logger.LogError("[GetPlayerSoundData] SqlConnection is null!");
            return null;
        }

        var query = @"SELECT * FROM player_sound WHERE player_auth = @Auth;";
        var reader = await _connection.ExecuteReaderAsync(query, new {
            Auth = steamid.ToString()
        });

        //_logger.LogInformation("[GetPlayerClassData] Getting Player Data start here.");

        if(await reader.ReadAsync())
        {
            var zombieVoice = Convert.ToBoolean(reader["zombie_voice"]);
            var zombieCountdown = Convert.ToBoolean(reader["zombie_countdown"]);

            ZombieSound sound = new()
            {
                ZombieVoice = zombieVoice,
                Countdown = zombieCountdown
            };

            //_logger.LogInformation("[GetPlayerClassData] We done here.");
            return sound;
        }
        
        return null;
    } 

    public async Task InsertPlayerSoundData(ulong steamid, ZombieSound sound)
    {
        if(_connection == null)
        {
            _logger.LogError("[GetPlayerClassData] SqlConnection is null!");
            return;
        }

        var query = @"INSERT INTO player_sound (player_auth, zombie_voice, zombie_countdown) VALUES(@Auth, @ZombieVoice, @Countdown) ON CONFLICT(player_auth) DO UPDATE SET zombie_voice = @ZombieVoice, zombie_countdown = @Countdown";

        await _connection.ExecuteAsync(query, new {
            Auth = steamid.ToString(),
            ZombieVoice = sound.ZombieVoice,
            Countdown = sound.Countdown
        });
    }
}   