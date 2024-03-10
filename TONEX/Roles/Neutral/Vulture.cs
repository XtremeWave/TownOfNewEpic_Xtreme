using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TONEX.Roles.Core;
using static TONEX.Translator;
using UnityEngine;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Neutral;

public sealed class Vulture : RoleBase, INeutral
{
    public static readonly SimpleRoleInfo RoleInfo =
               SimpleRoleInfo.Create(
            typeof(Vulture),
                  player => new Vulture(player),
                  CustomRoles.Vulture,
                  () => RoleTypes.Engineer,
                  CustomRoleTypes.Neutral,
                  489965,
                  SetupOptionItem,
                   "tu|秃鹫|禿鷲",
                   "#663333"
                   );
    public Vulture(PlayerControl player)
: base(
    RoleInfo,
    player
)
    {
        ForVulture = new();
    }
    static OptionItem OptionEatLimitPerMeeting;
    static OptionItem OptionEatTime;
    enum OptionName
    {
        VultureNeedEatLimit,
        VultureEatTime,
    }

    private static int EatLimit;
    private List<byte> ForVulture;
    private long EatTime;
    private static void SetupOptionItem()
    {
        OptionEatLimitPerMeeting = IntegerOptionItem.Create(RoleInfo, 10, OptionName.VultureNeedEatLimit, new(1, 99, 1), 4, false)
            .SetValueFormat(OptionFormat.Times);
        OptionEatTime = FloatOptionItem.Create(RoleInfo, 11, OptionName.VultureEatTime, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(EatLimit >=1 ? Utils.GetRoleColor(CustomRoles.Vulture) : Color.green, $"({EatLimit})");
    public override void Add()
    {
        EatLimit = OptionEatLimitPerMeeting.GetInt();
        EatTime = Utils.GetTimeStamp();
    }
    public override void OnGameStart()
    {
        EatTime = Utils.GetTimeStamp();
    }
    public override string GetReportButtonText() => GetString("VultureReportButtonText");
    public override bool GetReportButtonSprite(out string buttonName)
    {
        buttonName = "VultureEat";
        return true;
    }
    private static void SendRPCLimit()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VultureLimit, SendOption.Reliable, -1);
        writer.Write(EatLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_Limit(MessageReader reader)
    {
        int count = reader.ReadInt32();
        EatLimit = count;
    }
    private void SendRPC(bool add, Vector3 loc = new())
    {
        using var sender = CreateSender();
        sender.Writer.Write(add);
        if (add)
        {
            sender.Writer.Write(loc.x);
            sender.Writer.Write(loc.y);
            sender.Writer.Write(loc.z);
        }
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        
        if (reader.ReadBoolean())
            LocateArrow.Add(Player.PlayerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        else LocateArrow.RemoveAllTarget(Player.PlayerId);
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        var target = player;
        var pos = target.GetTruePosition();
        float minDis = float.MaxValue;
        string minName = string.Empty;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == target.PlayerId) continue;
            var dis = Vector2.Distance(pc.GetTruePosition(), pos);
            if (dis < minDis && dis < 1.5f)
            {
                minDis = dis;
                minName = pc.GetRealName();
            }
        }

        LocateArrow.Add(Player.PlayerId, target.transform.position);
        SendRPC(true, target.GetTruePosition());
    }
    public override void OnStartMeeting()
    {
        LocateArrow.RemoveAllTarget(Player.PlayerId);
        SendRPC(false);
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        Logger.Info("1", "test");
        if (target != null && ForVulture.Contains(target.PlayerId))
        {
            reporter.Notify(Utils.ColorString(RoleInfo.RoleColor, Translator.GetString("ReportEatBodies")));
            Logger.Info($"{target.Object.GetNameWithRole()} 的尸体已被吞噬，无法被报告", "Cleaner.OnCheckReportDeadBody");
            return false;
        }
        Logger.Info("2", "test");
        if (!Is(reporter) || target == null) return true;
        
        if (EatTime != -1)
        {
            var cooldown = EatTime + (long)OptionEatTime.GetFloat() - Utils.GetTimeStamp();
            Player.Notify(string.Format(GetString("ShowEatCooldown"), cooldown, 1f));
            return false;
        }
        Logger.Info("3", "test");
        ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
        ForVulture.Add(target.PlayerId);
        EatLimit -= 1;
        Logger.Info("4", "test");
        EatTime = Utils.GetTimeStamp();
        Player.Notify(string.Format(GetString("EatTimeCooldown"), EatLimit));
        Logger.Info("5", "test");
        SendRPCLimit();
        if (EatLimit <=0) Win();


        return false;
    }
    public void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Vulture);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var now = Utils.GetTimeStamp();
        if (EatTime + (long)OptionEatTime.GetFloat() < now && EatTime != -1)
        {
            EatTime = -1;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("EatTimeReady")));
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || !(seen) || isForMeeting) return "";
        return (Utils.ColorString(Utils.GetRoleColor(CustomRoles.Vulture), LocateArrow.GetArrows(seer)));
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        EatTime = Utils.GetTimeStamp();
    }
}