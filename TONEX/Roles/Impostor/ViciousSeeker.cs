using AmongUs.GameOptions;
using TONEX.Roles.Core;
using UnityEngine;
using TONEX.Modules;
using static TONEX.Translator;
using Hazel;
using Epic.OnlineServices.Presence;
using System.Linq;
using TONEX.Roles.Crewmate;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Impostor;
public sealed class ViciousSeeker : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ViciousSeeker),
            player => new ViciousSeeker(player),
            CustomRoles.ViciousSeeker,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            11625746,
            SetupOptionItem,
            "vi|恶猎|"
        );
    public ViciousSeeker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { 
       Limit = 0;
        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_After);
    }
    enum OptionName
    {
        ViciousSeekerReduceKillCooldown,
        ViciousSeekerCountMode,
        CanStartMeet,
    }
    static OptionItem OptionReduceKillCooldown;
    static OptionItem OptionKillCooldown;
    static OptionItem OptionCanStartMeet;
    public static OptionItem OptionViciousSeekerCountMode;
    static readonly string[] ViciousSeekerCountMode =
    {
        "ViciousSeekerCountMode.DeadPlayer",
        "ViciousSeekerCountMode.Kill",
        "ViciousSeekerCountMode.ImpostorKill",
    };
    private float KillCooldown;
    public static int Limit;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 35f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionReduceKillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.ViciousSeekerReduceKillCooldown, new(2.5f, 180f, 2.5f), 5f, false)
    .SetValueFormat(OptionFormat.Seconds);
        OptionViciousSeekerCountMode = StringOptionItem.Create(RoleInfo, 12, OptionName.ViciousSeekerCountMode, ViciousSeekerCountMode, 0, false);
        OptionCanStartMeet = BooleanOptionItem.Create(RoleInfo, 13, OptionName.CanStartMeet, false, false);
    }
    public override void Add()
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        Limit = 0;
    }
    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ViciousSeekerKill, SendOption.Reliable, -1);
        writer.Write(Limit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_Limit(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.ViciousSeekerKill) return;
        Limit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => KillCooldown;
    public static bool OnCheckMurderPlayerOthers_After(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide) return true;
        if (killer.Is(CustomRoleTypes.Impostor))
        {
            if (OptionViciousSeekerCountMode.GetInt() == 2)
            {
                Limit++;
                SendRPC();
            }
        }
        return true;
    }
    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var (killer,target) = info.AttemptTuple;
        if (killer.PlayerId == Player.PlayerId)
        {
            if (OptionViciousSeekerCountMode.GetInt() == 1)
            {
                Limit++;
                SendRPC();
                if (Limit >= 6)
                {
                    KillCooldown = KillCooldown - OptionReduceKillCooldown.GetFloat();
                    killer.ResetKillCooldown();
                    killer.SyncSettings();
                }
            }
        }
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (Is(reporter) && target == null && !OptionCanStartMeet.GetBool())   return false;
        return true;
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        if (player == Player) return;
        if (OptionViciousSeekerCountMode.GetInt() == 0)
        {
           Limit++;
           SendRPC();
        }
          
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        if (Limit>=2)
        {
           Limit-=2;
            return false;
        }
        return true;
   }
    public override void AfterMeetingTasks()
    {
        Limit++;
        SendRPC();
        if(Limit >= 8)Limit -= 6;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(Limit >= 1 ? Utils.GetRoleColor(CustomRoles.Veteran) : Color.gray, $"({Limit})");
}
