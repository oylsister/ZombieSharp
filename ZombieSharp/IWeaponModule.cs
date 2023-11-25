namespace ZombieSharp
{
    public interface IWeaponModule
    {
        WeaponConfig WeaponDatas { get; }

        void Initialize();
    }
}