namespace ZombieSharp
{
    public class ZTeleModule
    {
        private struct ClientSpawnData
        {
            public Vector PlayerPosition;
            public QAngle PlayerAngle;
        }
        
        private ZombieSharp _core;

        private readonly Dictionary<CCSPlayerController, ClientSpawnData> _clientSpawnDatas = new();

        public ZTeleModule(ZombieSharp plugin)
        {
            _core = plugin;
        }

        public void ZTele_GetClientSpawnPoint(CCSPlayerController client, Vector position, QAngle angle)
        {
            _clientSpawnDatas.Add(client, new ClientSpawnData
            {
                PlayerAngle = angle,
                PlayerPosition = position
            });
        }

        public void ZTele_TeleportClientToSpawn(CCSPlayerController client)
        {
            var playerpawn = client.PlayerPawn.Value;

            var position = _clientSpawnDatas[client].PlayerPosition;
            var angle = _clientSpawnDatas[client].PlayerAngle;

            playerpawn.Teleport(position, angle, null!);
        }
    }
}
