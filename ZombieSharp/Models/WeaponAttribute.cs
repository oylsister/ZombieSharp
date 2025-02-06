namespace ZombieSharp.Models;

public class WeaponAttribute
{
    public string? WeaponName { get; set; }
    public string? WeaponEntity { get; set; }
    public float Knockback { get; set; } = 1f;
    public int WeaponSlot { get; set; }
    public int Price { get; set; }
    public int MaxPurchase { get; set; } = 0;
    public bool Restrict { get; set; } = false;
    public List<string>? PurchaseCommand { get; set; } = [];
}

public class PurchaseCount
{
    public PurchaseCount()
    {
        WeaponCount = new Dictionary<string, int>();
    }

    public Dictionary<string, int>? WeaponCount { get; set; } = [];
}