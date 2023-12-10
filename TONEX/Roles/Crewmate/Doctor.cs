using AmongUs.GameOptions;
using System.Linq;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;

namespace TONEX.Roles.Crewmate;
public sealed class Doctor : RoleBase, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Doctor),
            player => new Doctor(player),
            CustomRoles.Doctor,
            () => RoleTypes.Scientist,
            CustomRoleTypes.Crewmate,
            21100,
            SetupOptionItem,
            "doc|法醫",
            "#80ffdd"
        );
    public Doctor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskCompletedBatteryCharge = OptionTaskCompletedBatteryCharge.GetFloat();
        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_After);
    }
    private static OptionItem OptionTaskCompletedBatteryCharge;
    enum OptionName
    {
        DoctorTaskCompletedBatteryCharge
    }
    private static float TaskCompletedBatteryCharge;
    private static void SetupOptionItem()
    {
        OptionTaskCompletedBatteryCharge = FloatOptionItem.Create(RoleInfo, 10, OptionName.DoctorTaskCompletedBatteryCharge, new(0f, 10f, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ScientistCooldown = 0f;
        AURoleOptions.ScientistBatteryCharge = TaskCompletedBatteryCharge;
    }
    private static bool OnCheckMurderPlayerOthers_After(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (!info.IsSuicide || target.Is(CustomRoles.Doctor)) return true;
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
        {
            if (pc.Is(CustomRoles.Doctor) && info.IsSuicide)
            {
                if (pc.Is(CustomRoles.Madmate) && !killer.GetCustomRole().IsImpostorTeam())
                    Logger.Info($"{pc.GetRealName()} 是个叛徒，所以他选择无视杀人现场", "Bodyguard.OnCheckMurderPlayerOthers_After");
                else
                     return false;
            }
        }
        return true;
    }
}