using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;
using UnityEngine.XR;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Neutral;
public sealed class RewardOfficer : RoleBase, IKiller, IIndependent
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(RewardOfficer),
            player => new RewardOfficer(player),
            CustomRoles.RewardOfficer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            1656870,
            SetupOptionItem,
            "re|悬赏",
            "#339966",
           true
        );
    public RewardOfficer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ForRewardOfficer = new();
    }

    private static OptionItem OptionKillCooldown;
    static OptionItem RewardOfficerCanSeeRoles;
    enum OptionName
    {
        CanSeeRole,
    }
    public static List<byte> ForRewardOfficer;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        RewardOfficerCanSeeRoles = BooleanOptionItem.Create(RoleInfo, 17, OptionName.CanSeeRole, true, false);
    }
    public string Name = "";
    public Color RolesColor;
    public override void Add()
    {
        ForRewardOfficer = new();
        //旧base的随机一个玩家未目标
        if (!AmongUsClient.Instance.AmHost) return;
        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != Player.PlayerId && x.IsAlive()).ToList();
        var SelectedTarget = pcList[IRandom.Instance.Next(0, pcList.Count)];
        if (RewardOfficerCanSeeRoles.GetBool())
        {
        ForRewardOfficer.Add( SelectedTarget.PlayerId);
            Name=SelectedTarget.GetAllRoleName();
            RolesColor = Utils.GetRoleColor(SelectedTarget.GetCustomRole());
        }
        else
        {
            ForRewardOfficer.Add(SelectedTarget.PlayerId);
        }
        SendRPC();
        SendRPC_SyncList();
        Utils.NotifyRoles(Player);
    }

    private static void SendRPC_SyncList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRewardOfficerTarget, SendOption.Reliable, -1);
        writer.Write(ForRewardOfficer.Count);
        for (int i = 0; i < ForRewardOfficer.Count; i++)
            writer.Write(ForRewardOfficer[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ForRewardOfficer = new();
        for (int i = 0; i < count; i++)
            ForRewardOfficer.Add(reader.ReadByte());
    }
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetRewardOfficerName);
        sender.Writer.Write(Name);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetRewardOfficerName) return;
        Name = reader.ReadString();
    }
    public override string GetProgressText(bool comms = false)
    {
        if (RewardOfficerCanSeeRoles.GetBool())
            return Utils.ColorString(RolesColor, $"({Name})");
        else
            return "";
    }
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseKillButton() => true;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (ForRewardOfficer.Contains(target.PlayerId)) Win();
        else
        {
          killer.RpcMurderPlayerV2(killer);
            return false;
        }
        return true;
    }
    public void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.RewardOfficer);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting)
    {
        var pc = player;
        if (ForRewardOfficer.Contains(pc.PlayerId))
        {
            ForRewardOfficer.Remove(pc.PlayerId);
            var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != Player.PlayerId && x.IsAlive()).ToList();
            var SelectedTarget = pcList[IRandom.Instance.Next(0, pcList.Count)];
            if (RewardOfficerCanSeeRoles.GetBool())
            {
                ForRewardOfficer.Add(SelectedTarget.PlayerId);
                Name = SelectedTarget.GetAllRoleName();
                RolesColor = Utils.GetRoleColor(SelectedTarget.GetCustomRole());
            }
            else    ForRewardOfficer.Add(SelectedTarget.PlayerId);
            Player.Notify("TargetIsDead");
            SendRPC();
            SendRPC_SyncList();
            Utils.NotifyRoles(Player);
        }
    }
}
