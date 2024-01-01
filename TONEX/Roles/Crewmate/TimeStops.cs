using AmongUs.GameOptions;
using TONEX.Modules;
using TONEX.Roles.Core;
using UnityEngine;
using static TONEX.Translator;
using Hazel;
using System.Collections.Generic;
using TONEX.Roles.Neutral;

namespace TONEX.Roles.Crewmate;
public sealed class TimeStops : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(TimeStops),
            player => new TimeStops(player),
            CustomRoles.TimeStops,
            () => Options.UsePets.GetBool() ? RoleTypes.Crewmate : RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            15546396,
            SetupOptionItem,
            "shi|时停",
            "#f6f657"
        );
    public TimeStops(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TimeStopsstop = new();
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
    private List<byte> TimeStopsstop;
    private long ProtectStartTime;
    private float Cooldown;
    public int UsePetCooldown;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.TimeStopsSkillCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        ReduceCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.ReduceCooldown, new(2.5f, 180f, 2.5f), 10f, false)
    .SetValueFormat(OptionFormat.Seconds);
        MaxCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.MaxCooldown, new(2.5f, 250f, 2.5f), 60f, false)
  .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 13, OptionName.TimeStopsSkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        
    }
    public override void Add()
    {
        ProtectStartTime = 0;
        Cooldown = OptionSkillCooldown.GetFloat();
    }
    public override void OnGameStart()
    {
        UsePetCooldown = OptionSkillCooldown.GetInt();
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
        public override bool GetAbilityButtonSprite(out string buttonName)
    {
        buttonName = "TheWorld";
        return true;
    }
            public override bool GetPetButtonSprite(out string buttonName)
    {
        buttonName = "TheWorld";
        return !(UsePetCooldown != 0);
    }
    public override bool GetGameStartSound(out string sound)
    {
        sound = "TheWorld";
        return true;
    }
    public override bool GetPetButtonText(out string text)
    {
        text = GetString("TimeStopsVetnButtonText");
        return !(UsePetCooldown != 0);
    }
    public void ReduceNowCooldown()
    {
        Cooldown = Cooldown + ReduceCooldown.GetFloat();
        if (Cooldown > MaxCooldown.GetFloat())Cooldown -= ReduceCooldown.GetFloat();    
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        ReduceNowCooldown();
       Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
        ProtectStartTime = Utils.GetTimeStamp();
            if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
            Player.Notify(GetString("TimeStopsOnGuard"));
        CustomSoundsManager.RPCPlayCustomSoundAll("TheWorld");
        foreach (var player in Main.AllAlivePlayerControls)
        {
            if (Player == player) continue;
            if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
            NameNotifyManager.Notify(player, Utils.ColorString(Utils.GetRoleColor(CustomRoles.TimeStops), GetString("ForTimeStops")));
            var tmpSpeed1 = Main.AllPlayerSpeed[player.PlayerId];
            TimeStopsstop.Add(player.PlayerId);
            Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
            player.MarkDirtySettings();
            new LateTask(() =>
            {
                Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - Main.MinSpeed + tmpSpeed1;
                ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
                player.MarkDirtySettings();
              TimeStopsstop.Remove(player.PlayerId);
                RPC.PlaySoundRPC(player.PlayerId, Sounds.TaskComplete);
            }, OptionSkillDuration.GetFloat(), "Time Stop");
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
            player.Notify(string.Format(GetString("TimeStopsOffGuard")));
        }
    }
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;     
        if (UsePetCooldown == 0 || !Options.UsePets.GetBool()) return;
        if (UsePetCooldown >= 1 && Player.IsAlive() && !GameStates.IsMeeting)  UsePetCooldown -= 1;
        if (UsePetCooldown <= 0 && Player.IsAlive())
        {
             player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")));
        }
    }
    public override void OnUsePet()
    {
        if (!Options.UsePets.GetBool()) return;
        if (UsePetCooldown != 0)
        {
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), UsePetCooldown, 1f));
            return;
        }
        ReduceNowCooldown();
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
        ProtectStartTime = Utils.GetTimeStamp();
        UsePetCooldown = OptionSkillCooldown.GetInt();
        if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
        Player.Notify(GetString("TimeStopsOnGuard"));
        foreach (var player in Main.AllAlivePlayerControls)
        {
            if (Player == player) continue;
            if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
            NameNotifyManager.Notify(player, Utils.ColorString(Utils.GetRoleColor(CustomRoles.TimeStops), GetString("ForTimeStops")));
            var tmpSpeed1 = Main.AllPlayerSpeed[player.PlayerId];
            TimeStopsstop.Add(player.PlayerId);
            Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
            player.MarkDirtySettings();
            new LateTask(() =>
            {
                Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - Main.MinSpeed + tmpSpeed1;
                ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
                player.MarkDirtySettings();
                TimeStopsstop.Remove(player.PlayerId);
                RPC.PlaySoundRPC(player.PlayerId, Sounds.TaskComplete);
            }, OptionSkillDuration.GetFloat(), "Time Stop");
        }
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (TimeStopsstop.Contains(reporter.PlayerId))    return false;
                
        
        return true;
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        Player.RpcResetAbilityCooldown();
    }
    public override void AfterMeetingTasks()
    {
        UsePetCooldown = OptionSkillCooldown.GetInt();
    }
    public override void OnStartMeeting()
    {
        ProtectStartTime = 0;
    }
}
