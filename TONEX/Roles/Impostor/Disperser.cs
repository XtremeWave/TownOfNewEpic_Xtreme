using AmongUs.GameOptions;
using Hazel;
using System.Linq;
using UnityEngine;
using TONEX.Roles.Core;
using static TONEX.Translator;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using static Il2CppSystem.Net.Http.Headers.Parser;
using TMPro;
using static UnityEngine.GraphicsBuffer;
using TONEX.Roles.GameModeRoles;
using YamlDotNet.Core.Tokens;
using TONEX.Modules.SoundInterface;
using TONEX.Roles.Vanilla;
using InnerNet;

namespace TONEX.Roles.Impostor;
public sealed class Disperser : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Disperser),
            player => new Disperser(player),
            CustomRoles.Disperser,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            94_1_4_1700,
            SetupOptionItem,
            "dp"
        );
    public Disperser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {

    }

    static OptionItem OptionCooldown;
    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(0f, 180f, 2.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("DisperserButtonText");
        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = OptionCooldown.GetFloat();
    public override bool OnCheckVanish()
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        var rd = new System.Random();
        var vents = Object.FindObjectsOfType<Vent>();

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc.Data.IsDead || pc.onLadder || pc.inVent || GameStates.IsMeeting)
            {
                pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Disperser), string.Format(GetString("ErrorTeleport"), pc.GetRealName())));
                continue;
            }

            pc.RPCPlayCustomSound("Teleport");
            var vent = vents[rd.Next(0, vents.Count)];
            pc.RpcTeleport(vent.transform.position);
            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Disperser), string.Format(GetString("TeleportedInRndVentByDisperser"), pc.GetRealName())));
        }
        return false;
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner) => Player.RpcResetAbilityCooldown();
}


