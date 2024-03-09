using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TONEX.Roles.Core;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using System.Linq;
using UnityEngine.Video;
using TONEX.Roles.Neutral;
using UnityEngine.UIElements.UIR;

namespace TONEX.Roles.Crewmate;
public sealed class Hunter : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Hunter),
            player => new Hunter(player),
            CustomRoles.Hunter,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            94_1_1_0800,
            SetupOptionItem,
            "hu",
            "#FFEC80",
            true
        );
    public Hunter(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        HunterLimit = HunterLimits.GetInt();
        ForHunter = new();
    }

    static OptionItem HunterCooldown;
    static OptionItem HunterLimits;
    static OptionItem AfterMeetClearTarget;
    static OptionItem TargetMax;
    enum OptionName
    {
        AfterMeetClearTarget,
        TargetMaxCount
    }
    private int HunterLimit;
    private static List<byte> ForHunter;
    public bool IsKiller { get; private set; } = false;
    private static void SetupOptionItem()
    {
        HunterCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        HunterLimits = IntegerOptionItem.Create(RoleInfo, 11, GeneralOption.SkillLimit, new(1, 180, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
        AfterMeetClearTarget = BooleanOptionItem.Create(RoleInfo, 12, OptionName.AfterMeetClearTarget, true, false);
        TargetMax = IntegerOptionItem.Create(RoleInfo, 13, OptionName.TargetMaxCount, new(1, 180, 1), 1, false, AfterMeetClearTarget)
            .SetValueFormat(OptionFormat.Times);
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.HunterKill);
        sender.Writer.Write(HunterLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.HunterKill) return;
        HunterLimit = reader.ReadInt32();
    }
    private static void SendRPC_SyncList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetHunterList, SendOption.Reliable, -1);
        writer.Write(ForHunter.Count);
        for (int i = 0; i < ForHunter.Count; i++)
            writer.Write(ForHunter[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ForHunter = new();
        for (int i = 0; i < count; i++)
            ForHunter.Add(reader.ReadByte());
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? HunterCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && HunterLimit >= 1;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (HunterLimit >= 1)
        {
            HunterLimit --;
            SendRPC();
            if (ForHunter.Count >= TargetMax.GetInt())
            {
                Player.Notify(GetString("TargetMax"));
                return false;
            }
            ForHunter.Add(target.PlayerId);
            SendRPC_SyncList();
            killer.SetKillCooldownV2(target: target);
        }
        info.CanKill = false;
        return false;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        foreach (var pc in ForHunter)
        {
            var player = Utils.GetPlayerById(pc);
            if (!player.IsAlive() || player == null || Pelican.IsEaten(pc)) continue;
            player.RpcMurderPlayerV2(player);
            player.SetRealKiller(target);
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (AfterMeetClearTarget.GetBool()) ForHunter.Clear();
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Utils.GetRoleColor(CustomRoles.Hunter) : Color.gray, $"({HunterLimit})");
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        return ForHunter.Contains(seen.PlayerId) ? "+" : "";
    }
}