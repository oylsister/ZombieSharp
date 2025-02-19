using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Runtime.InteropServices;

namespace ZombieSharp.Models;

// TODO: Figure out how to create a CRecipientFilter without needing a CS2 function
public class CRecipientFilter : IDisposable
{
    // How to find
    // 1. Search for `Flashbang.Ring.Medium`
    // 2. Almost at the very top of the method you will find these two methods adjacent to the offset of `CSingleUserRecipientFilter`
    //
    // Alternative Strings
    // * `Flashbang.Ring.Long`
    // * `control.deafenLong`
    // * `control.deafenMedium`
    // * `control.deafenShort`
    // 
    // How to find CRecipientFilter_Init (only)
    // 1. Search for the `CRecipientFilter` vtable
    // 2. Cross reference the offset and there should only be one method
    public static readonly MemoryFunctionWithReturn<nint, nint> CRecipientFilter_Init =
        new("55 48 8D 05 ? ? ? ? 48 89 E5 41 54 53 4C 8D 67 ? 48 89 FB 48 89 07");
    public static readonly MemoryFunctionWithReturn<nint, nint, int> CRecipientFilter_AddPlayer =
        new("48 85 F6 0F 84 ? ? ? ? 55 48 89 E5 41 56 41 89 D6 41 55 49 89 F5");
    
    private bool _freed;
    private IntPtr _ptr;

    public IntPtr Handle => GetHandle();

    public CRecipientFilter()
    {
        // TODO: How large should it be? The largest offset in CRecipientFilter_Init is 298
        int size = 400;

        IntPtr pointer = Marshal.AllocHGlobal(size);
        _ptr = pointer;
        for (int i = 0; i < size; i++)
        {
            Marshal.WriteByte(pointer, i, 0);
        }

        CRecipientFilter_Init.Invoke(pointer);
    }

    private void AddPlayer(IntPtr pointer)
    {
        CRecipientFilter_AddPlayer.Invoke(Handle, pointer);
    }

    public CRecipientFilter AddPlayers(params CBasePlayerController[] players)
    {
        return AddPlayers(players.AsEnumerable());
    }

    public CRecipientFilter AddPlayers(IEnumerable<CBasePlayerController> players)
    {
        foreach (CBasePlayerController player in players)
        {
            AddPlayer(player.Handle);
        }

        return this;
    }

    public CRecipientFilter AddPlayers(params CBasePlayerPawn[] pawns)
    {
        return AddPlayers(pawns.AsEnumerable());
    }

    public CRecipientFilter AddPlayers(IEnumerable<CBasePlayerPawn> pawns)
    {
        foreach (CBasePlayerPawn pawn in pawns)
        {
            AddPlayer(pawn.Handle);
        }

        return this;
    }

    public CRecipientFilter AddAllPlayers()
    {
        return AddPlayers(Utilities.GetPlayers());
    }

    private IntPtr GetHandle()
    {
        if (_freed || _ptr == IntPtr.Zero)
        {
            throw new Exception("Object has been freed");
        }

        return _ptr;
    }

    private void Free()
    {
        if (_freed)
        {
            return;
        }

        _freed = true;

        if (_ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_ptr);
            _ptr = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        Free();
        GC.SuppressFinalize(this);
    }

    ~CRecipientFilter()
    {
        Free();
    }

    public static implicit operator IntPtr(CRecipientFilter filter)
    {
        return filter.Handle;
    }
}