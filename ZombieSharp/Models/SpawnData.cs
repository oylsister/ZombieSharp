using CounterStrikeSharp.API.Modules.Utils;

namespace ZombieSharp.Models;

public class SpawnData
{
    public Vector PlayerPosition { get; set; } = new(0, 0, 0);
    public QAngle PlayerAngle { get; set; } = new(0, 0, 0);
}