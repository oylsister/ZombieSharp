using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;

namespace ZombieSharp.Models;

[StructLayout(LayoutKind.Explicit)]
public struct CAttackerInfo
{
    public CAttackerInfo(CEntityInstance attacker)
    {
        NeedInit = false;
        IsWorld = true;
        Attacker = attacker.EntityHandle.Raw;
        if (attacker.DesignerName != "cs_player_controller") return;

        var controller = attacker.As<CCSPlayerController>();
        IsWorld = false;
        IsPawn = true;
        AttackerUserId = (ushort)(controller.UserId ?? 0xFFFF);
        TeamNum = controller.TeamNum;
        TeamChecked = controller.TeamNum;
    }

    [FieldOffset(0x0)] public bool NeedInit = true;
    [FieldOffset(0x1)] public bool IsPawn = false;
    [FieldOffset(0x2)] public bool IsWorld = false;

    [FieldOffset(0x4)]
    public uint Attacker;

    [FieldOffset(0x8)]
    public ushort AttackerUserId;

    [FieldOffset(0x0C)] public int TeamChecked = -1;
    [FieldOffset(0x10)] public int TeamNum = -1;
}