using AmongUs.GameOptions;
using TONEX.Roles.Core;
using static TONEX.Translator;
using System.Collections.Generic;
using Hazel;
using UnityEngine;
using System.Linq;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using TONEX.Roles.GameModeRoles;
using InnerNet;
using TONEX.Roles.Vanilla;
using static TONEX.Modules.HazelExtensions;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Impostor;
public sealed class Proxy : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Proxy),
            player => new Proxy(player),
            CustomRoles.Proxy,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            94_1_4_2000,
            SetupOptionItem,
            "prx|借刀杀人"
        );
    public Proxy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {

    }
   enum  OptionName{
        UseSKillResetCooldown
    }
    static OptionItem OptionShapeshiftCooldown;
    static OptionItem OptionResetCooldown;
    public float KillCooldown;
    private static void SetupOptionItem()
    {
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionResetCooldown = BooleanOptionItem.Create(RoleInfo, 11,OptionName.UseSKillResetCooldown, false, false);
    }
    public override void Add() => KillCooldown = Options.DefaultKillCooldown;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = OptionShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("ProxyButtonText");
        return true;
    }
    public float CalculateKillCooldown() => KillCooldown;
    public override bool OnCheckShapeshift(PlayerControl target, ref bool animate)
    {

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!target.IsAlive() || !target.CanUseKillButton())
        {
            Player.SetKillCooldownV2();
            return false;
        }
        animate = false;
        KillCooldown = Main.AllPlayerKillCooldown[target.PlayerId];
        if (OptionResetCooldown.GetBool())
            Player.SetKillCooldownV2(Main.AllPlayerKillCooldown[target.PlayerId]);
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
        return false;
    }

}
