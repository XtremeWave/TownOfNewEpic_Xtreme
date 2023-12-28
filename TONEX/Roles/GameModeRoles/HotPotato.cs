using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;

namespace TONEX.Roles.GameModeRoles;
public sealed class HotPotato : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(HotPotato),
            player => new HotPotato(player),
            CustomRoles.HotPotato,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            4900,
            null,
            "ho",
            "#ff9900",
            true,
           introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public HotPotato(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.MarkOthers.Add(MarkOthers);
    }
    public static int BoomTime;
    public bool IsKiller { get; private set; } = false;
    //public override bool CanUseAbilityButton() => true;
    public bool CanUseShapeShiftButton() => true;
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("HotBoom");
        return true;
    }
    public override void Add()
    {
        BoomTime = HotPotatoManager.BoomTimes;
    }
    public static bool KnowTargetRoleColor(PlayerControl target, bool isMeeting)
    => target.Is(CustomRoles.HotPotato);
    public static string MarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return (seen.Is(CustomRoles.HotPotato)) ? Utils.ColorString(RoleInfo.RoleColor, "●") : "";
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
        => enabled |= true;
    public float CalculateKillCooldown() => CanUseKillButton() ? 3f : 255f;
    public bool CanUseKillButton() => Player.Is(CustomRoles.HotPotato);
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.ShapeshifterCooldown = HotPotatoManager.Boom.GetInt();
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        info.CanKill = false;
        var (killer, target) = info.AttemptTuple;
        if(Options.CurrentGameMode != CustomGameMode.HotPotato || !killer.Is(CustomRoles.HotPotato) || target.Is(CustomRoles.HotPotato)) return false;
        target.RpcSetCustomRole(CustomRoles.HotPotato);
        killer.RpcSetCustomRole(CustomRoles.ColdPotato);
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
        killer.SetKillCooldownV2(target: target, forceAnime: true);
        Utils.NotifyRoles(killer);
        Utils.NotifyRoles(target);
        info.CanKill = false;
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !player.IsAlive()) return;
        if (HotPotatoManager.BoomTimes <= 0)
        {
            Player.RpcResetAbilityCooldown();
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return string.Format(GetString("HotPotatoTimeRemain"), HotPotatoManager.RoundTime.ToString());
    }
}
