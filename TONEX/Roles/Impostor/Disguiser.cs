using AmongUs.GameOptions;
using TONEX.Roles.Core;
using static TONEX.Translator;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using System.Linq;

namespace TONEX.Roles.Impostor;
public sealed class Disguiser : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Disguiser),
            player => new Disguiser(player),
            CustomRoles.Disguiser,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            94_1_4_0900,
            SetupOptionItem,
            "Di|伪造者|伪造师",
             experimental: true
        );
    public Disguiser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {

    }

    static OptionItem OptionShapeshiftCooldown;
    static OptionItem OptionShapeshiftDuration;

    private static void SetupOptionItem()
    {
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShapeshiftDuration = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.SkillDuration, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterLeaveSkin = true;
        AURoleOptions.ShapeshifterCooldown = OptionShapeshiftCooldown.GetFloat();
    }
    public override bool OnCheckShapeshift(PlayerControl target, ref bool animate)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!target.IsAlive()|| target==null)  return false;
        animate = false;
        var pcList = Main.AllAlivePlayerControls.Where(x => !x.GetCustomRole().IsImpostor() && x.IsAlive() && x!=target).ToList();
        var Di = pcList[IRandom.Instance.Next(0, pcList.Count)];
        target.RpcShapeshift(Di, true);
        Player.RpcResetAbilityCooldown();
        new LateTask(() => { 
            if(!GameStates.IsMeeting)
               target.RpcShapeshift(target, true);
        }, OptionShapeshiftDuration.GetFloat(), "伪造结束");
        return false;
    }

}
