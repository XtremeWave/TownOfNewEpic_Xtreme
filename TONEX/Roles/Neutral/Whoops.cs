using AmongUs.GameOptions;
using Hazel;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Neutral;
public sealed class Whoops : RoleBase, INeutral
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Whoops),
            player => new Whoops(player),
            CustomRoles.Whoops,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            94_1_0_0400,
            null,
            "wh|狈",
            "#00b4eb",
            true,
            countType: CountTypes.Jackal,
            ctop: true
        );
    public Whoops(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.True
    )
    {
        CanRecruit = false;
    }
    private bool CanRecruit;
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (seen.Is(CustomRoles.Sidekick)) return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), "△");
        //else if (seen.Is(CustomRoles.Wolfmate)) return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), "🔻");
        else if (seen.Is(CustomRoles.Jackal)) return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), "△");
        else
            return "";
    }
    public override bool OnCompleteTask(out bool cancel)
    {
        CanRecruit = true;
        cancel = false;
        return false;
    }
    public override bool CheckVoteAsVoter(PlayerControl votedFor)
    {
        if (votedFor == null || (votedFor.GetCountTypes() == CountTypes.Jackal || !CanRecruit)) return true;
        if (votedFor.CanUseKillButton())
            votedFor.RpcSetCustomRole(CustomRoles.Sidekick);
        else
            votedFor.RpcSetCustomRole(CustomRoles.Whoops);
        Utils.SendMessage(Translator.GetString("WhoopsRecruitTrue"), votedFor.PlayerId);
        return false;
    }
}
