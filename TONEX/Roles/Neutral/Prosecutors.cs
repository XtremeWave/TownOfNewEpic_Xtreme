using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Neutral;
public sealed class Prosecutors : RoleBase, INeutralKiller,IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Prosecutors),
            player => new Prosecutors(player),
            CustomRoles.Prosecutors,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            94_1_0_0300,
            null,
            "pro",
            "#788514",
            true,
            ctop: true
        );
    public Prosecutors(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_After);
        ForProsecutors = new();
    }
    public bool IsKiller { get; private set; } = false;
    public bool IsNE { get; private set; } = false;
    private int ProsecutorsLimit;
    public static List<byte> ForProsecutors;
    public override void Add()
    {
        ProsecutorsLimit = Lawyer.OptionSkillLimit.GetInt();
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ProsecutorsLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        
        ProsecutorsLimit = reader.ReadInt32();
    }
    public bool CheckWin(ref CustomRoles winnerRole , ref CountTypes winnerCountType)
    {
        return Player.IsAlive();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? Lawyer.OptionSkillCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && ProsecutorsLimit >= 1;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (ProsecutorsLimit >= 1)
        {
            ProsecutorsLimit -= 1;
            SendRPC();
            ForProsecutors.Add(target.PlayerId);
        }
        info.CanKill = false;
        killer.RpcProtectedMurderPlayer(target);
        killer.SetKillCooldownV2();
        return false;
    }
    public static bool OnCheckMurderPlayerOthers_After(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide) return true;
        if (ForProsecutors.Contains(killer.PlayerId)) return false;
        return true;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? RoleInfo.RoleColor : Color.gray, $"({ProsecutorsLimit})");
    public override void OnStartMeeting() => ForProsecutors.Clear();
}
