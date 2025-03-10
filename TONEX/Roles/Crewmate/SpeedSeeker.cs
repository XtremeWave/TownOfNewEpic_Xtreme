using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TONEX.Roles.Core;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Crewmate;
public sealed class SpeedSeeker : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SpeedSeeker),
            player => new SpeedSeeker(player),
            CustomRoles.SpeedSeeker,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            94_1_4_1800,
            SetupOptionItem,
            "Sds",
            "#009966",
            true
        );
    public SpeedSeeker(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
    }

    static OptionItem OptionCooldown;
    static OptionItem OptionGetSpeed;
    enum OptionName { 
       GetSpeed
    }
    public float Speed;
    public bool IsKiller { get; private set; } = false;
    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionGetSpeed = FloatOptionItem.Create(RoleInfo, 11, OptionName.GetSpeed, new(0.1f, 0.9f, 0.1f), 0.1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Add() => Speed = Main.AllPlayerSpeed[Player.PlayerId];
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Speed);
    }
    public override void ReceiveRPC(MessageReader reader) => Speed = reader.ReadSingle();
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("SpeedSeekerButtonText");
        return true;
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? OptionCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => Player.IsAlive();
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        killer.SetKillCooldownV2(OptionCooldown.GetFloat());
        if (Main.AllPlayerSpeed[target.PlayerId] <= 0.1f) return false;
        Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] + (Main.AllPlayerSpeed[target.PlayerId]*OptionGetSpeed.GetFloat());
        Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - (Main.AllPlayerSpeed[target.PlayerId] * OptionGetSpeed.GetFloat());
        killer.MarkDirtySettings();
        target.MarkDirtySettings();
        Speed = Main.AllPlayerSpeed[Player.PlayerId];
        SendRPC();
        Utils.NotifyRoles(Player);
        info.CanKill = false;
        return false;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Utils.GetRoleColor(CustomRoles.SpeedSeeker) : Color.gray, $"({Speed})");
}

