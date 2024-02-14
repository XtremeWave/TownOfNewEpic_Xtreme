using AmongUs.GameOptions;
using TONEX.Modules;
using TONEX.Roles.Core;
using UnityEngine;
using static TONEX.Translator;
using Hazel;
using System.Collections.Generic;
using TONEX.Roles.Neutral;

namespace TONEX.Roles.Crewmate;
public sealed class NiceTimeStops : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(NiceTimeStops),
            player => new NiceTimeStops(player),
            CustomRoles.NiceTimeStops,
            () => Options.UsePets.GetBool() ? RoleTypes.Crewmate : RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            15546396,
            SetupOptionItem,
            "shi|时停",
            "#f6f657"
        );
    public NiceTimeStops(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NiceTimeStopsstop = new();
    }

    static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillDuration;
    static OptionItem ReduceCooldown;
    static OptionItem MaxCooldown;
    enum OptionName
    {
        NiceTimeStopsSkillCooldown,
        NiceTimeStopsSkillDuration,
        ReduceCooldown,
        MaxCooldown,
    }
    private List<byte> NiceTimeStopsstop;
    private long ProtectStartTime;
    private float Cooldown;
    public long UsePetCooldown;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.NiceTimeStopsSkillCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        ReduceCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.ReduceCooldown, new(2.5f, 180f, 2.5f), 10f, false)
    .SetValueFormat(OptionFormat.Seconds);
        MaxCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.MaxCooldown, new(2.5f, 250f, 2.5f), 60f, false)
  .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 13, OptionName.NiceTimeStopsSkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        
    }
    public override void Add()
    {
        ProtectStartTime = -1;
        Cooldown = OptionSkillCooldown.GetFloat();
        if (Options.UsePets.GetBool()) UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void OnGameStart()
    {
        if (Options.UsePets.GetBool()) UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("NiceTimeStopsVetnButtonText");
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
        return !(UsePetCooldown != -1);
    }
    public override bool GetGameStartSound(out string sound)
    {
        sound = "TheWorld";
        return true;
    }
    public override bool GetPetButtonText(out string text)
    {
        text = GetString("NiceTimeStopsVetnButtonText");
        return !(UsePetCooldown != -1);
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
            Player.Notify(GetString("NiceTimeStopsOnGuard"));
        Player.ColorFlash(Utils.GetRoleColor(CustomRoles.SchrodingerCat));
        CustomSoundsManager.RPCPlayCustomSoundAll("TheWorld");
        foreach (var player in Main.AllAlivePlayerControls)
        {
            if (Player == player) continue;
            if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
            NameNotifyManager.Notify(player, Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceTimeStops), GetString("ForNiceTimeStops")));
            var tmpSpeed1 = Main.AllPlayerSpeed[player.PlayerId];
            NiceTimeStopsstop.Add(player.PlayerId);
            Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
            Main.CantDoActList.Add(player.PlayerId);
            ExtendedPlayerControl.SendCantDoActPlayer(true);
            player.MarkDirtySettings();
            new LateTask(() =>
            {
                Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - Main.MinSpeed + tmpSpeed1;
                Main.CantDoActList.Remove(player.PlayerId);
                ExtendedPlayerControl.SendCantDoActPlayer(false);
                player.MarkDirtySettings();
              NiceTimeStopsstop.Remove(player.PlayerId);
                RPC.PlaySoundRPC(player.PlayerId, Sounds.TaskComplete);
            }, OptionSkillDuration.GetFloat(), "Time Stop");
        }
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var now = Utils.GetTimeStamp();
        if (Player.IsAlive() && ProtectStartTime + (long)OptionSkillDuration.GetFloat() < now && ProtectStartTime != -1)
        {
            ProtectStartTime = -1;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("NiceTimeStopsOffGuard")));
        }
        if (Player.IsAlive() && UsePetCooldown + (long)Cooldown < now && UsePetCooldown != -1 && Options.UsePets.GetBool())
        {
            UsePetCooldown = -1;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")));
        }
    }
    public override void OnUsePet()
    {
        if (!Options.UsePets.GetBool()) return;
        if (UsePetCooldown != -1)
        {
            var cooldown = UsePetCooldown + (long)Cooldown - Utils.GetTimeStamp();
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), cooldown, 1f));
            return;
        }
        ReduceNowCooldown();
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
        ProtectStartTime = Utils.GetTimeStamp();
        UsePetCooldown = Utils.GetTimeStamp();
        if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
        Player.Notify(GetString("NiceTimeStopsOnGuard"));
        foreach (var player in Main.AllAlivePlayerControls)
        {
            if (Player == player) continue;
            if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
            NameNotifyManager.Notify(player, Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceTimeStops), GetString("ForNiceTimeStops")));
            var tmpSpeed1 = Main.AllPlayerSpeed[player.PlayerId];
            NiceTimeStopsstop.Add(player.PlayerId);
            Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
            Main.CantDoActList.Add(player.PlayerId);
            ExtendedPlayerControl.SendCantDoActPlayer(true);
            player.MarkDirtySettings();
            new LateTask(() =>
            {
                Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - Main.MinSpeed + tmpSpeed1;
                Main.CantDoActList.Remove(player.PlayerId);
                ExtendedPlayerControl.SendCantDoActPlayer(false);
                player.MarkDirtySettings();
                NiceTimeStopsstop.Remove(player.PlayerId);
                RPC.PlaySoundRPC(player.PlayerId, Sounds.TaskComplete);
            }, OptionSkillDuration.GetFloat(), "Time Stop");
        }
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (NiceTimeStopsstop.Contains(reporter.PlayerId))    return false;
        return true;
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        Player.RpcResetAbilityCooldown();
    }
    public override void AfterMeetingTasks()
    {
        UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void OnStartMeeting()
    {
        ProtectStartTime = -1;
    }
}
