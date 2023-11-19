using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZombieSharp
{
    public class ZTeleModule
    {
        private ZombieSharp _Core;

        public Dictionary<CCSPlayerController, ClientSpawnData> ClientSpawnDatas = new Dictionary<CCSPlayerController, ClientSpawnData>();

        public ZTeleModule(ZombieSharp plugin)
        {
            _Core = plugin;
        }

        public void ZTele_GetClientSpawnPoint(CCSPlayerController client, Vector position, QAngle angle)
        {
            ClientSpawnDatas.Add(client, new ClientSpawnData() 
                { PlayerPosition = position, 
                PlayerAngle = angle 
            });
        }

        public void ZTele_TeleportClientToSpawn(CCSPlayerController client)
        {
            CCSPlayerPawn playerpawn = client.PlayerPawn.Value;

            var position = ClientSpawnDatas[client].PlayerPosition;
            var angle = ClientSpawnDatas[client].PlayerAngle;

            playerpawn.Teleport(position, angle, null);
        }
    }
}

public class ClientSpawnData
{
    public Vector PlayerPosition { get; set; }
    public QAngle PlayerAngle { get; set; }
}
