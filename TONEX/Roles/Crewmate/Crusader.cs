using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TONEX.Roles.Core;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using System.Linq;

namespace TONEX.Roles.Crewmate;
public sealed class Crusader : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Crusader),
            player => new Crusader(player),
            CustomRoles.Crusader,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            94_1_4_1300,
            SetupOptionItem,
            "cr",
            "#cc3300",
            true
        );
    public Crusader(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ForCrusader = new();
        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_After);
    }

    static OptionItem OptionCooldown;
    static OptionItem OptionLimit;
    private int Limit;
    private static List<byte> ForCrusader;
    public bool IsKiller { get; private set; } = false;
    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionLimit = IntegerOptionItem.Create(RoleInfo, 11, GeneralOption.SkillLimit, new(1, 180, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        Limit = OptionLimit.GetInt();
        ForCrusader = new();
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Limit);
    }
    public override void ReceiveRPC(MessageReader reader) => Limit = reader.ReadInt32();
    private static void SendRPC_SyncList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCrusaderList, SendOption.Reliable, -1);
        writer.Write(ForCrusader.Count);
        for (int i = 0; i < ForCrusader.Count; i++)
            writer.Write(ForCrusader[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ForCrusader = new();
        for (int i = 0; i < count; i++)
            ForCrusader.Add(reader.ReadByte());
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("CrusaderButtonText");
        return true;
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? OptionCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && Limit >= 1;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Limit >= 1 && !ForCrusader.Contains(target.PlayerId))
        {
             Limit -= 1;   
            ForCrusader.Add(target.PlayerId);
            SendRPC();
            SendRPC_SyncList();
            Utils.NotifyRoles(Player);
            killer.SetKillCooldownV2(target: target);
        }
        info.CanKill = false;
        return false;
    }
    private static bool OnCheckMurderPlayerOthers_After(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide || target.Is(CustomRoles.Crusader)) return true;
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
        {
            if(ForCrusader.Contains(target.PlayerId) && pc.IsAlive() && pc.Is(CustomRoles.Crusader)) {
                if (pc.Is(CustomRoles.Madmate) && killer.GetCustomRole().IsImpostorTeam()) {
                    pc.RpcMurderPlayerV2(target);
                    ForCrusader.Remove(target.PlayerId);
                    SendRPC_SyncList();
                    Logger.Info($"{pc.GetRealName()} 是个叛徒，所以他选择直接杀死被保护的目标", "Crusader.OnCheckMurderPlayerOthers_After");
                    return false;
                }
                ForCrusader.Remove(target.PlayerId);
                SendRPC_SyncList();
                pc.RpcMurderPlayerV2(killer);
                return false;
            }
        }
        return true;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Utils.GetRoleColor(CustomRoles.Crusader) : Color.gray, $"({Limit})");
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (ForCrusader.Contains(seen.PlayerId))
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Crusader), "✝");
        else
            return "";
    }
}