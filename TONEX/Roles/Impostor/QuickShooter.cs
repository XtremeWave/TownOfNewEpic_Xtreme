using AmongUs.GameOptions;
using Hazel;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;
using static TONEX.Translator;

namespace TONEX.Roles.Impostor;
public sealed class QuickShooter : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(QuickShooter),
            player => new QuickShooter(player),
            CustomRoles.QuickShooter,
       () => Options.UsePets.GetBool() ? RoleTypes.Impostor : RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            3700,
            SetupOptionItem,
            "qs|快槍手|快枪"
        );
    public QuickShooter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionMeetingReserved;
    static OptionItem OptionShapeshiftCooldown;
    enum OptionName
    {
        KillCooldown,
        QuickShooterShapeshiftCooldown,
        MeetingReserved,
    }

    private int ShotLimit;
    private bool Storaging;
    public long UsePetCooldown;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.KillCooldown, new(2.5f, 180f, 2.5f), 35f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.QuickShooterShapeshiftCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMeetingReserved = IntegerOptionItem.Create(RoleInfo, 14, OptionName.MeetingReserved, new(0, 15, 1), 2, false)
            .SetValueFormat(OptionFormat.Pieces);
    }
    public override void Add()
    {
        ShotLimit = 0;
        if (Options.UsePets.GetBool()) UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void OnGameStart()
    {
        if (Options.UsePets.GetBool()) UsePetCooldown = Utils.GetTimeStamp();
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ShotLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        
        ShotLimit = reader.ReadInt32();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = OptionShapeshiftCooldown.GetFloat();
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(ShotLimit >= 1 ? Color.red : Color.gray, $"({ShotLimit})");
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("QuickShooterShapeshiftText");
        return true;
    }
    public override bool GetPetButtonText(out string text)
    {
        text = GetString("QuickShooterShapeshiftText");
        return !(UsePetCooldown != -1);
    }
    public override int OverrideAbilityButtonUsesRemaining() => ShotLimit;
    public override void OnShapeshift(PlayerControl target)
    {
        var shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (Player.killTimer < 1 && shapeshifting)
        {
            ShotLimit++;
            SendRPC();
            Storaging = true;
            Player.ResetKillCooldown();
            Player.SetKillCooldownV2();
            Player.Notify(GetString("QuickShooterStoraging"));
            Logger.Info($"{Utils.GetPlayerById(Player.PlayerId)?.GetNameWithRole()} : 剩余子弹{ShotLimit}发", "QuickShooter.OnShapeshift");
        }
    }
    public override void OnUsePet()
    {
        if (!Options.UsePets.GetBool()) return;
        if (UsePetCooldown != -1)
        {
            var cooldown = UsePetCooldown + (long)OptionShapeshiftCooldown.GetFloat() - Utils.GetTimeStamp();
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), cooldown, 1f));
            return;
        }
        if (Player.killTimer < 1)
        {
            ShotLimit++;
            SendRPC();
            Storaging = true;
            Player.ResetKillCooldown();
            Player.SetKillCooldownV2();
            Player.Notify(GetString("QuickShooterStoraging"));
            Logger.Info($"{Utils.GetPlayerById(Player.PlayerId)?.GetNameWithRole()} : 剩余子弹{ShotLimit}发", "QuickShooter.OnShapeshift");
        }
    }
    public float CalculateKillCooldown()
    {
        float cooldown = (Storaging || ShotLimit < 1) ? OptionKillCooldown.GetFloat() : 0.01f;
        Storaging = false;
        return cooldown;
    }
    public override void OnStartMeeting()
    {
        int before = ShotLimit;
        ShotLimit = Mathf.Clamp(ShotLimit, 0, OptionMeetingReserved.GetInt());
        if (ShotLimit != before) SendRPC();
    }
    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        int before = ShotLimit;
        ShotLimit--;
        ShotLimit = Mathf.Max(ShotLimit, 0);
        if (ShotLimit != before) SendRPC();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var now = Utils.GetTimeStamp();
        if (Player.IsAlive() && UsePetCooldown + (long)OptionShapeshiftCooldown.GetFloat() < now && UsePetCooldown != -1 && Options.UsePets.GetBool())
        {
            UsePetCooldown = -1;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")));
        }
    }
    public override void AfterMeetingTasks()
    {
        UsePetCooldown = Utils.GetTimeStamp();
    }
}
