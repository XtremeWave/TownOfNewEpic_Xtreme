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
    public static Vector2 Signalbacktrack = new();
    private static bool hasSended = false;
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
        hasSended = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        hasSended = false;
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !player.IsAlive()) return;
        
        if (player.Is(CustomRoles.Signal))
        {
            if(!GameStates.IsInTask)
                Signalbacktrack = player.GetTruePosition();
            else if (!hasSended)
            {
                SendRPC();
                hasSended = false;
            }
        }
    }
    public static void AfterMeet()
    {
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.Is(CustomRoles.Signal))
            {
                Utils.TP(pc.NetTransform, Signalbacktrack);
            }
        }
        hasSended = false;
    }
    public static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MiniAge, SendOption.Reliable, -1);
        writer.Write(Signalbacktrack.x);
        writer.Write(Signalbacktrack.y);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.MiniAge) return;
        var x = reader.ReadSingle();
        var y = reader.ReadSingle();
        Signalbacktrack = new(x, y);
    }
}
