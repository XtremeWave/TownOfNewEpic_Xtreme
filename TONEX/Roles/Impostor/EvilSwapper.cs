using AmongUs.GameOptions;
using TONEX.Modules;
using System.Collections.Generic;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using static TONEX.SwapperHelper;
using Hazel;

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
            "eg|邪惡换票|邪恶的换票|坏换票|邪恶换票|恶换票|换票"
        );
    public EvilSwapper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        SwapList = new();
    }

    public static OptionItem OptionGuessNums;
    public static OptionItem SwapperCanStartMetting;
    enum OptionName
    {
        SwapperCanSwapTimes,
        SwapperCanStartMetting,
    }

    public int SwapLimit;
    public List<byte> SwapList;
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
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive() && target != Player;
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
        Swap(Player, target, out var reason);
        if (reason != null)
        Player.ShowPopUp(Utils.ColorString(UnityEngine.Color.cyan, Translator.GetString("SwapTitle")) + "\n" + Translator.GetString(reason));
        return false;
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (!isIntentional || sourceVotedForId >= 253 || SwapList.Contains(sourceVotedForId) || !Player.IsAlive() || SwapList.Count != 2)
        {
            return baseVote;
        }
        if (sourceVotedForId == SwapList[0])
        {
            votedForId = SwapList[1];
        }
        else if (sourceVotedForId == SwapList[1])
        {
            votedForId = SwapList[0];
        }
        return (votedForId, numVotes, false);
    }
    public override void AfterMeetingTasks()
    {

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
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {

        if ((seer.GetRoleClass() as EvilSwapper).SwapList.Contains(seen.PlayerId)) return Utils.ColorString(RoleInfo.RoleColor, "▲"); ;
        return "";
    }
}