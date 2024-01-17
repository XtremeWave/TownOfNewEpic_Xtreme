using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using TONEX.Modules;
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
    private static OptionItem OptionSidekickLimit;
    private static OptionItem OptionItemCanSidekick;
    private static OptionItem JackalCanSaveSidekick;
    private static OptionItem RecruitModeSwitchAction;
    enum OptionName
    {
        JackalCanWinBySabotageWhenNoImpAlive,
        ResetKillCooldownWhenPlayerGetKilled,
        CanBeSidekick,
        JackalSidekickLimit,
        CanSaveSidekick,
        RecruitModeSwitchAction,
    }
    public enum SwitchTrigger
    {
        Kill,
        TriggerDouble,
    };
    private static float KillCooldown;
    public static bool CanVent;
    public static bool CanUseSabotage;
    public static bool WinBySabotage;
    private static bool HasImpostorVision;
    private int SidekickLimit;
    public SwitchTrigger NowSwitchTrigger;

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
        RecruitModeSwitchAction = StringOptionItem.Create(RoleInfo, 10, OptionName.RecruitModeSwitchAction, EnumHelper.GetAllNames<SwitchTrigger>(), 2, false);
    }
    public override void Add()
    {
        SidekickLimit = OptionSidekickLimit.GetInt();
        NowSwitchTrigger = (SwitchTrigger)RecruitModeSwitchAction.GetValue();
        Player.AddDoubleTrigger();
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
    public override bool GetAdminButtonSprite(out string buttonName)
    {
        buttonName = "Sidekick";
        return SidekickLimit >= 1;
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
    public void Recruit(PlayerControl target)
    {
        SidekickLimit--;
        SendRPC();
        Player.SetKillCooldownV2();
        NameColorManager.Add(Player.PlayerId, target.PlayerId, "#00b4eb");
        NameColorManager.Add(target.PlayerId, Player.PlayerId, "#00b4eb");
        if (JackalCanSaveSidekick.GetBool())   
            target.RpcSetCustomRole(CustomRoles.Attendant);
        else
        {
            if (target.CanUseKillButton())
                target.RpcSetCustomRole(CustomRoles.Sidekick);
            else
                target.RpcSetCustomRole(CustomRoles.Whoops);
        }

        Player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("GangsterSuccessfullyRecruited")));
        target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("BeRecruitedByJackal")));
        Utils.NotifyRoles();
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (SidekickLimit < 1 || !CanBeSidekick(target) || !OptionItemCanSidekick.GetBool()) return true;
        if (SidekickLimit < 1 || !CanBeSidekick(target) || NowSwitchTrigger == SwitchTrigger.TriggerDouble && !killer.CheckDoubleTrigger(target, () => { Recruit(target); })) return true;
        if (NowSwitchTrigger != SwitchTrigger.TriggerDouble)
            Recruit(target);
        return false;
    }
    public static bool CanBeSidekick(PlayerControl pc) => pc != null && (!pc.GetCustomRole().IsNeutral()) && !pc.Is(CountTypes.Jackal);
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seen¤¬Ê¡ÂÔ¤ÎˆöºÏseer
        seen ??= seer;
        if (seen.Is(CustomRoles.Sidekick)) return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), "$$");
        else if (seen.Is(CustomRoles.Whoops)) return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), "$");
        else if (seen.Is(CustomRoles.Attendant)) return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), "??");
        else
        return "";
    }
}