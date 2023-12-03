using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;
using static TONEX.Translator;

namespace TONEX.Roles.Neutral;
public sealed class Sidekick : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Sidekick),
            player => new Sidekick(player),
            CustomRoles.Sidekick,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            50900,
          null,
            "si",
            "#00b4eb",
            true,
            countType: CountTypes.Jackal,
            assignCountRule: new(1, 1, 1)
        );
    public Sidekick(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    { }
    public bool CanUseSabotageButton() => false;
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        var target = player;
       if (target.Is(CustomRoles.Jackal) && Player.Is(CustomRoles.Sidekick))
        {
            Player.RpcSetCustomRole(CustomRoles.Jackal);
            Player.ResetKillCooldown();
            Player.SetKillCooldown();

        }
    }
}
