using AmongUs.GameOptions;
using Hazel;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using static TONEX.Translator;

namespace TONEX.Roles.Neutral;
public sealed class NightWolf : RoleBase, INeutralKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
       SimpleRoleInfo.Create(
           typeof(NightWolf),
           player => new NightWolf(player),
           CustomRoles.NightWolf,
           () => RoleTypes.Impostor,
           CustomRoleTypes.Neutral,
           94_1_4_0100,
           SetupOptionItem,
           "nw|月狼",
           "#a77738",
           true,
           true,
           countType: CountTypes.NightWolf
#if RELEASE
,
ctop:true
#endif
       );
    public NightWolf(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {

    }
    public bool IsNK { get; private set; } = true;
    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.NightWolf;
    static OptionItem OptionKillCooldown;
    static OptionItem OptionCooldown;
    static OptionItem OptionHasImpostorVision;
    static OptionItem OptionProtectDuration;
    static OptionItem OptionSpeed;
    enum OptionName
    {
        NWDuration,
        NWCooldown,
        NWSpeed,
    }

    private long ProtectStartTime;
    private long Cooldown;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 4f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
        OptionProtectDuration = FloatOptionItem.Create(RoleInfo, 14, OptionName.NWDuration, new(1f, 999f, 1f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 15, OptionName.NWCooldown, new(2.5f, 180f, 2.5f), 15f, false)
           .SetValueFormat(OptionFormat.Seconds);
        OptionSpeed = FloatOptionItem.Create(RoleInfo, 16, OptionName.NWSpeed, new(0.25f, 5f, 0.25f), 2f, false)
    .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Add()
    {
     ProtectStartTime = -1;
       Cooldown = Utils.GetTimeStamp();
        Speed = Main.AllPlayerSpeed[Player.PlayerId];
    }
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(OptionHasImpostorVision.GetBool());
    public bool CanUseSabotageButton() => false;
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (ProtectStartTime == -1)
        {
            Logger.Info("非狂暴状态，击杀被阻塞", "Night _瓜（划掉）Wolf");
            return false;
        }
        return true;
    }
    public float Speed;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Options.UsePets.GetBool()) return true;
        if (Cooldown == -1)
        {
            Speed = Main.AllPlayerSpeed[Player.PlayerId];
            ProtectStartTime = Utils.GetTimeStamp();
            Cooldown = Utils.GetTimeStamp();
            Main.AllPlayerSpeed[Player.PlayerId] = OptionSpeed.GetFloat();
            Player.MarkDirtySettings();
            return false;
        }
        else return true;
    }
    public override void OnUsePet()
    {
        if (!Options.UsePets.GetBool()) return;
        if (Cooldown == -1)
        {
            Speed = Main.AllPlayerSpeed[Player.PlayerId];
            ProtectStartTime = Utils.GetTimeStamp();
            Cooldown = Utils.GetTimeStamp();
            Main.AllPlayerSpeed[Player.PlayerId] = OptionSpeed.GetFloat();
            Player.MarkDirtySettings();
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (ProtectStartTime != -1 && ProtectStartTime + (long)OptionProtectDuration.GetFloat() < Utils.GetTimeStamp())
        {
            ProtectStartTime = -1;
            Main.AllPlayerSpeed[Player.PlayerId] = Speed;
            Player.MarkDirtySettings();
            Player.Notify(GetString("NWProtectOut"));
        }
        if (Cooldown != -1 && Cooldown + (long)OptionCooldown.GetFloat() < Utils.GetTimeStamp())
        {
            Cooldown = -1;
            Player.Notify(GetString("NWReady"));
        }
    }
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        Logger.Info($"{ProtectStartTime}", "test");
    }
}