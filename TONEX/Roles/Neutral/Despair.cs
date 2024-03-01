using AmongUs.GameOptions;
using Hazel;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;

namespace TONEX.Roles.Neutral;
public sealed class Despair : RoleBase, INeutral
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Despair),
            player => new Despair(player),
            CustomRoles.Despair,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            168648,
            SetupOptionItem,
            "de|绝望|先生",
            "#666666",
             introSound: () => DestroyableSingleton<HudManager>.Instance.TaskCompleteSound
        );
    public Despair(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    { }

    private static OptionItem OptionKillCooldown;
    private static Options.OverrideTasksData Tasks;
    private int KillCooldown;
    enum OptionName
    {
        BeKillCooldown,
    }

    private static void SetupOptionItem()
    {
        OptionKillCooldown = IntegerOptionItem.Create(RoleInfo, 10, OptionName.BeKillCooldown, new(10, 180, 1), 20, false)
            .SetValueFormat(OptionFormat.Seconds);
        // 20-23を使用
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void Add()
    {
        KillCooldown = OptionKillCooldown.GetInt() + 8 ;
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.DespairBeKill);
        sender.Writer.Write(KillCooldown);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.DespairBeKill) return;
        KillCooldown = reader.ReadInt32();
    }
    public override bool OnCompleteTask(out bool cancel)
    {
        if (MyTaskState.IsTaskFinished && Player.IsAlive())
        {
            Logger.Info("Workaholic completed all tasks", "Workaholic");
            Win();
        }
        else if (Player.IsAlive())
        {
            KillCooldown = OptionKillCooldown.GetInt();
            SendRPC();
        }
        cancel = false;
        return false;
    }
    public void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Despair);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    public override void OnSecondsUpdate(PlayerControl player,long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (KillCooldown == 0) return;
        if (KillCooldown >= 1 && player.IsAlive() && !GameStates.IsMeeting)
        {
           KillCooldown -= 1;
            SendRPC();
        }
        if (KillCooldown <= 0 && player.IsAlive()) 
        {
            player.RpcMurderPlayerV2(player) ;
        }
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(KillCooldown >= 1 ? Utils.GetRoleColor(CustomRoles.Despair) : Color.red, $"({KillCooldown})");
    public override void AfterMeetingTasks() => KillCooldown = OptionKillCooldown.GetInt();
}

