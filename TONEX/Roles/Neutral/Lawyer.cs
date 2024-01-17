using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONEX.Roles.Neutral;
public sealed class Lawyer : RoleBase, IOverrideWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Lawyer),
            player => new Lawyer(player),
            CustomRoles.Lawyer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            50823,
            SetupOptionItem,
            "law",
            "#788514",
             true
        );
    public Lawyer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {

        Lawyers.Add(this);
        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);

        TargetExiled = false;
    }
    public static byte WinnerID;

    private static OptionItem OptionCanTargetCrewmate;
    private static OptionItem OptionCanTargetJeater;
    public static OptionItem OptionKnowTargetRole;
    public static OptionItem OptionTargetKnowsLawyer;
    public static OptionItem OptionSkillCooldown;
    public static OptionItem OptionSkillLimit;
    enum OptionName
    {
        ExecutionerCanTargetImpostor,
        ExecutionerCanTargetNeutralKiller,
        ExecutionerChangeRolesAfterTargetKilled,
        ProsecutorsSkillCooldown,
        ProsecutorsSkillLimit,
    }


    public static HashSet<Lawyer> Lawyers = new(15);
    public byte TargetId;
    private bool TargetExiled;

    private static void SetupOptionItem()
    {
        OptionCanTargetCrewmate = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ExecutionerCanTargetImpostor, false, false);
        OptionCanTargetJeater = BooleanOptionItem.Create(RoleInfo, 12, OptionName.ExecutionerCanTargetNeutralKiller, false, false);
        OptionKnowTargetRole = BooleanOptionItem.Create(RoleInfo, 11, OptionName.ExecutionerChangeRolesAfterTargetKilled,false, false);
        OptionTargetKnowsLawyer = BooleanOptionItem.Create(RoleInfo, 14, OptionName.ExecutionerCanTargetNeutralKiller, false, false);
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.ProsecutorsSkillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillLimit = IntegerOptionItem.Create(RoleInfo, 10, OptionName.ProsecutorsSkillLimit, new(1, 999, 1), 3, false);
    }
    public bool CanUseKillButton() => false;
        public bool IsKiller { get; private set; } = false;
    public override void Add()
    {
        //ターゲット割り当て
        if (!AmongUsClient.Instance.AmHost) return;

        var playerId = Player.PlayerId;
        List<PlayerControl> targetList = new();
        var rand = IRandom.Instance;
        foreach (var target in Main.AllPlayerControls)
        {
            if (playerId == target.PlayerId) continue;
            else if (!OptionCanTargetCrewmate.GetBool() && target.GetCustomRole().IsCrewmate()) continue;
            else if (!OptionCanTargetJeater.GetBool() && target.Is(CustomRoles.Jester)) continue;
            if (target.Is(CustomRoles.GM)) continue;

            targetList.Add(target);
        }
        var SelectedTarget = targetList[rand.Next(targetList.Count)];
        TargetId = SelectedTarget.PlayerId;
        NameColorManager.Add(Player.PlayerId, SelectedTarget.PlayerId, "#788514");
        SendRPC();
    }
    public override void OnDestroy()
    {
        Lawyers.Remove(this);

        if (Lawyers.Count <= 0)
        {
            CustomRoleManager.OnMurderPlayerOthers.Remove(OnMurderPlayerOthers);
        }
    }
    public void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        using var sender = CreateSender(CustomRPC.SetExecutionerTarget);
        sender.Writer.Write(TargetId);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        byte targetId = reader.ReadByte();
        TargetId = targetId;
    }
    public void CheckWin(ref CustomWinner WinnerTeam, ref HashSet<byte> WinnerIds)
    {
        if (Player.IsAlive() && WinnerIds.Contains(TargetId))
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lawyer);
            CustomWinnerHolder.WinnerIds.Add(TargetId);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
        }
    }
    public override void OnMurderPlayerAsTarget(MurderInfo _)
    {
        TargetId = byte.MaxValue;
        SendRPC();
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (OptionKnowTargetRole.GetBool() && seen.PlayerId == TargetId) enabled = true;
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (seer.Is(CustomRoles.Lawyer) && OptionTargetKnowsLawyer.GetBool()) enabled = true;
    }
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        var target = info.AttemptTarget;

        foreach (var executioner in Lawyers.ToArray())
        {
            if (executioner.TargetId == target.PlayerId)
            {
                executioner.ChangeRole();
                break;
            }
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        return TargetId == seen.PlayerId ? Utils.ColorString(RoleInfo.RoleColor, "§") : "";
    }
    public bool CheckWin(ref CustomRoles winnerRole , ref CountTypes winnerCountType)
    {
        return TargetExiled && CustomWinnerHolder.WinnerTeam != CustomWinner.Default;
    }
    public void ChangeRole()
    {
        Player.RpcSetCustomRole(CustomRoles.Prosecutors);
        Utils.NotifyRoles();
    }

    public static void ChangeRoleByTarget(byte targetId)
    {
        foreach (var executioner in Lawyers)
        {
            if (executioner.TargetId != targetId) continue;

            executioner.ChangeRole();
            break;
        }
    }
}