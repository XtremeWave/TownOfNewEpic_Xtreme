using AmongUs.GameOptions;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using System.Collections.Generic;
using static TONEX.SwapperHelper;

namespace TONEX.Roles.Crewmate;
public sealed class NiceSwapper : RoleBase, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(NiceSwapper),
            player => new NiceSwapper(player),
            CustomRoles.NiceSwapper,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            75_1_1_0200,
            SetupOptionItem,
            "ng|正義賭怪|正义的赌怪|好赌|正义赌|正赌|挣亿的赌怪|挣亿赌怪",
            "#eede26"
        );
    public NiceSwapper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionSwapNums;
    public static OptionItem SwapperCanSelf;
    public static OptionItem SwapperCanStartMetting;
    enum OptionName
    {
        SwapperCanSwapTimes,
        SwapperCanSelf,
        SwapperCanStartMetting,
    }

    public int SwapLimit;
    public static List<byte> SwapList;
    private static void SetupOptionItem()
    {
        OptionSwapNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SwapperCanSwapTimes, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        SwapperCanStartMetting = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SwapperCanStartMetting, true, false);
        SwapperCanSelf = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SwapperCanSelf, false, false);
    }
    public override void Add()
    {
        SwapLimit = OptionSwapNums.GetInt();
    }
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(RoleInfo.RoleColor, seen.PlayerId.ToString()) + " " + nameText;
        }
    }
    
    public bool ShouldShowButton() => Player.IsAlive();
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive();
    public override bool GetGameStartSound(out string sound)
    {
        sound = "Gunfire";
        return true;
    }
    public override bool OnSendMessage(string msg, out MsgRecallMode recallMode)
    {
        bool isCommand = SwapperMsg(Player, msg, out bool spam);
        recallMode = spam ? MsgRecallMode.Spam : MsgRecallMode.None;
        return isCommand;
    }
    public bool OnClickButtonLocal(PlayerControl target)
    {
        Swap(Player,target, out var reason);
        return false;
    }
}