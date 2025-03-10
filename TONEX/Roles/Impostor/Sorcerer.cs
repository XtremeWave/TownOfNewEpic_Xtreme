using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TONEX.Roles.Core;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using System.Diagnostics.Contracts;
using System.Linq;

namespace TONEX.Roles.Impostor;
public sealed class Sorcerer : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Sorcerer),
            player => new Sorcerer(player),
            CustomRoles.Sorcerer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            94_1_4_1200,
            SetupOptionItem,
            "so"
        );
    public Sorcerer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionLimit;
    public int Limit;
    public static List<byte> ForSorcerer;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(2.5f, 180f, 2.5f), 35f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionLimit = IntegerOptionItem.Create(RoleInfo, 11, GeneralOption.SkillLimit, new(1, 14, 1), 2, false)
            .SetValueFormat(OptionFormat.Players);
    }
    public override void Add()
    {
        Limit = OptionLimit.GetInt();
        ForSorcerer = new();
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Limit);
    }

    public override void ReceiveRPC(MessageReader reader) => Limit = reader.ReadInt32();
    private static void SendRPC_SyncList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSorcererList, SendOption.Reliable, -1);
        writer.Write(ForSorcerer.Count);
        for (int i = 0; i < ForSorcerer.Count; i++)
            writer.Write(ForSorcerer[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ForSorcerer = new();
        for (int i = 0; i < count; i++)
            ForSorcerer.Add(reader.ReadByte());
    }
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    public override string GetProgressText(bool comms = false) => Utils.ColorString(Limit>=1? Utils.GetRoleColor(CustomRoles.Sorcerer) : Color.gray, $"({Limit})");
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Limit < 1) return true;
        var (killer, target) = info.AttemptTuple;
        killer.ResetKillCooldown();
        killer.SetKillCooldownV2(target: Player, forceAnime: true);
        Limit--;
        ForSorcerer.Add(target.PlayerId);
        SendRPC();
        SendRPC_SyncList();
        Utils.NotifyRoles(Player);
        return false;
    }
    public override bool OnCheckMurderAsTargetAfter(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        if (ForSorcerer.Count!=0)
        {
            var (killer, target) = info.AttemptTuple;
           killer.ResetKillCooldown();
           killer.SetKillCooldownV2(target: Player, forceAnime: true);
            var pc = Utils.GetPlayerById(ForSorcerer.First());
            pc.RpcMurderPlayerV2(pc);
            pc.SetRealKiller(target);
            ForSorcerer.Remove(ForSorcerer.First());
            SendRPC_SyncList();
            Utils.NotifyRoles(Player);
            return false;
        }
        return true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (ForSorcerer.Contains(seen.PlayerId))
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "□");
        else
            return "";
    }
}
