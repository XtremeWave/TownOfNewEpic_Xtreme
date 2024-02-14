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

public sealed class Vagator : RoleBase, INeutralKilling, IKiller, IIndependent
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Vagator),
            player => new Vagator(player),
            CustomRoles.Vagator,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            7565_1_1_1,
            null,
            "Zhongli|Vagator|帝君|闲游",
             "#E6AD0A",
            true,
            true,
            countType: CountTypes.FAFL
        );
    public Vagator(PlayerControl player)
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

    #region 全局变量
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
        using var sender = CreateSender(CustomRPC.SetVagator);
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
        if (rpcType == CustomRPC.SetVagator)
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
        if (ShieldsCount > 0 && target.Is(CustomRoles.Vagator))
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

        return $"{GetString("VagatorKillTimesTotalCount")}:{KillTimesTotalCount},{GetString("VagatorSkillTimesTotalCount")}:{SkillTimesTotalCount},{GetString("VagatorElementPowerCount")}:{ElementPowerCount}";

    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (seer != seen || seer.IsModClient()) return "";
        return $"\n<color=#e6adoa>{GetString("VagatorKillTimesTotalCount")}:{KillTimesTotalCount},{GetString("VagatorSkillTimesTotalCount")}:{SkillTimesTotalCount},{GetString("VagatorElementPowerCount")}:{ElementPowerCount}</color>";

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
                        List<byte> NiceTimeStopsstop = new();
                        if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) continue;
                        NameNotifyManager.Notify(pc, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Vagator), GetString("ForZhongLi")));
                        var tmpSpeed1 = Main.AllPlayerSpeed[pc.PlayerId];
                        NiceTimeStopsstop.Add(pc.PlayerId);
                        Main.AllPlayerSpeed[pc.PlayerId] = Main.MinSpeed;
                        ReportDeadBodyPatch.CanReport[pc.PlayerId] = false;
                        pc.MarkDirtySettings();
                        new LateTask(() =>
                        {
                            Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] - Main.MinSpeed + tmpSpeed1;
                            ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                            pc.MarkDirtySettings();
                            NiceTimeStopsstop.Remove(pc.PlayerId);
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
