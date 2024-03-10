using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using TONEX.Roles.Neutral;

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
            75_1_1_0400,
            null,
            "捕快",
            "#f8cd46",
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
        DeputyLimit = Sheriff.DeputySkillLimit.GetInt();
    }
    public bool IsKiller { get; private set; } = false;
    private int DeputyLimit;
    public static List<byte> ForDeputy;
    private void SendRPC(bool clear = false)
    {
        using var sender = CreateSender(CustomRPC.SetDeputyLimit);
        sender.Writer.Write(DeputyLimit);
        sender.Writer.Write(clear);
        if (!clear)
            foreach (var pc in ForDeputy)
                sender.Writer.Write(pc);

    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetDeputyLimit) return;
        DeputyLimit = reader.ReadInt32();
        var clear = reader.ReadBoolean();
        if (clear)
            ForDeputy.Clear();
        else
        for (int i = 0; i < ForDeputy.Count; i++)
        {
            var id = reader.ReadByte();
            if (!ForDeputy.Contains(id))
            {
                ForDeputy.Add(id);
            }
        }
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? Sheriff.DeputySkillCooldown.GetFloat() : 255f;
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

            ForDeputy.Add(target.PlayerId);
            SendRPC();
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
        if (ForDeputy.Contains(killer.PlayerId)) return false;
        return true;
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting)
    {
        var target = player;

        if (target.Is(CustomRoles.Sheriff) && Player.Is(CustomRoles.Deputy) && Sheriff.DeputyCanBecomeSheriff.GetBool())
        {
            Player.Notify(GetString("BeSheriff"));
            Player.RpcProtectedMurderPlayer();
            Player.RpcSetCustomRole(CustomRoles.Sheriff);
        }

    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!CustomRoles.Sheriff.IsExist() && Sheriff.DeputyCanBecomeSheriff.GetBool())
        {
            Player.Notify(GetString("BeSheriff"));
            Player.RpcProtectedMurderPlayer();
            Player.RpcSetCustomRole(CustomRoles.Sheriff);
        }
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? RoleInfo.RoleColor : Color.gray, $"({DeputyLimit})");
    public override void OnStartMeeting()
    {
        ForDeputy.Clear();
        SendRPC(true);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (seen.Is(CustomRoles.Sheriff) && Sheriff.DeputyKnowSheriff.GetBool()) return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Deputy), "△");
        else
            return "";
    }
    public bool OverrideKillButtonText(out string text)
    { 
        text = GetString("DeputyText");
        return true;
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = "Deputy";
        return true;
    }
}
