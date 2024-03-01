using AmongUs.GameOptions;
using Hazel;
using System.Linq;
using TONEX.Roles.Core;
using UnityEngine;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using static TONEX.Translator;
using TONEX.Roles.Crewmate;

namespace TONEX.Roles.Neutral;
public sealed class Martyr : RoleBase, IAdditionalWinner, INeutralKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Martyr),
            player => new Martyr(player),
            CustomRoles.Martyr,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            94_1_1_0200,
            SetupOptionItem,
            "mar",
            "#A94F34",
            true,
            CanKill,
            countType: CountTypes.Martyr,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Martyr(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {

        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_After);
    }
    public static byte WinnerID;

    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionCanGetKillButton;
    public static OptionItem OptionCanUseSabotage;
    private static OptionItem OptionHasImpostorVision;
    enum OptionName
    {
        MartryCanUseKillButtonOnGameStart,
    }

    public static PlayerControl TargetId;
    public static bool CanKill;
    public static bool HasProtect;
    public bool IsNK { get; private set; } = CanKill;
    public bool IsNE { get; private set; } = CanKill;
    public static List<PlayerControl> player;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanGetKillButton = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MartryCanUseKillButtonOnGameStart, true, false);
        OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
    }
    public override void Add()
    {
        //ターゲット割り当て
        if (!AmongUsClient.Instance.AmHost) return;
        player.Add(Player);
        CanKill = OptionCanGetKillButton.GetBool();
        HasProtect = false;
        var playerId = Player.PlayerId;
        List<PlayerControl> targetList = new();
        var rand = IRandom.Instance;
        foreach (var target in Main.AllPlayerControls)
        {
            if (playerId == target.PlayerId) continue;
            if (target.Is(CustomRoles.GM)) continue;
            targetList.Add(target);
        }
        var SelectedTarget = targetList[rand.Next(targetList.Count)];
        TargetId = SelectedTarget;
        SendRPC();
    }
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    public bool CanUseSabotageButton() => OptionCanUseSabotage.GetBool();
    public bool CanUseKillButton() => CanKill && Player.IsAlive();
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(OptionHasImpostorVision.GetBool());
    public void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        using var sender = CreateSender(CustomRPC.SetMartyrTarget);
        sender.Writer.Write(TargetId);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        byte targetId = reader.ReadByte();
        TargetId.PlayerId = targetId;
    }
    private static bool OnCheckMurderPlayerOthers_After(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide) return true;
        if (target.PlayerId == TargetId.PlayerId)
        {
             foreach (var pc in Main.AllPlayerControls.Where(x => x.PlayerId != target.PlayerId && player.Contains(x)))
             {
                if (pc.IsAlive())
                {
                    if (HasProtect)
                    {
                        pc.RpcTeleport(target.transform.position);
                        killer.RpcTeleport(pc.transform.position);
                        killer.RpcMurderPlayerV2(pc);
                        killer.ResetKillCooldown();
                        killer.SetKillCooldownV2();
                        return false;
                    }
                    else
                    {
                        CanKill = true;
                        pc.ResetKillCooldown();
                        pc.SetKillCooldownV2();
                        pc.RpcSetCustomRole(CustomRoles.Martyr);
                    }
                }
                else
                {
                    if (HasProtect)
                    {
                        HasProtect = false;
                        return false;
                    }
                }

            }
        }
                return true;
    }
     
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        return TargetId.PlayerId == seen.PlayerId ? Utils.ColorString(RoleInfo.RoleColor, "♦") : "";
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Options.UsePets.GetBool()) return false;
        if (HasProtect) Player.Notify(GetString("HasProtect"));
        HasProtect = true;
        return false;
    }
    public override void OnUsePet()
    {
       if(Options.UsePets.GetBool()) return;
        if (HasProtect) Player.Notify(GetString("HasProtect"));
        HasProtect = true;
    }
    public bool CheckWin(ref CustomRoles winnerRole, ref CountTypes winnerCountType)
    {
        if (winnerRole == TargetId.GetCustomRole() && !CanKill)
        {
            return true;
        }
        return false;
    }
}