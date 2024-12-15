using CounterStrikeSharp.API.Modules.Utils;

namespace ZombieSharp.Models;

public class SpawnData
{
    public Vector PlayerPosition { get; set; } = Vector.Zero;
    public QAngle PlayerAngle { get; set; } = QAngle.Zero;
}