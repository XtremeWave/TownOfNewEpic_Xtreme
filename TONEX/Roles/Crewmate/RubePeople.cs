using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using UnityEngine;
using TONEX.Modules;
using TONEX.Roles.Core;
using static TONEX.Translator;
using TONEX.Roles.Core.Interfaces;
using MS.Internal.Xml.XPath;


namespace TONEX.Roles.Crewmate;

public sealed class RubePeople : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    SimpleRoleInfo.Create(
        typeof(RubePeople),
        player => new RubePeople(player),
        CustomRoles.RubePeople,
        () => RoleTypes.Engineer,
        CustomRoleTypes.Crewmate,
        22568,
        SetupOptionItem,
        "ru",
        "#66ff00"
    );
    public RubePeople(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ForRubePeople = new();
    }

        static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillDuration;
    static OptionItem ReduceCooldown;
    static OptionItem MaxCooldown;
    enum OptionName
    {
        TimeStopsSkillCooldown,
        TimeStopsSkillDuration,
        ReduceCooldown,
        MaxCooldown,
    }
   public static List<byte> ForRubePeople;
    private long ProtectStartTime;
    private float Cooldown;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        ReduceCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.ReduceCooldown, new(2.5f, 180f, 2.5f), 10f, false)
    .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 13, GeneralOption.SkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MaxCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.MaxCooldown, new(2.5f, 250f, 2.5f), 60f, false)
          .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        ProtectStartTime = 0;
        Cooldown = OptionSkillCooldown.GetFloat();
        ForRubePeople = new();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("RubePeopleVetnButtonText");
        return true;
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
        Player.Notify(GetString("RubePeopleOnGuard"));

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
            player.Notify(string.Format(GetString("RubePeopleOffGuard")));
        }
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        if (ProtectStartTime != 0 && ProtectStartTime + OptionSkillDuration.GetFloat() >= Utils.GetTimeStamp())
        {
            var (killer, target) = info.AttemptTuple;
            target.RpcMurderPlayerV2(killer);
            killer.RpcMurderPlayerV2(target);
            ForRubePeople.Add(killer.PlayerId);
            return false;
        }
        return true;
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        Player.RpcResetAbilityCooldown();
    }
}