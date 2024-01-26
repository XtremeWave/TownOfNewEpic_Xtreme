using AmongUs.GameOptions;
using UnityEngine;
using TONEX.Modules;
using TONEX.Roles.Core;
using static TONEX.Translator;
using Hazel;
using static UnityEngine.GraphicsBuffer;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Crewmate;
public sealed class Unyielding : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Unyielding),
            player => new Unyielding(player),
            CustomRoles.Unyielding,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            21548,
            SetupOptionItem,
            "un|不屈者",
            "#666633",
            true
        );
    public Unyielding(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanKill = false;
    }

    static OptionItem OptionDieTime;
    static OptionItem OptionKillCooldown;
    public bool CanKill;
    enum OptionName
    {
        DieTime
    }
    private static void SetupOptionItem()
    {
        OptionDieTime = FloatOptionItem.Create(RoleInfo, 11, OptionName.DieTime, new(12f, 180f, 2.5f), 12f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        CanKill = false;
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? OptionKillCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && CanKill;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        if (Player.IsAlive() && Player.PlayerId != target.PlayerId)
        {
            killer.RpcTeleport(target.GetTruePosition());
            target.RpcProtectedMurderPlayer(target);
            CanKill = true;
            Player.SetKillCooldownV2();
            Player.Notify(string.Format(GetString("KillUnyielding")));
            new LateTask(() =>
            {
                Player.RpcMurderPlayerV2(Player);
                target.SetRealKiller(killer);
            }, OptionDieTime.GetFloat(), "DieTime");
            return false;
        }
        return true;
    }
}
