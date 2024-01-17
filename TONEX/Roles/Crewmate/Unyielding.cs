using AmongUs.GameOptions;
using Hazel;
using TONEX.Roles.Core;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Crewmate;
public sealed class Unyielding : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Unyielding),
            player => new Unyielding(player),
            CustomRoles.Unyielding,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21548,
            SetupOptionItem,
            "ad|探险家",
            "#666633"
        );
    public Unyielding(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionDieTime;
    enum OptionName
    {
        DieTime
    }
    private static void SetupOptionItem()
    {
        OptionDieTime = FloatOptionItem.Create(RoleInfo, 11, OptionName.DieTime, new(12f, 180f, 2.5f), 12f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        if (Player.IsAlive())
        {
            Player?.NoCheckStartMeeting(Player?.Data);
            new LateTask(() =>
            {
                target.SetRealKiller(killer);
                target.RpcMurderPlayerV2(target);
            }, OptionDieTime.GetFloat(), "DieTime");
            return false;
        }
        return true;
    }
}
