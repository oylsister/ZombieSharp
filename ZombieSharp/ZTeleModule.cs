using static ZombieSharp.ZTeleModule;

namespace ZombieSharp
{
    public interface IZTeleModule
    {
        void ZTele_GetClientSpawnPoint(CCSPlayerController client, Vector position, QAngle angle);
        void ZTele_TeleportClientToSpawn(CCSPlayerController client);
        Dictionary<int, ClientSpawnData> ClientSpawnDatas { get; set; }
    }

    public class ZTeleModule : IZTeleModule
    {
        public struct ClientSpawnData
        {
            public Vector PlayerPosition;
            public QAngle PlayerAngle;
        }

        private ZombieSharp _core;

        public Dictionary<int, ClientSpawnData> ClientSpawnDatas { get; set; } = new Dictionary<int, ClientSpawnData>();

        public ZTeleModule(ZombieSharp plugin)
        {
            _core = plugin;
        }

        public void ZTele_GetClientSpawnPoint(CCSPlayerController client, Vector position, QAngle angle)
        {
            if (ClientSpawnDatas.ContainsKey(client.UserId ?? 0))
            {
                ClientSpawnDatas[client.UserId ?? 0].PlayerAngle.X = angle.X;
                ClientSpawnDatas[client.UserId ?? 0].PlayerAngle.Y = angle.Y;
                ClientSpawnDatas[client.UserId ?? 0].PlayerAngle.Z = angle.Z;

                ClientSpawnDatas[client.UserId ?? 0].PlayerPosition.X = position.X;
                ClientSpawnDatas[client.UserId ?? 0].PlayerPosition.Y = position.Y;
                ClientSpawnDatas[client.UserId ?? 0].PlayerPosition.Z = position.Z;
            }

            else
            {
                ClientSpawnDatas.Add(client.UserId ?? 0, new ClientSpawnData
                {
                    PlayerAngle = angle,
                    PlayerPosition = position
                });
            }
        }

        public void ZTele_TeleportClientToSpawn(CCSPlayerController client)
        {
            var playerpawn = client.PlayerPawn.Value;

            var position = ClientSpawnDatas[client.UserId ?? 0].PlayerPosition;
            var angle = ClientSpawnDatas[client.UserId ?? 0].PlayerAngle;

            playerpawn.Teleport(position, angle, new(0f, 0f, 0f));
        }
    }
}
