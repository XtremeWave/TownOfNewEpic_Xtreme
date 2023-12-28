using AmongUs.GameOptions;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using System.Collections.Generic;
using static TONEX.Translator;
using Hazel;
using UnityEngine;

namespace TONEX.Roles.Impostor;
public sealed class DoubleKiller : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(DoubleKiller),
            player => new DoubleKiller(player),
            CustomRoles.DoubleKiller,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            1238687,
            SetupOptionItem,
            "du|双杀|双刀"
        );
    public DoubleKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        DoubleKillerReady = new();
    }

    static OptionItem DoubleKillerDefaultKillCooldown;
    static OptionItem TwoKillCooldown;
    public List<byte> DoubleKillerReady;
    public int DoubleKillerTwoTime;
    enum OptionName
    {
        DoubleKillerDefaultKillCooldown,
        DoubleKillerTwoKillCooldown,
    }
    private float KillCooldown;
    private static void SetupOptionItem()
    {
        DoubleKillerDefaultKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.DoubleKillerDefaultKillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        TwoKillCooldown = IntegerOptionItem.Create(RoleInfo, 11, OptionName.DoubleKillerTwoKillCooldown, new(0, 180, 1), 30, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        KillCooldown = DoubleKillerDefaultKillCooldown.GetFloat();
        DoubleKillerTwoTime = TwoKillCooldown.GetInt() + 8;
        DoubleKillerReady = new();
    }
    public float CalculateKillCooldown() => KillCooldown;
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.DoubleKillerBeKillTime);
        sender.Writer.Write(DoubleKillerTwoTime);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.DoubleKillerBeKillTime) return;
        DoubleKillerTwoTime = reader.ReadInt32();
    }
    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var killer = info.AttemptKiller;
        if (DoubleKillerReady.Contains(killer.PlayerId))
        {
            DoubleKillerReady.Remove(killer.PlayerId);
            KillCooldown = 0f;
            killer.ResetKillCooldown();
            killer.SyncSettings();
            DoubleKillerTwoTime = TwoKillCooldown.GetInt();
            SendRPC();
        }
        else
        {
            KillCooldown = DoubleKillerDefaultKillCooldown.GetFloat();
            killer.ResetKillCooldown();
            killer.SyncSettings();
        }
    }
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (DoubleKillerTwoTime == 0) return;
        if (DoubleKillerTwoTime >= 1 && Player.IsAlive() && !GameStates.IsMeeting)
        {
            DoubleKillerTwoTime -= 1;
            SendRPC();
        }
        if (DoubleKillerTwoTime <= 0 && Player.IsAlive() && !DoubleKillerReady.Contains(Player.PlayerId))
        {
            Player.Notify(GetString("DoubleKillerReady"));
            DoubleKillerReady.Add(Player.PlayerId);
        }
    }
    public override string GetProgressText(bool comms = false)
    {
        if (DoubleKillerTwoTime >= 1)
            return Utils.ColorString(Color.red, $"({DoubleKillerTwoTime})");
        else
            return Utils.ColorString(Color.yellow, GetString("(DoubleKillerTimeReady)")); 
    }
    public override void AfterMeetingTasks() => DoubleKillerTwoTime = TwoKillCooldown.GetInt();
}
