using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using Epic.OnlineServices;
using HarmonyLib;
using Hazel;
using InnerNet;
using MS.Internal.Xml.XPath;
using Rewired.Utils.Platforms.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using TONEX.Modules;
using TONEX.Modules.SoundInterface;
using TONEX.Roles.AddOns.Common; 
using TONEX.Roles.AddOns.Crewmate;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using TONEX.Roles.Crewmate;
using TONEX.Roles.Ghost.Crewmate;
using TONEX.Roles.Ghost.Impostor;
using TONEX.Roles.Ghost.Neutral;
using TONEX.Roles.Impostor;
using TONEX.Roles.Neutral;
using TONEX.Roles.Vanilla;
using UnityEngine;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem.PlaybackState;
using TONEX.MoreGameModes;

namespace TONEX;


#region 击杀事件
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))] // Local Side Click Kill Button
class CmdCheckMurderPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
       // if (Main.AssistivePluginMode.Value) return true;
        Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}", "CmdCheckMurder");

        if (AmongUsClient.Instance.AmHost && GameStates.IsModHost)
            __instance.CheckMurder(target);
        else
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.CheckMurder, SendOption.Reliable, -1);
            messageWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }

        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
class CheckMurderPatch
{
    public static Dictionary<byte, float> TimeSinceLastKill = new();
    public static void Update()
    {
        if (Main.AssistivePluginMode.Value) return;
        for (byte i = 0; i < 15; i++)
        {
            if (TimeSinceLastKill.ContainsKey(i))
            {
                TimeSinceLastKill[i] += Time.deltaTime;
                if (15f < TimeSinceLastKill[i]) TimeSinceLastKill.Remove(i);
            }
        }
    }
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        
        if (Main.AssistivePluginMode.Value) return true;
        if (!AmongUsClient.Instance.AmHost) return false;
        // 処理は全てCustomRoleManager側で行う
        if (!CustomRoleManager.OnCheckMurder(__instance, target))
        {
            // キル失敗
            __instance.RpcMurderPlayer(target, false);
        }

        return false;
    }

    // 不正キル防止チェック
    public static bool CheckForInvalidMurdering(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        // 检查凶手是否已经死亡
        if (!killer.IsAlive())
        {
            Logger.Info($"{killer.GetNameWithRole()}因为已经死亡而取消了杀人行为。", "CheckMurder");
            return false;
        }
        // 检查目标是否处于可以被杀的状态
        if (
            // 确认PlayerData不为null
            target.Data == null ||
            // 检查目标的状态
            target.inVent ||
            target.MyPhysics.Animations.IsPlayingEnterVentAnimation() ||
            target.MyPhysics.Animations.IsPlayingAnyLadderAnimation() ||
            target.inMovingPlat)
        {
            Logger.Info("目标当前处于无法被杀的状态。", "CheckMurder");
            return false;
        }
        // 检查目标是否已经死亡
        if (!target.IsAlive())
        {
            Logger.Info("目标已经死亡，取消了杀人行为。", "CheckMurder");
            return false;
        }
        // 检查是否处于会议中，如果是则取消杀人行为
        if (MeetingHud.Instance != null)
        {
            Logger.Info("因为会议已经开始，取消了杀人行为。", "CheckMurder");
            return false;
        }
        // 根据游戏模式设定的时间间隔检查是否连续杀人
        var device = Options.CurrentGameMode is CustomGameMode.HotPotato or CustomGameMode.InfectorMode or CustomGameMode.FFA ? 3000f : 2000f;
        float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / device * 6f); //※AmongUsClient.Instance.Ping的值是毫秒(ms)，因此要除以1000
                                                                                     // 如果距离上次杀人时间不足设定的最小时间，则取消杀人行为
        if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
        {
            Logger.Info("因为距离上次杀人时间太短，取消了杀人行为。", "CheckMurder");
            return false;
        }
        TimeSinceLastKill[killer.PlayerId] = 0f;

        // 检查是否是可以执行杀人操作的玩家（排除远程杀人）
        if (!info.IsFakeSuicide && !killer.CanUseKillButton())
        {
            return false;
        }
        //FFA
        if (Options.CurrentGameMode == CustomGameMode.FFA)
        {
            FFAManager.OnPlayerAttack(killer, target);
            return false;
        }

        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
class MurderPlayerPatch
{

    private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.MurderPlayer));
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] MurderResultFlags resultFlags, ref bool __state /* 成功したキルかどうか */ )
    {
        if (GameStates.IsLobby && !GameStates.IsFreePlay)
        {
            RPC.NotificationPop(GetString("Warning.RoomBroken"));
        }
        if (Main.AssistivePluginMode.Value) return true;
        logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}({resultFlags})");
        var isProtectedByClient = resultFlags.HasFlag(MurderResultFlags.DecisionByHost) && target.IsProtected();
        var isProtectedByHost = resultFlags.HasFlag(MurderResultFlags.FailedProtected);
        var isFailed = resultFlags.HasFlag(MurderResultFlags.FailedError);
        var isSucceeded = __state = !isProtectedByClient && !isProtectedByHost && !isFailed;
        if (isProtectedByClient)
        {
            logger.Info("守護されているため，キルは失敗します");
        }
        if (isProtectedByHost)
        {
            logger.Info("守護されているため，击杀已由房主取消");
        }
        if (isFailed)
        {
            logger.Info("击杀已由房主取消");
        }

        if (isSucceeded)
        {
            if (target.shapeshifting)
            {
                //シェイプシフトアニメーション中
                //アニメーション時間を考慮して1s、加えてクライアントとのラグを考慮して+0.5s遅延する
                _ = new LateTask(
                    () =>
                    {
                        if (GameStates.IsInTask)
                        {
                            target.RpcShapeshift(target, false);
                        }
                    },
                    1.5f, "RevertShapeshift");
            }
            else
            {
                if (Main.CheckShapeshift.TryGetValue(target.PlayerId, out var shapeshifting) && shapeshifting)
                {
                    //シェイプシフト強制解除
                    target.RpcShapeshift(target, false);
                }
            }
            if (!(target.GetRealKiller()?.Is(CustomRoles.Skinwalker) ?? false))
                Camouflage.RpcSetSkin(target, ForceRevert: true, RevertToDefault: true);

        }

        return true;


    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, bool __state)
    {
        // キルが成功していない場合，何もしない
        if (Main.AssistivePluginMode.Value) return;
        if (!__state)
        {
            return;
        }
        if (target.AmOwner) RemoveDisableDevicesPatch.UpdateDisableDevices();
        if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;
        //以降ホストしか処理しない
        // 処理は全てCustomRoleManager側で行う
        CustomRoleManager.OnMurderPlayer(__instance, target);

        //看看UP是不是被首刀了
        if (Main.FirstDied == byte.MaxValue && target.Is(CustomRoles.YouTuber))
        {
            CustomSoundsManager.RPCPlayCustomSoundAll("Congrats");
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.YouTuber); //UP主被首刀了，哈哈哈哈哈
            CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
        }

        //记录首刀
        if (Main.FirstDied == byte.MaxValue)
            Main.FirstDied = target.PlayerId;
    }
}
#endregion

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.UseClosest))]
class UsePatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        if (Main.AssistivePluginMode.Value) return true;
        if ((!__instance.GetRoleClass()?.OnUse() ?? false)) return false;
        else return true;
    }
}

#region 变形事件
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckShapeshift))]
public static class PlayerControlCheckShapeshiftPatch
{
    private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.CheckShapeshift));

    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool shouldAnimate)
    {
        if (Main.AssistivePluginMode.Value) return true;
        if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost)
        {
            return false;
        }
        if (__instance.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Shapeshift, ExtendedPlayerControl.PlayerActionInUse.All)) return false;

        // 無効な変身を弾く．これより前に役職等の処理をしてはいけない
        if (!CheckInvalidShapeshifting(__instance, target, shouldAnimate))
        {
            __instance.RpcRejectShapeshift();
            return false;
        }
        // 役職の処理
        if (!__instance.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Shapeshift, ExtendedPlayerControl.PlayerActionInUse.Skill))
        {
            __instance.DisableAction(target);
            target.DisableAction(__instance);
            var role = __instance.GetRoleClass();
            if (role?.OnCheckShapeshift(target, ref shouldAnimate) == false)
            {
                if (role.CanDesyncShapeshift)
                {
                    __instance.RpcSpecificRejectShapeshift(target, shouldAnimate);
                }
                else
                {
                    __instance.RpcRejectShapeshift();
                }
                return false;
            }


            __instance.RpcShapeshift(target, shouldAnimate);
        }
        return false;
    }
    private static bool CheckInvalidShapeshifting(PlayerControl instance, PlayerControl target, bool animate)
    {
        logger.Info($"Checking shapeshift {instance.GetNameWithRole()} -> {(target == null || target.Data == null ? "(null)" : target.GetNameWithRole())}");

        if (!target || target.Data == null)
        {
            logger.Info("targetがnullのため変身をキャンセルします");
            return false;
        }
        if (!instance.IsAlive())
        {
            logger.Info("変身者が死亡しているため変身をキャンセルします");
            return false;
        }
        // RoleInfoによるdesyncシェイプシフター用の判定を追加
        if (instance.Data.Role.Role != RoleTypes.Shapeshifter && instance.GetCustomRole().GetRoleInfo()?.BaseRoleType?.Invoke() != RoleTypes.Shapeshifter)
        {
            logger.Info("変身者がシェイプシフターではないため変身をキャンセルします");
            return false;
        }
        if (instance.Data.Disconnected)
        {
            logger.Info("変身者が切断済のため変身をキャンセルします");
            return false;
        }
        if (target.IsMushroomMixupActive() && animate)
        {
            logger.Info("キノコカオス中のため変身をキャンセルします");
            return false;
        }
        if (MeetingHud.Instance && animate)
        {
            logger.Info("会議中のため変身をキャンセルします");
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
class ShapeshiftPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!Main.AssistivePluginMode.Value)
        {
            Logger.Info($"{__instance?.GetNameWithRole()} => {target?.GetNameWithRole()}", "Shapeshift");

            var shapeshifter = __instance;
            var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

            if (shapeshifter.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Shapeshift, ExtendedPlayerControl.PlayerActionInUse.All)) return;

            if (!(shapeshifter.IsEaten() && shapeshifter.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Shapeshift, ExtendedPlayerControl.PlayerActionInUse.Skill)))
                if (Main.CheckShapeshift.TryGetValue(shapeshifter.PlayerId, out var last) && last == shapeshifting)
                {
                    Logger.Info($"{__instance?.GetNameWithRole()}:Cancel Shapeshift.Prefix", "Shapeshift");
                    return;
                }

            Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
            Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

            if (!(shapeshifter.IsEaten() && shapeshifter.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Shapeshift, ExtendedPlayerControl.PlayerActionInUse.Skill)))
                shapeshifter.GetRoleClass()?.OnShapeshift(target);
            if (!(shapeshifter.IsEaten() && shapeshifter.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Shapeshift, ExtendedPlayerControl.PlayerActionInUse.Skill)))
                shapeshifter.GetRoleClass()?.OnShapeshiftWithUsePet(target);

            if (!AmongUsClient.Instance.AmHost) return;

            if (!shapeshifting) Camouflage.RpcSetSkin(__instance);

            //変身解除のタイミングがずれて名前が直せなかった時のために強制書き換え
            if (!shapeshifting)
            {
                _ = new LateTask(() =>
                {
                    Utils.NotifyRoles(NoCache: true);
                },
                1.2f, "ShapeShiftNotify");
            }
        }
    }
}
#endregion

#region 会议事件
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
class ReportDeadBodyPatch
{
    public static Dictionary<byte, bool> CanReport;
    public static Dictionary<byte, List<NetworkedPlayerInfo>> WaitReport = new();
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
    {
        if (Main.AssistivePluginMode.Value) return true;
        if (GameStates.IsMeeting) return false;
        if (Options.DisableMeeting.GetBool()) return false;
        if (Options.CurrentGameMode is CustomGameMode.HotPotato or CustomGameMode.InfectorMode or CustomGameMode.FFA) return false; 
        if (__instance.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Report, ExtendedPlayerControl.PlayerActionInUse.All))
        {
            WaitReport[__instance.PlayerId].Add(target);
            Logger.Warn($"{__instance.GetNameWithRole()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");
            return false;
        }
        Logger.Info($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");
        if (!AmongUsClient.Instance.AmHost) return true;

        //通報者が死んでいる場合、本処理で会議がキャンセルされるのでここで止める
        if (__instance.Data.IsDead) return false;

        if (Options.SyncButtonMode.GetBool() && target == null)
        {
            Logger.Info("最大:" + Options.SyncedButtonCount.GetInt() + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
            if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
            {
                Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                return false;
            }
        }
        // 对于仅仅是报告的处理

        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
        {
            if (!__instance.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Report, ExtendedPlayerControl.PlayerActionInUse.Skill))
            {
                if (role.OnCheckReportDeadBody(__instance, target) == false)
                {
                    Logger.Info($"会议被 {role.Player.GetNameWithRole()} 取消", "ReportDeadBody");
                    return false;
                }
            }
            else
            {
                Logger.Info($" {role.Player.GetNameWithRole()} 技能被禁用", "ReportDeadBody");
            }
        }
        foreach (var item in CustomRoleManager.AllActiveAddons.Values)
        {
            if (!item.Any_Addons(role =>
            {
                if (!__instance.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Report, ExtendedPlayerControl.PlayerActionInUse.Skill))
                {
                    if (role.OnCheckReportDeadBody(__instance, target) == false)
                    {
                        Logger.Info($"会议被 {role.Player.GetNameWithRole()} 取消", "ReportDeadBody");
                        return false;
                    }
                }
                else
                {
                    Logger.Info($" {role.Player.GetNameWithRole()} 技能被禁用", "ReportDeadBody");
                }
                return true;
            }))
            {
                return false;
            }
        }

        //=============================================
        //以下、ボタンが押されることが確定したものとする。
        //=============================================

        if (Options.SyncButtonMode.GetBool() && target == null)
        {
            Options.UsedButtonCount++;
            if (Options.SyncedButtonCount.GetFloat() == Options.UsedButtonCount)
            {
                Logger.Info("使用可能ボタン回数が最大数に達しました。", "ReportDeadBody");
            }
        }

        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
        {
            role.OnReportDeadBody(__instance, target);
        }

        Main.AllPlayerControls
                    .Where(pc => Main.CheckShapeshift.ContainsKey(pc.PlayerId))
                    .Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true));
        MeetingTimeManager.OnReportDeadBody();

        Utils.NotifyRoles(isForMeeting: true, NoCache: true);

        Utils.SyncAllSettings();
        
        if (target != null)
            if (target.Object.GetRealKiller() != null && target.Object.GetRealKiller().Is(CustomRoles.Spiders))
            {
                Main.AllPlayerSpeed[__instance.PlayerId] = Spiders.OptionSpeed.GetFloat();
                __instance.MarkDirtySettings();
            }
        return true;
    }
    public static async void ChangeLocalNameAndRevert(string name, int time)
    {
        //async Taskじゃ警告出るから仕方ないよね。
        var revertName = PlayerControl.LocalPlayer.name;
        PlayerControl.LocalPlayer.RpcSetNameEx(name);
        await Task.Delay(time);
        PlayerControl.LocalPlayer.RpcSetNameEx(revertName);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
public static class PlayerControlStartMeetingPatch
{
    public static void Prefix()
    {
        if (!Main.AssistivePluginMode.Value)
        foreach (var kvp in PlayerState.AllPlayerStates)
        {
            var pc = Utils.GetPlayerById(kvp.Key);
            kvp.Value.LastRoom = pc.GetPlainShipRoom();
        }
    }
}
#endregion

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
class FixedUpdatePatch
{
    private static StringBuilder Mark = new(20);
    private static StringBuilder Suffix = new(120);
    private static int LevelKickBufferTime = 10;
    private static int NoModKickBufferTime = 100;
    public static void Postfix(PlayerControl __instance)
    {
        var player = __instance;
        if (__instance == null || __instance.PlayerId == 255) return;
        if (Main.AssistivePluginMode.Value && __instance != null)
        {

            if (GameStates.IsLobby)
            {
                if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver))
                {
                    if (Main.ForkId != ver.forkId)
                        __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.5>{ver.forkId}</size>\n{__instance?.name}</color>";
                    else if (Main.version.CompareTo(ver.version) == 0)
                        __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#31D5BA>{__instance.name}</color>" : $"<color=#ffff00><size=1.5>{ver.tag}</size>\n{__instance?.name}</color>";
                    else
                        __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.5>v{ver.version}</size>\n{__instance?.name}</color>";
                }
                else if (__instance == PlayerControl.LocalPlayer)
                {
                    __instance.cosmetics.nameText.text = $"<color=#31D5BA>{__instance?.name}</color>";
                }
                else
                {
                    __instance.cosmetics.nameText.text = $"<color=#E1E0B3>{__instance?.name}</color>";
                }
            }
            else if (GameStates.IsInGame)
            {
                if (Main.playerVersion.ContainsKey(0))
                {
                    Main.playerVersion.TryGetValue(0, out var ver);
                    if (Main.ForkId != ver.forkId)
                        return;
                }

                var roleType = __instance.Data.Role.Role;
                var cr = roleType.GetCustomRoleTypes();
                var color = Utils.GetRoleColorCode(cr);

                if (__instance == PlayerControl.LocalPlayer || (PlayerControl.LocalPlayer.Data.IsDead && __instance.Data.IsDead))
                {
                    __instance.cosmetics.nameText.text = $"<color={color}><size=80%>{Translator.GetRoleString(cr.ToString())}</size>\n\r{__instance?.name}</color>";
                }
                else if (PlayerControl.LocalPlayer.Data.Role.Role.GetCustomRoleTypes().IsImpostor() && cr.IsImpostor())
                {
                    if (PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        __instance.cosmetics.nameText.text = $"<color={color}><size=80%>{Translator.GetRoleString(cr.ToString())}</size>\n\r{__instance?.name}</color>";
                    }
                    else
                    {
                        __instance.cosmetics.nameText.text = $"<color=#FF1919>{__instance?.name}</color>";
                    }
                }
                else
                {
                    __instance.cosmetics.nameText.text = $"<color=#FFFFFF>{__instance?.name}</color>";
                }

            }

            return;

        }
           

        if (player.AmOwner && player.IsEACPlayer() && (GameStates.IsLobby || GameStates.IsInGame) && GameStates.IsOnlineGame)
            AmongUsClient.Instance.ExitGame(DisconnectReasons.Error);


        if (Utils.LocationLocked&& PlayerControl.LocalPlayer == player)
        {
            player.RpcTeleport(Utils.LocalPlayerLastTp);
        }

        if (!GameStates.IsModHost) return;
        bool localplayer = __instance.PlayerId == PlayerControl.LocalPlayer.PlayerId;
        if (!GameStates.IsLobby && localplayer)
            CustomNetObject.FixedUpdate();

        Zoom.OnFixedUpdate();
        NameNotifyManager.OnFixedUpdate(player);
        TargetArrow.OnFixedUpdate(player);
        LocateArrow.OnFixedUpdate(player);

        CustomRoleManager.OnFixedUpdate(player);
           

        if (AmongUsClient.Instance.AmHost)
        {//実行クライアントがホストの場合のみ実行
#if  RELEASE
            if (GameStates.IsLobby && ((ModUpdater.hasUpdate && ModUpdater.forceUpdate) || ModUpdater.isBroken || !Main.AllowPublicRoom || !VersionChecker.IsSupported || !Main.IsPublicAvailableOnThisVersion) && AmongUsClient.Instance.IsGamePublic)
                AmongUsClient.Instance.ChangeGamePublic(false);
#endif

            if (GameStates.IsInTask && ReportDeadBodyPatch.CanReport[__instance.PlayerId] && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
            {
                var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
                ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
                Logger.Info($"{__instance.GetNameWithRole()}:通報可能になったため通報処理を行います", "ReportDeadbody");
                __instance.ReportDeadBody(info);
            }

            //踢出低等级的人
            if (GameStates.IsLobby && !player.AmOwner && Options.KickLowLevelPlayer.GetInt() != 0 && (
                (player.Data.PlayerLevel != 0 && player.Data.PlayerLevel < Options.KickLowLevelPlayer.GetInt()) ||
                player.Data.FriendCode == ""
                ))
            {
                LevelKickBufferTime--;
                if (LevelKickBufferTime <= 0)
                {
                    LevelKickBufferTime = 100;
                    Utils.KickPlayer(player.GetClientId(), false, "LowLevel");
                    string msg = string.Format(GetString("KickBecauseLowLevel"), player.GetRealName().RemoveHtmlTags());
                    RPC.NotificationPop(msg);
                    Logger.Info(msg, "LowLevel Kick");
                }
            }
            DoubleTrigger.OnFixedUpdate(player);

            //ターゲットのリセット
            if (GameStates.IsInTask && player.IsAlive() && Options.LadderDeath.GetBool())
            {
                FallFromLadder.FixedUpdate(player);
            }

            if (GameStates.IsInGame)
            {
                //Lovers.LoversSuicide();
                AdmirerLovers.AdmirerLoversSuicide();
                AkujoLovers.AkujoLoversSuicide();
                CupidLovers.CupidLoversSuicide();
            }

            if (GameStates.IsInGame && player.AmOwner)
                DisableDevice.FixedUpdate();

            if (!Main.DoBlockNameChange)
                NameTagManager.ApplyFor(player);
        }
        //LocalPlayer専用
        if (__instance.AmOwner)
        {
            //覆盖杀手目标处理
            if (GameStates.IsInTask && !__instance.Is(CustomRoleTypes.Impostor) && __instance.CanUseKillButton() && !__instance.Data.IsDead)
            {
                var players = __instance.GetPlayersInAbilityRangeSorted(false);
                PlayerControl closest = players.Count <= 0 ? null : players[0];
                HudManager.Instance.KillButton.SetTarget(closest);
            }
        }

        //役職テキストの表示
        var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
        var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
        var colorblindtext = __instance.cosmetics.colorBlindText.text;
        if (RoleText != null && __instance != null)
        {
            if (GameStates.IsLobby)
            {
                if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver))
                {
                    if (Main.ForkId != ver.forkId) // フォークIDが違う場合
                        __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.5>{ver.forkId}</size>\n{__instance?.name}</color>";
                    else if (Main.version.CompareTo(ver.version) == 0)
                        __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#31D5BA>{__instance.name}</color>" : $"<color=#ffff00><size=1.5>{ver.tag}</size>\n{__instance?.name}</color>";
                    else __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.5>v{ver.version}</size>\n{__instance?.name}</color>";
                }
                else
                {
                    __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                    if (Options.IsAllCrew)
                    {
                        NoModKickBufferTime--;
                        if (NoModKickBufferTime <= 0)
                        {
                            NoModKickBufferTime = 100;
                            Utils.KickPlayer(player.GetClientId(), false, "NoMod");
                            string msg = string.Format(GetString("Message.NotInstalled"), player.GetRealName().RemoveHtmlTags());
                            RPC.NotificationPop(msg);
                            Logger.Info($"{Utils.GetClientById(player.PlayerId).PlayerName}无模组", "BAN");
                        }
                    }
                }
            }
            if (GameStates.IsInGame)
            {
                //if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                //{
                //    var hasRole = main.AllPlayerCustomRoles.TryGetValue(__instance.PlayerId, out var role);
                //    if (hasRole) RoleTextData = Utils.GetRoleTextHideAndSeek(__instance.Data.Role.Role, role);
                //}
                if (Options.CurrentGameMode == CustomGameMode.FFA) RoleText.text = string.Empty;

                if (__instance.AmOwner || Options.CurrentGameMode == CustomGameMode.FFA) RoleText.enabled = true;

                (RoleText.enabled, RoleText.text) = Utils.GetRoleNameAndProgressTextData(PlayerControl.LocalPlayer, __instance);
                if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                {
                    RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                    if (!__instance.AmOwner) __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                }

                //変数定義
                var seer = PlayerControl.LocalPlayer;
                var seerRole = seer.GetRoleClass();
                var seerAddon = seer.GetAddonClasses();
                var target = __instance;
                string RealName;
                Mark.Clear();
                Suffix.Clear();

                //名前変更
                RealName = target.GetRealName();

                // 名前色変更処理
                //自分自身の名前の色を変更
                if (target.AmOwner && GameStates.IsInTask)
                { //targetが自分自身
                    if (Options.CurrentGameMode == CustomGameMode.FFA)
                        FFAManager.GetNameNotify(target, ref RealName);

                    if (seer.IsEaten())
                        RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pelican), GetString("EatenByPelican"));
                    if (NameNotifyManager.GetNameNotify(target, out var name))
                        RealName = name;
                }

                //NameColorManager準拠の処理
                RealName = RealName.ApplyNameColorData(seer, target, false);

                // 模组端色盲文字处理

                NiceGrenadier.ChangeColorBlindText();
                EvilGrenadier.ChangeColorBlindText();

                //seer役職が対象のMark
                Mark.Append(seerRole?.GetMark(seer, target, false));
                seerAddon.Do_Addons(x => Mark.Append(x?.GetMark(seer, target, false)));

                //seerに関わらず発動するMark
                Mark.Append(CustomRoleManager.GetMarkOthers(seer, target, false));

                //ハートマークを付ける(会議中MOD視点)
                Lovers.Marks(__instance, ref Mark);
                AdmirerLovers.Marks(__instance, ref Mark);
                AkujoLovers.Marks(__instance, ref Mark);
                AkujoFakeLovers.Marks(__instance, ref Mark);
                CupidLovers.Marks(__instance, ref Mark);
                Neptune.Marks(__instance, ref Mark);

                if (!seer.IsModClient())
                {
                    Suffix.Append(seerRole?.GetLowerText(seer, target));
                    seerAddon.Do_Addons(x => Suffix.Append(x?.GetLowerText(seer, target)));

                    //seerに関わらず発動するLowerText
                    Suffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target));
                }
                //seer役職が対象のSuffix
                Suffix.Append(seerRole?.GetSuffix(seer, target));
                seerAddon?.Do_Addons(x => Suffix.Append(x?.GetSuffix(seer, target)));

                //seerに関わらず発動するSuffix
                Suffix.Append(CustomRoleManager.GetSuffixOthers(seer, target));

                if (Options.CurrentGameMode == CustomGameMode.FFA)
                    Suffix.Append(FFAManager.GetPlayerArrow(seer, target));
                /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                    Mark = isBlocked ? "(true)" : "(false)";
                }*/
                if ((Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool()) || Concealer.IsHidding)
                    RealName = $"<size=0>{RealName}</size> ";

                string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.MedicalExaminer), Utils.GetVitalText(target.PlayerId))})" : "";
                //Mark・Suffixの適用
                target.cosmetics.nameText.text = $"{RealName}{DeathReason}{Mark}";

                if (Suffix.ToString() != "")
                {
                    //名前が2行になると役職テキストを上にずらす必要がある
                    RoleText.transform.SetLocalY(0.35f);
                    target.cosmetics.nameText.text += "\r\n" + Suffix.ToString();

                }
                else
                {
                    //役職テキストの座標を初期値に戻す
                    RoleText.transform.SetLocalY(0.2f);
                }
            }
            else
            {
                //役職テキストの座標を初期値に戻す
                RoleText.transform.SetLocalY(0.2f);
            }
        }
    }
    //FIXME: 役職クラス化のタイミングで、このメソッドは移動予定
    
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
class PlayerStartPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
        roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
        roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        roleText.transform.localScale = new(1f, 1f, 1f);
        roleText.fontSize = Main.RoleTextSize;
        roleText.text = "RoleText";
        roleText.gameObject.name = "RoleText";
        roleText.enabled = false;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
class SetColorPatch
{
    public static bool IsAntiGlitchDisabled = false;
    public static bool Prefix(PlayerControl __instance, int bodyColor)
    {
        //色変更バグ対策
        if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
        return true;
    }
}

#region 管道事件
[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
class EnterVentPatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
    {
        if (Main.AssistivePluginMode.Value) return;
        Main.LastEnteredVent.Remove(pc.PlayerId);
        Main.LastEnteredVent.Add(pc.PlayerId, __instance);
        Main.LastEnteredVentLocation.Remove(pc.PlayerId);
        Main.LastEnteredVentLocation.Add(pc.PlayerId, pc.GetTruePosition());
    }
}
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
class CoEnterVentPatch
{
    public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (Main.AssistivePluginMode.Value) return true;
        Logger.Info($"{__instance.myPlayer.GetNameWithRole()} CoEnterVent: {id}", "CoEnterVent");
        //FFA
        if (Options.CurrentGameMode == CustomGameMode.FFA && FFAManager.CheckCoEnterVent(__instance, id))
            return true;
       
        var user = __instance.myPlayer;
        if (user.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.EnterVent) 
            || user.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.EnterVent, ExtendedPlayerControl.PlayerActionInUse.Skill) 
            && user.GetCustomRole() is CustomRoles.EvilInvisibler or CustomRoles.Arsonist or CustomRoles.Veteran or CustomRoles.NiceTimePauser
            or CustomRoles.TimeMaster or CustomRoles.Instigator or CustomRoles.Paranoia or CustomRoles.Mayor or CustomRoles.DoveOfPeace
            or CustomRoles.NiceGrenadier or CustomRoles.Akujo or CustomRoles.Miner)
        {
            _ = new LateTask(() =>
            {
                __instance.RpcBootFromVent(id);
            }, 0.5f, "Cancel Vent");
        }

        if ((!user.GetRoleClass()?.OnEnterVent(__instance, id) ?? false) 
            || (user.Data.Role.Role != RoleTypes.Engineer //非工程师
            && !user.CanUseImpostorVentButton()) //也不能使用内鬼管道
        )
        {
      
            _ = new LateTask(() =>
            {
                __instance.RpcBootFromVent(id);
            }, 0.5f, "Cancel Vent");
        }
        if ((!user.GetRoleClass()?.OnEnterVentWithUsePet(__instance, id) ?? false)
            || (user.Data.Role.Role != RoleTypes.Engineer //非工程师
            && !user.CanUseImpostorVentButton()) //也不能使用内鬼管道
        )
        {
            
            _ = new LateTask(() =>
            {
                __instance.RpcBootFromVent(id);
            }, 0.5f, "Cancel Vent");
        }
        return true;
    }
}
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoExitVent))]
class CoExitVentPatch
{
    public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (Main.AssistivePluginMode.Value) return true;
        Logger.Info($"{__instance.myPlayer.GetNameWithRole()} CoExitVent: {id}", "CoExitVent");
        
        var user = __instance.myPlayer;
        if (Options.CurrentGameMode == CustomGameMode.FFA && FFAManager.FFA_DisableVentingWhenKCDIsUp.GetBool())
                    FFAManager.CoExitVent(user);
        
        if (user.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.ExitVent) 
            || user.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.ExitVent, ExtendedPlayerControl.PlayerActionInUse.Skill))
        {
            _ = new LateTask(() =>
            {
                int clientId = user.GetClientId();
                MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                writer2.Write(id);
                AmongUsClient.Instance.FinishRpcImmediately(writer2);
            }, 0.5f, "Fix DesyncImpostor Stuck");
            return false;
        }
        if ((!user.GetRoleClass()?.OnExitVent(__instance, id) ?? false)
             || (user.Data.Role.Role != RoleTypes.Engineer //非工程师
             && !user.CanUseImpostorVentButton()) //也不能使用内鬼管道
         )
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.EnterVent, SendOption.Reliable, -1);
            writer.WritePacked(127);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            _ = new LateTask(() =>
            {
                int clientId = user.GetClientId();
                MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.EnterVent, SendOption.Reliable, clientId);
                writer2.Write(id);
                AmongUsClient.Instance.FinishRpcImmediately(writer2);
            }, 0.5f, "Fix DesyncImpostor Stuck");
            return false;
        }
        return true;
    }
}

#endregion

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetName))]
class SetNamePatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] string name)
    {
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
class PlayerControlCompleteTaskPatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        if (Main.AssistivePluginMode.Value) return true;
        var pc = __instance;

        Logger.Info($"TaskComplete:{pc.GetNameWithRole()}", "CompleteTask");
        var taskState = pc.GetPlayerTaskState();
        taskState.Update(pc);

        var roleClass = pc.GetRoleClass();
        var addonClasses = pc.GetAddonClasses();
        bool ret = true;
        if (roleClass != null && roleClass.OnCompleteTask(out bool cancel))
        {
            ret = cancel;
        }

        bool cancel2 = true;
        if (addonClasses != null && addonClasses.Any(x=>x.OnCompleteTask(out cancel2)))
        {
            ret = cancel2;
        }
        //属性クラスの扱いを決定するまで仮置き
        ret &= Workhorse.OnCompleteTask(pc);
        ret &= Capitalist.OnCompleteTask(pc);

        Utils.NotifyRoles();
        return ret;
    }
    public static void Postfix()
    {
        if (Main.AssistivePluginMode.Value) return;
        //人外のタスクを排除して再計算
        GameData.Instance.RecomputeTaskCounts();
        Logger.Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "TaskState.Update");
    }
}

#region 保护事件
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
class PlayerControlProtectPlayerPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (Main.AssistivePluginMode.Value) return;
        var player = __instance;
        if (!target.IsEaten())
        {
            if (!(player.GetRoleClass()?.OnProtectPlayer(target) ?? false))
            {
                Logger.Info($"凶手阻塞了击杀", "CheckMurder");
                return;
            }
        }


        Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}", "ProtectPlayer");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RemoveProtection))]
class PlayerControlRemoveProtectionPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (Main.AssistivePluginMode.Value) return;
        Logger.Info($"{__instance.GetNameWithRole()}", "RemoveProtection");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
class CheckProtectPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (Main.AssistivePluginMode.Value) return true;
        if (!AmongUsClient.Instance.AmHost) return false;
        Logger.Info("CheckProtect発生: " + __instance.GetNameWithRole() + "=>" + target.GetNameWithRole(), "CheckProtect");
        if (__instance.Is(CustomRoles.Sheriff))
        {
            if (__instance.Data.IsDead)
            {
                Logger.Info("守護をブロックしました。", "CheckProtect");
                return false;
            }
        }
        return true;
    }
}
#endregion

#region 设置职业 / SetRole
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
class PlayerControlRpcSetRolePatch
{
    public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType, ref bool canOverrideRole )
    {
        if (Main.AssistivePluginMode.Value) return true;
        canOverrideRole = false;
        var target = __instance;
        var targetName = __instance.GetNameWithRole();
        Logger.Info($"{targetName} =>{roleType}", "PlayerControl.RpcSetRole");
        if (!ShipStatus.Instance.enabled) return true;
        if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost or RoleTypes.GuardianAngel)
        {
            var targetIsKiller = target.GetRoleClass() is IKiller;
            var ghostRoles = new Dictionary<PlayerControl, RoleTypes>();
            foreach (var seer in Main.AllPlayerControls)
            {
                var self = seer.PlayerId == target.PlayerId;
                var seerIsKiller = seer.GetRoleClass() is IKiller;

                
                if(target.Is(CustomRoles.EvilAngel) || target.Is(CustomRoles.Specterraid) || target.Is(CustomRoles.InjusticeSpirit))
                {
                   ghostRoles[seer] = RoleTypes.GuardianAngel;
                }
                else if ((self && targetIsKiller) || (!seerIsKiller && target.Is(CustomRoleTypes.Impostor)))
                {
                    ghostRoles[seer] = RoleTypes.Impostor;
                }
                else
                {
                    ghostRoles[seer] = RoleTypes.CrewmateGhost;
                }
            }
            if (ghostRoles.All(kvp => kvp.Value == RoleTypes.CrewmateGhost))
            {
                roleType = RoleTypes.CrewmateGhost;
            }
            else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.ImpostorGhost))
            {
                roleType = RoleTypes.ImpostorGhost;
            }
            else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.GuardianAngel))
            {
                roleType = RoleTypes.GuardianAngel;
            }
            else
            {
                foreach ((var seer, var role) in ghostRoles)
                {
                    Logger.Info($"Desync {targetName} =>{role} for{seer.GetNameWithRole()}", "PlayerControl.RpcSetRole");
                    target.RpcSetRoleDesync(role, false,seer.GetClientId());
                }
                return false;
            }
        }
        return true;
    }
}
/*[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CoSetRole))]*/
public static class PlayerControlSetRolePatch
{
    public static bool playanima = true;
    public static bool InGameSetRole = false;

    public static void OnGameStartAndEnd()
    {
        playanima = true;
        InGameSetRole = false;

    }
//    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes role, [HarmonyArgument(1)] bool canOverride)
//    {
//        bool ghostRole = RoleManager.IsGhostRole(role);
//        if (!DestroyableSingleton<TutorialManager>.InstanceExists && __instance.roleAssigned && !ghostRole)
//        {
//            return;
//        }
//        if (!canOverride)
//        {
//            __instance.roleAssigned = true;
//        }
//        int attempts = 0;
//        while ((!__instance.Data || GameManager.Instance == null || !GameManager.Instance) && attempts < 60)
//        {
//            attempts++;
//            Effects.Wait(0.1f);
//        }
//        if (!__instance.Data)
//        {
//            Debug.LogWarning("CoSetRole timed out waiting for NetworkedPlayerInfo");
//            return;
//        }
//        if (GameManager.Instance == null || !GameManager.Instance)
//        {
//            Debug.LogWarning("CoSetRole timed out waiting for GameManager");
//            return;
//        }
//        if (ghostRole)
//        {
//            DestroyableSingleton<RoleManager>.Instance.SetRole(__instance, role);
//            __instance.Data.Role.SpawnTaskHeader(__instance);
//            if (__instance.AmOwner)
//            {
//                DestroyableSingleton<HudManager>.Instance.ReportButton.gameObject.SetActive(false);
//            }
//        }
//        else
//        {
//            __instance.RemainingEmergencies = GameManager.Instance.LogicOptions.GetNumEmergencyMeetings();
//            DestroyableSingleton<RoleManager>.Instance.SetRole(__instance, role);
//            __instance.Data.Role.SpawnTaskHeader(__instance);
//            __instance.MyPhysics.SetBodyType(__instance.BodyType);
//            if (__instance.AmOwner)
//            {
//                if (__instance.Data.Role.IsImpostor)
//                {
//                    StatsManager.Instance.IncrementStat(StringNames.StatsGamesImpostor);
//                    StatsManager.Instance.ResetStat(StringNames.StatsCrewmateStreak);
//                }
//                else
//                {
//                    StatsManager.Instance.IncrementStat(StringNames.StatsGamesCrewmate);
//                    StatsManager.Instance.IncrementStat(StringNames.StatsCrewmateStreak);
//                }
//                DestroyableSingleton<HudManager>.Instance.MapButton.gameObject.SetActive(true);
//                DestroyableSingleton<HudManager>.Instance.ReportButton.gameObject.SetActive(true);
//                DestroyableSingleton<HudManager>.Instance.UseButton.gameObject.SetActive(true);
//            }
//            if (!DestroyableSingleton<TutorialManager>.InstanceExists)
//            {
//                if (Enumerable.All<PlayerControl>(Main.AllPlayerControls, (PlayerControl pc) => pc.Data != null && (pc.roleAssigned || pc.Data.Disconnected)))
//                {
//                    foreach (var pc in Main.AllPlayerControls)
//                    {
//                        PlayerNameColor.Set(pc);
//                    }
//                    __instance.StopAllCoroutines();
//                    DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
//                    DestroyableSingleton<HudManager>.Instance.HideGameLoader();
//                }
//            }
//        }
//}
}
    #endregion

#region 死亡事件
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class PlayerControlDiePatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        if (GameStates.IsLobby && !GameStates.IsFreePlay)
        {
            RPC.NotificationPop(GetString("Warning.RoomBroken"));
        }
        return true;
    }


    public static void Postfix(PlayerControl __instance)
    {
        if (Main.AssistivePluginMode.Value) return;
        if (AmongUsClient.Instance.AmHost)
        {
            __instance.RpcSetScanner(false);
         
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.OnPlayerDeath(__instance, PlayerState.GetByPlayerId(__instance.PlayerId).DeathReason, GameStates.IsMeeting));
#if DEBUG
            if (__instance.Is(CustomRoles.Madmate))
            {
                EvilAngel.CheckForSet(__instance);
            }
            else if (__instance.Is(CustomRoles.Wolfmate) || __instance.Is(CustomRoles.Charmed))
            {
                Specterraid.CheckForSet(__instance);
            }
            else if ((__instance.Is(CustomRoleTypes.Crewmate) || __instance.Is(CustomRoleTypes.Impostor))
                && !__instance.Is(CustomRoles.Lovers) 
                && !__instance.Is(CustomRoles.AdmirerLovers)
                && !__instance.Is(CustomRoles.AkujoLovers) 
                && !__instance.Is(CustomRoles.CupidLovers)
                || __instance.Is(CustomRoleTypes.Neutral))
            {
                switch (__instance.GetCustomRole().GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Crewmate:
                        if (InjusticeSpirit.CheckForSet(__instance)) { }
                        else if (GuardianAngel.CheckForSet(__instance)) { }
                        break;
                    case CustomRoleTypes.Neutral:
                        Specterraid.CheckForSet(__instance);
                        break;
                    case CustomRoleTypes.Impostor:
                        EvilAngel.CheckForSet(__instance);
                        break;
                }
            }
#endif
            // 死者の最終位置にペットが残るバグ対応
            __instance.SetOutFit(petId:"");
        }
    }
}
#endregion

#region Fungle
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MixUpOutfit))]
public static class PlayerControlMixupOutfitPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (Main.AssistivePluginMode.Value) return;
        if (!__instance.IsAlive())
        {
            return;
        }
        // 自分がDesyncインポスターで，バニラ判定ではインポスターの場合，バニラ処理で名前が非表示にならないため，相手の名前を非表示にする
        if (
            PlayerControl.LocalPlayer.Data.Role.IsImpostor &&  // バニラ判定でインポスター
            !PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) &&  // Mod判定でインポスターではない
            PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)  // Desyncインポスター
        {
            // 名前を隠す
            __instance.cosmetics.ToggleNameVisible(false);
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckSporeTrigger))]
public static class PlayerControlCheckSporeTriggerPatch
{
    public static bool Prefix()
    {
        if (Main.AssistivePluginMode.Value) return true;
        if (Options.DisableFungleSporeTrigger.GetBool())
        {
            return false;
        }
        return true;
    }
}
#endregion

/*
 *  I have no idea how the check vanish is approved by host & server and how to reject it
 *  Suggest leaving phantom stuffs after 2.1.0
 */
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckVanish))]
class CheckVanishPatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        if (__instance.GetRoleClass()?.OnCheckVanish() == false && AmongUsClient.Instance.AmHost)
        {

            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.StartVanish, SendOption.Reliable, __instance.GetClientId());
            messageWriter.WriteNetObject(__instance);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);


            MessageWriter messageWriter1 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.StartAppear, SendOption.Reliable, __instance.GetClientId());
            messageWriter1.WriteNetObject(__instance);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter1);

           
            __instance.RpcResetAbilityCooldown();


            return false;
        }

        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckAppear))]
class CmdCheckAppearPatch
{
    public static bool Prefix([HarmonyArgument(0)] PlayerControl __instance, [HarmonyArgument(1)] bool shouldAnimate)
    {
        if (!__instance.IsEaten())
        {
            if (__instance.GetRoleClass()?.OnAppear(shouldAnimate) == false)
                return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckAppear))]
class CheckAppearPatch
{
    public static bool Prefix([HarmonyArgument(0)] PlayerControl __instance, [HarmonyArgument(1)] bool shouldAnimate)
    {
        if (!__instance.IsEaten())
        {
            if (__instance.GetRoleClass()?.OnAppear(shouldAnimate) == false)
                return false; 
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetRoleInvisibility))]
class SetRoleInvisibilityPatch
{
    public static void Postfix(PlayerControl __instance, bool isActive, bool shouldAnimate, bool playFullAnimation)
    {
        return;
    }
}

#region 名称检查
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckName))]
class PlayerControlCheckNamePatch
{
    public static void Postfix(PlayerControl __instance, string playerName)
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsLobby ) return;

        var name = playerName;
        if (!Main.AssistivePluginMode.Value)
        {
            if (Options.FormatNameMode.GetInt() == 2)
                name = Main.Get_TName_Snacks;
            else
            {
                // 删除非法字符
                name = name.RemoveHtmlTags().Replace(@"\", string.Empty).Replace("/", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\0", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty);
                // 删除超出10位的字符
                if (name.Length > 10) name = name[..10];
                // 删除Emoji
                if (Options.DisableEmojiName.GetBool()) name = Regex.Replace(name, @"\p{Cs}", string.Empty);
                // 若无有效字符则随机取名
                if (Regex.Replace(Regex.Replace(name, @"\s", string.Empty), @"[\x01-\x1F,\x7F]", string.Empty).Length < 1) name = Main.Get_TName_Snacks;
                // 替换重名
                string fixedName = name;
                int suffixNumber = 0;
                while (Main.AllPlayerNames.ContainsValue(fixedName))
                {
                    suffixNumber++;
                    fixedName = $"{name} {suffixNumber}";
                }
                if (!fixedName.Equals(name)) name = fixedName;
            }
        }
        Main.AllPlayerNames.Remove(__instance.PlayerId);
        Main.AllPlayerNames.TryAdd(__instance.PlayerId, name);
        if (Main.AssistivePluginMode.Value) return;
        if (!name.Equals(playerName))
        {
            _ = new LateTask(() =>
            {
                if (__instance == null) return;
                Logger.Warn($"规范昵称：{playerName} => {name}", "Name Format");
                //__instance.RpcSetName(name);

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.SetName, SendOption.None, -1);
                writer.Write(__instance.Data.NetId);
                writer.Write(name);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

            }, 1f, "Name Format");
        }

    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckName))]
class CmdCheckNameVersionCheckPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (AmongUsClient.Instance.AmHost)
        RPC.RpcVersionCheck();
    }
}
#endregion