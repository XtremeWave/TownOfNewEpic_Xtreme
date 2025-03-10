using TONEX.Roles.Core;

namespace TONEX.Roles.AddOns.Common;
public sealed class Oblivious : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    SimpleRoleInfo.Create(
    typeof(Oblivious),
    player => new Oblivious(player),
    CustomRoles.Oblivious,
     81100,
    null,
    "pb|đС��|��С",
    "#424242"
    );
    public Oblivious(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (!Is(reporter) || target == null) return true;
        return Utils.GetPlayerById(target.PlayerId).Is(CustomRoles.Bait) || Utils.GetPlayerById(reporter.PlayerId).Is(CustomRoles.Mayor);
    }



}