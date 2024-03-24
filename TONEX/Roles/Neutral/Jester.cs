using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Neutral;
public sealed class Jester : RoleBase, INeutral
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Jester),
            player => new Jester(player),
            CustomRoles.Jester,
            () => CanVent.GetBool()? RoleTypes.Engineer:RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50000,
            SetupOptionItem,
            "je|小丑|丑皇",
            "#ec62a5"
        );
    public Jester(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    static OptionItem OptionCanUseButton;
    static OptionItem CanVent;
    enum OptionName
    {
        JesterCanUseButton
    }
    private static void SetupOptionItem()
    {
        OptionCanUseButton = BooleanOptionItem.Create(RoleInfo, 10, OptionName.JesterCanUseButton, false, false);
        CanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public override Action CheckExile(GameData.PlayerInfo exiled, ref bool DecidedWinner, ref List<string> WinDescriptionText)
    {
        if (!AmongUsClient.Instance.AmHost || Player.PlayerId != exiled.PlayerId) return null;

        DecidedWinner = true;
        WinDescriptionText.Add(Translator.GetString("ExiledJester"));
        return () =>
        {
            CustomWinnerHolder.SetWinnerOrAdditonalWinner(CustomWinner.Jester);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
        };
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (Is(reporter) && target == null && !OptionCanUseButton.GetBool())
        {
            Logger.Info("因禁止小丑拍灯取消会议", "Jester.OnCheckReportDeadBody");
            return false;
        }
        return true;
    }
}