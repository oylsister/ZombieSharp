using static ZombieSharp.ZTeleModule;

namespace ZombieSharp
{
    public interface IZTeleModule
    {
        void ZTele_GetClientSpawnPoint(CCSPlayerController client, Vector position, QAngle angle);
        void ZTele_TeleportClientToSpawn(CCSPlayerController client);
        Dictionary<CCSPlayerController, ClientSpawnData> ClientSpawnDatas { get; set; }
    }

    public class ZTeleModule : IZTeleModule
    {
        public struct ClientSpawnData
        {
            public Vector PlayerPosition;
            public QAngle PlayerAngle;
        }

        private ZombieSharp _core;

        public Dictionary<CCSPlayerController, ClientSpawnData> ClientSpawnDatas { get; set; } = new();

        public ZTeleModule(ZombieSharp plugin)
        {
            _core = plugin;
        }

        public void ZTele_GetClientSpawnPoint(CCSPlayerController client, Vector position, QAngle angle)
        {
            ClientSpawnDatas.Add(client, new ClientSpawnData
            {
                PlayerAngle = angle,
                PlayerPosition = position
            });
        }

        public void ZTele_TeleportClientToSpawn(CCSPlayerController client)
        {
            var playerpawn = client.PlayerPawn.Value;

            var position = ClientSpawnDatas[client].PlayerPosition;
            var angle = ClientSpawnDatas[client].PlayerAngle;

            playerpawn.Teleport(position, angle, new(0f, 0f, 0f));
        }
    }
}
