using AmongUs.GameOptions;
using Hazel;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;
using static TONEX.Translator;

namespace TONEX.Roles.Neutral;
public sealed class Jackal : RoleBase, IKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Jackal),
            player => new Jackal(player),
            CustomRoles.Jackal,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            50900,
            SetupOptionItem,
            "jac|²òÀÇ",
            "#00b4eb",
            true,
            countType: CountTypes.Jackal,
            assignCountRule: new(1, 1, 1)
        );
    public Jackal(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        CanVent = OptionCanVent.GetBool();
        CanUseSabotage = OptionCanUseSabotage.GetBool();
        WinBySabotage = OptionCanWinBySabotageWhenNoImpAlive.GetBool();
        HasImpostorVision = OptionHasImpostorVision.GetBool();

    }

    private static OptionItem OptionKillCooldown;
    public static OptionItem OptionCanVent;
    public static OptionItem OptionCanUseSabotage;
    public static OptionItem OptionCanWinBySabotageWhenNoImpAlive;
    private static OptionItem OptionHasImpostorVision;
    static OptionItem OptionSidekickLimit;
    static OptionItem OptionItemCanSidekick;
    static OptionItem JackalCanSaveSidekick;
    enum OptionName
    {
        JackalCanWinBySabotageWhenNoImpAlive,
        ResetKillCooldownWhenPlayerGetKilled,
        CanBeSidekick,
        JackalSidekickLimit,
        CanSaveSidekick,
    }
    private static float KillCooldown;
    public static bool CanVent;
    public static bool CanUseSabotage;
    public static bool WinBySabotage;
    private static bool HasImpostorVision;
    private int SidekickLimit;
    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Jackal;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
        OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
        OptionCanWinBySabotageWhenNoImpAlive = BooleanOptionItem.Create(RoleInfo, 14, OptionName.JackalCanWinBySabotageWhenNoImpAlive, true, false, OptionCanUseSabotage);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
        OptionItemCanSidekick = BooleanOptionItem.Create(RoleInfo, 15, OptionName.CanBeSidekick, true, false);
        OptionSidekickLimit = IntegerOptionItem.Create(RoleInfo, 16, OptionName.JackalSidekickLimit, new(1, 15, 1), 1, false, OptionItemCanSidekick)
            .SetValueFormat(OptionFormat.Times);
        JackalCanSaveSidekick = BooleanOptionItem.Create(RoleInfo, 17, OptionName.CanSaveSidekick, true, false , OptionItemCanSidekick);
    }
    public override void Add()
    {
        SidekickLimit = OptionSidekickLimit.GetInt();
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetJackalRewardLimit);
        sender.Writer.Write(SidekickLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetJackalRewardLimit) return;
        SidekickLimit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => SidekickLimit >= 1 ? OptionKillCooldown.GetFloat() : Options.DefaultKillCooldown;
    public override string GetProgressText(bool comms = false)
    {
        if (SidekickLimit >= 1 && OptionItemCanSidekick.GetBool())
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), $"({SidekickLimit})");
        else
        return "";
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("GangsterButtonText");
        return SidekickLimit  >= 1;
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = "Sidekick";
        return SidekickLimit >= 1;
    }
    public bool CanUseSabotageButton() => CanUseSabotage;
    public bool CanUseImpostorVentButton() => CanVent;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
    public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (SidekickLimit < 1) return true;
        var (killer, target) = info.AttemptTuple;

        if (CanBeSidekick(target))
        {
            SidekickLimit--;
            SendRPC();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(killer);
            target.RpcProtectedMurderPlayer(target);
            if (JackalCanSaveSidekick.GetBool())
            {
                target.RpcSetCustomRole(CustomRoles.Attendant);
            }
            else
            {
                if (target.CanUseKillButton())
                    target.RpcSetCustomRole(CustomRoles.Sidekick);
                else
                    target.RpcSetCustomRole(CustomRoles.Whoops);
            }

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("GangsterSuccessfullyRecruited")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("BeRecruitedByJackal")));
            Utils.NotifyRoles();

            return false;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("GangsterRecruitmentFailure")));
        return true;
    }
    public static bool CanBeSidekick(PlayerControl pc) => pc != null && (!pc.GetCustomRole().IsNeutral()) && !pc.Is(CountTypes.Jackal);
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (seen.Is(CustomRoles.Whoops) || seen.Is(CustomRoles.Sidekick) || seen.Is(CustomRoles.Attendant)) enabled = true;
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (seer.Is(CustomRoles.Whoops) || seer.Is(CustomRoles.Sidekick) || seer.Is(CustomRoles.Attendant)) enabled = true;
    }
}