using AmongUs.GameOptions;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Impostor;
public sealed class Gamblers : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Gamblers),
            player => new Gamblers(player),
            CustomRoles.Gamblers,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            1648687,
            SetupOptionItem,
            "ga|赌博|土博"
        );
    public Gamblers(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem DefaultKillCooldown;
    static OptionItem MinKillCooldown;
    static OptionItem MaxKillCooldown;
    static OptionItem OptionProbability;
    enum OptionName
    {
        GamblersDefaultKillCooldown,
        GamblersMaxKillCooldown,
        GamblersMinKillCooldown,
        GamblersProbability,
    }
    private float KillCooldown;
    private static void SetupOptionItem()
    {
        DefaultKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.GamblersDefaultKillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.GamblersMinKillCooldown, new(2.5f, 180f, 2.5f), 2.5f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MaxKillCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.GamblersMaxKillCooldown, new(2.5f, 180f, 2.5f), 35f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionProbability = IntegerOptionItem.Create(RoleInfo, 14, OptionName.GamblersProbability, new(0, 100, 5), 50, false)
    .SetValueFormat(OptionFormat.Percent);
    }
    public override void Add()
    {
        KillCooldown = DefaultKillCooldown.GetFloat();
    }
    public float CalculateKillCooldown() => KillCooldown;

    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var killer = info.AttemptKiller;
        if (IRandom.Instance.Next(0, 100) < OptionProbability.GetInt())
        {                  
            KillCooldown = MinKillCooldown.GetFloat();      

           
            killer.SyncSettings();
            killer.SetKillCooldownV2(MinKillCooldown.GetFloat());
        }
        else
        {
            KillCooldown = MaxKillCooldown.GetFloat();
            killer.SetKillCooldownV2(MaxKillCooldown.GetFloat());
            killer.SyncSettings();
        }
    }
}
