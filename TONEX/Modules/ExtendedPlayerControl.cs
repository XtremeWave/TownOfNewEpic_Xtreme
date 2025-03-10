using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TONEX.Modules;
using static TONEX.PlayerControlSetRolePatch;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Impostor;
using TONEX.Roles.Neutral;
using UnityEngine;
using static TONEX.Translator;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using Rewired.Utils.Platforms.Windows;
using TONEX.Attributes;
using TONEX.Roles.AddOns.Common;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine.Purchasing;
using TONEX.Patches;

namespace TONEX;

static class ExtendedPlayerControl
{
    public static void SetRole(this PlayerControl player, RoleTypes role, bool canOverrideRole = false)
    {
        AmongUsClient.Instance.StartCoroutine(player.CoSetRole(role, canOverrideRole));
    }
    public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player.Is(role)) return;

        bool IsGM = role is CustomRoles.GM;
        var id = player.PlayerId;
        if (!IsGM && GameStates.IsInGame)
        {
            if (!Main.SetRolesList.ContainsKey(id))
            {
                Main.SetRolesList[id] = new ();
            }

            var trueRoleName = Utils.GetTrueRoleName(id, false);
            var subRolesText = Utils.GetSubRolesText(id, false, false, true);
            var allRoles = trueRoleName + subRolesText;

            Main.SetRolesList[id].Add(allRoles);

        }


        if (role < CustomRoles.NotAssigned)
        {
            player.GetRoleClass()?.Dispose();
            PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);

        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
        {
            PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(role);
        }
        CustomRoleManager.CreateInstance(role, player);

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.WritePacked((int)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, SendOption.Reliable, -1);
        writer.Write(PlayerId);
        writer.WritePacked((int)role);
        writer.Write(false);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        if (Options.IsAllCrew)
            RpcSetRoleInGame(PlayerId, role.GetRoleInfo().BaseRoleType.Invoke());
    }
    public static void RpcSetRoleInGame(this PlayerControl player,RoleTypes role)
    {
        playanima = false;
        InGameSetRole = true;
        player.RpcSetRole(role, false);
        playanima = true;
        InGameSetRole = false;
    }

    public static void RpcSetRoleInGame(byte PlayerId, RoleTypes role)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        playanima = false;
        InGameSetRole = true;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRoleInGame, SendOption.Reliable, -1);
        writer.Write(playanima);
        writer.Write(InGameSetRole);
        writer.Write(PlayerId);
        writer.WritePacked((int)role);
        playanima = true;
        InGameSetRole = false;
        writer.Write(playanima);
        writer.Write(InGameSetRole);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void RpcExile(this PlayerControl player)
    {
        RPC.ExileAsync(player);
    }
    public static ClientData GetClient(this PlayerControl player)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Character.PlayerId == player.PlayerId).FirstOrDefault();
            return client;
        }
        catch
        {
            return null;
        }
    }
    public static int GetClientId(this PlayerControl player)
    {
        if (player == null) return -1;
        var client = player.GetClient();
        return client == null ? -1 : client.Id;
    }
    public static CustomRoles GetCustomRole(this NetworkedPlayerInfo player)
    {
        return player == null || player.Object == null ? CustomRoles.Crewmate : player.Object.GetCustomRole();
    }
    /// <summary>
    /// ※サブロールは取得できません。
    /// </summary>
    public static CustomRoles GetCustomRole(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + "がCustomRoleを取得しようとしましたが、対象がnullでした。", "GetCustomRole");
            return CustomRoles.Crewmate;
        }
        var state = PlayerState.GetByPlayerId(player.PlayerId);

        return state?.MainRole ?? CustomRoles.Crewmate;
    }

    public static List<CustomRoles> GetCustomSubRoles(this PlayerControl player)
    {
        if (player == null)
        {
            Logger.Warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
            return new() { CustomRoles.NotAssigned };
        }
        return PlayerState.GetByPlayerId(player.PlayerId).SubRoles;
    }
    public static CountTypes GetCountTypes(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + "がCountTypesを取得しようとしましたが、対象がnullでした。", "GetCountTypes");
            return CountTypes.None;
        }

        return PlayerState.GetByPlayerId(player.PlayerId)?.CountType ?? CountTypes.None;
    }
    public static void RpcSetNameEx(this PlayerControl player, string name)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
        }
        HudManagerPatch.LastSetNameDesyncCount++;

        Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for All", "RpcSetNameEx");
        player.RpcSetName(name);
    }
    public static void SetOutFit(
        this PlayerControl target, 
        int colorId = 255, 
        string hatId = "null", string skinId = "null", string visorId = "null", string petId = "null")
    {
        var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.PlayerName})");
        if (colorId != 255)
        {
            target.SetColor(colorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                .Write(target.Data.NetId)
                .Write((byte)colorId)
            .EndRpc();
        }
        if (hatId != "null")
        {
            target.SetHat(hatId, colorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(hatId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();
        }
        if (skinId != "null")
        {
            target.SetSkin(skinId, colorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(skinId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();
        }
        if (visorId != "null")
        {
            target.SetVisor(visorId, colorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(visorId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
            .EndRpc();
        }
        if (petId != "null")
        {
            target.SetPet(petId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                .Write(petId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                .EndRpc();
        }
        sender.SendMessage();
    }
    public static void CheckDistanceAndDoActions(Vector2 center, Action<PlayerControl> action, PlayerControl centerPc = null, float radius = 0.3f)
    {
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (centerPc != null && pc == centerPc) continue;
            var posi = pc.GetTruePosition();

            var dis = Vector2.Distance(center, posi);
            if (dis > radius) continue;
            action(pc);
        }
    }
    public static void RpcSetNamePrivate(this PlayerControl player, string name,  PlayerControl seer = null, bool force = false)
    {
        //player: 名前の変更対象
        //seer: 上の変更を確認することができるプレイヤー
        if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
        if (seer == null) seer = player;
        if (!force && Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] == name)
        {
            //Logger.info($"Cancel:{player.name}:{name} for {seer.name}", "RpcSetNamePrivate");
            return;
        }
        Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
        HudManagerPatch.LastSetNameDesyncCount++;
        Logger.Info($"Set: {player?.Data?.PlayerName} => {name} for {seer.GetNameWithRole()}", "RpcSetNamePrivate");
        if (seer == null || player == null) return;
        var clientId = seer.GetClientId();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, SendOption.Reliable, clientId);
        writer.Write(player.Data.NetId);
        writer.Write(name);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, bool canOverride, int clientId)
    {
        //player: 名前の変更対象

        if (player == null) return;
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            player.SetRole(role, false);
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, SendOption.Reliable, clientId);
        writer.Write((ushort)role);
        writer.Write(false);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void SetKillCooldown(this PlayerControl player, float time = -1f)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        player.SyncSettings();
        player.RpcProtectedMurderPlayer();
        player.ResetKillCooldown();
    }
    public static void SetKillCooldownV2(this PlayerControl player, float time = -1f, PlayerControl target = null, bool forceAnime = false)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (target == null) target = player;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        if (forceAnime || !player.IsModClient())
        {
            player.SyncSettings();
            player.RpcProtectedMurderPlayer(target);
        }
        else
        {
            time = Main.AllPlayerKillCooldown[player.PlayerId] / 2;
            if (player.AmOwner) PlayerControl.LocalPlayer.SetKillTimer(time);
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillTimer, SendOption.Reliable, player.GetClientId());
                writer.Write(time);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            if (!MeetingStates.FirstMeeting)
                Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Observer) && target.PlayerId != x.PlayerId).Do(x => x.RpcProtectedMurderPlayer(target));
        }
        player.ResetKillCooldown();
    }
    public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target = null)
    {
        if (target == null) target = killer;
        if (killer.AmOwner)
        {
            killer.MurderPlayer(target);
        }
        else
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, killer.GetClientId());
            messageWriter.WriteNetObject(target); 
            
            messageWriter.Write((int)SucceededFlags);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
    }
    [Obsolete]
    public static void RpcSpecificProtectPlayer(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
    {
        if (AmongUsClient.Instance.AmClient)
        {
            killer.ProtectPlayer(target, colorId);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, killer.GetClientId());
        messageWriter.WriteNetObject(target);
        messageWriter.Write(colorId);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void RpcResetAbilityCooldown(this PlayerControl target)
    {
        if (target == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }
        Logger.Info($"アビリティクールダウンのリセット:{target.name}({target.PlayerId})", "RpcResetAbilityCooldown");
        if (PlayerControl.LocalPlayer == target)
        {
            //targetがホストだった場合
            PlayerControl.LocalPlayer.Data.Role.SetCooldown();
        }
        else
        {
            //targetがホスト以外だった場合
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
            writer.WriteNetObject(target);
            writer.Write(0);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        /*
            プレイヤーがバリアを張ったとき、そのプレイヤーの役職に関わらずアビリティーのクールダウンがリセットされます。
            ログの追加により無にバリアを張ることができなくなったため、代わりに自身に0秒バリアを張るように変更しました。
            この変更により、役職としての守護天使が無効化されます。
            ホストのクールダウンは直接リセットします。
        */
    }
    public static void RpcSpecificShapeshift(this PlayerControl player, PlayerControl target, bool shouldAnimate)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player.OwnerId == AmongUsClient.Instance.HostId)
        {
            player.Shapeshift(target, shouldAnimate);
            return;
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Shapeshift, SendOption.Reliable, player.GetClientId());
        messageWriter.WriteNetObject(target);
        messageWriter.Write(shouldAnimate);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void RpcSpecificRejectShapeshift(this PlayerControl player, PlayerControl target, bool shouldAnimate)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var seer in Main.AllPlayerControls)
        {
            if (seer != player)
            {
                MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.RejectShapeshift, SendOption.Reliable, seer.GetClientId());
                AmongUsClient.Instance.FinishRpcImmediately(msg);
            }
            else
            {
                player.RpcSpecificShapeshift(target, shouldAnimate);
            }
        }
    }
    public static void RpcSpecificVanish(this PlayerControl player, PlayerControl seer)
    {
        /*
         *  Unluckily the vanish animation cannot be disabled
         *  For vanila client seer side, the player must be with Phantom Role behavior, or the rpc will do nothing
         */
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.StartVanish, SendOption.None, seer.GetClientId());
        AmongUsClient.Instance.FinishRpcImmediately(msg);
    }

    public static void RpcSpecificAppear(this PlayerControl player, PlayerControl seer, bool shouldAnimate)
    {
        /*
         *  For vanila client seer side, the player must be with Phantom Role behavior, or the rpc will do nothing
         */
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.StartAppear, SendOption.None, seer.GetClientId());
        msg.Write(shouldAnimate);
        AmongUsClient.Instance.FinishRpcImmediately(msg);
    }
    public static void RpcDesyncUpdateSystem(this PlayerControl target, SystemTypes systemType, int amount)
    {
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, target.GetClientId());
        messageWriter.Write((byte)systemType);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((byte)amount);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    /*public static void RpcBeKilled(this PlayerControl player, PlayerControl KilledBy = null) {
        if(!AmongUsClient.Instance.AmHost) return;
        byte KilledById;
        if(KilledBy == null)
            KilledById = byte.MaxValue;
        else
            KilledById = KilledBy.PlayerId;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)CustomRPC.BeKilled, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(KilledById);
        AmongUsClient.Instance.FinishRpcImmediately(writer);

        RPC.BeKilled(player.PlayerId, KilledById);
    }*/
    public static void MarkDirtySettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
    }
    public static void SyncSettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
        GameOptionsSender.SendAllGameOptions();
    }
    public static TaskState GetPlayerTaskState(this PlayerControl player)
    {
        return PlayerState.GetByPlayerId(player.PlayerId).GetTaskState();
    }

    /*public static GameOptionsData DeepCopy(this GameOptionsData opt)
    {
        var optByte = opt.ToBytes(5);
        return GameOptionsData.FromBytes(optByte);
    }*/

    public static string GetTrueRoleName(this PlayerControl player)
    {
        return Utils.GetTrueRoleName(player.PlayerId);
    }
    public static string GetSubRoleName(this PlayerControl player, bool forUser)
    {
        var SubRoles = PlayerState.GetByPlayerId(player.PlayerId).SubRoles;
        if (SubRoles.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var role in SubRoles)
        {
            if (role is CustomRoles.NotAssigned) continue;
            sb.Append($"{Utils.ColorString(Color.white, " + ")}{Utils.GetRoleName(role, forUser)}");
        }

        return sb.ToString();
    }
    public static string GetAllRoleName(this PlayerControl player)
    {
        if (!player) return null;
        var text = Utils.GetRoleName(player.GetCustomRole());
        text += player.GetSubRoleName(false);
        return text;
    }
    public static string GetNameWithRole(this PlayerControl player, bool forUser = false)
    {
        var ret = $"{player?.Data?.PlayerName}" + (GameStates.IsInGame&& !Main.AssistivePluginMode.Value ? $"({player?.GetAllRoleName()})" : "");
        return (forUser ? ret : ret.RemoveHtmlTags());
    }
    public static string GetRoleColorCode(this PlayerControl player)
    {
        return Utils.GetRoleColorCode(player.GetCustomRole());
    }
    public static Color GetRoleColor(this PlayerControl player)
    {
        return Utils.GetRoleColor(player.GetCustomRole());
    }
    public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;

        var systemtypes = Utils.GetCriticalSabotageSystemType();

        _ = new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 128);
        }, 0f + delay, "Reactor Desync");

        _ = new LateTask(() =>
        {
            pc.RpcSpecificMurderPlayer();
        }, 0.2f + delay, "Murder To Reset Cam");

        _ = new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 16);
            if (Main.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncUpdateSystem(systemtypes, 17);
        }, 0.4f + delay, "Fix Desync Reactor");
    }
    public static void ReactorFlash(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null) return;
        int clientId = pc.GetClientId();
        // Logger.Info($"{pc}", "ReactorFlash");
        var systemtypes = Utils.GetCriticalSabotageSystemType();
        float FlashDuration = Options.KillFlashDuration.GetFloat();

        pc.RpcDesyncUpdateSystem(systemtypes, 128);

        _ = new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 16);
            if (Main.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncUpdateSystem(systemtypes, 17);
        }, FlashDuration + delay, "Fix Desync Reactor");
    }

    public static string GetTrueName(this PlayerControl player)
    {
        return Main.AllPlayerNames.TryGetValue(player.PlayerId, out var name) ? name : GetRealName(player, GameStates.IsMeeting);
    }
    public static string GetRealName(this PlayerControl player, bool isMeeting = false)
    {
        return isMeeting ? player?.Data?.PlayerName : player?.name;
    }
    public static bool CanUseKillButton(this PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel || pc.IsEaten()) return false;

        var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseKillButton();

        return roleCanUse ?? pc.Is(CustomRoleTypes.Impostor);
    }
    public static bool CanUseImpostorVentButton(this PlayerControl pc)
    {
        if (!pc.IsAlive()) return false;

        var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseImpostorVentButton();

        return roleCanUse ?? false;
    }
    public static bool CanUseSabotageButton(this PlayerControl pc)
    {
        var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseSabotageButton();
        if (pc.IsImp() && !pc.IsAlive() && Options.DeadImpCantSabotage.GetBool()) roleCanUse = false;
        return roleCanUse ?? false;
    }
    public static void ResetKillCooldown(this PlayerControl player)
    {
        var killerRole = player.GetRoleClass() as IKiller;
        var cooldown = killerRole != null ? killerRole.CalculateKillCooldown() : Options.DefaultKillCooldown;
        Main.AllPlayerKillCooldown[player.PlayerId] = cooldown;
        //Main.AllPlayerKillCooldown[player.PlayerId] = (player.As_Addons<IKiller>())?.CalculateKillCooldown() ?? Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更
        player.SyncSettings();
        if (player.PlayerId == LastImpostor.currentId)
            LastImpostor.SetKillCooldown();
        var IsKiller = ((player.GetRoleClass() as IKiller)?.IsKiller ?? false) || (player.As_Addons<IKiller>()?.IsKiller ?? false);
        if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Diseased) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId && !x.Is(CustomRoles.Hangman)))
        {
            Main.AllPlayerKillCooldown[player.PlayerId] *= Diseased.OptionVistion.GetFloat();
        }

        if (player.Is(CustomRoles.Mini) && IsKiller)
        {
            var miniAge = player.GetAddonClasses().Select(x => x as Mini).Where(x => x != null).Select(x => x.Age).FirstOrDefault();
            Main.AllPlayerKillCooldown[player.PlayerId] =
                miniAge < 18 ? Mini.OptionKidKillCoolDown.GetFloat() : Mini.OptionAdultKillCoolDown.GetFloat();

        }
    }
    public static void RpcExileV2(this PlayerControl player)
    {
        player.Exiled();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.None, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public const MurderResultFlags SucceededFlags = MurderResultFlags.Succeeded | MurderResultFlags.DecisionByHost;
    #region MurderPlayer
    public static void MurderPlayer(this PlayerControl killer, PlayerControl target)
    {
        killer.MurderPlayer(target, SucceededFlags);
    }
    public static void RpcMurderPlayer(this PlayerControl killer, PlayerControl target)
    {
        if (killer.IsDisabledAction(PlayerActionType.Kill)) return;
        killer.RpcMurderPlayer(target, true);
    }
    public static void RpcMurderPlayerV2(this PlayerControl killer, PlayerControl target = null)
    {
        if (target == null) target = killer;
        if (killer.IsDisabledAction(PlayerActionType.Kill)) return;
        if (AmongUsClient.Instance.AmClient)
        {
            killer.MurderPlayer(target);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((int)SucceededFlags);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        Utils.NotifyRoles();
    }
    public static void RpcProtectedMurderPlayer(this PlayerControl killer, PlayerControl target = null)
    {
        //killerが死んでいる場合は実行しない
        if (!killer.IsAlive()) return;

        if (target == null) target = killer;
        // Host
        if (killer.AmOwner)
        {
            killer.MurderPlayer(target, MurderResultFlags.FailedProtected);
        }
        // Other Clients
        if (killer.PlayerId != 0)
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable,killer.GetClientId());
            writer.WriteNetObject(target);
            writer.Write((int)MurderResultFlags.FailedProtected);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void RpcSuicideWithAnime(this PlayerControl pc, bool fromHost = false)
    {
        if (!fromHost && !AmongUsClient.Instance.AmHost) return;
        var amOwner = pc.AmOwner;
        if (AmongUsClient.Instance.AmHost)
        {
            pc.Data.IsDead = true;
            pc.RpcExileV2();
            PlayerState.GetByPlayerId(pc.PlayerId)?.SetDead();

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SuicideWithAnime, SendOption.Reliable, -1);
            writer.Write(pc.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
    }
    #endregion
    public static void NoCheckStartMeeting(this PlayerControl reporter, NetworkedPlayerInfo target, bool force = false)
    { /*サボタージュ中でも関係なしに会議を起こせるメソッド
        targetがnullの場合はボタンとなる*/

        if (GameStates.IsMeeting) return;
        if (Options.DisableMeeting.GetBool()) return;
        Logger.Info($"{reporter.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "NoCheckStartMeeting");

        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
        {
            role.OnReportDeadBody(reporter, target);
        }

        Main.AllPlayerControls
                    .Where(pc => Main.CheckShapeshift.ContainsKey(pc.PlayerId))
                    .Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true));
        MeetingTimeManager.OnReportDeadBody();

        Utils.NotifyRoles(isForMeeting: true, NoCache: true);

        Utils.SyncAllSettings();

        MeetingRoomManager.Instance.AssignSelf(reporter, target);
        DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
        reporter.RpcStartMeeting(target);
    }
    public static bool IsModClient(this PlayerControl player) => Main.playerVersion.ContainsKey(player.PlayerId);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
    ///</summary>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
    ///</summary>
    ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
    {
        var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
        List<PlayerControl> rangePlayers = new();
        player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
        foreach (var pc in rangePlayersIL)
        {
            if (predicate(pc)) rangePlayers.Add(pc);
        }
        return rangePlayers;
    }
    #region Imp
    public static bool IsImp(this PlayerControl player) => player.Is(CustomRoleTypes.Impostor);
    public static bool IsImpTeam(this PlayerControl player) => (player.IsImp() || player.Is(CustomRoles.Madmate))
        && !player.Is(CustomRoles.Charmed) && !player.Is(CustomRoles.Wolfmate)
        && !player.Is(CustomRoles.Lovers) && !player.Is(CustomRoles.AdmirerLovers) && !player.Is(CustomRoles.AkujoLovers) && !player.Is(CustomRoles.CupidLovers);
    #endregion
    #region Crew
    public static bool IsCrew(this PlayerControl player) => player.Is(CustomRoleTypes.Crewmate);
    public static bool IsCrewTeam(this PlayerControl player) => player.Is(CustomRoleTypes.Crewmate) 
        && !player.Is(CustomRoles.Madmate) && !player.Is(CustomRoles.Charmed) && !player.Is(CustomRoles.Wolfmate) 
        && !player.Is(CustomRoles.Lovers) && !player.Is(CustomRoles.AdmirerLovers) && !player.Is(CustomRoles.AkujoLovers) && !player.Is(CustomRoles.CupidLovers);
    public static bool IsCrewKiller(this PlayerControl player) => player.IsCrew() && ((CustomRoleManager.GetRoleBaseByPlayerId(player.PlayerId) as IKiller)?.IsKiller ?? false);
    public static bool IsCrewNonKiller(this PlayerControl player) => !player.IsCrewKiller();
    #endregion
    #region Neu
    public static bool IsNeutral(this PlayerControl player) => player.Is(CustomRoleTypes.Neutral);

    public static bool IsNeutralKiller(this PlayerControl player) => player.IsNeutral() && ((CustomRoleManager.GetRoleBaseByPlayerId(player.PlayerId) as INeutralKiller)?.IsNK ?? false);
    public static bool IsNeutralNonKiller(this PlayerControl player) => !player.IsNeutralKiller();

    public static bool IsNeutralEvil(this PlayerControl player) => player.IsNeutral() && ((CustomRoleManager.GetRoleBaseByPlayerId(player.PlayerId) as INeutral)?.IsNE ?? false);
    public static bool IsNeutralBenign(this PlayerControl player) => !player.IsNeutralEvil();

    #endregion
    #region SpecialNeu
    public static bool IsJackalTeam(this PlayerControl player) => 
        player.Is(CustomRoles.Jackal) || player.Is(CustomRoles.Wolfmate) || player.Is(CustomRoles.Sidekick) || player.Is(CustomRoles.Whoops);

    public static bool IsSuccubusTeam(this PlayerControl player) =>
     player.Is(CustomRoles.Succubus) || player.Is(CustomRoles.Charmed);

    #endregion


    public static bool IsShapeshifting(this PlayerControl player) => Main.CheckShapeshift.TryGetValue(player.PlayerId, out bool ss) && ss;
    public static bool KnowDeathReason(this PlayerControl seer, PlayerControl seen)
    {
        // targetが生きてたらfalse
        if (seen.IsAlive())
        {
            return false;
        }
        // seerが死亡済で，霊界から死因が見える設定がON
        if (!seer.IsAlive() && Options.GhostCanSeeDeathReason.GetBool())
        {
            return true;
        }

        // 役職による仕分け
        if (seer.GetRoleClass() is IDeathReasonSeeable deathReasonSeeable || seer.Contains_Addons(out deathReasonSeeable))
        {
            return deathReasonSeeable.CheckSeeDeathReason(seen);
        }
        return false;
    }
    public static string GetRoleInfo(this PlayerControl player, bool InfoLong = false, bool forVanilla = false)
    {
        var roleClass = player.GetRoleClass();
        var role = player.GetCustomRole();
        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
            InfoLong = false;

        var text = role.ToString();

        var Prefix = "";
        if (!InfoLong)
            switch (role)
            {
                case CustomRoles.Mafia:
                    if (roleClass is not Mafia mafia) break;

                    Prefix = mafia.CanUseKillButton() ? "After" : "Before";
                    break;
            };
        var Info = (role.IsVanilla() ? "Blurb" : "Info") + (InfoLong ? "Long" : "");
        
        return GetString($"{Prefix}{text}{Info}");
    }
    public static string GetRoleInfoForVanilla(this CustomRoles role, bool InfoLong = false)
    {
        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
            InfoLong = false;

        var text = role.ToString();

        var Info = (role.IsVanilla() ? "Blurb" : "Info") + (InfoLong ? "Long" : "");

        return GetString($"{text}{Info}");
    }
    public static string GetRoleInfoWithRole(this CustomRoles role, bool InfoLong = false)
    {
        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
            InfoLong = false;

        var text = role.ToString();

        var Prefix = "";
        
        var Info = (role.IsVanilla() ? "Blurb" : "Info") + (InfoLong ? "Long" : "");
        return GetString($"{Prefix}{text}{Info}");
    }
    public static void SetDeathReason(this PlayerControl target, CustomDeathReason reason)
    {
        if (target == null)
        {
            Logger.Info("target=null", "SetDeathReason");
            return;
        }
        var State = PlayerState.GetByPlayerId(target.PlayerId);
        State.DeathReason = reason;
    }
    public static void SetRealKiller(this PlayerControl target, byte killerId, bool NotOverRide = false)
    {
        if (target == null)
        {
            Logger.Info("target=null", "SetRealKiller");
            return;
        }
        var State = PlayerState.GetByPlayerId(target.PlayerId);
        if (State.RealKiller.Item1 != DateTime.MinValue && NotOverRide) return; //既に値がある場合上書きしない
        RPC.SetRealKiller(target.PlayerId, killerId);
    }
    public static void SetRealKiller(this PlayerControl target, PlayerControl killer, bool NotOverRide = false)
    {
        if (target == null)
        {
            Logger.Info("target=null", "SetRealKiller");
            return;
        }
        if (killer == null)
        {
            Logger.Info("killer=null", "SetRealKiller");
            return;
        }
        var State = PlayerState.GetByPlayerId(target.PlayerId);
        if (State.RealKiller.Item1 != DateTime.MinValue && NotOverRide) return; //既に値がある場合上書きしない
        RPC.SetRealKiller(target.PlayerId, killer.PlayerId);
    }
    public static PlayerControl GetRealKiller(this PlayerControl target)
    {
        var killerId = PlayerState.GetByPlayerId(target.PlayerId).GetRealKiller();
        return killerId == byte.MaxValue ? null : Utils.GetPlayerById(killerId);
    }
    public static PlainShipRoom GetPlainShipRoom(this PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.IsEaten()) return null;
        var Rooms = ShipStatus.Instance.AllRooms;
        if (Rooms == null) return null;
        foreach (var room in Rooms)
        {
            if (!room.roomArea) continue;
            if (pc.Collider.IsTouching(room.roomArea))
                return room;
        }
        return null;
    }
    public static bool IsProtected(this PlayerControl self) => self.protectedByGuardianId > -1;

    #region 玩家行为设置 / Player Action Set
    [Flags]
    // 玩家行为类型
    public enum PlayerActionType
    {
        None = 0,
        Kill = 1 << 0,
        EnterVent = 1 << 1,
        ExitVent = 1 << 2,
        Shapeshift = 1 << 3,
        Sabotage = 1 << 4,
        Report = 1 << 5,
        Meeting = 1 << 6,
        Pet = 1 << 7,
        Move = 1 << 8,
        All = ~0
    }

    // 玩家行为执行
    public enum PlayerActionInUse : int
    {
        None = 0,
        Skill = 1,

        All = 255
    }

    // 禁用玩家行为的字典记录
    public static Dictionary<byte, int> DisableKill = new();
    public static Dictionary<byte, int> DisableEnterVent = new();
    public static Dictionary<byte, int> DisableExitVent = new();
    public static Dictionary<byte, int> DisableShapeshift = new();
    public static Dictionary<byte, int> DisableSabotage = new();
    public static Dictionary<byte, int> DisableReport = new();
    public static Dictionary<byte, int> DisableMeeting = new();
    public static Dictionary<byte, int> DisablePet = new();
    public static Dictionary<byte, int> DisableMove = new();

    // 曾被禁用行为玩家的列表记录
    public static List<byte> HasDisabledKill = new();
    public static List<byte> HasDisabledEnterVent = new();
    public static List<byte> HasDisabledExitVent = new();
    public static List<byte> HasDisabledShapeshift = new();
    public static List<byte> HasDisabledSabotage = new();
    public static List<byte> HasDisabledReport = new();
    public static List<byte> HasDisabledMeeting = new();
    public static List<byte> HasDisabledPet = new();
    public static List<byte> HasDisabledMove = new();

    [GameModuleInitializer]
    public static void AllActInit()
    {
        // 禁用玩家行为的字典记录
        DisableKill = new();
        DisableEnterVent = new();
        DisableExitVent = new();
        DisableShapeshift = new();
        DisableSabotage = new();
        DisableReport = new();
        DisableMeeting = new();
        DisablePet = new();
        DisableMove = new();

        // 曾被禁用行为玩家的列表记录
        HasDisabledKill = new();
        HasDisabledEnterVent = new();
        HasDisabledExitVent = new();
        HasDisabledShapeshift = new();
        HasDisabledSabotage = new();
        HasDisabledReport = new();
        HasDisabledMeeting = new();
        HasDisabledPet = new();
        HasDisabledMove = new();
    }

    // 速度记录
    public static Dictionary<byte, float> PlayerSpeedRecord = new();

    public static void EnableActionAll(this PlayerControl pc)
    {
        // 移除禁用 Kill 行为的记录
        DisableKill.Remove(pc.PlayerId);
        HasDisabledKill.Remove(pc.PlayerId);
        HasDisabledKill.Add(pc.PlayerId);

        // 移除禁用 EnterVent 行为的记录
        DisableEnterVent.Remove(pc.PlayerId);
        HasDisabledEnterVent.Remove(pc.PlayerId);
        HasDisabledEnterVent.Add(pc.PlayerId);

        // 移除禁用 ExitVent 行为的记录
        DisableExitVent.Remove(pc.PlayerId);
        HasDisabledExitVent.Remove(pc.PlayerId);
        HasDisabledExitVent.Add(pc.PlayerId);

        // 移除禁用 Shapeshift 行为的记录
        DisableShapeshift.Remove(pc.PlayerId);
        HasDisabledShapeshift.Remove(pc.PlayerId);
        HasDisabledShapeshift.Add(pc.PlayerId);

        // 移除禁用 Sabotage 行为的记录
        DisableSabotage.Remove(pc.PlayerId);
        HasDisabledSabotage.Remove(pc.PlayerId);
        HasDisabledSabotage.Add(pc.PlayerId);

        // 移除禁用 Report 行为的记录
        DisableReport.Remove(pc.PlayerId);
        HasDisabledReport.Remove(pc.PlayerId);
        HasDisabledReport.Add(pc.PlayerId);

        // 移除禁用 Meeting 行为的记录
        DisableMeeting.Remove(pc.PlayerId);
        HasDisabledMeeting.Remove(pc.PlayerId);
        HasDisabledMeeting.Add(pc.PlayerId);

        // 移除禁用 Pet 行为的记录
        DisablePet.Remove(pc.PlayerId);
        HasDisabledPet.Remove(pc.PlayerId);
        HasDisabledPet.Add(pc.PlayerId);

        // 移除禁用 Move 行为的记录
        DisableMove.Remove(pc.PlayerId);
        HasDisabledMove.Remove(pc.PlayerId);
        HasDisabledMove.Add(pc.PlayerId);
        if (PlayerSpeedRecord.ContainsKey(pc.PlayerId))
        {
            Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] - Main.MinSpeed + PlayerSpeedRecord[pc.PlayerId];

            pc.MarkDirtySettings();
            PlayerSpeedRecord.Remove(pc.PlayerId);
        }

        // 发送设置动作的消息
        SendSetAction();
        SendIsDisabledActionion();
        pc.RpcRejectShapeshift();
    }
    public static bool IsDisabledAction(this PlayerControl pc, PlayerActionType actionTypes = PlayerActionType.None, PlayerActionInUse actionInUses = PlayerActionInUse.All)
    {
        if (actionInUses == PlayerActionInUse.All)
            switch (actionTypes)
            {
                // 懒得写注释了，自己看
                case PlayerActionType.Kill:
                    return DisableKill.ContainsKey(pc.PlayerId);

                case PlayerActionType.EnterVent:
                    return DisableEnterVent.ContainsKey(pc.PlayerId);

                case PlayerActionType.ExitVent:
                    return DisableExitVent.ContainsKey(pc.PlayerId);

                case PlayerActionType.Shapeshift:
                    return DisableShapeshift.ContainsKey(pc.PlayerId);

                case PlayerActionType.Sabotage:
                    return DisableSabotage.ContainsKey(pc.PlayerId);

                case PlayerActionType.Report:
                    return DisableReport.ContainsKey(pc.PlayerId);

                case PlayerActionType.Meeting:
                    return DisableMeeting.ContainsKey(pc.PlayerId);

                case PlayerActionType.Pet:
                    return DisablePet.ContainsKey(pc.PlayerId);
                default:
                    return false;
            }
        else
            switch (actionTypes)
            {
                case PlayerActionType.Kill:
                    return DisableKill.ContainsKey(pc.PlayerId) && DisableKill[pc.PlayerId] == (int)actionInUses;

                case PlayerActionType.EnterVent:
                    return DisableEnterVent.ContainsKey(pc.PlayerId) && DisableEnterVent[pc.PlayerId] == (int)actionInUses;

                case PlayerActionType.ExitVent:
                    return DisableExitVent.ContainsKey(pc.PlayerId) && DisableExitVent[pc.PlayerId] == (int)actionInUses;

                case PlayerActionType.Shapeshift:
                    return DisableShapeshift.ContainsKey(pc.PlayerId) && DisableShapeshift[pc.PlayerId] == (int)actionInUses;

                case PlayerActionType.Sabotage:
                    return DisableSabotage.ContainsKey(pc.PlayerId) && DisableSabotage[pc.PlayerId] == (int)actionInUses;

                case PlayerActionType.Report:
                    return DisableReport.ContainsKey(pc.PlayerId) && DisableReport[pc.PlayerId] == (int)actionInUses;

                case PlayerActionType.Meeting:
                    return DisableMeeting.ContainsKey(pc.PlayerId) && DisableMeeting[pc.PlayerId] == (int)actionInUses;

                case PlayerActionType.Pet:
                    return DisablePet.ContainsKey(pc.PlayerId) && DisablePet[pc.PlayerId] == (int)actionInUses;
                default:
                    return false;
            }
    }
    public static bool HasDisabledAction(this PlayerControl pc, PlayerActionType actionTypes = PlayerActionType.None)
    {
        var HasDisabledd = false;
        switch (actionTypes)
        {
            case PlayerActionType.Kill:
                if (HasDisabledKill.Contains(pc.PlayerId))
                {
                    HasDisabledKill.Remove(pc.PlayerId);
                    HasDisabledd = true;
                }
                break;

            case PlayerActionType.EnterVent:
                if (HasDisabledEnterVent.Contains(pc.PlayerId))
                {
                    HasDisabledEnterVent.Remove(pc.PlayerId);
                    HasDisabledd = true;
                }
                break;

            case PlayerActionType.ExitVent:
                if (HasDisabledExitVent.Contains(pc.PlayerId))
                {
                    HasDisabledExitVent.Remove(pc.PlayerId);
                    HasDisabledd = true;
                }
                break;

            case PlayerActionType.Shapeshift:
                if (HasDisabledShapeshift.Contains(pc.PlayerId))
                {
                    HasDisabledShapeshift.Remove(pc.PlayerId);
                    HasDisabledd = true;
                }
                break;

            case PlayerActionType.Sabotage:
                if (HasDisabledSabotage.Contains(pc.PlayerId))
                {
                    HasDisabledSabotage.Remove(pc.PlayerId);
                    HasDisabledd = true;
                }
                break;

            case PlayerActionType.Report:
                if (HasDisabledReport.Contains(pc.PlayerId))
                {
                    HasDisabledReport.Remove(pc.PlayerId);
                    HasDisabledd = true;
                }
                break;

            case PlayerActionType.Meeting:
                if (HasDisabledMeeting.Contains(pc.PlayerId))
                {
                    HasDisabledMeeting.Remove(pc.PlayerId);
                    HasDisabledd = true;
                }
                break;

            case PlayerActionType.Pet:
                if (HasDisabledPet.Contains(pc.PlayerId))
                {
                    HasDisabledPet.Remove(pc.PlayerId);
                    HasDisabledd = true;
                }
                break;
        }

        return HasDisabledd;
    }

    /// <summary>
    /// 禁用玩家行为
    /// </summary>
    /// <param name="player">禁用者
    /// </param>
    /// <param name="pc">被禁用者
    /// </param>
    /// <param name="actionTypes">禁用行为，可输入多个:
    /// Kill:击杀;
    /// EnterVent:进入通风管;
    /// ExitVent:退出通风管;
    /// Shapeshift:变形;
    /// Sabotage:破坏;
    /// Report:报告尸体;
    /// Meeting:开启会议;
    /// Pet:摸宠物;
    /// Move:移动;
    /// All:以上所有行为.
    /// </param>
    /// <param name="actionInUses">禁用类别:
    /// PlayerActionType.All:全部类型行为;
    /// PlayerActionType.Skill:技能类型行为.
    /// </param>
    /// <param name="isIntentional">这是玩家自愿禁用行为吗</param>
    public static void DisableAction(
        this PlayerControl player,
        PlayerControl pc,
        PlayerActionType actionTypes = PlayerActionType.None,
        PlayerActionInUse actionInUses = PlayerActionInUse.All,
        bool isIntentional = false)
    {
        if (Main.AssistivePluginMode.Value) return;
        // 瘟疫之源处理
        if (CustomRoles.Plaguebearer.IsExist() && !isIntentional)
        {
            foreach (var plague in Main.AllAlivePlayerControls.Where(p => p.Is(CustomRoles.Plaguebearer)))
            {
                var role = (plague.GetRoleClass() as Plaguebearer);
                if (!role.PlaguePlayers.Contains(player.PlayerId)) continue;
                role.PlaguePlayers.Remove(pc.PlayerId);
                role.PlaguePlayers.Add(pc.PlayerId);
                role.SendRPC();
            }
        }
        if (actionTypes == PlayerActionType.None)
        {
            // 处理空情况
            return;
        }
        pc.EnableActionAll();

        // 处理包含 Kill 的情况
        if ((actionTypes & PlayerActionType.Kill) != PlayerActionType.None)
        {

            DisableKill.Add(pc.PlayerId, (int)actionInUses);

        }

        // 处理包含 EnterVent 的情况
        if ((actionTypes & PlayerActionType.EnterVent) != PlayerActionType.None)
        {

            DisableEnterVent.Add(pc.PlayerId, (int)actionInUses);

        }

        // 处理包含 ExitVent 的情况
        if ((actionTypes & PlayerActionType.ExitVent) != PlayerActionType.None)
        {

            DisableExitVent.Add(pc.PlayerId, (int)actionInUses);

        }

        // 处理包含 Shapeshift 的情况
        if ((actionTypes & PlayerActionType.Shapeshift) != PlayerActionType.None)
        {

            DisableShapeshift.Add(pc.PlayerId, (int)actionInUses);

        }

        // 处理包含 Sabotage 的情况
        if ((actionTypes & PlayerActionType.Sabotage) != PlayerActionType.None)
        {

            DisableSabotage.Add(pc.PlayerId, (int)actionInUses);

        }

        // 处理包含 Report 的情况
        if ((actionTypes & PlayerActionType.Report) != PlayerActionType.None)
        {

            DisableReport.Add(pc.PlayerId, (int)actionInUses);

        }

        // 处理包含 Meeting 的情况
        if ((actionTypes & PlayerActionType.Meeting) != PlayerActionType.None)
        {

            DisableMeeting.Add(pc.PlayerId, (int)actionInUses);

        }

        // 处理包含 Pet 的情况
        if ((actionTypes & PlayerActionType.Pet) != PlayerActionType.None)
        {


            DisablePet.Add(pc.PlayerId, (int)actionInUses);

        }

        // 处理包含 Move 的情况
        if ((actionTypes & PlayerActionType.Move) != PlayerActionType.None)
        {
            PlayerSpeedRecord.Remove(pc.PlayerId);
            PlayerSpeedRecord.Add(pc.PlayerId, Main.AllPlayerSpeed[pc.PlayerId]);
            Main.AllPlayerSpeed[pc.PlayerId] = Main.MinSpeed;
            DisableMove.Add(pc.PlayerId, (int)actionInUses);
            pc.MarkDirtySettings();

        }

        SendSetAction();
    }
    /// <summary>
    /// 启用玩家行为
    /// </summary>
    /// <param name="actionTypes">禁用行为，可输入多个:
    /// Kill:击杀;
    /// EnterVent:进入通风管;
    /// ExitVent:退出通风管;
    /// Shapeshift:变形;
    /// Sabotage:破坏;
    /// Report:报告尸体;
    /// Meeting:开启会议;
    /// Pet:摸宠物;
    /// Move:移动;
    /// All:以上所有行为.
    /// </param>
    /// /// <param name="isIntentional">这是玩家自愿启用行为吗</param>
    public static void EnableAction(
        this PlayerControl player,
        PlayerControl pc,
        PlayerActionType actionTypes = PlayerActionType.None,
        bool isIntentional = false)
    {
        if (CustomRoles.Plaguebearer.IsExist() && !isIntentional)
        {
            foreach (var plague in Main.AllAlivePlayerControls.Where(p => p.Is(CustomRoles.Plaguebearer)))
            {
                var role = (plague.GetRoleClass() as Plaguebearer);
                if (!role.PlaguePlayers.Contains(player.PlayerId)) continue;
                role.PlaguePlayers.Remove(pc.PlayerId);
                role.PlaguePlayers.Add(pc.PlayerId);
                role.SendRPC();
            }
        }
        if (actionTypes == PlayerActionType.None)
        {
            // 处理空情况
            return;
        }

        // 处理包含 Kill 的情况
        if ((actionTypes & PlayerActionType.Kill) != PlayerActionType.None)
        {
            DisableKill.Remove(pc.PlayerId);
            HasDisabledKill.Remove(pc.PlayerId);
            HasDisabledKill.Add(pc.PlayerId);
        }

        // 处理包含 EnterVent 的情况
        if ((actionTypes & PlayerActionType.EnterVent) != PlayerActionType.None)
        {
            DisableEnterVent.Remove(pc.PlayerId);
            HasDisabledEnterVent.Remove(pc.PlayerId);
            HasDisabledEnterVent.Add(pc.PlayerId);
        }

        // 处理包含 ExitVent 的情况
        if ((actionTypes & PlayerActionType.ExitVent) != PlayerActionType.None)
        {
            DisableExitVent.Remove(pc.PlayerId);
            HasDisabledExitVent.Remove(pc.PlayerId);
            HasDisabledExitVent.Add(pc.PlayerId);
        }

        // 处理包含 Shapeshift 的情况
        if ((actionTypes & PlayerActionType.Shapeshift) != PlayerActionType.None)
        {
            DisableShapeshift.Remove(pc.PlayerId);
            HasDisabledShapeshift.Remove(pc.PlayerId);
            HasDisabledShapeshift.Add(pc.PlayerId);
        }

        // 处理包含 Sabotage 的情况
        if ((actionTypes & PlayerActionType.Sabotage) != PlayerActionType.None)
        {
            DisableSabotage.Remove(pc.PlayerId);
            HasDisabledSabotage.Remove(pc.PlayerId);
            HasDisabledSabotage.Add(pc.PlayerId);
        }

        // 处理包含 Report 的情况
        if ((actionTypes & PlayerActionType.Report) != PlayerActionType.None)
        {
            DisableReport.Remove(pc.PlayerId);
            HasDisabledReport.Remove(pc.PlayerId);
            HasDisabledReport.Add(pc.PlayerId);
        }

        // 处理包含 Meeting 的情况
        if ((actionTypes & PlayerActionType.Meeting) != PlayerActionType.None)
        {
            DisableMeeting.Remove(pc.PlayerId);
            HasDisabledMeeting.Remove(pc.PlayerId);
            HasDisabledMeeting.Add(pc.PlayerId);
        }

        // 处理包含 Pet 的情况
        if ((actionTypes & PlayerActionType.Pet) != PlayerActionType.None)
        {
            DisablePet.Remove(pc.PlayerId);
            HasDisabledPet.Remove(pc.PlayerId);
            HasDisabledPet.Add(pc.PlayerId);
        }

        // 处理包含 Move 的情况
        if ((actionTypes & PlayerActionType.Move) != PlayerActionType.None)
        {
            DisableMove.Remove(pc.PlayerId);
            HasDisabledMove.Remove(pc.PlayerId);
            HasDisabledMove.Add(pc.PlayerId);
            if (PlayerSpeedRecord.ContainsKey(pc.PlayerId))
            {
                Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] - Main.MinSpeed + PlayerSpeedRecord[pc.PlayerId];
                PlayerSpeedRecord.Remove(pc.PlayerId);
                pc.MarkDirtySettings();
            }
        }
        SendIsDisabledActionion();
        SendSetAction();
        pc.RpcRejectShapeshift();
    }

    // 曾被禁用行为玩家RPC
    public static void SendSetAction()
    {
        if (!PlayerControl.LocalPlayer.AmOwner) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetAction, SendOption.Reliable, -1);
        // 写入禁用 Kill 行为的记录
        writer.Write(DisableKill.Count);
        foreach (var pc in DisableKill)
        {
            writer.Write(pc.Key);
            writer.Write(pc.Value);
        }

        // 写入禁用 EnterVent 行为的记录
        writer.Write(DisableEnterVent.Count);
        foreach (var pc in DisableEnterVent)
        {
            writer.Write(pc.Key);
            writer.Write(pc.Value);
        }

        // 写入禁用 ExitVent 行为的记录
        writer.Write(DisableExitVent.Count);
        foreach (var pc in DisableExitVent)
        {
            writer.Write(pc.Key);
            writer.Write(pc.Value);
        }

        // 写入禁用 Shapeshift 行为的记录
        writer.Write(DisableShapeshift.Count);
        foreach (var pc in DisableShapeshift)
        {
            writer.Write(pc.Key);
            writer.Write(pc.Value);
        }

        // 写入禁用 Sabotage 行为的记录
        writer.Write(DisableSabotage.Count);
        foreach (var pc in DisableSabotage)
        {
            writer.Write(pc.Key);
            writer.Write(pc.Value);
        }

        // 写入禁用 Report 行为的记录
        writer.Write(DisableReport.Count);
        foreach (var pc in DisableReport)
        {
            writer.Write(pc.Key);
            writer.Write(pc.Value);
        }

        // 写入禁用 Meeting 行为的记录
        writer.Write(DisableMeeting.Count);
        foreach (var pc in DisableMeeting)
        {
            writer.Write(pc.Key);
            writer.Write(pc.Value);
        }

        // 写入禁用 Pet 行为的记录
        writer.Write(DisablePet.Count);
        foreach (var pc in DisablePet)
        {
            writer.Write(pc.Key);
            writer.Write(pc.Value);
        }

        // 写入玩家速度记录
        writer.Write(PlayerSpeedRecord.Count);
        foreach (var pc in PlayerSpeedRecord)
        {
            writer.Write(pc.Key);
            writer.Write(pc.Value);
        }

        // 写入禁用 Move 行为的记录
        writer.Write(DisableMove.Count);
        foreach (var pc in DisableMove)
        {
            writer.Write(pc.Key);
            writer.Write(pc.Value);
        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveSetAction(MessageReader reader)
    {
        DisableKill = new();
        var disableKillCount = reader.ReadInt32();
        for (var i = 0; i < disableKillCount; i++)
        {
            DisableKill.Add(reader.ReadByte(), reader.ReadInt32());
        }

        DisableEnterVent = new();
        var disableEnterVentCount = reader.ReadInt32();
        for (var i = 0; i < disableEnterVentCount; i++)
        {
            DisableEnterVent.Add(reader.ReadByte(), reader.ReadInt32());
        }

        DisableExitVent = new();
        var disableExitVentCount = reader.ReadInt32();
        for (var i = 0; i < disableExitVentCount; i++)
        {
            DisableExitVent.Add(reader.ReadByte(), reader.ReadInt32());
        }

        DisableShapeshift = new();
        var disableShapeshiftCount = reader.ReadInt32();
        for (var i = 0; i < disableShapeshiftCount; i++)
        {
            DisableShapeshift.Add(reader.ReadByte(), reader.ReadInt32());
        }

        DisableSabotage = new();
        var disableSabotageCount = reader.ReadInt32();
        for (var i = 0; i < disableSabotageCount; i++)
        {
            DisableSabotage.Add(reader.ReadByte(), reader.ReadInt32());
        }

        DisableReport = new();
        var disableReportCount = reader.ReadInt32();
        for (var i = 0; i < disableReportCount; i++)
        {
            DisableReport.Add(reader.ReadByte(), reader.ReadInt32());
        }

        DisableMeeting = new();
        var disableMeetingCount = reader.ReadInt32();
        for (var i = 0; i < disableMeetingCount; i++)
        {
            DisableMeeting.Add(reader.ReadByte(), reader.ReadInt32());
        }

        DisablePet = new();
        var disablePetCount = reader.ReadInt32();
        for (var i = 0; i < disablePetCount; i++)
        {
            DisablePet.Add(reader.ReadByte(), reader.ReadInt32());
        }

        PlayerSpeedRecord = new();
        var playerSpeedRecord = reader.ReadInt32();
        for (var i = 0; i < playerSpeedRecord; i++)
        {
            PlayerSpeedRecord.Add(reader.ReadByte(), reader.ReadInt32());
        }

        DisableMove = new();
        var disableMoveCount = reader.ReadInt32();
        for (var i = 0; i < disableMoveCount; i++)
        {
            DisableMove.Add(reader.ReadByte(), reader.ReadInt32());
        }

    }

    // 禁用玩家行为RPC
    public static void SendIsDisabledActionion()
    {
        if (!PlayerControl.LocalPlayer.AmOwner) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.IsDisabledAction, SendOption.Reliable, -1);
        // 写入禁用 Kill 行为的记录
        writer.Write(HasDisabledKill.Count);
        foreach (var pc in HasDisabledKill)
        {
            writer.Write(pc);

        }

        // 写入禁用 EnterVent 行为的记录
        writer.Write(HasDisabledEnterVent.Count);
        foreach (var pc in HasDisabledEnterVent)
        {
            writer.Write(pc);

        }

        // 写入禁用 ExitVent 行为的记录
        writer.Write(HasDisabledExitVent.Count);
        foreach (var pc in HasDisabledExitVent)
        {
            writer.Write(pc);

        }

        // 写入禁用 Shapeshift 行为的记录
        writer.Write(HasDisabledShapeshift.Count);
        foreach (var pc in HasDisabledShapeshift)
        {
            writer.Write(pc);

        }

        // 写入禁用 Sabotage 行为的记录
        writer.Write(HasDisabledSabotage.Count);
        foreach (var pc in HasDisabledSabotage)
        {
            writer.Write(pc);

        }

        // 写入禁用 Report 行为的记录
        writer.Write(HasDisabledReport.Count);
        foreach (var pc in HasDisabledReport)
        {
            writer.Write(pc);

        }

        // 写入禁用 Meeting 行为的记录
        writer.Write(HasDisabledMeeting.Count);
        foreach (var pc in HasDisabledMeeting)
        {
            writer.Write(pc);

        }

        // 写入禁用 Pet 行为的记录
        writer.Write(HasDisabledPet.Count);
        foreach (var pc in HasDisabledPet)
        {
            writer.Write(pc);

        }

        // 写入禁用 Move 行为的记录
        writer.Write(HasDisabledMove.Count);
        foreach (var pc in HasDisabledMove)
        {
            writer.Write(pc);

        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveIsDisabledActionion(MessageReader reader)
    {
        HasDisabledKill = new();
        var HasDisabledKillCount = reader.ReadInt32();
        for (var i = 0; i < HasDisabledKillCount; i++)
        {
            HasDisabledKill.Add(reader.ReadByte());
        }

        HasDisabledEnterVent = new();
        var HasDisabledEnterVentCount = reader.ReadInt32();
        for (var i = 0; i < HasDisabledEnterVentCount; i++)
        {
            HasDisabledEnterVent.Add(reader.ReadByte());
        }

        HasDisabledExitVent = new();
        var HasDisabledExitVentCount = reader.ReadInt32();
        for (var i = 0; i < HasDisabledExitVentCount; i++)
        {
            HasDisabledExitVent.Add(reader.ReadByte());
        }

        HasDisabledShapeshift = new();
        var HasDisabledShapeshiftCount = reader.ReadInt32();
        for (var i = 0; i < HasDisabledShapeshiftCount; i++)
        {
            HasDisabledShapeshift.Add(reader.ReadByte());
        }

        HasDisabledSabotage = new();
        var HasDisabledSabotageCount = reader.ReadInt32();
        for (var i = 0; i < HasDisabledSabotageCount; i++)
        {
            HasDisabledSabotage.Add(reader.ReadByte());
        }

        HasDisabledReport = new();
        var HasDisabledReportCount = reader.ReadInt32();
        for (var i = 0; i < HasDisabledReportCount; i++)
        {
            HasDisabledReport.Add(reader.ReadByte());
        }

        HasDisabledMeeting = new();
        var HasDisabledMeetingCount = reader.ReadInt32();
        for (var i = 0; i < HasDisabledMeetingCount; i++)
        {
            HasDisabledMeeting.Add(reader.ReadByte());
        }

        HasDisabledPet = new();
        var HasDisabledPetCount = reader.ReadInt32();
        for (var i = 0; i < HasDisabledPetCount; i++)
        {
            HasDisabledPet.Add(reader.ReadByte());
        }

        HasDisabledMove = new();
        var HasDisabledMoveCount = reader.ReadInt32();
        for (var i = 0; i < HasDisabledMoveCount; i++)
        {
            HasDisabledMove.Add(reader.ReadByte());
        }
    }
    #endregion
    //汎用
    public static bool Is(this PlayerControl target, CustomRoles role) =>
        role > CustomRoles.NotAssigned ? target.GetCustomSubRoles().Contains(role) : target.GetCustomRole() == role;
    public static bool Is(this PlayerControl target, CustomRoleTypes type) { return target.GetCustomRole().GetCustomRoleTypes() == type; }
    public static bool Is(this PlayerControl target, RoleTypes type) { return target.GetCustomRole().GetRoleTypes() == type; }
    public static bool Is(this PlayerControl target, CountTypes type) { return target.GetCountTypes() == type; }
    public static bool IsEaten(this PlayerControl target) => GameStates.IsInGame && Pelican.IsEaten(target.PlayerId);
    /// <summary>
    /// <para>判断玩家是否存活</para>
    /// </summary>
    public static bool IsAlive(this PlayerControl target)
    {
        //ロビーなら生きている
        if (GameStates.IsLobby)
        {
            return true;
        }
        //targetがnullならば切断者なので生きていない
        if (target == null)
        {
            return false;
        }
        //目标为活死人
        if (target.Is(CustomRoles.Glitch))
        {
            return false;
        }
        //targetがnullでなく取得できない場合は登録前なので生きているとする
        if (PlayerState.GetByPlayerId(target.PlayerId) is not PlayerState state)
        {
            return true;
        }
        return !state.IsDead;
    }
    public static void RevivePlayer(this PlayerControl repl)
    {
        var aliveplayer = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.Data.IsDead == false && !p.Data.Disconnected);
        foreach (var playeralive in aliveplayer)
        {
            playeralive.Data.Disconnected = true;
        }
        repl.RpcSetRole(RoleTypes.Crewmate, true);
        foreach (var playeralive in aliveplayer)
        {
            playeralive.Data.Disconnected = false;
        }
    }
    public static void RpcRandomVentTeleport(this PlayerControl player)
    {
        var vents = UnityEngine.Object.FindObjectsOfType<Vent>();
        var rand = new System.Random();
        var vent = vents[rand.Next(0, vents.Count)];
        player.RpcTeleport(new Vector2(vent.transform.position.x, vent.transform.position.y + 0.3636f));
    }
}