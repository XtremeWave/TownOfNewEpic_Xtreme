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

namespace TONEX.Roles.Neutral;
public sealed class Ranger : RoleBase, INeutralKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Ranger),
            player => new Ranger(player),
            CustomRoles.Ranger,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            94_1_4_1000,
            SetupOptionItem,
            "ra|罗宾汉",
            "#339966",
           true
        );
    public Ranger(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_After);
    }

    private static OptionItem OptionKillCooldown;
    public static OptionItem OptionLimit;
    public bool IsNK { get; private set; } = true;

    public static List<string> ForRanger;
    public static int Limit;
    enum OptionName {
        NeedKillPlayers
    }
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionLimit = IntegerOptionItem.Create(RoleInfo, 11,OptionName.NeedKillPlayers, new(1, 14, 1), 15, false)
       .SetValueFormat(OptionFormat.Players);
    }
    public string Name = "";
    public override void Add()
    {
        ForRanger = new();
        Limit = 0;
    }
    private static void SendRPC_SyncList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRangerList, SendOption.Reliable, -1);
        writer.Write(ForRanger.Count);
        for (int i = 0; i < ForRanger.Count; i++)
        {
            if (ForRanger[i] == null)
                ForRanger[i] = "";      
            writer.Write(ForRanger[i]);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ForRanger = new List<string>();
        for (int i = 0; i < count; i++)
            ForRanger.Add(reader.ReadString());
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
        if (ForRanger != null || ForRanger.Count != 0)
        {
            var text = new StringBuilder();
            foreach (var ranger in ForRanger)
                text.AppendLine(ranger);
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Ranger), $"({Limit})") + "\n" +text.ToString();
        }
        else return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Ranger), $"({Limit})");
    }
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseKillButton() => true;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (ForRanger.Contains(target.GetRealName())) {
            Limit++;
            SendRPC();
            Utils.NotifyRoles(Player);
            if (Limit >= OptionLimit.GetInt()) Win();
        }
        else
        {
            killer.RpcMurderPlayerV2(killer);
            return false;
        }
        return true;
    }
    private static bool OnCheckMurderPlayerOthers_After(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide || target.Is(CustomRoles.Ranger) || killer.Is(CustomRoles.Ranger)) return true;
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
        {
            if (pc.Is(CustomRoles.Ranger))
            {
                if (ForRanger.Contains(killer.GetRealName())) break;
                ForRanger.Add(killer.GetRealName());
                SendRPC_SyncList();
                Utils.NotifyRoles(pc);
            }
        }
        return true;
    }
    public void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Ranger);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting)
    {
        var pc = player;
        if (ForRanger.Contains(pc.GetRealName()))
        {
           ForRanger.Remove(pc.GetRealName());
            SendRPC_SyncList();
            Utils.NotifyRoles(Player);
        }
    }
}
