using AmongUs.GameOptions;
using static TONEX.Translator;
using TONEX.Roles.Core;
using UnityEngine;
using MS.Internal.Xml.XPath;
using static UnityEngine.GraphicsBuffer;
using TONEX.Roles.Neutral;
using System.Collections.Generic;
using Hazel;
using static Il2CppSystem.Net.Http.Headers.Parser;
using TONEX.Modules;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using TONEX.Roles.Core.Interfaces;
using System.Linq;

namespace TONEX.Roles.Neutral;
/*
public sealed class Vagor_FAFL : RoleBase, INeutralKilling, IKiller, IIndependent
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Vagor_FAFL),
            player => new Vagor_FAFL(player),
            CustomRoles.Vagor_FAFL,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            7565_1_1_1,
            null,
            "Zhongli|Vagor|帝君|闲游",
             "#E6AD0A",
            true,
            true,
            countType: CountTypes.FAFL
#if RELEASE
,
            Hidden: true
#endif
        );
    public Vagor_FAFL(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_Before);
        IsFallen = false;
        IsShield = false;
        ElementPowerCount = 0;
        NormalKillCount = 0;
        KillCount = 0;
        SkillCount = 0;
        ShieldTimes = 0;
    }
    #region 参数
    public static int ElementPowerCount;
    public static bool IsFallen;
    public static int NormalKillCount;
    public static int KillCount;
    public static int SkillCount;
    public static int ShieldTimes;
    public static bool IsShield;
    private float KillCooldown;
    public int UsePetCooldown;
    #endregion
    public override bool GetGameStartSound(out string sound)
    {
        var soundId = Random.Range(1, 3);
        sound = $"Join{soundId}";
        return true;

    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetVagor);
        sender.Writer.Write(ElementPowerCount);
        sender.Writer.Write(NormalKillCount);
        sender.Writer.Write(KillCount);
        sender.Writer.Write(SkillCount);
        sender.Writer.Write(ShieldTimes);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetVagor) return;
        ElementPowerCount = reader.ReadInt32();
        NormalKillCount = reader.ReadInt32();
        KillCount = reader.ReadInt32();
        SkillCount = reader.ReadInt32();
        ShieldTimes = reader.ReadInt32();
    }
    public bool CanUseKillButton() => true;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public float CalculateKillCooldown() => KillCooldown;
    private static bool OnCheckMurderPlayerOthers_Before(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        if (!IsShield || !target.Is(CustomRoles.Vagor_FAFL)) return true;
        if (ShieldTimes >= 3)
        {
            ShieldTimes = 0;
            IsShield = false;
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            killer.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer();
        }
        else
        {
            ShieldTimes++;
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            killer.RpcProtectedMurderPlayer(target);
        }
        return false;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        if (!IsFallen)
        {
            if (killer.CheckDoubleTrigger(target, () => { NormalKill(killer, target, info); }))
            {
                var killpercent = Random.Range(0, 100);
                if (killpercent <= 2)
                {
                    KillCount = 0;
                    IsFallen = true;
                    killer.RpcMurderPlayerV2(target);
                    ElementPowerCount++;
                    SendRPC();
                }
                else
                {
                    KillCount++;
                    killer.RpcProtectedMurderPlayer(target);
                    SendRPC();
                }
                NormalKillCount = 0;
                KillCooldown = 20f;
                killer.ResetKillCooldown();
                killer.SyncSettings();
            }
        }
        else
        {
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc != Player)
                {
                    var posi = Player.transform.position;
                    var diss = Vector2.Distance(posi, pc.transform.position);
                    if (diss < 1f)
                    {
                        killer.RpcMurderPlayerV2(pc);
                        pc.SetRealKiller(Player);
                        ElementPowerCount++;
                        SendRPC();
                    }
                }
            }
            NormalKillCount = 0;
            KillCooldown = 20f;
            killer.ResetKillCooldown();
            killer.SyncSettings();
        }
        //切れない相手ならキルキャンセル
        return false;
    }
    public bool IsKiller { get; private set; } = true;
    public void NormalKill(PlayerControl killer, PlayerControl target, MurderInfo info)
    {
        var killpercent = Random.Range(0, 100);
        if (NormalKillCount < 6)
        {
            NormalKillCount++;
            KillCooldown = 0f;
            killer.ResetKillCooldown();
            killer.SyncSettings();
            SendRPC();
        }
        else
        {
            NormalKillCount = 0;
            KillCooldown = 20f;
            killer.ResetKillCooldown();
            killer.SyncSettings();
            SendRPC();
        }
        if (killpercent <= 1 || KillCount >= 90)
        {
            KillCount = 0;
            killer.RpcMurderPlayerV2(target);
            ElementPowerCount++;
            SendRPC();
        }
        else
        {
            KillCount++;
            killer.RpcProtectedMurderPlayer(target);
            SendRPC();
        }
    }
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (UsePetCooldown == 0 || !Options.UsePets.GetBool()) return;
        if (UsePetCooldown >= 1 && Player.IsAlive() && !GameStates.IsMeeting) UsePetCooldown -= 1;
        if (UsePetCooldown <= 0 && Player.IsAlive())
        {
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")), 2f);
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!seer == PlayerControl.LocalPlayer) return"";
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return $"{GetString("VagorKillCount")}:{KillCount},{GetString("VagorSkillCount")}:{SkillCount},{GetString("VagorElementPowerCount")}:{ElementPowerCount}";

    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (seer != seen || seer == PlayerControl.LocalPlayer) return "";
        return $"\n<color=#e6adoa>{GetString("VagorKillCount")}:{KillCount},{GetString("VagorSkillCount")}:{SkillCount},{GetString("VagorElementPowerCount")}:{ElementPowerCount}</color>";

    }
    public override void OnUsePet()
    {
        if (!Options.UsePets.GetBool()) return;
        if (UsePetCooldown != 0)
        {
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), UsePetCooldown, 1f));
            return;
        }
        if (ElementPowerCount < 20)
        {
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (IsShield)
                {

                    LateTask task = new LateTask(null, new(), "");
                    for (int i = 0; i < LateTask.Tasks.Count; i++)
                    {
                        task = LateTask.Tasks[i];
                        if (task.name == "ZhongLiShield")
                        {
                            LateTask.Tasks.Remove(task);
                            break;
                        }
                    }
                }
                UsePetCooldown = 12;
                var posi = Player.transform.position;
                var diss = Vector2.Distance(posi, pc.transform.position);
                var killpercent = Random.Range(0, 100);
                if (diss < 1f)
                {
                    ElementPowerCount++;
                    if ((killpercent <= 1 || SkillCount >= 90)&& pc != Player)
                    {
                        SkillCount = 0;
                        Player.RpcMurderPlayerV2(pc);
                        pc.SetRealKiller(Player);
                        ElementPowerCount++;
                        SendRPC();
                    }
                    else
                    {
                        SkillCount++;
                        SendRPC();
                    }
                    IsShield = true;
                    new LateTask(() =>
                    {
                        IsShield = false;
                    }, 20f, "ZhongLiShield");
                }
            }
            var soundId = Random.Range(1, 3);
            Player.RPCPlayCustomSound($"ElementSkill{soundId}");
        }
        else
        {
            UsePetCooldown = 12;
            ElementPowerCount -= 20;
            SendRPC();
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc != Player)
                {
                    var posi = Player.transform.position;
                    var diss = Vector2.Distance(posi, pc.transform.position);
                    var killpercent = Random.Range(0, 100);
                    if (diss < 5f)
                    {
                        ElementPowerCount++;
                        if (killpercent <= 1 || SkillCount >= 90)
                        {
                            SkillCount = 0;
                            Player.RpcMurderPlayerV2(pc);
                            pc.SetRealKiller(Player);
                            ElementPowerCount++;
                            SendRPC();
                        }
                        else
                        {
                            SkillCount++;
                            SendRPC();
                            var ProtectStartTime = Utils.GetTimeStamp();
                            
                            if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
                            Player.Notify(GetString("BeGeo"));
                            List<byte> TimeStopsstop = new();
                            if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) continue;
                            NameNotifyManager.Notify(pc, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Vagor_FAFL), GetString("ForZhongLi")));
                            var tmpSpeed1 = Main.AllPlayerSpeed[pc.PlayerId];
                            TimeStopsstop.Add(pc.PlayerId);
                            Main.AllPlayerSpeed[pc.PlayerId] = Main.MinSpeed;
                            pc.MarkDirtySettings();
                            new LateTask(() =>
                            {
                                Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] - Main.MinSpeed + tmpSpeed1;
                                pc.MarkDirtySettings();
                                TimeStopsstop.Remove(pc.PlayerId);
                                RPC.PlaySoundRPC(pc.PlayerId, Sounds.TaskComplete);
                            }, 5f, "ZhongLi Stop");


                        }
                    }
                }

            }
            var soundId = Random.Range(1, 3);
            Player.RPCPlayCustomSound($"ElementMaxi{soundId}");
        }
    }
    public override void AfterMeetingTasks()
    {
        KillCooldown = 20f;
        UsePetCooldown = 12;
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        
        if (!IsShield || seer != seen) return "";
        return Utils.ColorString(RoleInfo.RoleColor, "●");
    }
    public override bool GetPetButtonText(out string text)
    {
        if (ElementPowerCount < 20)
        {
            text = GetString("CoreOfTheLand");
            return true;
        }
        else
        {
            text = GetString("StarFromHeaven");
            return true;
        }
        
    }
    public override bool GetPetButtonSprite(out string buttonName)
    {
        if (ElementPowerCount < 20)
        {
            buttonName = "Core";
            return true;
        }
        else
        {
            buttonName = "Star";
            return true;
        }
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("ZhongLiKillButtonText");
        return true;
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = "RainOfGeo";
        return true;
    }
}*/
///*v1.1
public sealed class Vagor_FAFL : RoleBase, INeutralKilling, IKiller, IIndependent
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Vagor_FAFL),
            player => new Vagor_FAFL(player),
            CustomRoles.Vagor_FAFL,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            7565_1_1_1,
            null,
            "Zhongli|Vagor|帝君|闲游",
             "#E6AD0A",
            true,
            true,
            countType: CountTypes.FAFL
#if RELEASE
,
            Hidden: true
#endif
        );
    public Vagor_FAFL(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_Before);
        ElementPowerCount = 0;
        NormalKillTimesCount = 0;
        KillTimesTotalCount = 0;
        SkillTimesTotalCount = 0;
        ShieldsCount = 0;
        Feeble = new(15);
    }

    #region 参数
    public static int ElementPowerCount;
    public static int NormalKillTimesCount;
    public static int KillTimesTotalCount;
    public static int SkillTimesTotalCount;
    public static int ShieldsCount;
    public static List<byte> Feeble;
    private float KillCooldown;
    public int UsePetCooldown;
    #endregion
    public override bool GetGameStartSound(out string sound)
    {
        var soundId = Random.Range(1, 3);
        sound = $"Join{soundId}";
        return true;
    }
    #region RPC相关
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetVagor);
        sender.Writer.Write(ElementPowerCount);
        sender.Writer.Write(NormalKillTimesCount);
        sender.Writer.Write(KillTimesTotalCount);
        sender.Writer.Write(SkillTimesTotalCount);
        sender.Writer.Write(ShieldsCount);
        
    }
    private void SendRPC_AddFeeble()
    {
        using var sender = CreateSender(CustomRPC.AddFeeble);
        foreach (var pid in Feeble)
            sender.Writer.Write(pid);
    }
    private void SendRPC_RemoveFeeble()
    {
        using var sender = CreateSender(CustomRPC.RemoveFeeble);
        foreach (var pid in Feeble)
            sender.Writer.Write(pid);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType == CustomRPC.SetVagor)
        {
            ElementPowerCount = reader.ReadInt32();
            NormalKillTimesCount = reader.ReadInt32();
            KillTimesTotalCount = reader.ReadInt32();
            SkillTimesTotalCount = reader.ReadInt32();
            ShieldsCount = reader.ReadInt32();
        }
        else if (rpcType == CustomRPC.AddFeeble)
        {
            var pid = reader.ReadByte();
            if (!Feeble.Contains(pid))
                 Feeble.Add(pid);
        }
        else if (rpcType == CustomRPC.RemoveFeeble)
        {
            var pid = reader.ReadByte();
            if (Feeble.Contains(pid))
                Feeble.Remove(pid);
        }
    }
    #endregion
    public bool CanUseKillButton() => true;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public float CalculateKillCooldown() => KillCooldown;
    private static bool OnCheckMurderPlayerOthers_Before(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        if (ShieldsCount > 0 && target.Is(CustomRoles.Vagor_FAFL))
        {
            ShieldsCount--;
            killer.RpcProtectedMurderPlayer();
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            target.RpcProtectedMurderPlayer();
            return false;
        }
        return true;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        var killpercent = Random.Range(0, 100);
        float killsucceed = 5;
        if (Feeble.Contains(target.PlayerId))
            killsucceed += killsucceed * 1.5f;
        if (NormalKillTimesCount < 6)
        {
            NormalKillTimesCount++;
            KillCooldown = 2f;
            killer.ResetKillCooldown();
            killer.SyncSettings();
            SendRPC();
        }
        else
        {
            NormalKillTimesCount = 0;
            KillCooldown = 20f;
            killer.ResetKillCooldown();
            killer.SyncSettings();
            SendRPC();
        }
        if (killpercent <= killsucceed || KillTimesTotalCount >= 80)
        {
            KillTimesTotalCount = 0;
            ElementPowerCount++;
            SendRPC();
            return true;
        }
        else
        {
            KillTimesTotalCount++;
            killer.RpcProtectedMurderPlayer(target);
            SendRPC();
        }
        return false;
    }
    public bool IsKiller { get; private set; } = true;
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (UsePetCooldown == 0 || !Options.UsePets.GetBool()) return;
        if (UsePetCooldown >= 1 && Player.IsAlive() && !GameStates.IsMeeting) UsePetCooldown -= 1;
        if (UsePetCooldown <= 0 && Player.IsAlive())
        {
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")), 2f);
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!seer.IsModClient()) return"";
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return $"{GetString("VagorKillTimesTotalCount")}:{KillTimesTotalCount},{GetString("VagorSkillTimesTotalCount")}:{SkillTimesTotalCount},{GetString("VagorElementPowerCount")}:{ElementPowerCount}";

    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (seer != seen || seer.IsModClient()) return "";
        return $"\n<color=#e6adoa>{GetString("VagorKillTimesTotalCount")}:{KillTimesTotalCount},{GetString("VagorSkillTimesTotalCount")}:{SkillTimesTotalCount},{GetString("VagorElementPowerCount")}:{ElementPowerCount}</color>";

    }
    public override void OnUsePet()
    {
        if (UsePetCooldown != 0)
        {
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), UsePetCooldown, 1f));
            return;
        }
        float killsucceed = 5;
        var killpercent = Random.Range(0, 100);
        var soundId = Random.Range(1, 3);
        var feb = false;
        List<PlayerControl> maydielist = new(14);
        if (ElementPowerCount < 20)
        {
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                var posi = Player.transform.position;
                var diss = Vector2.Distance(posi, pc.transform.position);
                if (pc != Player && diss <= 2.5f)
                {
                    if (Feeble.Contains(pc.PlayerId) && !feb)
                    {
                        killsucceed += killsucceed * 1.5f;
                        feb = true;
                    }
                    ElementPowerCount++;
                    maydielist.Add(pc);
                    Feeble.Add(pc.PlayerId);
                    SendRPC_AddFeeble();
                    new LateTask(() =>
                    {
                        if (Feeble.Contains(pc.PlayerId))
                        {
                            Feeble.Remove(pc.PlayerId);
                            SendRPC_RemoveFeeble();
                        }
                    }, 40f, "ZhongLiShield");
                }
            }
            ElementPowerCount++;
            ShieldsCount += 2;
            SkillTimesTotalCount++;
            Player.RPCPlayCustomSound($"ElementSkill{soundId}");
            new LateTask(() =>
            {
                if (ShieldsCount >= 2)
                    ShieldsCount -= 2;
                else
                    ShieldsCount = 0;
            }, 20f, "ZhongLiShield");
            
        }
        else 
        {
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                var posi = Player.transform.position;
                var diss = Vector2.Distance(posi, pc.transform.position);
                if (pc != Player)
                {
                    if (diss < 5f)
                    {
                        if (Feeble.Contains(pc.PlayerId) && !feb)
                        {
                            killsucceed += killsucceed * 1.5f;
                            feb = true;
                        }
                        ElementPowerCount++;
                        SendRPC();
                        var ProtectStartTime = Utils.GetTimeStamp();
                        if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
                        Player.Notify(GetString("BeGeo"));
                        List<byte> TimeStopsstop = new();
                        if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) continue;
                        NameNotifyManager.Notify(pc, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Vagor_FAFL), GetString("ForZhongLi")));
                        var tmpSpeed1 = Main.AllPlayerSpeed[pc.PlayerId];
                        TimeStopsstop.Add(pc.PlayerId);
                        Main.AllPlayerSpeed[pc.PlayerId] = Main.MinSpeed;
                        ReportDeadBodyPatch.CanReport[pc.PlayerId] = false;
                        pc.MarkDirtySettings();
                        new LateTask(() =>
                        {
                            Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] - Main.MinSpeed + tmpSpeed1;
                            ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                            pc.MarkDirtySettings();
                            TimeStopsstop.Remove(pc.PlayerId);
                            RPC.PlaySoundRPC(pc.PlayerId, Sounds.TaskComplete);
                        }, 5f, "ZhongLi ");
                    }
                    Player.RPCPlayCustomSound($"ElementMaxi{soundId}");
                    maydielist.Add(pc);
                }
            }
            ElementPowerCount -= 20;
            if (ElementPowerCount >= 10)
                new LateTask(() =>
                {
                    if (ShieldsCount >= 1)
                        ShieldsCount -= 1;
                }, 20f, "ZhongLiShield");
            SkillTimesTotalCount++;
            SendRPC();
        }
        if (killpercent <= killsucceed || SkillTimesTotalCount >= 80)
            foreach (var pc in maydielist)
            {
                SkillTimesTotalCount = 0;
                ElementPowerCount++;
                SendRPC();
                Player.RpcMurderPlayerV2(pc);
                pc.SetRealKiller(Player);
            }
        UsePetCooldown = 20;
        ElementPowerCount++;
    }
    public override void AfterMeetingTasks()
    {
        KillCooldown = 20f;
        UsePetCooldown = 20;
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {

        if (Feeble.Contains(seen.PlayerId)) return "🔻";
        else if (seer == seen)
        return Utils.ColorString(RoleInfo.RoleColor, $"({ShieldsCount})");
        return "";
    }
    public override bool GetPetButtonText(out string text)
    {
        if (ElementPowerCount < 20)
        {
            text = GetString("CoreOfTheLand");
            return true;
        }
        else
        {
            text = GetString("StarFromHeaven");
            return true;
        }
        
    }
    public override bool GetPetButtonSprite(out string buttonName)
    {
        if (ElementPowerCount < 20)
        {
            buttonName = "Core";
            return true;
        }
        else
        {
            buttonName = "Star";
            return true;
        }
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("ZhongLiKillButtonText");
        return true;
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = "RainOfGeo";
        return true;
    }
}
//*/
