using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System.Linq;
using UnityEngine;
using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TONEX.Modules;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.AddOns.Crewmate;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Impostor;
using static TONEX.Translator;
using TONEX.Attributes;
using static UnityEngine.GraphicsBuffer;
using static TONEX.Modules.MeetingVoteManager;
using TONEX.Roles.GameModeRoles;

namespace TONEX;

internal static class HotPotatoManager
{
    public static int RoundTime = new();
    public static int BoomTimes = new();
    public static int HotPotatoMax = new();
    public static int  IsAliveHot = new();
    public static int IsAliveCold = new();
    public static bool IsDistribution = true;
    //设置11

    public static OptionItem HotQuan;//热土豆数量
    public static OptionItem Boom; //爆炸时间;Remaining time of explosion
    public static OptionItem TD;//总时长;Totalduration

    public static void SetupCustomOption()
    {
        HotQuan = IntegerOptionItem.Create(62_293_009, "HotMax", new(1, 4, 1), 2, TabGroup.GameSettings, false)
           .SetGameMode(CustomGameMode.HotPotato)
           .SetColor(new Color32(245, 82, 82, byte.MaxValue))
           .SetValueFormat(OptionFormat.Players);
        Boom = IntegerOptionItem.Create(62_293_008, "BoomTime", new(10, 60, 5), 15, TabGroup.GameSettings, false)
           .SetGameMode(CustomGameMode.HotPotato)
           .SetColor(new Color32(245, 82, 82, byte.MaxValue))
           .SetValueFormat(OptionFormat.Seconds);
        TD = IntegerOptionItem.Create(62_293_010, "RoundTime", new(100, 300, 20), 300, TabGroup.GameSettings, false)
      .SetGameMode(CustomGameMode.HotPotato)
      .SetColor(new Color32(245, 82, 82, byte.MaxValue))
      .SetValueFormat(OptionFormat.Seconds);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.HotPotato) return;
        BoomTimes = Boom.GetInt() + 9;
        RoundTime = TD.GetInt() + 9 + BoomTimes;
        HotPotatoMax = HotQuan.GetInt();
        IsAliveCold = 0;
        IsAliveHot = 0;
        IsDistribution = false;
    }
    public static void OnFixedUpdate()
    {
        if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.HotPotato || !AmongUsClient.Instance.AmHost || Main.AllAlivePlayerControls.ToList().Count == 0) return;
        if (!IsDistribution)
        {
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                pc.Notify(GetString("GameStartHP"));
            }
            new LateTask(() =>
            {
                List<PlayerControl> HotPotatoList = new();
                if (Main.AllAlivePlayerControls.ToList().Count <= HotPotatoMax + 1)
                {
                    HotPotatoMax = 1;
                }
                for (int i = 0; i < HotPotatoMax; i++)
                {
                    var pcList = Main.AllAlivePlayerControls.Where(x => x.GetCustomRole() != CustomRoles.HotPotato).ToList();
                    var Ho = pcList[IRandom.Instance.Next(0, pcList.Count - 1)];
                    if (!HotPotatoList.Contains(Ho))
                    {
                        HotPotatoList.Add(Ho);
                    }
                    else
                    {
                        i--;
                    }
                }
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (HotPotatoList.Contains(pc))
                    {
                        pc.RpcSetCustomRole(CustomRoles.HotPotato);
                        IsAliveCold--;
                        IsAliveHot++;
                    }
                    pc.Notify(GetString("HotPotatoDistribution"));
                }
                IsDistribution = true;
            }, BoomTimes);
        }
        else
        {


            bool notifyRoles = false;
            //一些巴拉巴拉的东西
            var playerList = Main.AllAlivePlayerControls.ToList();
            if (playerList.Count == 1)
            {
                foreach (var cp in playerList)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.ColdPotato);
                    CustomWinnerHolder.WinnerIds.Add(cp.PlayerId);
                }
            }
            else if (RoundTime <= 0)
            {
                foreach (var cp in Main.AllAlivePlayerControls)
                {
                    if (cp.Is(CustomRoles.ColdPotato))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.ColdPotato);
                        CustomWinnerHolder.WinnerIds.Add(cp.PlayerId);
                    }
                }

            }
            //土豆数量检测
            if (playerList.Count >= 9 && playerList.Count <= 11 && HotPotatoMax >= 3)
            {
                HotPotatoMax = 2;
            }
            else if (playerList.Count >= 5 && playerList.Count <= 7 && HotPotatoMax >= 2)
            {
                HotPotatoMax = 1;
            }



            if (playerList.Count <= HotPotatoMax + 1)
            {
                HotPotatoMax = 1;
            }
            //爆炸时间为0时
            if (BoomTimes <= 0)
            {
                List<PlayerControl> HPL = new();
                BoomTimes = Boom.GetInt();
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.Is(CustomRoles.HotPotato)) pc.RpcMurderPlayerV2(pc);
                    Logger.Info($"炸死一群", "awa");
                }
                for (int i = 0; i < HotPotatoMax; i++)
                {
                    var pcList = Main.AllAlivePlayerControls.Where(x => x.GetCustomRole() != CustomRoles.HotPotato).ToList();
                    var HP = pcList[IRandom.Instance.Next(0, pcList.Count - 1)];
                    if (!HPL.Contains(HP))
                    {
                        IsAliveCold--;
                        IsAliveHot++;
                        HP.RpcSetCustomRole(CustomRoles.HotPotato);
                        HP.Notify(GetString("GetHotPotato"));
                        Logger.Info($"分配热土豆", "awa");  
                        var pos = HP.GetTruePosition();
                    float minDis = float.MaxValue;
                    string minName = string.Empty;
                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (pc.PlayerId == HP.PlayerId || !pc.Is(CustomRoles.ColdPotato)) continue;
                        if (pc.Is(CustomRoles.ColdPotato))
                        {
                        var dis = Vector2.Distance(pc.GetTruePosition(), pos);
                        if (dis < minDis && dis < 1.5f)
                        {
                            minDis = dis;
                            minName = pc.GetRealName();
                        }
                            LocateArrow.Add(HP.PlayerId, pc.transform.position);
                               ColdPotato.SendRPC(HP.PlayerId,true, pc.GetTruePosition());
                            }
                    }

                    }
                    else
                    {
                        i--;
                    }
                }
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                // 必要时刷新玩家名字
                if (notifyRoles) Utils.NotifyRoles(pc);
            }
        }
    }
    public static void OnSecondsUpdate()
    {
        if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.HotPotato || !AmongUsClient.Instance.AmHost) return;
        //减少爆炸冷却
        BoomTimes--;
        RoundTime--;
    }
 
}

