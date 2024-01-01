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
    public int UsePetCooldown;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.GrenadierSkillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 11, OptionName.GrenadierSkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCauseVision = FloatOptionItem.Create(RoleInfo, 12, OptionName.GrenadierCauseVision, new(0f, 5f, 0.05f), 0.3f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanAffectNeutral = BooleanOptionItem.Create(RoleInfo, 13, OptionName.GrenadierCanAffectNeutral, false, false);
    }
    public override void Add()
    {
        BlindingStartTime = 0;
        MadBlindingStartTime = 0;
    }
    public override void OnGameStart() => UsePetCooldown = OptionSkillCooldown.GetInt();
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
        return !(UsePetCooldown != 0);
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
        if (UsePetCooldown != 0)
        {
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), UsePetCooldown, 1f));
            return;
        }
        UsePetCooldown = OptionSkillCooldown.GetInt();
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
        if (BlindingStartTime != 0 && BlindingStartTime + OptionSkillDuration.GetFloat() < Utils.GetTimeStamp())
        {
            BlindingStartTime = 0;
            Player.RpcProtectedMurderPlayer();
            Player.Notify(GetString("GrenadierSkillStop"));
            Utils.MarkEveryoneDirtySettings();
        }
        if (MadBlindingStartTime != 0 && MadBlindingStartTime + OptionSkillDuration.GetFloat() < Utils.GetTimeStamp())
        {
            MadBlindingStartTime = 0;
            Player.RpcProtectedMurderPlayer();
            Player.Notify(GetString("GrenadierSkillStop"));
            Utils.MarkEveryoneDirtySettings();
        }
    }
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (UsePetCooldown == 0 || !Options.UsePets.GetBool()) return;
        if (UsePetCooldown >= 1 && Player.IsAlive() && !GameStates.IsMeeting) UsePetCooldown -= 1;
        if (UsePetCooldown <= 0 && Player.IsAlive())
        {
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")), 2f);
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
            if (roleClass.BlindingStartTime != 0)
            {
                if ((target.IsImp() || target.Is(CustomRoles.Madmate))
                    || target.IsNeutral() && OptionCanAffectNeutral.GetBool())
                {
                    return true;
                }
            }
            else if (roleClass.MadBlindingStartTime != 0)
            {
                if (!target.IsImp() && !target.Is(CustomRoles.Madmate))
                    return true;
            }
        }
        return false;
    }
    public override void AfterMeetingTasks()
    {
        UsePetCooldown = OptionSkillCooldown.GetInt();
    }
    public override void OnStartMeeting()
    {
        MadBlindingStartTime = 0;
        BlindingStartTime = 0;
    }
}