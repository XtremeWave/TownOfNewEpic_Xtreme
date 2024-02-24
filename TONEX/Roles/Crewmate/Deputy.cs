using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Crewmate;
public sealed class Deputy : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Deputy),
            player => new Deputy(player),
            CustomRoles.Deputy,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            94_1_0_0300,
            null,
            "pro",
            "#788514",
            true,
            ctop: true
        );
    public Deputy(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_After);
        ForDeputy = new();
    }
    public bool IsKiller { get; private set; } = false;
    private int DeputyLimit;
    public static List<byte> ForDeputy;
    public override void Add()
    {
        
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetDeputyLimit);
        sender.Writer.Write(DeputyLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetDeputyLimit) return;
        DeputyLimit = reader.ReadInt32();
    }
    //public float CalculateKillCooldown() => CanUseKillButton() ? Lawyer.OptionSkillCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && DeputyLimit >= 1;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (DeputyLimit >= 1)
        {
            DeputyLimit -= 1;
            SendRPC();
            ForDeputy.Add(target.PlayerId);
        }
        info.CanKill = false;
        return false;
    }
    public static bool OnCheckMurderPlayerOthers_After(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide) return true;
        if (ForDeputy.Contains(killer.PlayerId)) return false;
        return true;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? RoleInfo.RoleColor : Color.gray, $"({DeputyLimit})");
    public override void OnStartMeeting() => ForDeputy.Clear();
}
