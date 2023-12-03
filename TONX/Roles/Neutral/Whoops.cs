using AmongUs.GameOptions;
using Hazel;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;
using static TONEX.Translator;

namespace TONEX.Roles.Neutral;
public sealed class Whoops : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Whoops),
            player => new Whoops(player),
            CustomRoles.Whoops,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            50900,
            null,
            "jac|豺狼",
            "#00b4eb",
            true,
            countType: CountTypes.Jackal,
            assignCountRule: new(1, 1, 1)
        );
    public Whoops(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.True
    )
    { }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
}
