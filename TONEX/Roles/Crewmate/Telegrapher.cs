using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using Hazel;
using TONEX.Roles.Core;
using static TONEX.Translator;
using System.Linq;
using static Il2CppSystem.Diagnostics.Tracing.TraceLoggingMetadataCollector;

namespace TONEX.Roles.Crewmate;
public sealed class Telegrapher : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Telegrapher),
            player => new Telegrapher(player),
            CustomRoles.Telegrapher,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            94_1_4_1500,
            null,
            "te",
            "#339900"
        );
    public Telegrapher(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != Player.PlayerId).ToList();
        var SelectedTarget = pcList[IRandom.Instance.Next(0, pcList.Count)];
        msgToSend.Add((string.Format(GetString("TelegrapherResult"), SelectedTarget.GetRealName(), SelectedTarget.GetAllRoleName().Length), Player.PlayerId, null));
    }
}