using AmongUs.GameOptions;

using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Vanilla;

public sealed class Shapeshifter : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Shapeshifter),
            player => new Shapeshifter(player),
            RoleTypes.Shapeshifter
        );
    public Shapeshifter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}