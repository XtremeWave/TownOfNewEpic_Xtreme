using System.Collections.Generic;
using TONEX.Attributes;
using TONEX.Roles.Core;
using UnityEngine;
using static TONEX.Options;

namespace TONEX.Roles.AddOns.Impostor;
public static class Spiders
{
    private static readonly int Id = 94_1_1_0100;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Spiders);
    private static List<byte> playerIdList = new();
    public static OptionItem OptionSpeed;

    public static OptionItem OptionTicketsPerKill;
    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Spiders);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Spiders, false, true, false);
        OptionSpeed = FloatOptionItem.Create(Id + 20, "SpidersSpeed", new(0.25f, 5f, 0.25f), 0.5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiders])
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

