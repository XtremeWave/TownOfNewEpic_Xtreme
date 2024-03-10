using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using static TONEX.Translator;

namespace TONEX.Roles.Impostor;
public sealed class EvilInvisibler : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilInvisibler),
            player => new EvilInvisibler(player),
            CustomRoles.EvilInvisibler,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3900,
            SetupOptionItem,
            "sw|隱匿者|隐匿|隐身人|隐身"
        );

    public EvilInvisibler(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        InvisTime = -1;
        LastTime = -1;
        VentedId = -1;
    }

    static OptionItem EvilInvisiblerCooldown;
    static OptionItem EvilInvisiblerDuration;
    enum OptionName
    {
        EvilInvisiblerCooldown,
        EvilInvisiblerDuration,
    }

    private long InvisTime;
    private long LastTime;
    private int VentedId;
    private static void SetupOptionItem()
    {
        EvilInvisiblerCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.EvilInvisiblerCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        EvilInvisiblerDuration = FloatOptionItem.Create(RoleInfo, 11, OptionName.EvilInvisiblerDuration, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(InvisTime.ToString());
        sender.Writer.Write(LastTime.ToString());
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        
        InvisTime = long.Parse(reader.ReadString());
        LastTime = long.Parse(reader.ReadString());
    }
    public bool CanGoInvis() => GameStates.IsInTask && InvisTime == -1 && LastTime == -1;
    public bool IsInvis() => InvisTime != -1;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || LastTime == -1) return;
        var now = Utils.GetTimeStamp();

        if (LastTime + (long)EvilInvisiblerCooldown.GetFloat() < now)
        {
            LastTime = -1;
            if (!player.IsModClient()) player.Notify(GetString("EvilInvisiblerCanVent"));
            SendRPC();
        }
    }
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost || !IsInvis()) return;
        var remainTime = InvisTime + (long)EvilInvisiblerDuration.GetFloat() - now;
        if (remainTime < 0)
        {
            LastTime = now;
            InvisTime = -1;
            SendRPC();
            player?.MyPhysics?.RpcBootFromVent(VentedId != -1 ? VentedId : Main.LastEnteredVent[player.PlayerId].Id);
            NameNotifyManager.Notify(player, GetString("EvilInvisiblerInvisStateOut"));
            return;
        }
        else if (remainTime <= 10)
        {
            if (!player.IsModClient()) player.Notify(string.Format(GetString("EvilInvisiblerInvisStateCountdown"), remainTime));
        }
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        var now = Utils.GetTimeStamp();
        if (IsInvis())
        {
            LastTime = now;
            InvisTime = -1;
            SendRPC();
            NameNotifyManager.Notify(Player, GetString("EvilInvisiblerInvisStateOut"));
            return false;
        }
        else
        {
            new LateTask(() =>
            {
                if (CanGoInvis())
                {
                    VentedId = ventId;

                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(physics.NetId, 34, SendOption.Reliable, Player.GetClientId());
                    writer.WritePacked(ventId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);

                    InvisTime = now;
                    SendRPC();

                    NameNotifyManager.Notify(Player, GetString("EvilInvisiblerInvisState"), EvilInvisiblerDuration.GetFloat());
                }
                else
                {
                    physics.RpcBootFromVent(ventId);
                    NameNotifyManager.Notify(Player, GetString("EvilInvisiblerInvisInCooldown"));
                }
            }, 0.5f, "EvilInvisibler Vent");
            return true;
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!isForHud || isForMeeting) return "";

        var str = new StringBuilder();
        if (IsInvis())
        {
            var remainTime = InvisTime + (long)EvilInvisiblerDuration.GetFloat() - Utils.GetTimeStamp();
            return string.Format(GetString("EvilInvisiblerInvisStateCountdown"), remainTime);
        }
        else if (LastTime != -1)
        {
            var cooldown = LastTime + (long)EvilInvisiblerCooldown.GetFloat() - Utils.GetTimeStamp();

            return string.Format(GetString("EvilInvisiblerInvisCooldownRemain"), cooldown);
        }
        else
        {
            str.Append(GetString("EvilInvisiblerCanVent"));
        }
        return str.ToString();
    }
    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (!IsInvis()) return;
        var (killer, target) = info.AttemptTuple;

        target.RpcTeleport(target.GetTruePosition());
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        killer.SetKillCooldownV2();

        target.SetRealKiller(killer);
        target.RpcMurderPlayerV2(target);

        info.DoKill = false;
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        LastTime = -1;
        InvisTime = -1;
        SendRPC();
    }
    public override void OnGameStart()
    {
        LastTime = Utils.GetTimeStamp();
        SendRPC();
    }
}