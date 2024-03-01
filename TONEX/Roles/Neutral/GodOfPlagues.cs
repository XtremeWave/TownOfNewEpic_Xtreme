using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;

namespace TONEX.Roles.Neutral;
public sealed class GodOfPlagues: RoleBase, IKiller, IIndependent, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GodOfPlagues),
            player => new GodOfPlagues(player),
            CustomRoles.GodOfPlagues,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            94_1_1_0500,
             null,
            "go|瘟神",
            "#4f4f4f",
            true,
            true,
            countType: CountTypes.GodOfPlagues,
             ctop: true
        );
    public GodOfPlagues(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
    }
    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.GodOfPlagues;
    public bool CanUseKillButton() => true;
    public float CalculateKillCooldown()=> Plaguebearer.OptionGodOfPlaguesKillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool CanUseSabotageButton() => false;
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return true;
            var (killer, target) = info.AttemptTuple;
            target.RpcMurderPlayerV2(killer);
            return false;
    }
}
