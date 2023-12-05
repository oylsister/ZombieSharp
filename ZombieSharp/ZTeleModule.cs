using System.Dynamic;
using static ZombieSharp.ZTeleModule;

namespace ZombieSharp
{
    public interface IZTeleModule
    {
        void ZTele_GetClientSpawnPoint(CCSPlayerController client, Vector position, QAngle angle);
        void ZTele_TeleportClientToSpawn(CCSPlayerController client);
        ClientSpawnData[] ClientSpawnDatas { get; set; }
    }

    public class ZTeleModule : IZTeleModule
    {
        public class ClientSpawnData
        {
            public Vector PlayerPosition;
            public QAngle PlayerAngle;
        }

        private ZombieSharp _core;

        public ClientSpawnData[] ClientSpawnDatas { get; set; } =  new ClientSpawnData[Server.MaxPlayers];

        public ZTeleModule(ZombieSharp plugin)
        {
            _core = plugin;
        }

        public void ZTele_GetClientSpawnPoint(CCSPlayerController client, Vector position, QAngle angle)
        {
            if(!client.PawnIsAlive)
                return;
                
            ClientSpawnDatas[client.Slot].PlayerAngle = angle;
            ClientSpawnDatas[client.Slot].PlayerPosition = position;
        }

        public void ZTele_TeleportClientToSpawn(CCSPlayerController client)
        {
            var playerpawn = client.PlayerPawn.Value;

            var position = ClientSpawnDatas[client.Slot].PlayerPosition;
            var angle = ClientSpawnDatas[client.Slot].PlayerAngle;

            playerpawn.Teleport(position, angle, new(0f, 0f, 0f));
        }
    }
}
