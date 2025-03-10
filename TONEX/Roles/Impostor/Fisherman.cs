using AmongUs.GameOptions;
using TONEX.Roles.Core;
using static TONEX.Translator;
using System.Collections.Generic;
using Hazel;
using UnityEngine;
using System.Linq;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Impostor;
public sealed class Fisherman : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Fisherman),
            player => new Fisherman(player),
            CustomRoles.Fisherman,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            94_1_4_0800,
            SetupOptionItem,
            "Fm|钓鱼的人"
        );
    public Fisherman(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {

    }

    static OptionItem OptionShapeshiftCooldown;

    private static void SetupOptionItem()
    {
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = OptionShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("FishermanButtonText");
        return true;
    }
    public override bool OnCheckShapeshift(PlayerControl target, ref bool animate)
    {

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!target.IsAlive()) return false;
        animate = false;
        target.RpcTeleport(Player.GetTruePosition());
        Player.RpcResetAbilityCooldown();
        return false;
    }

}