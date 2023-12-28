using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using System.Collections.Generic;

namespace TONEX.Roles.GameModeRoles;
public sealed class ColdPotato : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ColdPotato),
            player => new ColdPotato(player),
            CustomRoles.ColdPotato,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Neutral,
            4900,
            null,
            "cold|冷土豆",
            "#66ffff",
            true,
           introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public ColdPotato(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.MarkOthers.Add(MarkOthers);
    }
    public static int BoomTime;
    public bool IsKiller { get; private set; } = false;
    //public override bool CanUseAbilityButton() => true;
    public bool CanUseShapeShiftButton() => true;
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("HotBoom");
        return true;
    }
    public static bool KnowTargetRoleColor(PlayerControl target, bool isMeeting)
    => target.Is(CustomRoles.ColdPotato);
    public static string MarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return (seen.Is(CustomRoles.ColdPotato)) ? Utils.ColorString(RoleInfo.RoleColor, "") : "";
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
        => enabled |= true;
    public bool CanUseKillButton() => false;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.ShapeshifterCooldown = HotPotatoManager.Boom.GetInt();
    }
    public static void SendRPC(byte playerId,bool add, Vector3 loc = new())
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMorticianArrow, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(add);
        if (add)
        {
            writer.Write(loc.x);
            writer.Write(loc.y);
            writer.Write(loc.z);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetVultureArrow) return;
        if (reader.ReadBoolean())
            LocateArrow.Add(Player.PlayerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        else LocateArrow.RemoveAllTarget(Player.PlayerId);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !player.IsAlive()) return;
        if (HotPotatoManager.BoomTimes<=0)
        {
            Player.RpcResetAbilityCooldown();
        }
    }
}