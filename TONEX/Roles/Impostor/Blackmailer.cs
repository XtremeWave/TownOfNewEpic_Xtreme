using AmongUs.GameOptions;
using TONEX.Roles.Core;
using System;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using static TONEX.Translator;
using System.Collections.Generic;
using TONEX.Modules;

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
            75_1_1_0600,
            SetupOptionItem,
            "bl|勒索"
        );
    public Blackmailer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ForBlackmailer = new();
        CustomRoleManager.MarkOthers.Add(MarkOthers);
        CustomRoleManager.ReceiveMessage.Add(OnReceiveMessage);
        BlackmailerLimit = new();
    }

    static OptionItem OptionShapeshiftCooldown;
    public static List<byte> ForBlackmailer;
    public static Dictionary<byte, int> BlackmailerLimit;

    enum OptionName
    {
        BlackmailerCooldown,
    }
    private static void SetupOptionItem()
    {
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.BlackmailerCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        ForBlackmailer = new();
        //ForBlackmailer.Add(Player.PlayerId);
        BlackmailerLimit = new();
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
    
    public override void OnStartMeeting()
    {
        foreach (var target in ForBlackmailer)
        {
            var player = Utils.GetPlayerById(target);
            Name = player.GetRealName();
            foreach (var pc in Main.AllPlayerControls)
            {
                Utils.SendMessageV2(string.Format(Translator.GetString("ForBlackmailer"), Name), pc.PlayerId, Utils.ColorString(RoleInfo.RoleColor, Translator.GetString("BlackmailerNewsTitle")));
            }
        }
    }
    public static string MarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return (ForBlackmailer.Contains(seen.PlayerId) && isForMeeting == true) ? Utils.ColorString(RoleInfo.RoleColor, "‼") : "";
    }
    public static void OnReceiveMessage(MessageControl msgControl)
    {
        msgControl.IsCommand = ForBlackmailer.Contains(msgControl.Player.PlayerId) ? true : false;
        msgControl.RecallMode = ForBlackmailer.Contains(msgControl.Player.PlayerId) ? MsgRecallMode.Spam : MsgRecallMode.None;
    }
}