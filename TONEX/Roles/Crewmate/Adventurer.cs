using AmongUs.GameOptions;
using Hazel;
using TONEX.Roles.Core;
using UnityEngine;
using Il2CppSystem.Collections.Generic;

namespace TONEX.Roles.Crewmate;
public sealed class Adventurer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Adventurer),
            player => new Adventurer(player),
            CustomRoles.Adventurer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            94_1_0_0300,
            SetupOptionItem,
            "ad|探险家",
            "#185abd"
        );
    public Adventurer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ForAdventurer = new();
    }
    public static List<byte> ForAdventurer;
    static OptionItem OptionLimit;
    enum OptionName
    {
        AdventurerLimit
    }
public int SabotageFalseLimit;
    private static void SetupOptionItem()
    {
        OptionLimit = IntegerOptionItem.Create(RoleInfo, 10, OptionName.AdventurerLimit, new(1, 15, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        SabotageFalseLimit = OptionLimit.GetInt();
        ForAdventurer = new();
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(SabotageFalseLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        
        SabotageFalseLimit = reader.ReadInt32();
    }
    public override bool OnSabotage(PlayerControl player, SystemTypes systemType)
    {
        if(ForAdventurer.Contains(player.PlayerId)) return false;
      if(SabotageFalseLimit >= 1)
      {
        SabotageFalseLimit--;
        SendRPC();
            ForAdventurer.Add(player.PlayerId);
                    player.ResetKillCooldown();
            player.SetKillCooldown();
               player.SyncSettings();
        player.RpcResetAbilityCooldown();
        player.RpcProtectedMurderPlayer(player);
        return false;
      }
return true;
    }
    public override void AfterMeetingTasks()   => ForAdventurer.Clear(); 
    public override string GetProgressText(bool comms = false) => Utils.ColorString(SabotageFalseLimit >= 1 ? RoleInfo.RoleColor : Color.gray, $"({SabotageFalseLimit})");
}