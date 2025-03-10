using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using TONEX.Roles.Core;
using TONEX.Roles.Impostor;

namespace TONEX.Modules;

public class MessageControl
{
    public string Message { get; set; }
    public string Args { get; set; }
    public bool HasValidArgs { get; set; }

    public PlayerControl Player { get; set; }
    public List<byte> SendToList { get; set; } = new();
    public bool IsAlive { get => Player.IsAlive(); }
    public bool IsFromMod { get => Player.IsModClient(); }
    public bool IsFromSelf { get => Player.AmOwner; }

    public bool IsRoleCommand { get; set; } = false;
    public bool ForceSend { get; set; } = false;
    public MsgRecallMode RecallMode { get; set; } = MsgRecallMode.None;

    public MessageControl(PlayerControl player, string message)
    {
        Player = player;
        Message = message;

        MsgRecallMode recallMode = MsgRecallMode.None;

        // Check if it is a role command
        IsRoleCommand = (Player.GetRoleClass()?.OnSendMessage(Message, out recallMode) ?? false) 
            || Player.Any_Addons(x => x.OnSendMessage(Message, out recallMode));

        if (GameStates.IsInGame && CustomRoles.Blackmailer.IsExist() && Blackmailer.ForBlackmailer.Contains(Player.PlayerId))
        {
            IsRoleCommand = true;
            recallMode = MsgRecallMode.Spam;
        }

        RecallMode = recallMode;

        if (IsRoleCommand && !AmongUsClient.Instance.AmHost) ForceSend = true;
        CustomRoleManager.ReceiveMessage.Do(a => a.Invoke(this));

        if (IsRoleCommand || !AmongUsClient.Instance.AmHost) return;
        if (ChatCommand.SpamCommands == null || !ChatCommand.SpamCommands.Any())
            ChatCommand.SpamInitOnly();



        foreach (var command in ChatCommand.SpamCommands)
        {
            if (command.Access switch
            {
                CommandAccess.All => false,
                CommandAccess.LocalMod => !IsFromMod,
                CommandAccess.Host => !AmongUsClient.Instance.AmHost || !IsFromSelf,
                CommandAccess.Debugger => !DebugModeManager.AmDebugger,
                _ => true,
            }) continue;

            string keyword = command.KeyWords.Find(k => Message.ToLower().StartsWith("/" + k.ToLower()));
            if (string.IsNullOrEmpty(keyword)) continue;

            Args = Message[(keyword.Length + 1)..].Trim();
            HasValidArgs = !string.IsNullOrWhiteSpace(Args);

            Logger.Info($"Command: /{keyword}, Args: {Args}", "ChatControl");

            (RecallMode, string msg) = command.Command(this);
            if (!string.IsNullOrEmpty(msg))
                foreach (var pid in SendToList)
                    Utils.SendMessage(msg, pid);
            SendToList = new();
            IsRoleCommand = true;
            return;
        }

        if (IsRoleCommand && !AmongUsClient.Instance.AmHost) ForceSend = true;
        CustomRoleManager.ReceiveMessage.Do(a => a.Invoke(this));


        if (IsRoleCommand || !AmongUsClient.Instance.AmHost) return;

        if (!IsRoleCommand)
        {

            if (ChatCommand.AllCommands == null || !ChatCommand.AllCommands.Any())
                ChatCommand.Init();
            // Not a role command, check for command list
            foreach (var command in ChatCommand.AllCommands)
            {
                if (command.Access switch
                {
                    CommandAccess.All => false,
                    CommandAccess.LocalMod => !IsFromMod,
                    CommandAccess.Host => !AmongUsClient.Instance.AmHost || !IsFromSelf,
                    CommandAccess.Debugger => !DebugModeManager.AmDebugger,
                    _ => true,
                }) continue;

                string keyword = command.KeyWords.Find(k => Message.ToLower().StartsWith("/" + k.ToLower()));
                if (string.IsNullOrEmpty(keyword)) continue;

                Args = Message[(keyword.Length + 1)..].Trim();
                HasValidArgs = !string.IsNullOrWhiteSpace(Args);

                Logger.Info($"Command: /{keyword}, Args: {Args}", "ChatControl");

                (RecallMode, string msg) = command.Command(this);
                if (!string.IsNullOrEmpty(msg))
                    foreach (var pid in SendToList)
                        Utils.SendMessage(msg, pid);
                SendToList = new();
                IsRoleCommand = true;
                return;
            }
        }
    }

    public static List<MessageControl> History;
    public static MessageControl Create(PlayerControl player, string message)
    {
        var mc = new MessageControl(player, message);
        if (!AmongUsClient.Instance.AmHost) return mc;
        History ??= new();
        if (!message.EndsWith('\0') && !(player?.Data?.PlayerName?.EndsWith('\0') ?? true)) History.Add(mc);
        if (History?.Count > 50) History.RemoveAt(0);
        return mc;
    }

    public static void TryHideMessage(bool includeHost, bool includeModded)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        SendHistoryMessages(includeHost, includeModded);
    }

    public static void SpamMessagesImmediately()
    {
        for (int i = 0; i <= 40; i++)
        {
            // The number of historical messages is not enough to cover all messages
            var player = Main.AllAlivePlayerControls.ToArray()[IRandom.Instance.Next(0, Main.AllAlivePlayerControls.Count())];
            SendMessageAsPlayerImmediately(player, "Hello " + Main.ModName, false, false);
        }
    }
    public static void SendHistoryMessages(bool includeHost = false, bool includeModded = false)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        
        var sendList = History.Where(m => m.IsAlive && m.RecallMode == MsgRecallMode.None);
        foreach (var mc in sendList)
        {
            SendMessageAsPlayerImmediately(mc.Player, mc.Message, includeHost, includeModded);
        }
    }


    public static void SendMessageAsPlayerImmediately(PlayerControl player, string text, bool hostCanSee = true, bool sendToModded = true)
    {
        if (Main.AssistivePluginMode.Value) return;
        if (hostCanSee) DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, text);
        if (!sendToModded) text += "\0";

        var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
        writer.StartMessage(-1);
        writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
            .Write(text)
            .EndRpc();
        writer.EndMessage();
        writer.SendMessage();
    }
}

public enum MsgRecallMode
{
    None,
    Block, // Cancel sending msg for modded client
    Spam, // Spam lot of messages to hide original message
}