using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TONEX.Roles.Core;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Crewmate;
public sealed class Prophet : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Prophet),
            player => new Prophet(player),
            CustomRoles.Prophet,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            1485796,
            SetupOptionItem,
            "pr",
            "#ffe185",
            true
        );
    public Prophet(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ForProphet = new();
    }

    static OptionItem ProphetCooldown;
    static OptionItem ProphetLimits;
    enum OptionName
    {
        ProphetCooldown,
        ProphetLimit,
    }
    private int ProphetLimit;
    private static List<byte> ForProphet;
    public bool IsKiller { get; private set; } = false;
    private static void SetupOptionItem()
    {
        ProphetCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.ProphetCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        ProphetLimits = IntegerOptionItem.Create(RoleInfo, 11, OptionName.ProphetLimit, new(1, 180, 1), 3, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        ProphetLimit = ProphetLimits.GetInt();
        ForProphet = new();
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.ProphetKill);
        sender.Writer.Write(ProphetLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.ProphetKill) return;
        ProphetLimit = reader.ReadInt32();
    }
    private static void SendRPC_SyncList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetProphetList, SendOption.Reliable, -1);
        writer.Write(ForProphet.Count);
        for (int i = 0; i < ForProphet.Count; i++)
            writer.Write(ForProphet[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ForProphet = new();
        for (int i = 0; i < count; i++)
            ForProphet.Add(reader.ReadByte());
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("ProphetButtonText");
        return true;
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? ProphetCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && ProphetLimit >= 1;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (ProphetLimit >= 1)
        {
            ProphetLimit -= 1;
            SendRPC();
            ForProphet.Add(target.PlayerId);
            SendRPC_SyncList();
            killer.SetKillCooldownV2(target: target);
            if (target.GetCustomRole().IsNeutral() || target.GetCustomRole().IsImpostor())
            {
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#ff1919");
                Player.Notify(string.Format(GetString("ProphetBad")));
            }
            else
            {
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#00ffff");
                Player.Notify(string.Format(GetString("ProphetNice")));
            }
        }
        info.CanKill = false;
        return false;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Utils.GetRoleColor(CustomRoles.Prophet) : Color.gray, $"({ProphetLimit})");
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if ((seen.GetCustomRole().IsNeutral() && ForProphet.Contains(seen.PlayerId)) || (seen.GetCustomRole().IsImpostor() && ForProphet.Contains(seen.PlayerId)))    
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("ProphetNotGood"));
        else if (seen.GetCustomRole().IsCrewmate() && ForProphet.Contains(seen.PlayerId))
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Snitch),GetString("ProphetGood"));
        else
            return "";
    }
}