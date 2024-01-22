using AmongUs.GameOptions;
using TONEX.Roles.Core;
using System.Collections.Generic;
using static TONEX.Translator;
using Hazel;
using UnityEngine;
using MS.Internal.Xml.XPath;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Impostor;
public sealed class DoubleKiller : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(DoubleKiller),
            player => new DoubleKiller(player),
            CustomRoles.DoubleKiller,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1238687,
            SetupOptionItem,
            "du|双杀|双刀"
        );
    public DoubleKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        DoubleKillerReady = new();
    }

    static OptionItem DoubleKillerDefaultKillCooldown;
    static OptionItem TwoKillCooldown;
    public List<byte> DoubleKillerReady;
    public long DoubleKillerTwoTime;
    enum OptionName
    {
        DoubleKillerDefaultKillCooldown,
        DoubleKillerTwoKillCooldown,
    }
    private float KillCooldown;
    private bool ShCooldown;
    private static void SetupOptionItem()
    {
        DoubleKillerDefaultKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.DoubleKillerDefaultKillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        TwoKillCooldown = IntegerOptionItem.Create(RoleInfo, 11, OptionName.DoubleKillerTwoKillCooldown, new(0, 180, 1), 30, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        KillCooldown = DoubleKillerDefaultKillCooldown.GetFloat();
        ShCooldown = false;
        DoubleKillerReady = new();
    }
    public override void OnGameStart()
    {
        DoubleKillerTwoTime = Utils.GetTimeStamp();
    }
    public float CalculateKillCooldown() => KillCooldown;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = ShCooldown ? 255f : TwoKillCooldown.GetFloat();
    }
    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var killer = info.AttemptKiller;
        if (DoubleKillerReady.Contains(killer.PlayerId))
        {
            DoubleKillerReady.Remove(killer.PlayerId);
            KillCooldown = 0f;
            killer.ResetKillCooldown();
            killer.SyncSettings();
            ShCooldown = false;
            DoubleKillerTwoTime = Utils.GetTimeStamp();
            Player.RpcResetAbilityCooldown();
            killer.SyncSettings();
        }
        else
        {
            KillCooldown = DoubleKillerDefaultKillCooldown.GetFloat();
            killer.ResetKillCooldown();
            killer.SyncSettings();
        }
    }
    public override bool CanUseAbilityButton() => DoubleKillerTwoTime != -1 && !ShCooldown;
    public override bool GetAbilityButtonSprite(out string buttonName)
    {
        return base.GetAbilityButtonSprite(out buttonName);
    }
    public override bool GetAbilityButtonText(out string text)
    {
        return base.GetAbilityButtonText(out text);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var now = Utils.GetTimeStamp();

        if (DoubleKillerTwoTime + (long)TwoKillCooldown.GetFloat() < now && DoubleKillerTwoTime != -1)
        {
            DoubleKillerTwoTime = -1;
            Player.Notify(GetString("DoubleKillerReady"),2f);
            DoubleKillerReady.Add(Player.PlayerId);
            ShCooldown = true;
            Player.RpcResetAbilityCooldown();
            Player.SyncSettings();
        }
    }
    public override void AfterMeetingTasks()
    {
        ShCooldown = false;
        DoubleKillerTwoTime = Utils.GetTimeStamp();
        Player.RpcResetAbilityCooldown();
    }
}
