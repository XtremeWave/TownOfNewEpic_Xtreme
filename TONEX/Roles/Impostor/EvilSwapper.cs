using AmongUs.GameOptions;
using TONEX.Modules;
using System.Collections.Generic;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using static TONEX.SwapperHelper;

namespace TONEX.Roles.Impostor;
public sealed class EvilSwapper : RoleBase, IImpostor, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilSwapper),
            player => new EvilSwapper(player),
            CustomRoles.EvilSwapper,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            75_1_1_0300,
            SetupOptionItem,
            "eg|邪惡賭怪|邪恶的赌怪|坏赌|邪恶赌|恶赌|赌怪"
        );
    public EvilSwapper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionGuessNums;
    public static OptionItem SwapperCanStartMetting;
    enum OptionName
    {
        SwapperCanSwapTimes,
        SwapperCanStartMetting,
    }

    public int SwapLimit;
    public static List<byte> SwapList;
    private static void SetupOptionItem()
    {
        OptionGuessNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SwapperCanSwapTimes, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        SwapperCanStartMetting = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SwapperCanStartMetting, true, false);
    }
    public override void Add()
    {
        SwapLimit = OptionGuessNums.GetInt();
    }
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(Utils.GetRoleColor(CustomRoles.EvilSwapper), seen.PlayerId.ToString()) + " " + nameText;
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
    /*public override bool ModifyVote(byte voter, byte target)
    {
        
        if (SwapList.Contains(target))
            

    }*/
}