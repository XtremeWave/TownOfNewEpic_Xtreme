using AmongUs.GameOptions;
using Hazel;
using Sentry.Internal.Http;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;

namespace TONEX.Roles.Crewmate;
public sealed class SpecterSlayer : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SpecterSlayer),
            player => new SpecterSlayer(player),
            CustomRoles.SpecterSlayer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            94_1_1_0700,
            null,
            "ss|恶魔猎手",
            "#9900ff",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public SpecterSlayer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    { }
    public int KillLimit;
    public override void Add()
    {
        KillLimit = 0;
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SpecterSlayerKill);
        sender.Writer.Write(KillLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SpecterSlayerKill) return;
        KillLimit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? 5f : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && KillLimit >= 1;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            if (KillLimit<=0) return false;
            KillLimit--;
            SendRPC();
            Player.ResetKillCooldown();
        }
        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (reporter == null || !Is(reporter) || target == null || reporter.PlayerId == target.PlayerId) return;
        KillLimit++;
        SendRPC();
        Player.ResetKillCooldown();
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Utils.GetRoleColor(CustomRoles.Vigilante) : Color.gray, $"({KillLimit})");
}