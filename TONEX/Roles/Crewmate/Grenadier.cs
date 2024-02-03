using AmongUs.GameOptions;
using HarmonyLib;
using System.Linq;
using TONEX.Modules;
using TONEX.Roles.Core;

using static TONEX.Translator;

namespace TONEX.Roles.Crewmate;
public sealed class Grenadier : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Grenadier),
            player => new Grenadier(player),
            CustomRoles.Grenadier,
         () => Options.UsePets.GetBool() ? RoleTypes.Crewmate : RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            22000,
            SetupOptionItem,
            "gr|擲雷兵|掷雷|闪光弹",
            "#3c4a16"
        );
    public Grenadier(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillDuration;
    public static OptionItem OptionCauseVision;
    static OptionItem OptionCanAffectNeutral;
    enum OptionName
    {
        GrenadierSkillCooldown,
        GrenadierSkillDuration,
        GrenadierCauseVision,
        GrenadierCanAffectNeutral,
    }

    private long BlindingStartTime;
    private long MadBlindingStartTime;
    public long UsePetCooldown;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.GrenadierSkillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 11, OptionName.GrenadierSkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCauseVision = FloatOptionItem.Create(RoleInfo, 12, OptionName.GrenadierCauseVision, new(0f, 5f, 0.05f), 0.3f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionCanAffectNeutral = BooleanOptionItem.Create(RoleInfo, 13, OptionName.GrenadierCanAffectNeutral, false, false);
    }
    public override void Add()
    {
        BlindingStartTime = -1;
        MadBlindingStartTime = -1;
        if (Options.UsePets.GetBool()) UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void OnGameStart()
    {
        if (Options.UsePets.GetBool()) UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = OptionSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("GrenadierVetnButtonText");
        return true;
    }
    public override bool GetPetButtonText(out string text)
    {
        text = GetString("GrenadierVetnButtonText");
        return !(UsePetCooldown != -1);
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Player.Is(CustomRoles.Madmate))
        {
            MadBlindingStartTime = Utils.GetTimeStamp();
            Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => !x.IsImp() && !x.Is(CustomRoles.Madmate)).Do(x => x.RPCPlayCustomSound("FlashBang"));
        }
        else
        {
            BlindingStartTime = Utils.GetTimeStamp();
            Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => x.IsImp() || (x.IsNeutral() && OptionCanAffectNeutral.GetBool())).Do(x => x.RPCPlayCustomSound("FlashBang"));
        }
        if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer();
        Player.RPCPlayCustomSound("FlashBang");
        Player.Notify(GetString("GrenadierSkillInUse"), OptionSkillDuration.GetFloat());
        Utils.MarkEveryoneDirtySettings();
        return true;
    }
    public override void OnUsePet()
    {
        if (!Options.UsePets.GetBool()) return;
        if (UsePetCooldown != -1)
        {
            var cooldown = UsePetCooldown + (long)OptionSkillCooldown.GetFloat() - Utils.GetTimeStamp();
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), cooldown, 1f));
            return;
        }
        UsePetCooldown = Utils.GetTimeStamp();
        if (Player.Is(CustomRoles.Madmate))
        {
            MadBlindingStartTime = Utils.GetTimeStamp();
            Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => !x.IsImp() && !x.Is(CustomRoles.Madmate)).Do(x => x.RPCPlayCustomSound("FlashBang"));
        }
        else
        {
            BlindingStartTime = Utils.GetTimeStamp();
            Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => x.IsImp() || (x.IsNeutral() && OptionCanAffectNeutral.GetBool())).Do(x => x.RPCPlayCustomSound("FlashBang"));
        }
        if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer();
        Player.RPCPlayCustomSound("FlashBang");
        Player.Notify(GetString("GrenadierSkillInUse"), OptionSkillDuration.GetFloat());
        Utils.MarkEveryoneDirtySettings();
        return;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var now = Utils.GetTimeStamp();
        if (BlindingStartTime != -1 && BlindingStartTime + (long)OptionSkillDuration.GetFloat() < now)
        {
            BlindingStartTime = -1;
            Player.RpcProtectedMurderPlayer();
            Player.Notify(GetString("GrenadierSkillStop"));
            Utils.MarkEveryoneDirtySettings();
        }
        if (MadBlindingStartTime != -1 && MadBlindingStartTime + (long)OptionSkillDuration.GetFloat() < now )
        {
            MadBlindingStartTime = -1;
            Player.RpcProtectedMurderPlayer();
            Player.Notify(GetString("GrenadierSkillStop"));
            Utils.MarkEveryoneDirtySettings();
        }
        if (UsePetCooldown + (long)OptionSkillCooldown.GetFloat() < now && UsePetCooldown != -1 && Options.UsePets.GetBool())
        {
            UsePetCooldown = -1;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")));
        }
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        Player.RpcResetAbilityCooldown();
    }
    public static bool IsBlinding(PlayerControl target)
    {
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Grenadier)))
        {
            if (pc.GetRoleClass() is not Grenadier roleClass) continue;
            if (roleClass.BlindingStartTime != -1)
            {
                if ((target.IsImp() || target.Is(CustomRoles.Madmate))
                    || target.IsNeutral() && OptionCanAffectNeutral.GetBool())
                {
                    return true;
                }
            }
            else if (roleClass.MadBlindingStartTime != -1)
            {
                if (!target.IsImp() && !target.Is(CustomRoles.Madmate))
                    return true;
            }
        }
        return false;
    }
    public override void AfterMeetingTasks()
    {
        UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void OnStartMeeting()
    {
        MadBlindingStartTime = -1;
        BlindingStartTime = -1;
    }
}