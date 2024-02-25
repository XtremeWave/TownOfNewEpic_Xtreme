using Hazel;
using System.Collections.Generic;
using TONEX.Attributes;
using TONEX.Roles.Core;
using UnityEngine;
using UnityEngine.UIElements.UIR;
using static TONEX.Options;

namespace TONEX.Roles.AddOns.Common;
public static class Diseased
{
    private static readonly int Id = 75_1_1_0500;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Diseased);
    private static List<byte> playerIdList = new();
    public static List<byte> DisList = new();
    public static OptionItem OptionVistion;

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Diseased);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Diseased, true, true, true);
        OptionVistion = FloatOptionItem.Create(Id + 20, "DiseasedVision", new(0.5f, 5f, 0.25f), 1.5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diseased])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MiniAge, SendOption.Reliable, -1);
        foreach (var pc in DisList)
        {
            writer.Write(pc);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.MiniAge) return;
        
        for (int i =0;i<DisList.Count; i++)
        {
            var pc = reader.ReadByte();
            if (!DisList.Contains(pc))
                DisList.Add(pc);
        }
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

}