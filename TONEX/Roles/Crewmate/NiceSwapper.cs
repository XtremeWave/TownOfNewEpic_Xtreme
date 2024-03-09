using AmongUs.GameOptions;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using System.Collections.Generic;
using static TONEX.SwapperHelper;
using static UnityEngine.GraphicsBuffer;
using Hazel;
using static TONEX.Modules.MeetingVoteManager;

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
            "ng|正義换票|正义的换票|好换票|正义换票|正换票|挣亿的换票|挣亿换票",
            "#7C3756"
        );
    public NiceSwapper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        SwapList = new();
    }

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
    public List<byte> SwapList;
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
    public string ButtonName { get; private set; } = "SwapNo";
    public bool ShouldShowButton() => Player.IsAlive();
    public bool ShouldShowButtonFor(PlayerControl target) => !SwapperCanSelf.GetBool() && target != Player || target.IsAlive() && SwapperCanSelf.GetBool();
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
        if (reason != null)
            Player.ShowPopUp(Utils.ColorString(UnityEngine.Color.cyan, Translator.GetString("SwapTitle")) + "\n" +(reason));
        return false;
    }
    public IReadOnlyDictionary<byte, VoteData> AllVotes => allVotes;
    private Dictionary<byte, VoteData> allVotes = new(15);
    public override void AfterMeetingTasks()
    {
        if (SwapList.Count == 2)
            SwapLimit--;
        SwapList.Clear();
        SendRPC(true);
    }
    public void SendRPC(bool cle = false)
    {
        using var sender = CreateSender(CustomRPC.NiceSwapperSync);
        sender.Writer.Write(SwapLimit);
        sender.Writer.Write(cle);
    }

    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.NiceSwapperSync) return;
        SwapLimit = reader.ReadInt32();
        var cle = reader.ReadBoolean();
        if (cle)
            SwapList.Clear();
    }
    
}