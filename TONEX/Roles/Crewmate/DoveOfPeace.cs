using AmongUs.GameOptions;
using HarmonyLib;
using System.Linq;

using TONEX.Modules;
using TONEX.Roles.Core;

using static TONEX.Translator;

namespace TONEX.Roles.Crewmate;
public sealed class DoveOfPeace : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(DoveOfPeace),
            player => new DoveOfPeace(player),
            CustomRoles.DoveOfPeace,
            () => Options.UsePets.GetBool() ? RoleTypes.Crewmate : RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            22800,
            SetupOptionItem,
            "dp|和平之鴿|和平的鸽子|和平|鸽子|和平鸟",
            "#FFFFFF"
        );
    public DoveOfPeace(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillNums;
    enum OptionName
    {
        DoveOfPeaceCooldown,
        DoveOfPeaceMaxOfUseage,
    }

    private int SkillLimit;
    public long UsePetCooldown;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.DoveOfPeaceCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillNums = IntegerOptionItem.Create(RoleInfo, 12, OptionName.DoveOfPeaceMaxOfUseage, new(1, 99, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    { 
    SkillLimit = OptionSkillNums.GetInt();
        if (Options.UsePets.GetBool()) UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void OnGameStart()
    {
        if (Options.UsePets.GetBool()) UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown =
            SkillLimit <= 0
            ? 255f
            : OptionSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("DoveOfPeaceVentButtonText");
        return true;
    }
    public override bool GetPetButtonText(out string text)
    {
        text = GetString("DoveOfPeaceVentButtonText");
        return !(UsePetCooldown != -1);
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (SkillLimit >= 1)
        {
            SkillLimit--;
            Player.RpcProtectedMurderPlayer();
            Main.AllAlivePlayerControls.Where(x =>
            Player.Is(CustomRoles.Madmate) ?
            (x.CanUseKillButton() && x.GetCustomRole().IsCrewmate()) :
            (x.CanUseKillButton())
            ).Do(x =>
            {
                x.RPCPlayCustomSound("Dove");
                x.ResetKillCooldown();
                x.SetKillCooldownV2();
                x.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DoveOfPeace), GetString("DoveOfPeaceSkillNotify")));
            });
            Player.RPCPlayCustomSound("Dove");
            Player.Notify(string.Format(GetString("DoveOfPeaceOnGuard"), SkillLimit));
            return true;
        }
        else
        {
            Player.Notify(GetString("DoveOfPeaceMaxUsage"));
            return false;
        }
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
        if (SkillLimit >= 1)
        {
            UsePetCooldown = Utils.GetTimeStamp();
            SkillLimit--;
            Player.RpcProtectedMurderPlayer();
            Main.AllAlivePlayerControls.Where(x =>
            Player.Is(CustomRoles.Madmate) ?
            (x.CanUseKillButton() && x.GetCustomRole().IsCrewmate()) :
            (x.CanUseKillButton())
            ).Do(x =>
            {
                x.RPCPlayCustomSound("Dove");
                x.ResetKillCooldown();
                x.SetKillCooldownV2();
                x.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DoveOfPeace), GetString("DoveOfPeaceSkillNotify")));
            });
            Player.RPCPlayCustomSound("Dove");
            Player.Notify(string.Format(GetString("DoveOfPeaceOnGuard"), SkillLimit));
            return;
        }
        else
        {
            Player.Notify(GetString("DoveOfPeaceMaxUsage"));
            return;
        }
    }
    public override bool CanUseAbilityButton() => SkillLimit > 0;
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        Player.RpcResetAbilityCooldown();
    }
    public override void AfterMeetingTasks()
    {
        Player.RpcResetAbilityCooldown();
        UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var now = Utils.GetTimeStamp();
        if (UsePetCooldown + (long)OptionSkillCooldown.GetFloat() < now && UsePetCooldown != -1 && Options.UsePets.GetBool())
        {
            UsePetCooldown = -1;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")));
        }
    }
}