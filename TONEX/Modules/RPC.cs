using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Crewmate;
using TONEX.Roles.AddOns.Common;
using static TONEX.Translator;
using TONEX.Roles.Impostor;
using TONEX.Roles.Neutral;
using TONEX.Modules.SoundInterface;
using TONEX.MoreGameModes;
using InnerNet;
namespace TONEX;

public enum CustomRPC
{
    VersionCheck = 80,
    RequestRetryVersionCheck = 81,

    SyncCustomSettings = 100,
    SetDeathReason,
    EndGame,
    PlaySound,
    SetCustomRole,
    SetNameColorData,
    SetLoversPlayers,
    SetRealKiller,
    CustomRoleSync,

    //TONX
    AntiBlackout,
    RestTONEXSetting,
    PlayCustomSound,
    SetKillTimer,
    SyncAllPlayerNames,
    SyncNameNotify,
    ShowPopUp,
    KillFlash,
    NotificationPop,
    SetKickReason,
    OnClickMeetingButton,

    //TONEX
    ColorFlash,
    SetAction,
    IsDisabledAction,
    SetAdmirerLoversPlayers,
    SetAkujoLoversPlayers,
    SetCupidLoversPlayers,
    SetRoleInGame,

    //GameMode
    SyncHpNameNotify,

    //Roles
   
    //阴阳
    SetYangPlayer,
    SetYinPlayer,
    SuicideWithAnime,
    SetMarkedPlayer,

    //医生
    SetMedicProtectList,

    //会议
    Judge,
    Guess,
    Swap,

    //魅惑者
    SetSuccubusCharmLimit,
    //控制狂
    SyncControlFreakList,
    //预言家
    SetProphetList,
    //迷你船员
    MiniAge,
    //通讯兵
    SignalPosition,
    //悬赏官
    SetRewardOfficerTarget,
    //恶猎手
    ViciousSeekerKill,
    //秃鹫
    VultureLimit,
    //闲游
    AddFeeble,
    //猎人
    SetHunterList,
    //玩家
    SetDemonHealth,
    //琥珀
    SetAmberProtectList,
    //正义掷弹兵
    SetNiceGraList,
    //邪恶掷弹兵
    SetEvilGraList,
    //起诉
    SetProsecutorList,
    //捕快
    SetDeputyList,
    //伪人
    SetSubstituteLimit,
    //基因学家
    SetGeneticistDNA2,
    //游侠
    SetRangerList,
    //国王
    SetKingList,
    //咒术师
    SetSorcererList,
    //十字军
    SetCrusaderList,

    //特效专用RPC
    FixModdedClientCNO,

    //游戏模式
    SyncFFAPlayer,
    SyncFFANameNotify,
}
public enum Sounds
{
    KillSound,
    TaskComplete,
    TaskUpdateSound,
    ImpTransform,
    Yeehawfrom,
    Test,
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class RPCHandlerPatch
{
    public static bool TrustedRpc(byte id)
 => (CustomRPC)id is CustomRPC.VersionCheck 
     or CustomRPC.RequestRetryVersionCheck 
     or CustomRPC.AntiBlackout 
     or CustomRPC.Judge 
     or CustomRPC.Swap 
     or CustomRPC.Guess 
     or CustomRPC.OnClickMeetingButton 
     or CustomRPC.PlaySound 
     or CustomRPC.IsDisabledAction 
     or CustomRPC.SetAction ;

    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {

        if (Main.AssistivePluginMode.Value) return true;
        var rpcType = (RpcCalls)callId;
        MessageReader subReader = MessageReader.Get(reader);
        //if (EAC.ReceiveRpc(__instance, callId, reader)) return false;
        Logger.Info($"{__instance?.Data?.PlayerId}({(__instance?.Data?.OwnerId == AmongUsClient.Instance.HostId ? "Host" : __instance?.Data?.PlayerName)}):{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");

        switch (rpcType)
        {
            case RpcCalls.SetName: //SetNameRPC
                subReader.ReadUInt32();
                string name = subReader.ReadString();
                if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                Logger.Info("RPC Set Name For Player: " + __instance.GetNameWithRole() + " => " + name, "SetName");
                break;
            case RpcCalls.SetRole: //SetRoleRPC
                var role = (RoleTypes)subReader.ReadUInt16();
                var canOverriddenRole = subReader.ReadBoolean();
                Logger.Info("RPC Set Role For Player: " + __instance.GetRealName() + " => " + role + " CanOverrideRole: " + canOverriddenRole, "SetRole");
                break;
            case RpcCalls.SendChat: // Free chat
                var text = subReader.ReadString();
                Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}:{text.RemoveHtmlTags()}", "ReceiveChat");
                ChatCommands.OnReceiveChat(__instance, text, out var canceled);
                if (canceled) return false;
                break;
            case RpcCalls.SendQuickChat:
                Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}:Some message from quick chat", "ReceiveChat");
                ChatCommands.OnReceiveChat(__instance, "Some message from quick chat", out var canceledQuickChat);
                if (canceledQuickChat) return false;
                break;
            case RpcCalls.StartMeeting:
                var p = Utils.GetPlayerById(subReader.ReadByte());
                Logger.Info($"{__instance.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
                break;
        }

        if (__instance.PlayerId != 0
                && Enum.IsDefined(typeof(CustomRPC), (int)callId)
                && !TrustedRpc(callId)) //ホストではなく、CustomRPCで、VersionCheckではない
        {
            Logger.Warn($"{__instance?.Data?.PlayerName}:{callId}({RPC.GetRpcName(callId)}) 已取消，因为它是由主机以外的其他人发送的。", "CustomRPC");
            if (AmongUsClient.Instance.AmHost)
            {
                if (!EAC.ReceiveInvalidRpc(__instance, callId)) return false;
                Utils.KickPlayer(__instance.GetClientId(), false, "InvalidRPC");
                Logger.Warn($"收到来自 {__instance?.Data?.PlayerName} 的不受信用的RPC，因此将其踢出。", "Kick");
                RPC.NotificationPop(string.Format(GetString("Warning.InvalidRpc"), __instance?.Data?.PlayerName));
            }
            return false;
        }

        return true;
        
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {

        if (Main.AssistivePluginMode.Value && (CustomRPC)callId is not CustomRPC.VersionCheck and not CustomRPC.RequestRetryVersionCheck) return;
        
        //CustomRPC以外は処理しない
        if (callId < (byte)CustomRPC.VersionCheck) return;

        var rpcType = (CustomRPC)callId;
        switch (rpcType)
        {
            case CustomRPC.AntiBlackout:
                if (Options.EndWhenPlayerBug.GetBool())
                {
                    Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): {reader.ReadString()} 错误，根据设定终止游戏", "Anti-black");
                    ChatUpdatePatch.DoBlockChat = true;
                    Main.OverrideWelcomeMsg = string.Format(GetString("RpcAntiBlackOutNotifyInLobby"), __instance?.Data?.PlayerName, GetString("EndWhenPlayerBug"));
                    _ = new LateTask(() =>
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutEndGame"), __instance?.Data?.PlayerName), true);
                    }, 3f, "Anti-Black Msg SendInGame");
                    _ = new LateTask(() =>
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                        GameManager.Instance.LogicFlow.CheckEndCriteria();
                        RPC.ForceEndGame(CustomWinner.Error);
                    }, 5.5f, "Anti-Black End Game");
                }
                else
                {
                    Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): Change Role Setting Postfix 错误，根据设定继续游戏", "Anti-black");
                    _ = new LateTask(() =>
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutIgnored"), __instance?.Data?.PlayerName), true);
                    }, 3f, "Anti-Black Msg SendInGame");
                }
                break;
            case CustomRPC.VersionCheck:
                try
                {
                    Version version = Version.Parse(reader.ReadString());
                    string tag = reader.ReadString();
                    string forkId = reader.ReadString();
                    Main.playerVersion[__instance.PlayerId] = new PlayerVersion(version, tag, forkId);

                    if (Main.VersionCheat.Value && __instance.OwnerId == AmongUsClient.Instance.HostId) RPC.RpcVersionCheck();

                    if (Main.VersionCheat.Value && AmongUsClient.Instance.AmHost)
                        Main.playerVersion[__instance.PlayerId] = Main.playerVersion[0];

                    // Kick Unmached Player Start
                    if (AmongUsClient.Instance.AmHost && tag != $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})")
                    {
                        if (forkId != Main.ForkId)
                            _ = new LateTask(() =>
                            {
                                if (__instance?.Data?.Disconnected is not null and not true)
                                {
                                    var msg = string.Format(GetString("KickBecauseDiffrentVersionOrMod"), __instance?.Data?.PlayerName);
                                    Logger.Warn(msg, "Version Kick");
                                    RPC.NotificationPop(msg);
                                    Utils.KickPlayer(__instance.GetClientId(), false, "ModVersionIncorrect");
                                }
                            }, 5f, "Kick");
                    }
                    // Kick Unmached Player End
                }
                catch
                {
                    Logger.Warn($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): バージョン情報が無効です", "RpcVersionCheck");
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, __instance.GetClientId());
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }, 1f, "Retry Version Check Task");
                }
                break;
            case CustomRPC.RequestRetryVersionCheck:
                RPC.RpcVersionCheck();
                break;
            case CustomRPC.SyncCustomSettings:
                if (AmongUsClient.Instance.AmHost) break;
                List<OptionItem> list = new();
                var startAmount = reader.ReadInt32();
                var lastAmount = reader.ReadInt32();
                for (var i = startAmount; i < OptionItem.AllOptions.Count && i <= lastAmount; i++)
                    list.Add(OptionItem.AllOptions[i]);
                Logger.Info($"{startAmount}-{lastAmount}:{list.Count}/{OptionItem.AllOptions.Count}", "SyncCustomSettings");
                foreach (var co in list) co.SetValue(reader.ReadPackedInt32());
                OptionShower.BuildText();
                break;
            case CustomRPC.SetDeathReason:
                RPC.GetDeathReason(reader);
                break;
            case CustomRPC.EndGame:
                RPC.EndGame(reader);
                break;
            case CustomRPC.PlaySound:
                byte playerID = reader.ReadByte();
                Sounds sound = (Sounds)reader.ReadByte();
                RPC.PlaySound(playerID, sound);
                break;
            case CustomRPC.ShowPopUp:
                string msg = reader.ReadString();
                HudManager.Instance.ShowPopUp(msg);
                break;
            case CustomRPC.SetCustomRole:
                byte CustomRoleTargetId = reader.ReadByte();
                CustomRoles role = (CustomRoles)reader.ReadPackedInt32();
                RPC.SetCustomRole(CustomRoleTargetId, role);
                bool IsGM = role is CustomRoles.GM;
                if (!IsGM && GameStates.IsInGame)
                {
                    if (!Main.SetRolesList.ContainsKey(CustomRoleTargetId))
                    {
                        Main.SetRolesList[CustomRoleTargetId] = new();
                    }

                    var trueRoleName = Utils.GetTrueRoleName(CustomRoleTargetId, false);
                    var subRolesText = Utils.GetSubRolesText(CustomRoleTargetId, false, false, true);
                    var allRoles = trueRoleName + subRolesText;

                    Main.SetRolesList[CustomRoleTargetId].Add(allRoles);

                }
                break;
            case CustomRPC.SetRoleInGame:
                PlayerControlSetRolePatch.playanima = reader.ReadBoolean();
                PlayerControlSetRolePatch.InGameSetRole = reader.ReadBoolean();
                byte RoleTargetId = reader.ReadByte();
                RoleTypes roles = (RoleTypes)reader.ReadPackedInt32();
                RPC.SetRoleInGame(RoleTargetId, roles);
                PlayerControlSetRolePatch.playanima = reader.ReadBoolean();
                PlayerControlSetRolePatch.InGameSetRole = reader.ReadBoolean();
                break;
            case CustomRPC.SetNameColorData:
                NameColorManager.ReceiveRPC(reader);
                break;
            case CustomRPC.SetAdmirerLoversPlayers:
                AdmirerLovers.ReceiveRPC(reader);
                break;
            case CustomRPC.SetAkujoLoversPlayers:
                AkujoLovers.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCupidLoversPlayers:
                CupidLovers.ReceiveRPC(reader);
                break;
            case CustomRPC.SetRealKiller:
                byte targetId = reader.ReadByte();
                byte killerId = reader.ReadByte();
                RPC.SetRealKiller(targetId, killerId);
                break;
            case CustomRPC.PlayCustomSound:
                CustomSoundsManager.ReceiveRPC(reader);
                break;
            case CustomRPC.RestTONEXSetting:
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValue(x.DefaultValue, false));
                OptionShower.BuildText();
                break;
            case CustomRPC.SuicideWithAnime:
                var playerId = reader.ReadByte();
                var pc = Utils.GetPlayerById(playerId);
                pc?.RpcSuicideWithAnime(true);
                break;
            case CustomRPC.SetKillTimer:
                float time = reader.ReadSingle();
                PlayerControl.LocalPlayer.SetKillTimer(time);
                break;
            case CustomRPC.SyncAllPlayerNames:
                Main.AllPlayerNames = new();
                int num = reader.ReadInt32();
                for (int i = 0; i < num; i++)
                    Main.AllPlayerNames.TryAdd(reader.ReadByte(), reader.ReadString());
                break;
            case CustomRPC.SyncNameNotify:
                NameNotifyManager.ReceiveRPC(reader);
                break;
            case CustomRPC.SetAction:
                ExtendedPlayerControl.ReceiveSetAction(reader);
                break;
            case CustomRPC.IsDisabledAction:
                ExtendedPlayerControl.ReceiveIsDisabledActionion(reader);
                break;
            case CustomRPC.KillFlash:
                Utils.FlashColor(new(1f, 0f, 0f, 0.3f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, Sounds.KillSound);
                break;
            case CustomRPC.ColorFlash:
                Utils.FlashColor(Utils.color2);
                break;
            case CustomRPC.OnClickMeetingButton:
                var target = Utils.GetPlayerById(reader.ReadByte());
                if (__instance.GetRoleClass() is IMeetingButton meetingButton || __instance.Contains_Addons(out meetingButton)) meetingButton.OnClickButton(target);
                break;
            case CustomRPC.Guess:
                GuesserHelper.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.SetMedicProtectList:
                Medic.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.NotificationPop:
                NotificationPopperPatch.AddItem(reader.ReadString());
                break;
            case CustomRPC.SetKickReason:
                ShowDisconnectPopupPatch.ReasonByHost = reader.ReadString();
                break;
            case CustomRPC.SetProphetList:
               Prophet.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.SetYangPlayer:
                Onmyoji.ReceiveRPC_SyncYangList(reader);
                break;
            case CustomRPC.SetYinPlayer:
                Onmyoji.ReceiveRPC_SyncYinList(reader);
                break;
            case CustomRPC.SetRewardOfficerTarget:
                RewardOfficer.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.VultureLimit:
                Vulture.ReceiveRPC_Limit(reader);
                break;
            case CustomRPC.ViciousSeekerKill:
                ViciousSeeker.ReceiveRPC_Limit(reader,rpcType);
                break;
            case CustomRPC.SetHunterList:
                Hunter.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.CustomRoleSync:
                CustomRoleManager.DispatchRpc(reader);
                break;
            case CustomRPC.AddFeeble:
                Vagator.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.Swap:
                SwapperHelper.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.SetAmberProtectList:
                Amber.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.SetNiceGraList:
                NiceGrenadier.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.SetEvilGraList:
                EvilGrenadier.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.SetProsecutorList:
                EvilGrenadier.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.SetDeputyList:
                Deputy.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.SetGeneticistDNA2:
                Geneticist.ReceiveRPC_DNA2(reader);
                break;
            case CustomRPC.SetRangerList:
                Ranger.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.SetKingList:
                King.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.SetSorcererList:
                Sorcerer.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.SetCrusaderList:
                Crusader.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.FixModdedClientCNO:
                var CNO = reader.ReadNetObject<PlayerControl>();
                bool active = reader.ReadBoolean();
                CNO.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(active);

                break;
            case CustomRPC.SyncFFAPlayer:
                FFAManager.ReceiveRPCSyncFFAPlayer(reader);
                break;
            case CustomRPC.SyncFFANameNotify:
                FFAManager.ReceiveRPCSyncNameNotify(reader);
                break;
            default:
                CustomRoleManager.DispatchRpc(reader, rpcType);
                break;

        }
    }
}

internal static class RPC
{
    //来源：https://github.com/music-discussion/TownOfHost-TheOtherRoles/blob/main/Modules/RPC.cs
    public static void SyncCustomSettingsRPC(int targetId = -1)
    {
        if (targetId != -1)
        {
            var client = Utils.GetClientById(targetId);
            if (client == null || client.Character == null || !Main.playerVersion.ContainsKey(client.Character.PlayerId)) return;
        }
        if (!AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null)) return;
        var amount = OptionItem.AllOptions.Count;
        int divideBy = amount / 10;
        for (var i = 0; i <= 10; i++)
            SyncOptionsBetween(i * divideBy, (i + 1) * divideBy, targetId);
    }
    public static void SyncCustomSettingsRPCforOneOption(OptionItem option)
    {
        List<OptionItem> allOptions = new(OptionItem.AllOptions);
        var placement = allOptions.IndexOf(option);
        if (placement != -1)
            SyncOptionsBetween(placement, placement);
    }
    static void SyncOptionsBetween(int startAmount, int lastAmount, int targetId = -1)
    {
        //判断发送请求是否有效
        if (
            Main.AllPlayerControls.Count() <= 1 ||
            AmongUsClient.Instance.AmHost == false ||
            PlayerControl.LocalPlayer == null
        ) return;
        //判断发送目标是否有效
        if (targetId != -1)
        {
            var client = Utils.GetClientById(targetId);
            if (client == null || client.Character == null || !Main.playerVersion.ContainsKey(client.Character.PlayerId))
                return;
        }

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCustomSettings, SendOption.Reliable, targetId);
        List<OptionItem> list = new();
        writer.Write(startAmount);
        writer.Write(lastAmount);
        for (var i = startAmount; i < OptionItem.AllOptions.Count && i <= lastAmount; i++)
            list.Add(OptionItem.AllOptions[i]);
        Logger.Info($"{startAmount}-{lastAmount}:{list.Count}/{OptionItem.AllOptions.Count}", "SyncCustomSettings");
        foreach (var co in list) writer.WritePacked(co.GetValue());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void PlaySoundRPC(byte PlayerID, Sounds sound)
    {
        if (AmongUsClient.Instance.AmHost)
            PlaySound(PlayerID, sound);
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, SendOption.Reliable, -1);
        writer.Write(PlayerID);
        writer.Write((byte)sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SyncAllPlayerNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAllPlayerNames, SendOption.Reliable, -1);
        writer.Write(Main.AllPlayerNames.Count);
        foreach (var name in Main.AllPlayerNames)
        {
            writer.Write(name.Key);
            writer.Write(name.Value);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendGameData(int clientId = -1)
    {
        MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
        writer.StartMessage((byte)(clientId == -1 ? 5 : 6)); //0x05 GameData
        {
            writer.Write(AmongUsClient.Instance.GameId);
            if (clientId != -1)
                writer.WritePacked(clientId);
            writer.StartMessage(1); //0x01 Data
            {
                writer.WritePacked(PlayerControl.LocalPlayer.NetId);// undecided
                PlayerControl.LocalPlayer.Serialize(writer, true);
            }
            writer.EndMessage();
        }
        writer.EndMessage();

        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }
    public static void ShowPopUp(this PlayerControl pc, string msg)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShowPopUp, SendOption.Reliable, pc.GetClientId());
        writer.Write(msg);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ExileAsync(PlayerControl player)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.Reliable, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        player.Exiled();
    }
    public static async void RpcVersionCheck()
    {
        if (Main.AssistivePluginMode.Value)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                if (!Main.playerVersion.ContainsKey(0)) return;
                Main.playerVersion.TryGetValue(0, out var ver);
                if (Main.ForkId != ver.forkId) return;
            }
            while (PlayerControl.LocalPlayer == null) await Task.Delay(500);
            if (Main.playerVersion.ContainsKey(0) || !Main.VersionCheat.Value)
            {
                bool cheating = Main.VersionCheat.Value;
                MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, SendOption.Reliable);
                writer.Write(cheating ? Main.playerVersion[0].version.ToString() : Main.PluginVersion);
                writer.Write(cheating ? Main.playerVersion[0].tag : $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
                writer.Write(cheating ? Main.playerVersion[0].forkId : Main.ForkId);
                writer.EndMessage();
            }
            Main.playerVersion[PlayerControl.LocalPlayer.PlayerId] = new PlayerVersion(Main.PluginVersion, $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})", Main.ForkId);

        }
        else
        {
            while (PlayerControl.LocalPlayer == null) await Task.Delay(500);
            if (Main.playerVersion.ContainsKey(0) || !Main.VersionCheat.Value)
            {
                bool cheating = Main.VersionCheat.Value;
                MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, SendOption.Reliable);
                writer.Write(cheating ? Main.playerVersion[0].version.ToString() : Main.PluginVersion);
                writer.Write(cheating ? Main.playerVersion[0].tag : $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
                writer.Write(cheating ? Main.playerVersion[0].forkId : Main.ForkId);
                writer.EndMessage();
            }
            Main.playerVersion[PlayerControl.LocalPlayer.PlayerId] = new PlayerVersion(Main.PluginVersion, $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})", Main.ForkId);
        }
    }
    public static void SendDeathReason(byte playerId, CustomDeathReason deathReason)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDeathReason, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((int)deathReason);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void GetDeathReason(MessageReader reader)
    {
        var playerId = reader.ReadByte();
        var deathReason = (CustomDeathReason)reader.ReadInt32();
        var state = PlayerState.GetByPlayerId(playerId);
        state.DeathReason = deathReason;
        state.IsDead = true;
    }
    public static void ForceEndGame(CustomWinner win)
    {
        if (ShipStatus.Instance == null) return;
        try { CustomWinnerHolder.ResetAndSetWinner(win); }
        catch { }
        if (AmongUsClient.Instance.AmHost)
        {
            ShipStatus.Instance.enabled = false;
            try { GameManager.Instance.LogicFlow.CheckEndCriteria(); }
            catch { }
            try { GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false); }
            catch { }
        }
    }
    public static void EndGame(MessageReader reader)
    {
        try
        {
            CustomWinnerHolder.ReadFrom(reader);
        }
        catch (Exception ex)
        {
            Logger.Error($"正常にEndGameを行えませんでした。\n{ex}", "EndGame", false);
        }
    }
    public static void PlaySound(byte playerID, Sounds sound)
    {
        if (PlayerControl.LocalPlayer.PlayerId == playerID)
        {
            switch (sound)
            {
                case Sounds.KillSound:
                    SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 1f);
                    break;
                case Sounds.TaskComplete:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskCompleteSound, false, 1f);
                    break;
                case Sounds.TaskUpdateSound:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskUpdateSound, false, 1f);
                    break;
                case Sounds.ImpTransform:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSOtherImpostorTransformSfx, false, 0.8f);
                    break;
                case Sounds.Yeehawfrom:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSLocalYeehawSfx, false, 0.8f);
                    break;
            }
        }
    }
    public static void SetCustomRole(byte targetId, CustomRoles role)
    {
        if (Utils.GetPlayerById(targetId).Is(role)) return;
        if (role < CustomRoles.NotAssigned)
        {
            CustomRoleManager.GetRoleBaseByPlayerId(targetId)?.Dispose();
            PlayerState.GetByPlayerId(targetId).SetMainRole(role);
        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
        {
            PlayerState.GetByPlayerId(targetId).SetSubRole(role);
        }
        CustomRoleManager.CreateInstance(role, Utils.GetPlayerById(targetId));

        HudManager.Instance.SetHudActive(true);
        if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
    }

    public static void SetRoleInGame(byte targetId, RoleTypes role)
    {
        var player = Utils.GetPlayerById(targetId);
        player.SetRole(role, false);

        HudManager.Instance.SetHudActive(true);
        if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
    }

    public static void SendRpcLogger(uint targetNetId, byte callId, int targetClientId = -1)
    {
        if (!DebugModeManager.AmDebugger) return;
        string rpcName = GetRpcName(callId);
        string from = targetNetId.ToString();
        string target = targetClientId.ToString();
        try
        {
            target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
            from = Main.AllPlayerControls.Where(c => c.NetId == targetNetId).FirstOrDefault()?.Data?.PlayerName;
        }
        catch { }
        Logger.Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName})", "SendRPC");
    }
    public static string GetRpcName(byte callId)
    {
        string rpcName;
        if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
        else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
        else rpcName = callId.ToString();
        return rpcName;
    }
    public static void SetRealKiller(byte targetId, byte killerId)
    {
        var state = PlayerState.GetByPlayerId(targetId);
        state.RealKiller.Item1 = DateTime.Now;
        state.RealKiller.Item2 = killerId;

        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, Hazel.SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(killerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void NotificationPop(string text)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.NotificationPop, Hazel.SendOption.Reliable, -1);
        writer.Write(text);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        NotificationPopperPatch.AddItem(text);
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpc))]
internal class StartRpcPatch
{
    public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId)
    {
        if (!Main.AssistivePluginMode.Value)
        {
            RPC.SendRpcLogger(targetNetId, callId);
        }
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
internal class StartRpcImmediatelyPatch
{
    public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(3)] int targetClientId = -1)
    {
        if (!Main.AssistivePluginMode.Value)
        {
            RPC.SendRpcLogger(targetNetId, callId, targetClientId);
        }
    }
}