using AmongUs.GameOptions;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using System;
using static TONEX.Translator;
using UnityEngine;
using Hazel;
using System.Collections.Generic;
using TONEX.Modules;
using MS.Internal.Xml.XPath;
using static UnityEngine.GraphicsBuffer;
using System.Runtime.Intrinsics.Arm;
using System.Linq;

namespace TONEX.Roles.Impostor;
public sealed class Blackmailer : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Blackmailer),
            player => new Blackmailer(player),
            CustomRoles.Blackmailer,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            414699,
            SetupOptionItem,
            "bl|勒索",
            experimental: true
        );
    public Blackmailer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ForBlackmailer = new();
    }

    static OptionItem OptionShapeshiftCooldown;
    public static List<byte> ForBlackmailer;
    enum OptionName
    {
        BlackmailerCooldown,
    }
    private static void SetupOptionItem()
    {
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.BlackmailerCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterLeaveSkin = false;
        AURoleOptions.ShapeshifterCooldown = OptionShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = Translator.GetString("SoulCatcherButtonText");
        return !Shapeshifting;
    }
    private bool Shapeshifting;
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (Shapeshifting)
        {
            if (!target.IsAlive())
                Player.Notify(GetString("TargetIsDead"));
            else
                ForBlackmailer.Add(target.PlayerId);
        }
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        ForBlackmailer.Clear();
    }
    string Name = "";
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        if (!ForBlackmailer.Contains(player.PlayerId)) return;
        Name = player.GetRealName();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (isOnMeeting)
            {
                Utils.SendMessage(string.Format(Translator.GetString("ForBlackmailerDead"), Name), pc.PlayerId, Utils.ColorString(RoleInfo.RoleColor, Translator.GetString("BlackmailerNewsTitle"))); ;
            }
        }
    }
    public static string MarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return (ForBlackmailer.Contains(seen.PlayerId) && isForMeeting == true) ? Utils.ColorString(RoleInfo.RoleColor, "‼") : "";
    }
    public override bool OnPlayerSendMessage(PlayerControl pc,string msg, out MsgRecallMode recallMode)
    {
        if (ForBlackmailer.Contains(pc.PlayerId))
        {
            pc.SetRealKiller(Player);
            pc.RpcSuicideWithAnime();
        }
        recallMode = MsgRecallMode.Spam;
        return true;
    }
}
