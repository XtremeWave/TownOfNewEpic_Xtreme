using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TONEX.Roles.Core;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Crewmate;
public sealed class Eggy : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Eggy),
            player => new Eggy(player),
            CustomRoles.Eggy,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            94_1_4_0200,
           null,
            "eggy|咔咔我的小揪揪",
            "#ffcccc",
            true
        );
    public Eggy(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {

    }

    public bool IsKiller { get; private set; } = false;
    public PlayerControl Target;
    public bool Start;
    public override void Add()
    {
        Target = null;
        Start = false;
    }


    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("PenguinKillButtonText");
        return true;
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? Options.DefaultKillCooldown : 255f;
    public bool CanUseKillButton() => Player.IsAlive();
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => true;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Target == target) {
            Start = false;
            Player.SetKillCooldownV2(Options.DefaultKillCooldown);
            Target = null;
        }
        else if (Target == null || !Target.IsAlive()) {
            Start = true;
            Target = target;
            Player.SetKillCooldownV2(1f);
        }
        info.CanKill = false;
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
       if(Target != null && Start && Player.IsAlive() && Target.IsAlive()) 
            Target.RpcTeleport(Player.GetTruePosition());
    }
}