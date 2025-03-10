using Sentry.Internal.Http;
using System.Collections.Generic;
using System.Linq;
using TONEX.Roles.Core;

namespace TONEX.Roles.AddOns.Common;
public sealed class Luckless : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    SimpleRoleInfo.Create(
    typeof(Luckless),
    player => new Luckless(player),
    CustomRoles.Luckless,
    94_1_4_1400,
    SetupOptionItem,
    "Lu",
    "#ffab1b"
    );
    public Luckless(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

   public  static OptionItem OptionProbability;
    enum OptionName
    {
        LucklessProbability
    }

    static void SetupOptionItem()
    {
        OptionProbability = IntegerOptionItem.Create(RoleInfo, 20, OptionName.LucklessProbability, new(0, 100, 5), 50, false)
            .SetValueFormat(OptionFormat.Percent);
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (IRandom.Instance.Next(0, 100) < OptionProbability.GetInt()) {
           Player.RpcMurderPlayerV2(Player);
            return false;
        }
            return true;
    }
    public override bool OnCheckShapeshift(PlayerControl target, ref bool animate)
    {
        if (IRandom.Instance.Next(0, 100) < OptionProbability.GetInt())
        {
            Player.RpcMurderPlayerV2(Player);
            return false;
        }
        return true;
    }
    public override bool OnInvokeSabotage(SystemTypes systemType)
    {
        if (IRandom.Instance.Next(0, 100) < OptionProbability.GetInt())
        {
            Player.RpcMurderPlayerV2(Player);
            return false;
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (IRandom.Instance.Next(0, 100) < OptionProbability.GetInt())
            Player.RpcMurderPlayerV2(Player);
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (!Is(reporter) || target == null) return true;
        if (IRandom.Instance.Next(0, 100) < OptionProbability.GetInt())
        {
            Player.RpcMurderPlayerV2(Player);
            return false;
        }
        return true;
    }
    public override void OnStartMeeting()
    {
        if (IRandom.Instance.Next(0, 100) < OptionProbability.GetInt())
            Player.RpcMurderPlayerV2(Player);
    }
}
