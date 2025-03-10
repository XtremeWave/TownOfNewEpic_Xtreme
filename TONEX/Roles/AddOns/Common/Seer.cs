using System.Collections.Generic;
using TONEX.Attributes;
using TONEX.Roles.Core;
using UnityEngine;
using System.Drawing;

namespace TONEX.Roles.AddOns.Common;
public sealed class Seer : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    SimpleRoleInfo.Create(
    typeof(Seer),
    player => new Seer(player),
    CustomRoles.Seer,
   80900,
    null,
    "se|�`ý",
    "#61b26c"
    );

    public Seer(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
    {
    }

}