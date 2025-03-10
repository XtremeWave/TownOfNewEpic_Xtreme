using TONEX.Roles.Core;
using AmongUs.GameOptions;
using System.Linq;
using HarmonyLib;

namespace TONEX.Roles.AddOns.Common;
public sealed class Bewilder : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    SimpleRoleInfo.Create(
    typeof(Bewilder),
    player => new Bewilder(player),
    CustomRoles.Bewilder,
    81200,
    SetupOptionItem,
    "bwd|迷惑者",
    "#c894f5"
    );
    public Bewilder(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }



    public static OptionItem OptionVision;
    enum OptionName
    {
        BewilderVision
    }

    static void SetupOptionItem()
    {;
        OptionVision = FloatOptionItem.Create(RoleInfo, 20, OptionName.BewilderVision, new(0f, 5f, 0.05f), 0.6f, false)
.SetValueFormat(OptionFormat.Multiplier);
    }
    public override void ApplyGameOptions(IGameOptions opt) 
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.CrewLightMod, (Main.DefaultCrewmateVision - OptionVision.GetFloat()) < 0f ? 0f : Main.DefaultCrewmateVision - OptionVision.GetFloat());
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, (Main.DefaultCrewmateVision - OptionVision.GetFloat()) < 0f ? 0f : Main.DefaultCrewmateVision - OptionVision.GetFloat());
        Main.AllPlayerControls.Where(x => !Player.IsAlive() && Player.GetRealKiller()?.PlayerId == x.PlayerId && !x.Is(CustomRoles.Hangman)).Do(player =>
        {

            player.RpcSetCustomRole(CustomRoles.Bewilder);
            Utils.NotifyRoles(player);
        });
    }
}