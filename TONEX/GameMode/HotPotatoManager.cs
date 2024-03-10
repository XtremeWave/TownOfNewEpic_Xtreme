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


    public static void SetupCustomOption()
    {
        HotQuan = IntegerOptionItem.Create(62_293_009, "HotMax", new(1, 4, 1), 2, TabGroup.GameSettings, false)
           .SetGameMode(CustomGameMode.HotPotato)
           .SetColor(new Color32(245, 82, 82, byte.MaxValue))
           .SetHeader(true)
           .SetValueFormat(OptionFormat.Players);
        Boom = IntegerOptionItem.Create(62_293_008, "BoomTime", new(10, 60, 5), 15, TabGroup.GameSettings, false)
           .SetGameMode(CustomGameMode.HotPotato)
           .SetColor(new Color32(245, 82, 82, byte.MaxValue))
           .SetValueFormat(OptionFormat.Seconds);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.HotPotato) return;
        BoomTimes = Boom.GetInt() + 9;
        HotPotatoMax = HotQuan.GetInt();
        IsAliveCold = 0;
        IsAliveHot = 0;
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        private static long LastFixedUpdate = new();
        public static void Postfix(PlayerControl __instance)
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.HotPotato || !AmongUsClient.Instance.AmHost || Main.AllAlivePlayerControls.ToList().Count == 0) return;
            bool notifyRoles = true;
                //一些巴拉巴拉的东西
                var playerList = Main.AllAlivePlayerControls.ToList();
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
                        if (pc.Is(CustomRoles.HotPotato))
                        {
                            pc.RpcMurderPlayerV2(pc);
                            notifyRoles = true;
                        }


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
                            HP.Notify(GetString("GetHotPotato"), 1f);
                            Logger.Info($"分配热土豆", "awa");
                            notifyRoles = true;
                        }
                        else
                        {
                            i--;
                        }
                    }
                }
                if (LastFixedUpdate == Utils.GetTimeStamp()) return;
                LastFixedUpdate = Utils.GetTimeStamp();
                //减少爆炸冷却
                BoomTimes--;
                foreach (var pc in Main.AllPlayerControls)
                {
                    // 必要时刷新玩家名字
                    if (notifyRoles) Utils.NotifyRoles(pc);
                }
        }

    }
 
}

