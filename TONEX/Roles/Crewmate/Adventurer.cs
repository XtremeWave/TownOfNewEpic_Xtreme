using AmongUs.GameOptions;
using Hazel;
using TONEX.Roles.Core;
using UnityEngine;

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
            21990,
            SetupOptionItem,
            "ad|探险家",
            "#185abd"
        );
    public Adventurer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

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
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.AdventurerSabotage);
        sender.Writer.Write(SabotageFalseLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.AdventurerSabotage) return;
        SabotageFalseLimit = reader.ReadInt32();
    }
    public override bool OnSabotage(PlayerControl player, SystemTypes systemType)
    {
      if(SabotageFalseLimit >= 1)
      {
        SabotageFalseLimit--;
        SendRPC();
                    player.ResetKillCooldown();
            player.SetKillCooldown();
               player.SyncSettings();
        player.RpcResetAbilityCooldown();
        player.RpcProtectedMurderPlayer(player);
        return false;
      }
return true;
    }
       public override string GetProgressText(bool comms = false) => Utils.ColorString(SabotageFalseLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Adventurer) : Color.gray, $"({SabotageFalseLimit})");
}