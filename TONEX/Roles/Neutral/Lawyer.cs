using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Neutral;
public sealed class Lawyer : RoleBase, IAdditionalWinner,IKiller
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
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public static byte WinnerID;

    private static OptionItem OptionCanTargetCrewmate;
    private static OptionItem OptionCanTargetJester;
    private static OptionItem OptionCanTargetNeutralKiller;
    public static OptionItem OptionKnowTargetRole;
    public static OptionItem OptionTargetKnowsLawyer;
    public static OptionItem OptionSkillCooldown;
    public static OptionItem OptionSkillLimit;


    public bool IsKiller { get; private set; } = false;
    public bool CanUseKillButton() => false;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    enum OptionName
    {
        CanTargetCrewmate,
        CanTargetJester,
        CanTargetNeutralKiller,
        OptionKnowTargetRole,
        OptionTargetKnowsLawyer,
        ProsecutorsSkillCooldown,
        ProsecutorsSkillLimit,
    }


    public static HashSet<Lawyer> Lawyers = new(15);
    public byte TargetId;

    private static void SetupOptionItem()
    {
        OptionCanTargetCrewmate = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanTargetCrewmate, false, false);
        OptionCanTargetJester = BooleanOptionItem.Create(RoleInfo, 12, OptionName.CanTargetJester, false, true);
        OptionCanTargetNeutralKiller = BooleanOptionItem.Create(RoleInfo, 13, OptionName.CanTargetNeutralKiller, false, true);
        OptionKnowTargetRole = BooleanOptionItem.Create(RoleInfo, 11, OptionName.OptionKnowTargetRole, false, false);
        OptionTargetKnowsLawyer = BooleanOptionItem.Create(RoleInfo, 14, OptionName.OptionTargetKnowsLawyer, false, false);
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 15, OptionName.ProsecutorsSkillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillLimit = IntegerOptionItem.Create(RoleInfo, 16, OptionName.ProsecutorsSkillLimit, new(1, 999, 1), 3, false);
    }
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
            else if (!OptionCanTargetJester.GetBool() && target.Is(CustomRoles.Jester)) continue;
            else if (!OptionCanTargetNeutralKiller.GetBool() && target.IsNeutralKiller()) continue;
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
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (OptionKnowTargetRole.GetBool() && seen.PlayerId == TargetId) enabled = true;
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
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        var can = false;
        foreach (var executioner in Lawyers.ToArray())
        {
            if (executioner.TargetId == seer.PlayerId && seer == seen)
            {
                can = true;
            }
        }
        return  OptionTargetKnowsLawyer.GetBool() && can? Utils.ColorString(RoleInfo.RoleColor, "§") : "";
    }
    public bool CheckWin(ref CustomRoles winnerRole , ref CountTypes winnerCountType)
    {
        if (!Player.IsAlive())
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.PlayerId == TargetId && pc.GetCountTypes() == winnerCountType)
                    return true;
            }
        else
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.PlayerId == TargetId && pc.GetCountTypes() == winnerCountType)
                { CustomWinnerHolder.WinnerIds.Remove(TargetId); return true; }

            }
        return false;
    }
    public void ChangeRole()
    {
        Player.RpcSetCustomRole(CustomRoles.Prosecutors);
        Utils.NotifyRoles();
    }
}