using static TONEX.Translator;
using TONEX.Roles.Core;
using AmongUs.GameOptions;
using UnityEngine;
using Hazel;

namespace TONEX.Roles.Crewmate;
public sealed class Mayor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mayor),
            player => new Mayor(player),
            CustomRoles.Mayor,
            () => !Options.UsePets.GetBool() && OptionHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20500,
            SetupOptionItem,
            "my| –ÈL| ≈≥§",
            "#204d42"
        );
    public Mayor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AdditionalVote = OptionAdditionalVote.GetInt();
        HasPortableButton = OptionHasPortableButton.GetBool();
        NumOfUseButton = OptionNumOfUseButton.GetInt();

        LeftButtonCount = NumOfUseButton;
    }

    private static OptionItem OptionAdditionalVote;
    private static OptionItem OptionHasPortableButton;
    private static OptionItem OptionNumOfUseButton;
    enum OptionName
    {
        MayorAdditionalVote,
        MayorHasPortableButton,
        MayorNumOfUseButton
    }
    public static int AdditionalVote;
    public static bool HasPortableButton;
    public static int NumOfUseButton;
    public static bool HideVote;

    public int LeftButtonCount;
    //UsePet
    public long SkillCooldown;
    public int NoUseCooldown;
    private static void SetupOptionItem()
    {
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MayorAdditionalVote, new(1, 15, 1), 4, false)
            .SetValueFormat(OptionFormat.Votes);
        OptionHasPortableButton = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MayorHasPortableButton, false, false);
        OptionNumOfUseButton = IntegerOptionItem.Create(RoleInfo, 12, OptionName.MayorNumOfUseButton, new(1, 99, 1), 3, false, OptionHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        SkillCooldown = Utils.GetTimeStamp();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown =
            LeftButtonCount <= 0
            ? 255f
            : opt.GetInt(Int32OptionNames.EmergencyCooldown);
        AURoleOptions.EngineerInVentMaxTime = 1;
        NoUseCooldown = opt.GetInt(Int32OptionNames.EmergencyCooldown);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (SkillCooldown == 0)  return;
         if (SkillCooldown + NoUseCooldown < Utils.GetTimeStamp())
        {
            SkillCooldown = 0;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")));
        }
    }
    public override bool CanUseAbilityButton() => LeftButtonCount > 0;
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.MayorCanUseButton);
        sender.Writer.Write(LeftButtonCount);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.MayorCanUseButton) return;
        LeftButtonCount = reader.ReadInt32();
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (LeftButtonCount > 0 && SkillCooldown + NoUseCooldown < Utils.GetTimeStamp())
        {
            var user = physics.myPlayer;
            physics.RpcBootFromVent(ventId);
            user?.ReportDeadBody(null);
            LeftButtonCount--;
            SkillCooldown = Utils.GetTimeStamp();
            SendRPC();
        }
        return false;
    }
    public override bool OnUsePet(PlayerControl player)
    {
        if (LeftButtonCount > 0)
        {
            var user = player;
            user?.ReportDeadBody(null);
            LeftButtonCount--;
            SendRPC();
        }
        return true;
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // º»∂®Çé
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (voterId == Player.PlayerId)
        {
            numVotes = AdditionalVote + 1;
        }
        return (votedForId, numVotes, doVote);
    }
    public override void AfterMeetingTasks()
    {
        if (HasPortableButton)
            Player.RpcResetAbilityCooldown();
        SendRPC();
    }
    public override void OnStartMeeting()
    {
        if (SkillCooldown != 0) SkillCooldown = 0;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(LeftButtonCount >= 1 ? Utils.GetRoleColor(CustomRoles.Mayor) : Color.gray, $"({LeftButtonCount})");
}