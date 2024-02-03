using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using InnerNet;
using System.Linq;
using TONEX.Attributes;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using TONEX.Roles.Crewmate;
using TONEX.Roles.Neutral;
using Mathf = UnityEngine.Mathf;

namespace TONEX.Modules;

public class PlayerGameOptionsSender : GameOptionsSender
{
    public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
    public static void SetDirty(byte playerId) =>
        AllSenders.OfType<PlayerGameOptionsSender>()
        .Where(sender => sender.player.PlayerId == playerId)
        .ToList().ForEach(sender => sender.SetDirty());
    public static void SetDirtyToAll() =>
        AllSenders.OfType<PlayerGameOptionsSender>()
        .ToList().ForEach(sender => sender.SetDirty());

    public override IGameOptions BasedGameOptions =>
        Main.RealOptionsData.Restore(new NormalGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
    public override bool IsDirty { get; protected set; }

    public PlayerControl player;

    public PlayerGameOptionsSender(PlayerControl player)
    {
        this.player = player;
    }
    public void SetDirty() => IsDirty = true;

    public override void SendGameOptions()
    {
        if (player.AmOwner)
        {
            var opt = BuildGameOptions();
            foreach (var com in GameManager.Instance.LogicComponents)
            {
                if (com.TryCast<LogicOptions>(out var lo))
                    lo.SetGameOptions(opt);
            }
            GameOptionsManager.Instance.CurrentGameOptions = opt;
        }
        else base.SendGameOptions();
    }

    public override void SendOptionsArray(Il2CppStructArray<byte> optionArray)
    {
        for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
        {
            if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
            {
                SendOptionsArray(optionArray, i, player.GetClientId());
            }
        }
    }
    public static void RemoveSender(PlayerControl player)
    {
        var sender = AllSenders.OfType<PlayerGameOptionsSender>()
        .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
        if (sender == null) return;
        sender.player = null;
        AllSenders.Remove(sender);
    }
    public override IGameOptions BuildGameOptions()
    {
        Main.RealOptionsData ??= new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

        var opt = BasedGameOptions;
        AURoleOptions.SetOpt(opt);
        var state = PlayerState.GetByPlayerId(player.PlayerId);
        opt.BlackOut(state.IsBlackOut);

        CustomRoles role = player.GetCustomRole();
        switch (role.GetCustomRoleTypes())
        {
            case CustomRoleTypes.Impostor:
                AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                break;
        }

        var roleClass = player.GetRoleClass();
        roleClass?.ApplyGameOptions(opt);
        foreach (var subRole in player.GetCustomSubRoles())
        {
            switch (subRole)
            {
                case CustomRoles.Watcher:
                    opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
                case CustomRoles.Flashman:
                    Main.AllPlayerSpeed[player.PlayerId] = Flashman.OptionSpeed.GetFloat();
                    break;
                case CustomRoles.Lighter:
                    opt.SetVision(true);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Lighter.OptionVistion.GetFloat());
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Lighter.OptionVistion.GetFloat());
                    break;
                case CustomRoles.Bewilder:
                    opt.SetVision(false);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Bewilder.OptionVision.GetFloat());
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Bewilder.OptionVision.GetFloat());
                    break;
                case CustomRoles.Reach:
                    opt.SetInt(Int32OptionNames.KillDistance, 2);
                    break;
                case CustomRoles.Mini:
                    if ((player.GetRoleClass() as IKiller)?.IsKiller ?? false)
                    {
                        if (Mini.Age < 18)
                            opt.SetFloat(FloatOptionNames.KillCooldown, Mini.OptionKidKillCoolDown.GetFloat());
                        else
                            opt.SetFloat(FloatOptionNames.KillCooldown, Mini.OptionAdultKillCoolDown.GetFloat());
                    }
                    break;
                case CustomRoles.Rambler:
                    Main.AllPlayerSpeed[player.PlayerId] = Rambler.OptionSpeed.GetFloat();
                    break;

            }
        }

        // 为迷惑者的凶手
        if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Bewilder) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId && !x.Is(CustomRoles.Hangman)))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Bewilder.OptionVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Bewilder.OptionVision.GetFloat());
            player.RpcSetCustomRole(CustomRoles.Bewilder);
            Utils.NotifyRoles(player);
        }

        // 为漫步者的凶手
        if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Rambler) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId && !x.Is(CustomRoles.Rambler)))
        {
            Main.AllPlayerSpeed[player.PlayerId] = Rambler.OptionSpeed.GetFloat();
            player.RpcSetCustomRole(CustomRoles.Rambler);
            Utils.NotifyRoles(player);
        }

        //最好的请过来
        if (Non_Villain.ComeAndAwayList != null)
        if (Non_Villain.ComeAndAwayList.Contains(player.PlayerId))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, 5f);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, 5f);
            Utils.NotifyRoles(player);
        }
        // 投掷傻瓜蛋啦！！！！！
        if (Grenadier.IsBlinding(player))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Grenadier.OptionCauseVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Grenadier.OptionCauseVision.GetFloat());
        }

        AURoleOptions.EngineerCooldown = Mathf.Max(0.01f, AURoleOptions.EngineerCooldown);

        if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
        {
            AURoleOptions.KillCooldown = Mathf.Max(0.01f, killCooldown);
        }

        if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
        {
            AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 3f);
        }

        state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
        if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead)
            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
        if (Options.AdditionalEmergencyCooldown.GetBool() &&
            Options.AdditionalEmergencyCooldownThreshold.GetInt() <= Utils.AllAlivePlayersCount)
        {
            opt.SetInt(
                Int32OptionNames.EmergencyCooldown,
                Options.AdditionalEmergencyCooldownTime.GetInt());
        }
        if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetValue() <= Options.UsedButtonCount)
        {
            opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
        }
        MeetingTimeManager.ApplyGameOptions(opt);


        AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
        AURoleOptions.ProtectionDurationSeconds = 0f;
        return opt;
    }

    public override bool AmValid()
    {
        return base.AmValid() && player != null && !player.Data.Disconnected && Main.RealOptionsData != null;
    }
}