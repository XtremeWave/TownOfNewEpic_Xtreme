using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TONEX.Attributes;
using TONEX.Modules;
using TONEX.Roles.AddOns;
using TONEX.Roles.Core;
using static TONEX.Modules.CustomRoleSelector;
using static TONEX.Translator;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.AddOns.Crewmate;
using TONEX.MoreGameModes;

namespace TONEX;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
internal class ChangeRoleSettings
{
    public static Dictionary<byte, bool> AllPlayers;
    public static Dictionary<byte, RoleTypes> PlayerRoleWhenAlive;

    public static void Postfix(AmongUsClient __instance)
    {
        if (Main.AssistivePluginMode.Value)
        {
            AllPlayers = new();
            PlayerRoleWhenAlive = new();

            foreach (var pc in PlayerControl.AllPlayerControls)
                AllPlayers.Add(pc.PlayerId, false);

            foreach (var pc in PlayerControl.AllPlayerControls)
                PlayerRoleWhenAlive.Add(pc.PlayerId, RoleTypes.CrewmateGhost);
            return;
        
        }

        try
        {
            //注:この時点では役職は設定されていません。
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
            if (Options.DisableVanillaRoles.GetBool())
            {
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
            }
            Main.SetRolesList = new();
            Main.OverrideWelcomeMsg = "";
            Main.AllPlayerKillCooldown = new();
            Main.AllPlayerSpeed = new();

            Main.LastEnteredVent = new();
            Main.LastEnteredVentLocation = new();

            Main.AfterMeetingDeathPlayers = new();
            Main.clientIdList = new();

            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();

            Main.ShieldPlayer = Options.ShieldPersonDiedFirst.GetBool() ? Main.FirstDied : byte.MaxValue;
            Main.FirstDied = byte.MaxValue;

            ReportDeadBodyPatch.CanReport = new();
            Options.UsedButtonCount = 0;

            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
            GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor = false;

            Main.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.FirstTP = new();

            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();

            Main.PlayerColors = new();

            ExtendedPlayerControl.PlayerSpeedRecord = new();

            ExtendedPlayerControl.DisableKill = new();
            ExtendedPlayerControl.DisableEnterVent = new();
            ExtendedPlayerControl.DisableExitVent = new();
            ExtendedPlayerControl.DisableShapeshift = new();
            ExtendedPlayerControl.DisableSabotage = new();
            ExtendedPlayerControl.DisableReport = new();
            ExtendedPlayerControl.DisableMeeting = new();
            ExtendedPlayerControl.DisablePet = new();
            ExtendedPlayerControl.DisableMove = new();

            ExtendedPlayerControl.HasDisabledKill = new();
            ExtendedPlayerControl.HasDisabledEnterVent = new();
            ExtendedPlayerControl.HasDisabledExitVent = new();
            ExtendedPlayerControl.HasDisabledShapeshift = new();
            ExtendedPlayerControl.HasDisabledSabotage = new();
            ExtendedPlayerControl.HasDisabledReport = new();
            ExtendedPlayerControl.HasDisabledMeeting = new();
            ExtendedPlayerControl.HasDisabledPet = new();
            ExtendedPlayerControl.HasDisabledMove = new();

            //名前の記録
            RPC.SyncAllPlayerNames();
            
            ConfirmEjections.LatestEjec = null;
            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Any())
            {
                var msg = Translator.GetString("Error.InvalidColor");
                Logger.SendInGame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

            GameModuleInitializerAttribute.InitializeAll();

            foreach (var target in Main.AllPlayerControls)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }

                target.RpcSetScanner(false);
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                var colorId = pc.Data.DefaultOutfit.ColorId;
                if (AmongUsClient.Instance.AmHost && Options.FormatNameMode.GetInt() == 1) //pc.RpcSetName(Palette.GetColorName(colorId));
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.SetName, SendOption.None, -1);
                    writer.Write(pc.Data.NetId);
                    writer.Write(Palette.GetColorName(colorId));
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
                PlayerState.Create(pc.PlayerId);
                //Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;
                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.FirstTP.Add(pc.PlayerId, false);
                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new NetworkedPlayerInfo.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId, outfit.NamePlateId);
                Main.clientIdList.Add(pc.GetClientId());
            }
            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
            }

            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (!pc.Is(CustomRoles.GM))
                {
                    var sender = CustomRpcSender.Create(name: $"PetsPatch.RpcSetPet)");
                    if (pc.Data.DefaultOutfit.PetId == null)
                    {
                        pc.SetPet("pet_Crewmate");
                        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetPetStr)
                        .Write("pet_Crewmate")
                        .EndRpc();
                        sender.SendMessage();
                    }
                    pc.CanPet();

                }
            }
            CustomNetObject.Reset();
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Change Role Setting Postfix");
            Logger.Fatal(ex.ToString(), "Change Role Setting Postfix");
        }
    }
}
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
internal class SelectRolesPatch
{
    public static bool Prefix()
    {
        if (Main.AssistivePluginMode.Value || !AmongUsClient.Instance.AmHost) return true;

        try
        {
            //CustomRpcSender和RpcSetRoleReplacer的初始化
            Dictionary<byte, CustomRpcSender> senders = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            if (Options.EnableGM.GetBool())
            {
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                PlayerControl.LocalPlayer.Data.IsDead = true;
                PlayerState.AllPlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            SelectCustomRoles();
            SelectAddonRoles();
            CalculateVanillaRoleCount();

            //指定原版特殊职业数量
            RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
            foreach (var roleTypes in RoleTypesList)
            {
                var roleOpt = Main.NormalOptions.roleOptions;
                int numRoleTypes = GetRoleTypesCount(roleTypes);
                roleOpt.SetRoleRate(roleTypes, numRoleTypes, numRoleTypes > 0 ? 100 : 0);
            }

            Dictionary<(byte, byte), RoleTypes> rolesMap = new();

            foreach (var cp in RoleResult.Where(x => x.Value == CustomRoles.CrewPostor))
            {
                AssignDesyncRole(cp.Value, cp.Key, senders, BaseRole: RoleTypes.Crewmate, hostBaseRole: RoleTypes.Impostor);
            }
            // 注册反职业
            foreach (var kv in RoleResult.Where(x => x.Value.GetRoleInfo().IsDesyncImpostor))
            {
                AssignDesyncRole(kv.Value, kv.Key, senders, BaseRole: kv.Value.GetRoleInfo().BaseRoleType.Invoke());
            }
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Select Role Prefix");
            ex.Message.Split(@"\r\n").Do(line => Logger.Fatal(line, "Select Role Prefix"));
        }
        //接下来，进入基本角色分配部分
        return true;
    }

    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost || Main.AssistivePluginMode.Value) return;
        try
        {
            List<(PlayerControl, RoleTypes)> newList = new();
            foreach (var sd in RpcSetRoleReplacer.StoragedData)
            {
                var kp = RoleResult.Where(x => x.Key.PlayerId == sd.Item1.PlayerId).FirstOrDefault();
                if (kp.Value.GetRoleInfo().IsDesyncImpostor || kp.Value == CustomRoles.CrewPostor)
                {
                    Logger.Warn($"反向原版职业 => {sd.Item1.GetRealName()}: {sd.Item2}", "Override Role Select");
                    continue;
                }
                newList.Add((sd.Item1, kp.Value.GetRoleTypes()));

                if (sd.Item2 == kp.Value.GetRoleTypes())
                    Logger.Warn($"注册原版职业 => {sd.Item1.GetRealName()}: {sd.Item2}", "Override Role Select");
                else
                    Logger.Warn($"覆盖原版职业 => {sd.Item1.GetRealName()}: {sd.Item2} => {kp.Value.GetRoleTypes()}", "Override Role Select");
            }
            if (Options.EnableGM.GetBool()) newList.Add((PlayerControl.LocalPlayer, RoleTypes.Crewmate));
            RpcSetRoleReplacer.StoragedData = newList;
            RpcSetRoleReplacer.Release(); //一口气写出保存的 SetRoleRpc
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());
            // 删除不必要的对象
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.DesyncImpostorList = null;
            RpcSetRoleReplacer.StoragedData = null;

            var rd = IRandom.Instance;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                pc.Data.IsDead = false; // 解除玩家的死亡状态
                var state = PlayerState.GetByPlayerId(pc.PlayerId);
                if (state.MainRole != CustomRoles.NotAssigned) continue; // 如果已经分配了自定义角色，则跳过
                var role = pc.Data.Role.Role.GetCustomRoleTypes();
                if (role == CustomRoles.NotAssigned)
                    Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                state.SetMainRole(role);
            }

            foreach (var (player, role) in RoleResult.Where(kvp => !(kvp.Value.GetRoleInfo()?.IsDesyncImpostor ?? false)))
            {
                SetColorPatch.IsAntiGlitchDisabled = true;

                PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);
                Logger.Info($"注册模组职业：{player?.Data?.PlayerName} => {role}", "AssignCustomRoles");

                SetColorPatch.IsAntiGlitchDisabled = false;
            }

            AddOnsAssignData.AssignAddOnsFromList();

            foreach (var pair in PlayerState.AllPlayerStates)
            {
                ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                foreach (var subRole in pair.Value.SubRoles)
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
            }
          
            CustomRoleManager.CreateInstance();

            foreach (var pc in Main.AllPlayerControls)
            {
                HudManager.Instance.SetHudActive(true);
                pc.ResetKillCooldown();
            }
            
            RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
            foreach (var roleTypes in RoleTypesList)
            {
                var roleOpt = Main.NormalOptions.roleOptions;
                roleOpt.SetRoleRate(roleTypes, 0, 0);
            }
          
            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.Standard:
                case CustomGameMode.AllCrewModMode:
                    GameEndChecker.SetPredicateToNormal();
                    break;
                case CustomGameMode.HotPotato:
                    GameEndChecker.SetPredicateToHotPotato();
                    break;
                case CustomGameMode.InfectorMode:
                    GameEndChecker.SetPredicateToZombie();
                    break;
                case CustomGameMode.FFA:
                    GameEndChecker.SetPredicateToFFA();
                    break;
            }

            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }
            
            Utils.CountAlivePlayers(true);
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Select Role Postfix");
            ex.Message.Split(@"\r\n").Do(line => Logger.Fatal(line, "Select Role Postfix"));
        }
    }
    static void AssignDesyncRole(CustomRoles role, PlayerControl player, Dictionary<byte, CustomRpcSender> senders,  RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        if (Main.AssistivePluginMode.Value) return;
        var hostId = PlayerControl.LocalPlayer.PlayerId;

        PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);
        RpcSetRoleReplacer.DesyncImpostorList.Add(player.PlayerId);

        var hostRole = player.PlayerId == hostId ? hostBaseRole : RoleTypes.Crewmate;
        foreach (var seer in Main.AllPlayerControls)
        {
            if (seer.PlayerId == hostId)
            {
                player.SetRole(hostRole, false);
            }
            else
            {
                var assignRole = seer.PlayerId == player.PlayerId ? BaseRole : RoleTypes.Scientist;
                senders[player.PlayerId].RpcSetRole(player, assignRole, seer.GetClientId());
            }
        }

        player.Data.IsDead = true;

        Logger.Info($"注册模组职业：{player?.Data?.PlayerName} => {role}", "AssignCustomRoles");
    }
    static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            var sender = senders[seer.PlayerId];
            foreach (var target in Main.AllPlayerControls)
            {
                if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var role))
                {
                    sender.RpcSetRole(seer, role, target.GetClientId());
                }
            }
        }
    }
    
    
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    private class RpcSetRoleReplacer
    {
        public static bool doReplace = false;
        public static Dictionary<byte, CustomRpcSender> senders;
        public static List<(PlayerControl, RoleTypes)> StoragedData = new();
        // 由于角色Desync等其他处理已经写入了SetRoleRpc，因此不需要额外写入的Sender列表
        public static List<byte> DesyncImpostorList;
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType, [HarmonyArgument(1)] bool canOverrideRole = false)
        {
            if (Main.AssistivePluginMode.Value) return true;
            if (doReplace && senders != null)
            {
                StoragedData.Add((__instance, roleType));
                Logger.Info(__instance.GetRealName(), roleType.ToString());
                return false;
            }
            else return true;
        }
        public static void Release()
        {
            foreach (var (player, role) in StoragedData)
            {
                player.SetRole(role);
                var impostorRole = role is RoleTypes.Impostor or RoleTypes.Shapeshifter or RoleTypes.Phantom;
                if (impostorRole && DesyncImpostorList.Count != 0)
                {
                    foreach (var seer in Main.AllPlayerControls)
                    {
                        var assignRole = DesyncImpostorList.Contains(seer.PlayerId) ? RoleTypes.Scientist : role;
                        senders[player.PlayerId].RpcSetRole(player, assignRole, seer.GetClientId());
                    }
                }
                else
                {
                    senders[player.PlayerId].RpcSetRole(player, role);
                }
            }
            doReplace = false;
        }
        public static void StartReplace(Dictionary<byte, CustomRpcSender> senders)
        {
            RpcSetRoleReplacer.senders = senders;
            StoragedData = new();
            DesyncImpostorList = new();
            doReplace = true;
        }
    }
}