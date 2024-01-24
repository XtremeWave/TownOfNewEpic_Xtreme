using AmongUs.GameOptions;
using static TONEX.Translator;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;
using System.Collections.Generic;
using Hazel;
using System.Linq;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Neutral;
public sealed class Non_Villain : RoleBase, IKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Non_Villain),
            player => new Non_Villain(player),
            CustomRoles.Non_Villain,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            7565_2_1_0,
null,
            "恭喜发财|刘德华|商场|Non_Villain|不演反派",
             "#FF0000",
            true,
            countType: CountTypes.None
#if RELEASE
,
assignCountRule: new(1, 1, 1)
#endif
        );
    public Non_Villain(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_After);
        DigitalLifeList = new();
        MoneyCount = new();
        WealthAndBrillianceDictionary = new();
        ComeAndAwayList = new();
        OvercomeList = new();
        FarAheadList = new();
        EtiquetteList = new();
    }
    #region 参数
    public static Dictionary<byte, int> MoneyCount;
    public static Dictionary<byte, int> WealthAndBrillianceDictionary;
    public static List<byte> ComeAndAwayList;
    public static List<byte> OvercomeList;
    public static List<byte> FarAheadList;
    public static List<byte> HasFarAheadList;
    public static List<byte> EtiquetteList;
    public static List<byte> DigitalLifeList;
    private float KillCooldown;
    #endregion
    #region 外部函数
    public bool CanUseKillButton() => true;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public float CalculateKillCooldown() => KillCooldown;
    public bool IsKiller { get; private set; } = true;
    #endregion
    #region 被击杀事件RPC
    public static void SendRPC_ForBeKilled(byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ForNVBeKilled, SendOption.Reliable, -1);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRPC_StaticOvercomeList(byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ForNVStaticOvercomeList, SendOption.Reliable, -1);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRPC_StaticFarAheadList(byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ForNVStaticFarAheadList, SendOption.Reliable, -1);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    #endregion
    #region 击杀事件&秒更新的RPC
    public void SendRPC_MoneyCount(byte targetId, int money)
    {
        using var sender = CreateSender(CustomRPC.ForNVMoney);
        sender.Writer.Write(targetId);
        sender.Writer.Write(money);
    }
    public void SendRPC_WealthAndBrillianceDictionary(byte targetId)
    {
        using var sender = CreateSender(CustomRPC.ForNVWAH);
        sender.Writer.Write(targetId);
    }
    public void SendRPC_ComeAndAwayList(byte targetId)
    {
        using var sender = CreateSender(CustomRPC.ForNVCAAList);
        sender.Writer.Write(targetId);
    }
    public void SendRPC_OvercomeList(byte targetId)
    {
        using var sender = CreateSender(CustomRPC.ForNVOvercomeList);
        sender.Writer.Write(targetId);
    }
    public void SendRPC_FarAheadList(byte targetId)
    {
        using var sender = CreateSender(CustomRPC.ForNVFarAheadList);
        sender.Writer.Write(targetId);
    }
    public void SendRPC_DFList(byte targetId)
    {
        using var sender = CreateSender(CustomRPC.ForNVFarAheadList);
        sender.Writer.Write(targetId);
    }
    #endregion
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType.IsNVRPC())
        {
            var targetId = reader.ReadByte();
            switch (rpcType)
            {
                case CustomRPC.ForNVBeKilled:
                    MoneyCount[targetId] = 0;
                    if (WealthAndBrillianceDictionary.ContainsKey(targetId))
                    {
                        WealthAndBrillianceDictionary.Remove(targetId);
                    }
                    if (ComeAndAwayList.Contains(targetId))
                    {
                        ComeAndAwayList.Remove(targetId);
                    }
                    if (OvercomeList.Contains(targetId))
                    {
                        OvercomeList.Remove(targetId);
                    }
                    if (FarAheadList.Contains(targetId))
                    {
                        FarAheadList.Remove(targetId);
                    }
                    if (!EtiquetteList.Contains(targetId))
                    {
                        EtiquetteList.Add(targetId);
                    }
                    break;
                case CustomRPC.ForNVStaticOvercomeList:
                    if (OvercomeList.Contains(targetId))
                    {
                        OvercomeList.Remove(targetId);
                    }
                    break;
                case CustomRPC.ForNVStaticFarAheadList:
                    if (FarAheadList.Contains(targetId))
                    {
                        FarAheadList.Remove(targetId);
                    }
                    break;
                case CustomRPC.ForNVMoney:
                    var money = reader.ReadInt32();
                    MoneyCount[targetId] = money;
                    break;
                case CustomRPC.ForNVWAH:
                    if (!WealthAndBrillianceDictionary.ContainsKey(targetId))
                    {
                        WealthAndBrillianceDictionary.TryAdd(targetId, 1);
                    }
                    else
                    {
                        WealthAndBrillianceDictionary[targetId] += 1;
                    }
                    break;
                case CustomRPC.ForNVCAAList:
                    if (!ComeAndAwayList.Contains(targetId))
                    {
                        ComeAndAwayList.Add(targetId);
                    }
                    break;
                case CustomRPC.ForNVOvercomeList:
                    if (!OvercomeList.Contains(targetId))
                    {
                        OvercomeList.Add(targetId);
                    }
                    break;
                case CustomRPC.ForNVFarAheadList:
                    if (!FarAheadList.Contains(targetId))
                    {
                        FarAheadList.Add(targetId);
                    }
                    if (!HasFarAheadList.Contains(targetId))
                    {
                        HasFarAheadList.Add(targetId);
                    }
                    break;
                case CustomRPC.ForNVDFList:
                    if (!DigitalLifeList.Contains(targetId))
                    {
                        DigitalLifeList.Add(targetId);
                    }
                    break;

            }
        }
        else return;
    }
    public override void Add()
    {
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc == Player) continue;
            MoneyCount.Add(pc.PlayerId, 0);
        }
    }
    private static void SetupOptionItem()
    {
    }
    private static bool OnCheckMurderPlayerOthers_After(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (target.Is(CustomRoles.Non_Villain))
        {
            var realkill = target.GetRealKiller();
            EtiquetteList.Add(realkill.PlayerId);
            Main.CantUseSkillList.Add(realkill);
            MoneyCount[realkill.PlayerId] = 0;
            if (WealthAndBrillianceDictionary.ContainsKey(realkill.PlayerId))
            {
                WealthAndBrillianceDictionary.Remove(realkill.PlayerId);
            }
            if (ComeAndAwayList.Contains(realkill.PlayerId))
            {
                ComeAndAwayList.Remove(realkill.PlayerId);
            }
            if (OvercomeList.Contains(realkill.PlayerId))
            {
                OvercomeList.Remove(realkill.PlayerId);
            }
            if (FarAheadList.Contains(realkill.PlayerId))
            {
                FarAheadList.Remove(realkill.PlayerId);
            }
            if (DigitalLifeList.Contains(realkill.PlayerId))
            {
                DigitalLifeList.Remove(realkill.PlayerId);
            }
            SendRPC_ForBeKilled(realkill.PlayerId);
        }
        else if (OvercomeList.Contains(target.PlayerId))
        {
            OvercomeList.Remove(target.PlayerId);
            SendRPC_StaticOvercomeList(target.PlayerId);
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            killer.RpcProtectedMurderPlayer(target);
            return false;
        }
        else if (FarAheadList.Contains(target.PlayerId))
        {
            FarAheadList.Remove(target.PlayerId);
            SendRPC_StaticFarAheadList(target.PlayerId);
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            killer.RpcProtectedMurderPlayer(target);
            Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] * (10 / Main.AllPlayerSpeed[target.PlayerId]);
            return false;
        }
        return true;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        ;
        var (killer, target) = info.AttemptTuple;
        var blessing = Random.Range(1, 3);
        var money = MoneyCount[target.PlayerId];
        money += Random.Range(100, 1000);

        new LateTask(() =>
        {
            if (WealthAndBrillianceDictionary.ContainsKey(target.PlayerId) && WealthAndBrillianceDictionary[target.PlayerId] >= 3 && ComeAndAwayList.Contains(target.PlayerId) && OvercomeList.Contains(target.PlayerId))
                money += 1000;
            else
            {
            Retry:
                switch (blessing)
                {
                    case 1:
                        if (!WealthAndBrillianceDictionary.ContainsKey(target.PlayerId))
                        {
                            WealthAndBrillianceDictionary.TryAdd(target.PlayerId, 1);
                            SendRPC_WealthAndBrillianceDictionary(target.PlayerId);
                        }
                        else if (WealthAndBrillianceDictionary[target.PlayerId] < 3)
                        {
                            WealthAndBrillianceDictionary[target.PlayerId]++;
                            SendRPC_WealthAndBrillianceDictionary(target.PlayerId);
                        }
                        else
                        {
                            blessing = Random.Range(2, 3);
                            goto Retry;
                        }
                        break;
                    case 2:
                        if (!ComeAndAwayList.Contains(target.PlayerId))
                        {
                            ComeAndAwayList.Add(target.PlayerId);
                            SendRPC_ComeAndAwayList(target.PlayerId);
                        }
                        else
                        {
                            blessing = Random.Range(1, 3);
                            goto Retry;
                        }
                        break;
                    case 3:
                        if (!OvercomeList.Contains(target.PlayerId))
                        {
                            OvercomeList.Add(target.PlayerId);
                            SendRPC_OvercomeList(target.PlayerId);
                        }
                        else
                        {
                            blessing = Random.Range(1, 2);
                            goto Retry;
                        }
                        break;
                }
            }
            MoneyCount[target.PlayerId] = money;
            SendRPC_MoneyCount(target.PlayerId, money);
            Utils.NotifyRoles(target);

        }, 2f, "RedPackageAndBlessing");
        return false;
    }
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc == Player) continue;
            if (EtiquetteList.Contains(pc.PlayerId)) continue;
            int money = MoneyCount[pc.PlayerId];
            if (WealthAndBrillianceDictionary.ContainsKey(pc.PlayerId))
            {
                var Multiply = WealthAndBrillianceDictionary[pc.PlayerId];
                money += 20 * Multiply;
            }
            if (FarAheadList.Contains(pc.PlayerId))
            {
                money += 25;
            }
            if (money >= 7500 && !FarAheadList.Contains(pc.PlayerId))
            {
                money -= 7500;
                FarAheadList.Add(pc.PlayerId);
                HasFarAheadList.Add(pc.PlayerId);
                SendRPC_FarAheadList(pc.PlayerId);
                Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] + Main.AllPlayerSpeed[pc.PlayerId] * 0.1f;
            }
            if (money >= 5000 && HasFarAheadList.Contains(pc.PlayerId) && !DigitalLifeList.Contains(pc.PlayerId))
            {
                money -= 5000;
                DigitalLifeList.Add(pc.PlayerId);
                SendRPC_DFList(pc.PlayerId);
            }
            MoneyCount[pc.PlayerId] = money;
            SendRPC_MoneyCount(pc.PlayerId, money);
            Utils.NotifyRoles(pc);
        }
    }
    public override void AfterMeetingTasks()
    {
        KillCooldown = 5f;
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        int money;
        int blessingcount = 0;
        string blessings;
        var isdf = "";
        if (seen.Is(CustomRoles.Non_Villain)) return "";
        if (!WealthAndBrillianceDictionary.ContainsKey(seen.PlayerId)&& !OvercomeList.Contains(seen.PlayerId)&&!ComeAndAwayList.Contains(seen.PlayerId) && !FarAheadList.Contains(seen.PlayerId) && !EtiquetteList.Contains(seen.PlayerId) &&  !DigitalLifeList.Contains(seen.PlayerId))
        {
            blessings = $"<color=#AAAAAA>{GetString("Non_Blessing")}</color>";
        }
        else
        {
            
            blessings = "";
            if (DigitalLifeList.Contains(seen.PlayerId))
            {
                isdf = "<color=#00D6BC>▲</color>";
            }
            blessings += $"{GetString("Blessing")}";
            if (WealthAndBrillianceDictionary.ContainsKey(seen.PlayerId))
            {
                blessingcount += 1;
                blessings += $"\n{GetString("Blessing1")} * {WealthAndBrillianceDictionary[seen.PlayerId]}";
            }
            if (ComeAndAwayList.Contains(seen.PlayerId))
            {
                blessingcount += 1;
                blessings += $"\n{GetString("Blessing2")}";
            }
            if (blessingcount >= 2)
                blessings += "\n";
            if (OvercomeList.Contains(seen.PlayerId))
            {
                blessingcount += 1;
                blessings += $"\n{GetString("Blessing3")}";
            }
            if (blessingcount >= 2)
                blessings += "\n";
            if (FarAheadList.Contains(seen.PlayerId)) 
            {
                blessingcount += 1;
                blessings += $"\n{GetString("Blessing4")}";
            }
            if (EtiquetteList.Contains(seen.PlayerId))
            {
                blessings = $"{GetString("Etiquette")}";
            }
        }
        if (!MoneyCount.ContainsKey(seen.PlayerId))
        {
            money = 0;
        }
        else
        {
            money = MoneyCount[seen.PlayerId];
        }
        return (seer == seen || seer.Is(CustomRoles.Non_Villain)) ? $"({isdf}<color=#ffff00>{GetString("MoneyCount")}: {money}</color>, {blessings})" : "";
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("LiuDeHuaKillButtonText");
        return true;
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = "RedPackage";
        return true;
    }
    public bool CheckWin(ref CustomRoles winnerRole, ref CountTypes winnerCountType)
    {
        List<CountTypes> refe = new();
        if (Player.IsAlive())
        {
            
            Dictionary<CountTypes, int> countTypeMapping = new();
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (!countTypeMapping.ContainsKey(pc.GetCountTypes()))
                countTypeMapping.Add(pc.GetCountTypes(), MoneyCount[pc.PlayerId]);
                else countTypeMapping[pc.GetCountTypes()]+= MoneyCount[pc.PlayerId];
                
            }
            refe.Add(countTypeMapping.OrderByDescending(kvp => kvp.Value).First().Key);
        }
        else
        {
            foreach (var pc in DigitalLifeList)
            {
                foreach (var player in Main.AllPlayerControls)
                {
                    if (player.PlayerId == pc)
                        if (!refe.Contains(player.GetCountTypes()))
                            refe.Add(player.GetCountTypes());
                }
            }
        }
        return refe.Contains(winnerCountType);
    }
}