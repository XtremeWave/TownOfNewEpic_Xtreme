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
using System.Text;
using Il2CppInterop.Generator.Extensions;
using static UnityEngine.GraphicsBuffer;
using TONEX.Roles.Crewmate;
using System.Diagnostics.Contracts;

namespace TONEX.Roles.Neutral;
public sealed class King : RoleBase, INeutralKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(King),
            player => new King(player),
            CustomRoles.King,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            94_1_4_1100,
            SetupOptionItem,
            "Ki|王子",
            "#FFFF00",
           true
        );
    public King(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
       
    }

    private static OptionItem OptionKillCooldown;
    public static OptionItem OptionLimit;
    public bool IsNK { get; private set; } = false;

    public static List<byte> ForKing;
    public static int Limit;
    enum OptionName {
        NeedPlayers
    }
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionLimit = IntegerOptionItem.Create(RoleInfo, 11,OptionName.NeedPlayers, new(1, 14, 1), 15, false)
       .SetValueFormat(OptionFormat.Players);
    }

    public override void Add()
    {
        ForKing = new();
        Limit = OptionLimit.GetInt();
    }
    private static void SendRPC_SyncList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKingList, SendOption.Reliable, -1);
        writer.Write(ForKing.Count);
        for (int i = 0; i < ForKing.Count; i++)
            writer.Write(ForKing[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ForKing = new();
        for (int i = 0; i < count; i++)
            ForKing.Add(reader.ReadByte());
    }
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Limit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        Limit = reader.ReadInt32();
    }
    public override string GetProgressText(bool comms = false)
    {
        if(Limit >=1)  return Utils.ColorString(Utils.GetRoleColor(CustomRoles.King), $"({Limit})");
        else return Utils.ColorString(Utils.GetRoleColor(CustomRoles.King), GetString("ReadyToWin"));
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("KingButtonText");
        return true;
    }
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => true;
    public bool CanUseKillButton() => true;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Limit <= 0) {
            Win();
            return true;
        }
        if (!ForKing.Contains(target.PlayerId))
        {
            killer.ResetKillCooldown();
            killer.SetKillCooldownV2(target: Player, forceAnime: true);
            ForKing.Add(target.PlayerId);
            Limit--;
            SendRPC_SyncList();
            SendRPC();
            NameColorManager.Add(killer.PlayerId, target.PlayerId, "#FFFF00");
            Utils.NotifyRoles(Player);
        }
        else return false;

        return false;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if(Limit <= 0) 
            WinV2();
        return true;
    }
    public void WinV2()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.King);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
        foreach(var pc in ForKing) 
            CustomWinnerHolder.WinnerIds.Add(pc);
    }
    public void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.King);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting)
    {
        var pc = player;
        if (ForKing.Contains(pc.PlayerId))
        {
            ForKing.Remove(pc.PlayerId);
            Limit++;
            SendRPC();
            SendRPC_SyncList();
            Utils.NotifyRoles(Player);
        }
    }
}
