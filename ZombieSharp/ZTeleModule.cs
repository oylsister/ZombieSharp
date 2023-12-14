using System.Dynamic;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public class ClientSpawnData
        {
            public Vector PlayerPosition { get; set; } = new Vector(0f, 0f, 0f);
            public QAngle PlayerAngle { get; set; } = new QAngle(0f, 0f, 0f);
        }

        public ClientSpawnData[] ClientSpawnDatas { get; set; } =  new ClientSpawnData[Server.MaxPlayers];

        public void ZTele_GetClientSpawnPoint(CCSPlayerController client, Vector position, QAngle angle)
        {
            if(client is null)
            {
                Server.PrintToChatAll("The client is null, you didn't get shit.");
                return;
            }

            if(!client.PawnIsAlive)
                return;

            var clientPawn = client.PlayerPawn.Value;

            Server.PrintToChatAll($"{client.PlayerName} Pos: {position}");
            Server.PrintToChatAll($"{client.PlayerName} Angle: {angle}");

            ClientSpawnDatas[client.UserId ?? 0].PlayerPosition = new Vector(position.X, position.Y, position.Z);
            ClientSpawnDatas[client.UserId ?? 0].PlayerAngle = new QAngle(angle.X, angle.Y, angle.Z);
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
