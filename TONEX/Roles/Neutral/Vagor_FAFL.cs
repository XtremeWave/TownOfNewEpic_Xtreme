using AmongUs.GameOptions;
using static TONEX.Translator;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;
using MS.Internal.Xml.XPath;
using static UnityEngine.GraphicsBuffer;
using TONEX.Roles.Neutral;
using System.Collections.Generic;
using Hazel;
using static Il2CppSystem.Net.Http.Headers.Parser;
using TONEX.Modules;

namespace TONEX.Roles.Neutral;

public sealed class Vagor_FAFL : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Vagor_FAFL),
            player => new Vagor_FAFL(player),
            CustomRoles.Vagor_FAFL,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            7565_1_1_1,
#if DEBUG
            SetupOptionItem,
#else
            null,
#endif

            "Zhongli|Vagor|帝君|闲游",
             "#E6AD0A",
            true,
            countType: CountTypes.FAFL
        );
    public Vagor_FAFL(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.OnCheckMurderPlayerOthers_Before.Add(OnCheckMurderPlayerOthers_Before);
        IsFallen = false;
        IsShield = false;
        ElementPowerCount = 0;
        NormalKillCount = 0;
        KillCount = 0;
        SkillCount = 0;
        ShieldTimes = 0;
    }
    public static int ElementPowerCount;
    public static bool IsFallen;
    public static int NormalKillCount;
    public static int KillCount;
    public static int SkillCount;
    public static int ShieldTimes;
    public static bool IsShield;
    private float KillCooldown;
    public int UsePetCooldown;
    private static void SetupOptionItem()
    {

    }
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
        var (killer, target) = info.AttemptTuple;
        if (!IsShield || !target.Is(CustomRoles.Vagor_FAFL)) return true;
        if (ShieldTimes >= 3)
        {
            ShieldTimes = 0;
            IsShield = false;
            target.RpcProtectedMurderPlayer();
        }
        else
        {
            ShieldTimes += 1;
        }
        return false;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
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
                    ElementPowerCount += 1;
                    SendRPC();
                }
                else
                {
                    KillCount++;
                    killer.RpcProtectedMurderPlayer(target);
                    SendRPC();
                }

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
                        ElementPowerCount += 1;
                        SendRPC();
                    }
                }
            }
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
            NormalKillCount += 1;
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
            ElementPowerCount += 1;
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
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return $"{GetString("VagorKillCount")}:{KillCount},{GetString("VagorSkillCount")}:{SkillCount},{GetString("VagorElementPowerCount")}:{ElementPowerCount}";

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
                    ElementPowerCount += 1;
                    if ((killpercent <= 1 || SkillCount >= 90)&& pc != Player)
                    {
                        SkillCount = 0;
                        Player.RpcMurderPlayerV2(pc);
                        pc.SetRealKiller(Player);
                        ElementPowerCount += 1;
                        SendRPC();
                    }
                    else
                    {
                        SkillCount += 1;
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
                        ElementPowerCount += 1;
                        if (killpercent <= 1 || SkillCount >= 90)
                        {
                            SkillCount = 0;
                            Player.RpcMurderPlayerV2(pc);
                            pc.SetRealKiller(Player);
                            ElementPowerCount += 1;
                            SendRPC();
                        }
                        else
                        {
                            SkillCount += 1;
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
        if (!IsShield) return "";
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
}