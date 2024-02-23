using AmongUs.GameOptions;
using TONEX.Modules;
using System.Collections.Generic;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using static TONEX.Translator;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Neutral;
public sealed class Rebels : RoleBase, IOverrideWinner, IIndependent
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Rebels),
            player => new Rebels(player),
            CustomRoles.Rebels,
         () => Options.UsePets.GetBool() ? RoleTypes.Crewmate : RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            94_1_1_0300,
            SetupOptionItem,
            "re",
            "#339900"
        );
    public Rebels(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillDuration;
    enum OptionName
    {
        RebelsSkillCooldown,
        RebelsSkillDuration
    }
    private long ProtectStartTime;
    private long UsePetCooldown;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.RebelsSkillCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 13, OptionName.RebelsSkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        ProtectStartTime = -1;
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
        text = GetString("RebelsVetnButtonText");
        return true;
    }
    public void CheckWin(ref CustomWinner WinnerTeam, ref HashSet<byte> WinnerIds)
    {
        if (Player.IsAlive() && WinnerTeam != CustomWinner.Rebels && ProtectStartTime != -1)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Rebels);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Rebels);
        }
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
            
            ProtectStartTime = Utils.GetTimeStamp();
            if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
            Player.Notify(string.Format(GetString("RebelsOnGuard"),2f));
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
            ProtectStartTime = Utils.GetTimeStamp();
            if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
            Player.RPCPlayCustomSound("Gunload");
            UsePetCooldown = Utils.GetTimeStamp();
            Player.Notify(string.Format(GetString("RebelsOnGuard"), 2f));
    }
    public override bool GetPetButtonText(out string text)
    {
        text = GetString("RebelsVetnButtonText");
        return !(UsePetCooldown != -1);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var now = Utils.GetTimeStamp();
        if (Player.IsAlive() && ProtectStartTime + (long)OptionSkillDuration.GetFloat() < now && ProtectStartTime != -1)
        {
            ProtectStartTime = -1;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("RebelsOffGuard")));
        }
        if (Player.IsAlive() && UsePetCooldown + (long)OptionSkillCooldown.GetFloat() < now && UsePetCooldown != -1 && Options.UsePets.GetBool())
        {
            UsePetCooldown = -1;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")));
        }
    }
    public override void OnStartMeeting()
    {
        ProtectStartTime = -1;
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        Player.RpcResetAbilityCooldown();
    }
}
