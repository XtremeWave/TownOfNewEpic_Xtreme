using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Neutral;
public sealed class RewardOfficer : RoleBase, IKiller
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
            "#339966"
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
    private static float KillCooldown;
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
        //ターゲット割り当て
        if (!AmongUsClient.Instance.AmHost) return;

        var playerId = Player.PlayerId;
        List<PlayerControl> targetList = new();
        var rand = IRandom.Instance;
        foreach (var target in Main.AllPlayerControls)
        {
            if (playerId == target.PlayerId) continue;

            if (target.Is(CustomRoles.GM)) continue;

            targetList.Add(target);
        }
        var SelectedTarget = targetList[rand.Next(targetList.Count)];
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
        SendRPC_SyncList();
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
    public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
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
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Despair);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            if (Player.IsAlive())
            {
                foreach (var re in ForRewardOfficer)
                {
                    var pc = Utils.GetPlayerById(re);
                    if (!pc.IsAlive() && ForRewardOfficer.Count == 1)
                    {
                        ForRewardOfficer.Remove(pc.PlayerId);

                        var playerId = Player.PlayerId;
                        List<PlayerControl> targetList = new();
                        var rand = IRandom.Instance;
                        foreach (var target in Main.AllPlayerControls)
                        {
                            if (playerId == target.PlayerId) continue;

                            if (target.Is(CustomRoles.GM)) continue;

                            targetList.Add(target);
                        }
                        var SelectedTarget = targetList[rand.Next(targetList.Count)];
                        if (RewardOfficerCanSeeRoles.GetBool())
                        {
                            ForRewardOfficer.Add(SelectedTarget.PlayerId);
                            Name = SelectedTarget.GetAllRoleName();
                            RolesColor = Utils.GetRoleColor(SelectedTarget.GetCustomRole());
                        }
                        else
                        {
                            ForRewardOfficer.Add(SelectedTarget.PlayerId);
                        }
                        SendRPC_SyncList();
                        break;
                    }
                }
            }
        }
    }
}
