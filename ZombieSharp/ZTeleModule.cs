namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public class ClientSpawnData
        {
            public Vector PlayerPosition { get; set; } = new Vector(0f, 0f, 0f);
            public QAngle PlayerAngle { get; set; } = new QAngle(0f, 0f, 0f);
        }

        public Dictionary<int, ClientSpawnData> ClientSpawnDatas { get; set; } = new Dictionary<int, ClientSpawnData>();

        public void ZTele_GetClientSpawnPoint(CCSPlayerController client, Vector position, QAngle angle)
        {
            if (client is null)
            {
                Server.PrintToChatAll("The client is null, you didn't get shit.");
                return;
            }

            if (!IsPlayerAlive(client))
                return;

            var clientPawn = client.PlayerPawn.Value;

            //Server.PrintToChatAll($"{client.PlayerName} Pos: {position}");
            //Server.PrintToChatAll($"{client.PlayerName} Angle: {angle}");

            ClientSpawnDatas[client.Slot].PlayerPosition = new Vector(position.X, position.Y, position.Z);
            ClientSpawnDatas[client.Slot].PlayerAngle = new QAngle(angle.X, angle.Y, angle.Z);
        }

        public void ZTele_TeleportClientToSpawn(CCSPlayerController client)
        {
            var playerpawn = client.PlayerPawn.Value;

            var position = ClientSpawnDatas[client.Slot].PlayerPosition;
            var angle = ClientSpawnDatas[client.Slot].PlayerAngle;
            var velocity = client.PlayerPawn.Value.AbsVelocity!;

            playerpawn.Teleport(position, angle, velocity);
        }
    }
}
