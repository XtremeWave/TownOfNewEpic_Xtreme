using System.Collections.Generic;
using System.Drawing;
using TONEX.Attributes;
using TONEX.Roles.Core;
using UnityEngine;
using static TONEX.Options;

namespace TONEX.Roles.AddOns.Common;
public static class Libertarian
{
    private static readonly int Id = 156674;
    public static List<byte> playerIdList = new();
    public static OptionItem OptionRadius;
    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Libertarian);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Libertarian, true, true, true);
        OptionRadius = FloatOptionItem.Create(Id + 20, "LibertarianRadius", new(0.5f, 10f, 0.5f), 1f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Libertarian])
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
