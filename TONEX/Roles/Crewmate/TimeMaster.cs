﻿using AmongUs.GameOptions;
using TONEX.Modules;
using TONEX.Roles.Core;
using System;
using static TONEX.Translator;
using UnityEngine;
using Hazel;
using System.Collections.Generic;

namespace TONEX.Roles.Crewmate;
public sealed class TimeMaster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(TimeMaster),
            player => new TimeMaster(player),
            CustomRoles.TimeMaster,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            226312,
            SetupOptionItem,
            "zhu|时主",
            "#44baff"
        );
    public TimeMaster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Marked = false;
        TimeMasterbacktrack = new();
    }
    private bool Marked;
    static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillDuration;
    static OptionItem ReduceCooldown;
    static OptionItem MaxCooldown;
    enum OptionName
    {
        TimeMasterSkillCooldown,
        TimeMasterSkillDuration,
        ReduceCooldown,
        MaxCooldown,
    }
    public static Dictionary<byte, Vector2> TimeMasterbacktrack = new();
    private long ProtectStartTime;
    private float Cooldown;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 14, OptionName.TimeMasterSkillCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        ReduceCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.ReduceCooldown, new(2.5f, 180f, 2.5f), 10f, false)
    .SetValueFormat(OptionFormat.Seconds);
        MaxCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.MaxCooldown, new(2.5f, 250f, 2.5f), 60f, false)
  .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 13, OptionName.TimeMasterSkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        Marked = false;
        ProtectStartTime = 0;
        Cooldown = OptionSkillCooldown.GetFloat();
        TimeMasterbacktrack = new();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("TimeStopsVetnButtonText");
        return true;
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SyncTimeMaster);
        sender.Writer.Write(Marked);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SyncTimeMaster) return;
        Marked = reader.ReadBoolean();
    }
    public void ReduceNowCooldown()
    {
        Cooldown = Cooldown + ReduceCooldown.GetFloat();
        if (Cooldown > MaxCooldown.GetFloat()) Cooldown -= ReduceCooldown.GetFloat();
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        ReduceNowCooldown();
        Player.SyncSettings();
        ProtectStartTime = Utils.GetTimeStamp();
        if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
        Player.Notify(GetString("TimeMasterOnGuard"));
        foreach (var player in Main.AllPlayerControls)
        {
            if (TimeMasterbacktrack.ContainsKey(player.PlayerId))
            {
                player.RPCPlayCustomSound("Teleport");
                var position = TimeMasterbacktrack[player.PlayerId];
                Utils.TP(player.NetTransform, position);
                TimeMasterbacktrack.Remove(player.PlayerId);
            }
            else
            {
                TimeMasterbacktrack.Add(player.PlayerId, player.GetTruePosition());
                SendRPC();
                Marked = true;
            }
        }
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (ProtectStartTime == 0) return;
        if (ProtectStartTime + OptionSkillDuration.GetFloat() < Utils.GetTimeStamp())
        {
            ProtectStartTime = 0;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("TimeMasterOffGuard")));
        }
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        Player.RpcResetAbilityCooldown();
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        if (ProtectStartTime != 0 && ProtectStartTime + OptionSkillDuration.GetFloat() >= Utils.GetTimeStamp() && Marked)
        {
            var (killer, target) = info.AttemptTuple;
            foreach (var player in Main.AllPlayerControls)
            {
                if (TimeMasterbacktrack.ContainsKey(player.PlayerId))
                {
                    var position = TimeMasterbacktrack[player.PlayerId];
                    Utils.TP(player.NetTransform, position);
                }
            }
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            return false;
        }
        return true;
    }
}