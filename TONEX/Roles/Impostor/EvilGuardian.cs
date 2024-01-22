/*using AmongUs.GameOptions;
using static TONEX.Translator;
using TONEX.Roles.Core;
using MS.Internal.Xml.XPath;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Impostor;
public sealed class EvilGuardian : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilGuardian),
            player => new EvilGuardian(player),
            CustomRoles.EvilGuardian,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3254,
           SetupOptionItem,
            "eg|邪恶守护"
        );
    public EvilGuardian(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    static OptionItem OptionProbability;
    static OptionItem OptionKiilCooldown;
    enum OptionName
    {
       KillProbability
    }

    private static void SetupOptionItem()
    {
        OptionProbability = IntegerOptionItem.Create(RoleInfo, 10, OptionName.KillProbability, new(0, 100, 5), 40, false)
            .SetValueFormat(OptionFormat.Percent);
        OptionKiilCooldown = FloatOptionItem.Create(RoleInfo,11, GeneralOption.KillCooldown, new(0, 100, 5), 40, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool CanUseAbilityButton() => true;
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("KillButtonText");
        return true;
    }
    public override bool GetAbilityButtonSprite(out string buttonName)
    {
        buttonName= "KillButton";
        return true;
    }
    public override void OnProtectPlayer(PlayerControl target)
    {
        if (Player.IsAlive()) return;
        if (IRandom.Instance.Next(0, 100) < OptionProbability.GetInt())
        {
 target.Notify(string.Format(GetString("KillForEvilGuardian")), 2f);
        Player.RpcTeleport(target.GetTruePosition());
        RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
        target.Data.IsDead = true;
        target.SetRealKiller(Player);
        target.SetDeathReason(CustomDeathReason.Quantization);
        target.RpcExileV2();
        PlayerState.GetByPlayerId(target.PlayerId)?.SetDead();
        }
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.GuardianAngelCooldown = OptionKiilCooldown.GetFloat();
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting)
    {
        if (player.PlayerId == Player.PlayerId)
        {
            player.RpcSetRole(RoleTypes.GuardianAngel);
            Player.RpcProtectedMurderPlayer();
        }
    }
}
*/