using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using TONEX.Modules;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;
using UnityEngine.UIElements.UIR;
using static TONEX.Translator;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TONEX.Roles.Neutral;
public sealed class Jackal : RoleBase, INeutralKilling, IKiller, ISchrodingerCatOwner, IIndependent
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
            "jac|豺狼",
            "#00b4eb",
            true,
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
    private static OptionItem OptionJackalCanSaveSidekick;
    private static OptionItem OptionRecruitModeSwitchAction;
    public static OptionItem OptionSidekickCanKill;
    public static OptionItem OptionSidekickCanBeJackal;
    public static OptionItem OptionSidekickCanVent;
    public static OptionItem OptionSidekickKillCoolDown;
    enum OptionName
    {
        JackalCanWinBySabotageWhenNoImpAlive,
        ResetKillCooldownWhenPlayerGetKilled,
        CanBeSidekick,
        JackalSidekickLimit,
        CanSaveSidekick,
        RecruitModeSwitchAction,
        SidekickCanKill,
        SidekickCanBeJackal,
        SidekickCanVent,
        SidekickKillCoolDown,
    }
    public enum SwitchTrigger
    {
        TriggerKill,
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
        OptionJackalCanSaveSidekick = BooleanOptionItem.Create(RoleInfo, 17, OptionName.CanSaveSidekick, true, false , OptionItemCanSidekick);
        OptionSidekickCanKill = BooleanOptionItem.Create(RoleInfo, 18, OptionName.SidekickCanKill, false, false, OptionJackalCanSaveSidekick);
        OptionSidekickKillCoolDown = FloatOptionItem.Create(RoleInfo, 19, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false);
        OptionSidekickCanVent = BooleanOptionItem.Create(RoleInfo, 20, OptionName.SidekickCanVent, true, false, OptionJackalCanSaveSidekick);
        OptionSidekickCanBeJackal = BooleanOptionItem.Create(RoleInfo, 21, OptionName.SidekickCanBeJackal, true, false, OptionJackalCanSaveSidekick);
        OptionRecruitModeSwitchAction = StringOptionItem.Create(RoleInfo, 22, OptionName.RecruitModeSwitchAction, EnumHelper.GetAllNames<SwitchTrigger>(), 1, false, OptionItemCanSidekick);
    }
    public override void Add()
    {
        SidekickLimit = OptionSidekickLimit.GetInt();
        NowSwitchTrigger = (SwitchTrigger)OptionRecruitModeSwitchAction.GetValue();
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
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
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
    public void Recruit(PlayerControl target)
    {
        SidekickLimit--;
        SendRPC();
        Player.SetKillCooldownV2();
        NameColorManager.Add(Player.PlayerId, target.PlayerId, "#00b4eb");
        NameColorManager.Add(target.PlayerId, Player.PlayerId, "#00b4eb");
        if (!OptionJackalCanSaveSidekick.GetBool())
            target.RpcSetCustomRole(CustomRoles.Wolfmate);
        else
        {
            if (target.CanUseKillButton())
                 target.RpcSetCustomRole(CustomRoles.Sidekick);
             else
                 target.RpcSetCustomRole(CustomRoles.Whoops);
            
             /*Player.RpcMurderPlayerV2(target);
            target.Revive();
            
            target.Data.IsDead = false;
            target.RpcSetCustomRole(CustomRoles.Sidekick);
            target.RpcSetRole(RoleTypes.Impostor);
            target.SetRole(RoleTypes.Impostor);
            target.isKilling = true;
            target.Data.Role.TeamType = RoleTeamTypes.Impostor;
            target.Data.Role.DefaultGhostRole = RoleTypes.Impostor;
            target.spawn
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.SetRole, SendOption.None, -1);
            writer.Write(target.isKilling = true);
            writer.Write((ushort)RoleTypes.Impostor);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            var sender = new CustomRpcSender("SetSidekick", SendOption.None, true);
            sender.StartRpc(target.NetId, RpcCalls.SetRole)
                .Write((ushort)RoleTypes.Impostor)
                .EndRpc();
            target.Data.Role.Role = RoleTypes.Impostor;
            target.Data.RoleType = RoleTypes.Impostor;
            target.Data.RoleWhenAlive = new(RoleTypes.Impostor);
            target.Data.Role.CanUseKillButton = true;
            target.Data.Role.CanVent = true;
            target.Data.Role.CanBeKilled = true;
            AntiBlackout.SendGameData("SetSidekick");*/
        }
       

        Player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("GangsterSuccessfullyRecruited")));
        target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("BeRecruitedByJackal")));
        Utils.NotifyRoles();
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if ((SidekickLimit < 1 || (OptionItemCanSidekick.GetBool() && !CanBeSidekick(target)))&& target.GetCountTypes()!=CountTypes.Jackal) return true;
        if (NowSwitchTrigger != SwitchTrigger.TriggerDouble && SidekickLimit >= 1)
        {
            Recruit(target);
            return false;
        }
        else if (NowSwitchTrigger == SwitchTrigger.TriggerDouble && SidekickLimit >= 1)
        {
            killer.CheckDoubleTrigger(target, () => { Recruit(target); });
            return false;
        }
        return true;
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (seen.Is(CustomRoles.Wolfmate) || seen.Is(CustomRoles.Sidekick) || seen.Is(CustomRoles.Whoops) || seen.Is(CustomRoles.Jackal)) enabled = true;
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (seer.Is(CustomRoles.Jackal) || seer.Is(CustomRoles.Wolfmate) || seer.Is(CustomRoles.Sidekick) || seer.Is(CustomRoles.Wolfmate)) enabled = true;
    }
    public static bool CanBeSidekick(PlayerControl pc) => pc != null && OptionJackalCanSaveSidekick.GetBool()? !pc.Is(CountTypes.Jackal) : (!pc.GetCustomRole().IsNeutral()) && !pc.Is(CountTypes.Jackal);
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (seen.Is(CustomRoles.Sidekick)) return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), "🔻");
        else if (seen.Is(CustomRoles.Whoops)) return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), "🔻");
        else if (seen.Is(CustomRoles.Wolfmate)) return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), "🔻");
        else
        return "";
    }
}