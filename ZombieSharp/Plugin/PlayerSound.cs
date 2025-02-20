using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using ZombieSharp.Database;
using ZombieSharp.Models;

namespace ZombieSharp.Plugin;

public class PlayerSound
{
    private ZombieSharp _core;
    private static DatabaseMain? _database;
    private static ILogger<ZombieSharp>? _logger;

    public PlayerSound(ZombieSharp core, DatabaseMain database, ILogger<ZombieSharp> logger)
    {
        _core = core;
        _database = database;
        _logger = logger;
    }

    public void OnLoad()
    {
        _core.AddCommand("css_zsound", "Toggle Zombie Sound Command", ZSoundCommand);
    }

    [CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
    public void ZSoundCommand(CCSPlayerController? client, CommandInfo info)
    {
        if(client == null)
            return;

        if(!PlayerData.PlayerSoundData.ContainsKey(client))
            PlayerData.PlayerSoundData.TryAdd(client, new());

        PlayerData.PlayerSoundData[client].ZombieVoice = !PlayerData.PlayerSoundData[client].ZombieVoice;
        info.ReplyToCommand($" {_core.Localizer["Prefix"]} {(PlayerData.PlayerSoundData[client].ZombieVoice ? _core.Localizer["ZSound.Enable"] : _core.Localizer["ZSound.Disable"])}");
    }

    public static void PlayerSoundOnClientPutInServer(CCSPlayerController client)
    {
        PlayerData.PlayerSoundData.Add(client, new());

        if(_database == null)
        {
            _logger?.LogError("[PlayerSound] Database is null!");
            return;
        }

        if(!client.IsBot)
        {
            var auth = client.AuthorizedSteamID?.SteamId64;

            if(auth == null || !auth.HasValue)
            {
                auth = client.SteamID;
            }

            Task.Run(async () => {
                var sound = await _database.GetPlayerSoundData(auth.Value);

                if(sound == null)
                    await _database.InsertPlayerSoundData(auth.Value, new());

                else
                    PlayerData.PlayerSoundData[client] = sound;
            });
        }
    }

    public static void PlayerSoundOnClientDisconnect(CCSPlayerController client)
    {
        PlayerData.PlayerSoundData.Remove(client);
    }
}