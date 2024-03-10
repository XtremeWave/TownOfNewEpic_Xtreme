using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using System.Linq;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Neutral;
public sealed class Plaguebearer : RoleBase, INeutralKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Plaguebearer),
            player => new Plaguebearer(player),
            CustomRoles.Plaguebearer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            94_1_1_0400,
            SetupOptionItem,
            "pl|瘟疫",
            "#fffcbe",
            true,
            true
        );
    public Plaguebearer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CanVent = OptionCanVent.GetBool();
    }

    static OptionItem OptionKillCooldown;
   public static OptionItem OptionGodOfPlaguesKillCooldown;
    static OptionItem OptionCanVent;
    enum OptionName
    {
        PlaguebearerKillCooldown,
        GodOfPlaguesKillCooldown
    }

    List<byte> PlaguePlayers;

    public static bool CanVent;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.PlaguebearerKillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionGodOfPlaguesKillCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.GodOfPlaguesKillCooldown, new(2.5f, 180f, 2.5f), 15f, false)
    .SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
    }
    public override void Add() => PlaguePlayers = new();
    public bool IsKiller => false;
    public float CalculateKillCooldown()
    {
        if (!CanUseKillButton()) return 255f;
        return OptionKillCooldown.GetFloat();
    }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool CanUseSabotageButton() => false;
    public bool CanUseKillButton() => Player.IsAlive();
    private void SendRPC()
    {
        var sender = CreateSender();
        sender.Writer.Write(PlaguePlayers.Count);
        PlaguePlayers.Do(sender.Writer.Write);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        
        PlaguePlayers = new();
        for (int i = 0; i < reader.ReadInt32(); i++)
            PlaguePlayers.Add(reader.ReadByte());
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(PlaguePlayers.Count >= 1 ? Utils.ShadeColor(RoleInfo.RoleColor, 0.25f) : Color.gray, $"({PlaguePlayers.Count}/{Main.AllAlivePlayerControls.ToList().Count - 1})");
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide) return true;
        if(PlaguePlayers.Contains(target.PlayerId)) return false;
        PlaguePlayers.Add(target.PlayerId);
        killer.SetKillCooldownV2();
        SendRPC();
       if (Main.AllAlivePlayerControls.ToList().Count - 1 == PlaguePlayers.Count)
       {
                Player.RpcSetCustomRole(CustomRoles.GodOfPlagues);
                killer.SetKillCooldownV2();
       }
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (Main.AllAlivePlayerControls.ToList().Count - 1 == PlaguePlayers.Count)
        {
            Player.RpcSetCustomRole(CustomRoles.GodOfPlagues);
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        return PlaguePlayers.Contains(seen.PlayerId) ? Utils.ColorString(RoleInfo.RoleColor, "♦") : "";
    }
}
