/*using AmongUs.GameOptions;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
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
            1000,
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
    public static OptionItem OptionCanGuessImp;
    public static OptionItem OptionCanGuessAddons;
    public static OptionItem OptionCanGuessVanilla;
    public static OptionItem OptionCanGuessTaskDoneSnitch;
    enum OptionName
    {
        SwapperCanGuessTimes,
        EGCanGuessImp,
        EGCanGuessAdt,
        EGCanGuessVanilla,
        EGCanGuessTaskDoneSnitch,
    }

    public int GuessLimit;
    private static void SetupOptionItem()
    {
        OptionGuessNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SwapperCanGuessTimes, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        OptionCanGuessImp = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EGCanGuessImp, true, false);
        OptionCanGuessAddons = BooleanOptionItem.Create(RoleInfo, 12, OptionName.EGCanGuessAdt, false, false);
        OptionCanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EGCanGuessVanilla, true, false);
        OptionCanGuessTaskDoneSnitch = BooleanOptionItem.Create(RoleInfo, 14, OptionName.EGCanGuessTaskDoneSnitch, true, false);
    }
    public override void Add()
    {
        GuessLimit = OptionGuessNums.GetInt();
    }
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(Utils.GetRoleColor(CustomRoles.EvilSwapper), seen.PlayerId.ToString()) + " " + nameText;
        }
    }
    public string ButtonName { get; private set; } = "Target";
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
        ShowGuessPanel(target.PlayerId, MeetingHud.Instance);
        return false;
    }
}*/