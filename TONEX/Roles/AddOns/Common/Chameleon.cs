using System.Collections.Generic;
using System.Drawing;
using TONEX.Attributes;
using TONEX.Roles.Core;
using UnityEngine;
using static TONEX.Options;

namespace TONEX.Roles.AddOns.Common;
public static class Chameleon
{
    private static readonly int Id = 2894763;
    private static List<byte> playerIdList = new();
    public static OptionItem OptionSpeed;
    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Chameleon);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Chameleon, true, true, true);
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
    public static void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player.Is(CustomRoles.Chameleon))
        {
            var color = IRandom.Instance.Next(0, 18);
            player.SetOutFitStatic(color);
        }
    }
}