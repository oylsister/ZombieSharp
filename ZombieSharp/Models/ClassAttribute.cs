namespace ZombieSharp.Models;

public class ClassAttribute
{
    public string? Name { get; set; }
    public bool Enable { get; set; } = false;
    public int Team { get; set; }
    public string? Model { get; set; }
    public bool MotherZombie { get; set; } = false;
    public int Health { get; set; } = 100;
    public float Knockback { get; set; }
    public float Speed { get; set; } = 250f;
}

public class PlayerClasses
{
    public ClassAttribute? HumanClass { get; set; } = null;
    public ClassAttribute? ZombieClass { get; set; } = null;
    public ClassAttribute? ActiveClass { get; set; } = null;
}