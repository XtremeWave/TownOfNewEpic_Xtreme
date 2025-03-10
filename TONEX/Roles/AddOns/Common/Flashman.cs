using System.Collections.Generic;
using TONEX.Attributes;
using TONEX.Roles.Core;

namespace TONEX.Roles.AddOns.Common;
public sealed class Flashman : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    SimpleRoleInfo.Create(
    typeof(Flashman),
    player => new Flashman(player),
    CustomRoles.Flashman,
    80500,
    SetupOptionItem,
    "fl|éWëŠ‚b|ÉÁµç|»ð³µÍ·",
    "#ff8400"
    );
    public Flashman(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }




    public static OptionItem OptionSpeed;
    enum OptionName
    {
        FlashmanSpeed
    }

    static void SetupOptionItem()
    {
        OptionSpeed = FloatOptionItem.Create(RoleInfo, 20, OptionName.FlashmanSpeed, new(0.25f, 5f, 0.25f), 2.5f, false)
.SetValueFormat(OptionFormat.Multiplier);
    }
}