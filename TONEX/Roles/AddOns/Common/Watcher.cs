using System.Collections.Generic;
using TONEX.Attributes;
using TONEX.Roles.Core;
using AmongUs.GameOptions;

namespace TONEX.Roles.AddOns.Common;
public sealed class Watcher : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    SimpleRoleInfo.Create(
    typeof(Watcher),
    player => new Watcher(player),
    CustomRoles.Watcher,
   80300,
    null,
    "wat|窺視者|窥视",
    "#800080"
    );
    public Watcher(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetBool(BoolOptionNames.AnonymousVotes, false);
}
