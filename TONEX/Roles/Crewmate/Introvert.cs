using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using UnityEngine;
using TONEX.Roles.Core;
using static TONEX.Translator;
using Hazel;
using static UnityEngine.GraphicsBuffer;
using TONEX.Modules.SoundInterface;

using Il2CppSystem.Collections.Generic;

namespace TONEX.Roles.Crewmate;
public sealed class Introvert : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Introvert),
            player => new Introvert(player),
            CustomRoles.Introvert,
         () => Options.UsePets.GetBool() ? RoleTypes.Crewmate : RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            94_1_4_1900,
            SetupOptionItem,
            "In",
            "#ff99cc"
        );
    public Introvert(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillDuration;
    static OptionItem OptionSkillNums;
    public PlayerControl Target;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.SkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillNums = IntegerOptionItem.Create(RoleInfo, 12, GeneralOption.SkillLimit, new(1, 99, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override long UsePetCooldown { get; set; } = (long)OptionSkillCooldown.GetFloat();
    public override bool EnablePetSkill() => true;
    private int SkillLimit;
    public override bool SetOffGuardProtect(out string notify, out int format_int, out float format_float)
    {
        notify = GetString("IntrovertOffGuard");
        format_int = SkillLimit;
        format_float = -255;
        return true;
    }
    public override void OnGameStart() => TargetArrow.Add(Player.PlayerId, Target.PlayerId);
    public override void Add()
    {
        Target = null;
        SkillLimit = OptionSkillNums.GetInt();
        CreateCountdown(OptionSkillDuration.GetFloat());
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown =
            SkillLimit <= 0
            ? 255f
            : OptionSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override int OverrideAbilityButtonUsesRemaining() => SkillLimit;
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("IntrovertVetnButtonText");
        return true;
    }
    public override bool CanUseAbilityButton() => SkillLimit >= 1;
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(SkillLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        SkillLimit = reader.ReadInt32();
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
       
        if (!CheckForOnGuard(0)) return "";
        if (seen != Player) return "";
        var arrow = TargetArrow.GetArrows(seer, Target.PlayerId);
        if (!isForMeeting)
            return Utils.ColorString(Target.Data.Color, arrow);
        else return "";
    }

    public override string GetProgressText(bool comms = false) => Utils.ColorString(SkillLimit >= 1 ? RoleInfo.RoleColor : Color.gray, $"({SkillLimit})");
    public override bool OnEnterVentWithUsePet(PlayerPhysics physics, int ventId)
    {
        if (SkillLimit >= 1)
        {
            SkillLimit--;
            SendRPC();
            ResetCountdown(0);
            if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
            Player.Notify(string.Format(GetString("IntrovertOnGuard"), SkillLimit, 2f));
            Target = null;
            Utils.NotifyRoles(Player);
            return true;
        }
        else
        {
            Player.Notify(GetString("SkillMaxUsage"));
            return false;
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsMeeting && Main.AllAlivePlayerControls.ToList().Count - 1 >= 1 && Player.IsAlive())
        {
            var list = Main.AllAlivePlayerControls.Where(x => x!=Player && x.IsAlive()).ToList();
            list = list.OrderBy(x => Vector2.Distance(Player.GetTruePosition(), x.GetTruePosition())).ToList();
            var target = list[0];
            if (Target != target) { 
                Target = target;
                TargetArrow.Remove(Player.PlayerId, Target.PlayerId);
                TargetArrow.Add(Player.PlayerId, Target.PlayerId);
                Utils.NotifyRoles(Player);
            }
           
        }
    }
    public override bool GetPetButtonText(out string text)
    {
        text = GetString("IntrovertVetnButtonText");
        return PetUnSet();
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        Player.RpcResetAbilityCooldown();
    }

}
