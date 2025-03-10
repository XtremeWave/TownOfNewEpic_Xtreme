using AmongUs.Data;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using InnerNet;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TONEX.Modules;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.Core;
using static TONEX.Translator;

namespace TONEX;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
class OnGameJoinedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        
        while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
        Logger.Info($"{__instance.GameId} 加入房间", "OnGameJoined");
        Main.playerVersion = new Dictionary<byte, PlayerVersion>();

        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
        if (Main.AssistivePluginMode.Value)
        {
            if (AmongUsClient.Instance.AmHost)
                RPC.RpcVersionCheck();
            return; 
        }
        if (!Main.VersionCheat.Value) RPC.RpcVersionCheck();
        Main.AllPlayerNames = new();
        ShowDisconnectPopupPatch.ReasonByHost = string.Empty;
        ChatUpdatePatch.DoBlockChat = false;
        GameStates.InGame = false;
        ErrorText.Instance.Clear();
        ServerAddManager.SetServerName();

        EAC.Init();

        if (AmongUsClient.Instance.AmHost) //以下、ホストのみ実行
        {
            GameStartManagerPatch.GameStartManagerUpdatePatch.exitTimer = -1;
            Main.DoBlockNameChange = false;
            Main.NewLobby = true;
            Main.DevRole = new();

            if (Main.NormalOptions.KillCooldown == 0f)
                Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

            AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
            if (AURoleOptions.PhantomCooldown == 0f)
                AURoleOptions.PhantomCooldown = Main.LastShapeshifterCooldown.Value;
        }
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.OnBecomeHost))]
class OnBecomeHostPatch
{
    public static void Postfix()
    {
        if (Main.AssistivePluginMode.Value) return;
        if (GameStates.InGame)
            GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
class DisconnectInternalPatch
{
    public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
    {
        ShowDisconnectPopupPatch.Reason = reason;
        ShowDisconnectPopupPatch.StringReason = stringReason;

        Logger.Info($"断开连接(理由:{reason}:{stringReason}，Ping:{__instance.Ping})", "Session");

        ErrorText.Instance.CheatDetected = false;
        ErrorText.Instance.SBDetected = false;
        ErrorText.Instance.Clear();
        Cloud.StopConnect();

        if (AmongUsClient.Instance.AmHost && GameStates.InGame)
            GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);

        CustomRoleManager.Dispose();
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
class OnPlayerJoinedPatch
{
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
    {

        Logger.Info($"{client.PlayerName}(ClientID:{client.Id}/FriendCode:{client.FriendCode}) 加入房间", "Session");
        if (Main.AssistivePluginMode.Value)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                RPC.RpcVersionCheck();
                
            }
            return;
        }
        if (AmongUsClient.Instance.AmHost && client.FriendCode == "" && Options.KickPlayerFriendCodeNotExist.GetBool())
        {
            Utils.KickPlayer(client.Id, false, "NotLogin");
            RPC.NotificationPop(string.Format(GetString("Message.KickedByNoFriendCode"), client.PlayerName));
            Logger.Info($"フレンドコードがないプレイヤーを{client?.PlayerName}をキックしました。", "Kick");
        }
        if (AmongUsClient.Instance.AmHost && client.PlatformData.Platform == Platforms.Android && Options.KickAndroidPlayer.GetBool())
        {
            Utils.KickPlayer(client.Id, false, "Andriod");
            string msg = string.Format(GetString("KickAndriodPlayer"), client?.PlayerName);
            RPC.NotificationPop(msg);
            Logger.Info(msg, "Android Kick");
        }
        if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
        {
            Utils.KickPlayer(client.Id, true, "BanList");
            Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
        }
        BanManager.CheckBanPlayer(client);
        BanManager.CheckDenyNamePlayer(client);
        RPC.RpcVersionCheck();
        var player = client.Character;
        
        if (AmongUsClient.Instance.AmHost)
        {
            if (Main.SayStartTimes.ContainsKey(client.Id)) Main.SayStartTimes.Remove(client.Id);
            if (Main.SayBanwordsTimes.ContainsKey(client.Id)) Main.SayBanwordsTimes.Remove(client.Id);
            if (Main.NewLobby && Options.ShareLobby.GetBool()) Cloud.ShareLobby();
        }
        
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
class OnPlayerLeftPatch
{
    static void Prefix([HarmonyArgument(0)] ClientData data)
    {
        if (!Main.AssistivePluginMode.Value)
        {
            if (!GameStates.IsInGame || !AmongUsClient.Instance.AmHost) return;
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.OnPlayerDeath(data.Character, PlayerState.GetByPlayerId(data.Character.PlayerId).DeathReason, GameStates.IsMeeting));

            if (AmongUsClient.Instance.AmHost && data.Character != null)
            {

                for (int i = 0; i < Main.MessagesToSend.Count; i++)
                {
                    var (msg, sendTo, title) = Main.MessagesToSend[i];
                    if (sendTo == data.Character.PlayerId)
                    {
                        Main.MessagesToSend.RemoveAt(i);
                        i--;
                    }
                }
            }
            var netid = data.Character.NetId;
            _ = new LateTask(() =>
            {
                if (GameStates.IsOnlineGame && AmongUsClient.Instance.AmHost)
                {
                    MessageWriter messageWriter = AmongUsClient.Instance.Streams[1];
                    messageWriter.StartMessage(5);
                    messageWriter.WritePacked(netid);
                    messageWriter.EndMessage();
                }
            }, 2.5f, "Repeat Despawn");
            if (GameStates.IsInGame)
            {
                if (data.Character != null) CustomNetObject.DespawnOnQuit(data.Character.PlayerId);
            }
        }
    }
    public static List<int> ClientsProcessed = new();
    public static void Add(int id)
    {
        ClientsProcessed.Remove(id);
        ClientsProcessed.Add(id);
    }
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        if (data == null)
        {
            Logger.Error("错误的客户端数据：数据为空", "Session");
        }
        else if (data.Character != null)
        {
            if (GameStates.IsInGame && !Main.AssistivePluginMode.Value)
            {
                Lovers.OnPlayerLeft(data);
                AdmirerLovers.OnPlayerLeft(data);
                AkujoLovers.OnPlayerLeft(data);
                CupidLovers.OnPlayerLeft(data);
                var state = PlayerState.GetByPlayerId(data.Character.PlayerId);
                if (state != null)
                {
                    if (state.DeathReason == CustomDeathReason.etc) // 如果死亡原因未设置
                    {
                        state.DeathReason = CustomDeathReason.Disconnected;
                        state.SetDead();
                    }
                }
                else
                {
                    Logger.Error("错误的玩家数据：数据为空", "Session");
                }
                AntiBlackout.OnDisconnect(data.Character.Data);
                PlayerGameOptionsSender.RemoveSender(data.Character);
            }
            Main.playerVersion.Remove(data.Character.PlayerId);
        }
        if (!Main.AssistivePluginMode.Value)
        Logger.Info($"{data?.PlayerName}(ClientID:{data?.Id}/FriendCode:{data?.FriendCode}/Role:{data?.Character?.GetNameWithRole()})断开连接(理由:{reason}，Ping:{AmongUsClient.Instance.Ping})", "Session");
        else
            Logger.Info($"{data?.PlayerName}(ClientID:{data?.Id}/FriendCode:{data?.FriendCode})断开连接(理由:{reason}，Ping:{AmongUsClient.Instance.Ping})", "Session");

        if (AmongUsClient.Instance.AmHost)
        {
            Main.SayStartTimes.Remove(__instance.ClientId);
            Main.SayBanwordsTimes.Remove(__instance.ClientId);

            // 附加描述掉线原因
            switch (reason)
            {
                case DisconnectReasons.Hacking:
                    RPC.NotificationPop(string.Format(GetString("PlayerLeftByAU-Anticheat"), data?.PlayerName));
                    break;
                case DisconnectReasons.Error:
                    RPC.NotificationPop(string.Format(GetString("PlayerLeftCuzError"), data?.PlayerName));
                    break;
                case DisconnectReasons.Kicked:
                case DisconnectReasons.Banned:
                    break;
                default:
                    if (!ClientsProcessed.Contains(data?.Id ?? 0))
                        RPC.NotificationPop(string.Format(GetString("PlayerLeft"), data?.PlayerName));
                    break;
            }
            ClientsProcessed.Remove(data?.Id ?? 0);
        }
    }
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Spawn))]
class InnerNetClientSpawnPatch
{
    public static void Prefix([HarmonyArgument(1)] int ownerId, [HarmonyArgument(2)] SpawnFlags flags)
    {
        if (!Main.AssistivePluginMode.Value)
        {
            if (!AmongUsClient.Instance.AmHost || flags != SpawnFlags.IsClientCharacter) return;

            ClientData client = Utils.GetClientById(ownerId);

            Logger.Msg($"Spawn player data: ID {ownerId}: {client.PlayerName}", "InnerNetClientSpawn");

            //规范昵称

            _ = new LateTask(() => { if (client.Character == null || !GameStates.IsLobby) return; OptionItem.SyncAllOptions(client.Id); }, 3f, "Sync All Options For New Player");

            _ = new LateTask(() =>
            {
                if (client.Character == null) return;
                if (Main.OverrideWelcomeMsg != "") Utils.SendMessage(Main.OverrideWelcomeMsg, client.Character.PlayerId);
                else TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
            }, 3f, "Welcome Message");
            if (Main.OverrideWelcomeMsg == "" && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
            {
                if (Options.AutoDisplayKillLog.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            Utils.ShowKillLog(client.Character.PlayerId);
                        }
                    }, 3f, "DisplayKillLog");
                }
                if (Options.AutoDisplayLastResult.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            Utils.ShowLastResult(client.Character.PlayerId);
                        }
                    }, 3.1f, "DisplayLastResult");
                }
                if (Options.EnableDirectorMode.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            Utils.SendMessage($"{GetString("Message.DirectorModeNotice")}", client.Character.PlayerId);
                        }
                    }, 3.2f, "DisplayDirectorModeWarnning");
                }
                if (Options.UsePets.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            Utils.SendMessage($"{GetString("Message.PetModeNotice")}", client.Character.PlayerId);
                        }
                    }, 3.2f, "DisplayDirectorModeWarnning");
                }
            }
        }
    }
}