using System.Collections.Generic;
using TONEX.Attributes;
using TONEX.Roles.Core;
using UnityEngine;
using AmongUs.GameOptions;

namespace TONEX.Roles.AddOns.Common;
public sealed class Reach : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    SimpleRoleInfo.Create(
    typeof(Reach),
    player => new Reach(player),
    CustomRoles.Reach,
     81600,
    null,
    "re|持槍|手长|长臂猿|关了吧没意思|开reach|reach开的真长",
    "#74ba43"
    );
    public Reach(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override void ApplyGameOptions(IGameOptions opt) => opt.SetInt(Int32OptionNames.KillDistance, 2);
}
