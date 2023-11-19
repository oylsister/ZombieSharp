﻿namespace ZombieSharp
{
    public class ZTeleModule
    {
        private ZombieSharp _core;

        public readonly Dictionary<CCSPlayerController, ClientSpawnData> ClientSpawnDatas = new();

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

            playerpawn.Teleport(position, angle, null!);
        }
    }
}

public class ClientSpawnData
{
    public Vector PlayerPosition { get; set; }
    public QAngle PlayerAngle { get; set; }
}
