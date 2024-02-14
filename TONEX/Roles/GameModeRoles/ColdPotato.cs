using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TONEX.Roles.Core;
using static TONEX.Translator;
using System.Text;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.GameModeRoles;
public sealed class ColdPotato : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ColdPotato),
            player => new ColdPotato(player),
            CustomRoles.ColdPotato,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            94_1_0_0100,
            null,
            "cold|冷土豆",
            "#66ffff",
            true,
           introSound: () => GetIntroSound(RoleTypes.Crewmate),
           ctop: true

        );
    public ColdPotato(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.MarkOthers.Add(MarkOthers);
        LastTime = -1;
    }
    public static int BoomTime;
    public long LastTime;
    public bool IsKiller { get; private set; } = false;
    //public override bool CanUseAbilityButton() => true;
    public bool CanUseShapeShiftButton() => true;
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("HotBoom");
        return true;
    }
    public static bool KnowTargetRoleColor(PlayerControl target, bool isMeeting)
    => target.Is(CustomRoles.ColdPotato);
    public static string MarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return (seen.Is(CustomRoles.ColdPotato)) ? Utils.ColorString(RoleInfo.RoleColor, "") : "";
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
        => enabled |= true;
    public bool CanUseKillButton() => false;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return string.Format(GetString("HotPotatoTimeRemain"), HotPotatoManager.BoomTimes);
    }
}