using Hazel;
using System.Collections.Generic;
using TONEX.Attributes;
using TONEX.Roles.Core;
using UnityEngine;
using UnityEngine.UIElements.UIR;
using static TONEX.Options;

namespace TONEX.Roles.AddOns.Common;
public static class Signal
{
    private static readonly int Id = 7565_3_1_0;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Signal);
    private static List<byte> playerIdList = new();
    public static Dictionary<byte, Vector2> Signalbacktrack = new();
    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Signal);
        AddOnsAssignData.Create(Id + 10, TabGroup.Addons, CustomRoles.Signal, true, true, true);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
        Signalbacktrack = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static void AddPosi(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !player.IsAlive() || !player.Is(CustomRoles.Signal) || !GameStates.IsInTask) return;
        Signalbacktrack.Add(player.PlayerId, player.GetTruePosition());
        SendRPC();
    }
    public static void AfterMeet()
    {
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.Is(CustomRoles.Signal))
            {
                pc.RpcTeleport(Signalbacktrack[pc.PlayerId]);
            }
        }
        Signalbacktrack = new();
    }
    public static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SignalPosition, SendOption.Reliable, -1);
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (Signalbacktrack.ContainsKey(pc.PlayerId))
            {
                writer.Write(pc.PlayerId);
                writer.Write(Signalbacktrack[pc.PlayerId].x);
                writer.Write(Signalbacktrack[pc.PlayerId].y);
            }
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SignalPosition) return;
        var pc = reader.ReadByte();
        var x = reader.ReadSingle();
        var y = reader.ReadSingle();
        Signalbacktrack.Add(pc,new(x, y));
    }
}
