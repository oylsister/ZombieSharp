namespace ZombieSharp.Plugin.Listeners;

public abstract class ListenerBase
{
    public abstract Delegate DelegateMethod { get; }
}

public abstract class ListenerBase<T> : ListenerBase where T : Delegate
{
    public abstract override T DelegateMethod { get; }
}