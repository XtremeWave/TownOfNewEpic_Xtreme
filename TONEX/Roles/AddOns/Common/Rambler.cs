using System.Collections.Generic;
using System.Drawing;
using TONEX.Attributes;
using TONEX.Roles.Core;
using UnityEngine;
using static TONEX.Options;

namespace TONEX.Roles.AddOns.Common;
public static class Rambler
{
    private static readonly int Id = 154564874;
    private static List<byte> playerIdList = new();
   public static OptionItem OptionSpeed;
    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Rambler);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Rambler, true, true, true);
        OptionSpeed = FloatOptionItem.Create(Id + 20, "RamblerSpeed", new(0.25f, 5f, 0.25f), 0.5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rambler])
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
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}



