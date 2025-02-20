using CounterStrikeSharp.API.Core;
using ZombieSharp.Plugin;

namespace ZombieSharp.Models;

public class ZombieSound(bool zombie = true, bool countdown = true)
{
    public bool ZombieVoice = zombie;
    public bool Countdown = countdown;

    public static void ZombieEmitSound(CBaseEntity entity, string soundPath)
    {
        if(entity == null || !entity.IsValid)
            return;

        foreach(var client in PlayerData.PlayerSoundData)
        {
            if(client.Key == null || !client.Key.IsValid)
                continue;

            if(!client.Value.ZombieVoice)
                continue;

            Utils.EmitSoundToClient(client.Key, entity, soundPath);
        }
    }
}