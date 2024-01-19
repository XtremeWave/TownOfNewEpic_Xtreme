using AmongUs.GameOptions;

using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;

namespace TONEX.Roles.Impostor;
public sealed class Bard : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Bard),
            player => new Bard(player),
            CustomRoles.Bard,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            4900,
#if RELEASE
            null,
#else
            SetUpCustomOptions,
#endif
            "ba|吟游詩人|诗人"
        );

    public Bard(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    private static void SetUpCustomOptions() { }
    private float KillCooldown;
    public override void Add() => KillCooldown = Options.DefaultKillCooldown;
    public float CalculateKillCooldown() => KillCooldown;
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (exiled != null) KillCooldown /= 2;
    }
#if DEBUG
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        new LateTask(() =>
        {
            target.Data.IsDead = false;
            target.Data.Role.Role = RoleTypes.Impostor;
            target.Data.RoleType = RoleTypes.Impostor;
            AntiBlackout.SendGameData();
        }, 5f);
        return true;
    }
#endif
}