using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Crewmate;
using TONEX.Roles.Impostor;
using UnityEngine;
using static TONEX.Translator;

namespace TONEX;
public static class SwapperHelper
{
    public static string GetFormatString()
    {
        string text = GetString("PlayerIdList");
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            string id = pc.PlayerId.ToString();
            string name = pc.GetRealName();
            text += $"\n{id} → {name}";
        }
        return text;
    }
    public static bool MatchCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
    public static int GetColorFromMsg(string msg)
    {
        if (ComfirmIncludeMsg(msg, "红|紅|red")) return 0;
        if (ComfirmIncludeMsg(msg, "蓝|藍|深蓝|blue")) return 1;
        if (ComfirmIncludeMsg(msg, "绿|綠|深绿|green")) return 2;
        if (ComfirmIncludeMsg(msg, "粉红|粉紅|深粉|pink")) return 3;
        if (ComfirmIncludeMsg(msg, "橘|橘|orange")) return 4;
        if (ComfirmIncludeMsg(msg, "黄|黃|yellow")) return 5;
        if (ComfirmIncludeMsg(msg, "黑|黑|black")) return 6;
        if (ComfirmIncludeMsg(msg, "白|白|white")) return 7;
        if (ComfirmIncludeMsg(msg, "紫|紫|perple")) return 8;
        if (ComfirmIncludeMsg(msg, "棕|棕|brown")) return 9;
        if (ComfirmIncludeMsg(msg, "青|青|cyan")) return 10;
        if (ComfirmIncludeMsg(msg, "黄绿|黃綠|浅绿|淡绿|lime")) return 11;
        if (ComfirmIncludeMsg(msg, "红褐|紅褐|深红|maroon")) return 12;
        if (ComfirmIncludeMsg(msg, "玫红|玫紅|浅粉|淡粉|rose")) return 13;
        if (ComfirmIncludeMsg(msg, "焦黄|焦黃|浅黄|淡黄|banana")) return 14;
        if (ComfirmIncludeMsg(msg, "灰|灰|gray")) return 15;
        if (ComfirmIncludeMsg(msg, "茶|茶|tan")) return 16;
        if (ComfirmIncludeMsg(msg, "珊瑚|珊瑚|coral")) return 17;
        else return -1;
    }
    private static bool ComfirmIncludeMsg(string msg, string key) => key.Split('|').Any(msg.Contains);
    public static bool SwapperMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;

        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.NiceSwapper) && !pc.Is(CustomRoles.EvilSwapper)) return false;

        int operate; // 1:ID 2:猜测
        msg = msg.ToLower().Trim();
        if (MatchCommond(ref msg, "id|swaplist|sw编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (MatchCommond(ref msg, "swap|change|sp|sw|cg|sc|换|换票", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("SwapDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {
            spam = true;
            if (!AmongUsClient.Instance.AmHost) return true;

            if (!MsgToPlayer(msg, out byte targetId,  out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }

            var target = Utils.GetPlayerById(targetId);
            if (!Swap(pc, target, out var reason))
                Utils.SendMessage(reason, pc.PlayerId);
        }
        return true;
    }
    public static bool Swap(PlayerControl swapper, PlayerControl target, out string reason, bool isUi = false)
    {
        reason = string.Empty;
        if (swapper.GetRoleClass() is NiceSwapper ngClass && ngClass.SwapLimit < 1)
        {
            reason = GetString("GGSwapMax");
            return false;
        }
        if (swapper.GetRoleClass() is EvilSwapper egClass && egClass.SwapLimit < 1)
        {
            reason = GetString("EGSwapMax");
            return false;
        }
        if ((swapper.GetRoleClass() is EvilSwapper || swapper.GetRoleClass() is NiceSwapper && !NiceSwapper.SwapperCanSelf.GetBool()) && swapper == target)
        {
            reason = GetString("CantSwapSelf");
            return false;
        }
        if (swapper.Is(CustomRoles.NiceSwapper) && target != null)
        {
            if ((swapper.GetRoleClass() as NiceSwapper).SwapList.Contains(target.PlayerId))
            {
                reason = string.Format(GetString("UnChooseTarget"), target.GetRealName());
                (swapper.GetRoleClass() as NiceSwapper).SwapList.Remove(target.PlayerId);
            }
            else if ((swapper.GetRoleClass() as NiceSwapper).SwapList.Count < 2 && !(swapper.GetRoleClass() as NiceSwapper).SwapList.Contains(target.PlayerId))
            {
                (swapper.GetRoleClass() as NiceSwapper).SwapList.Add(target.PlayerId);
                reason = string.Format(GetString("ChooseTarget"), target.GetRealName());
            }
            else
            {
                reason = GetString("ChooseMax");
                return false;
            }
        }
        else if (swapper.Is(CustomRoles.EvilSwapper) && target != null)
        {
            if ((swapper.GetRoleClass() as EvilSwapper).SwapList.Contains(target.PlayerId))
            {
                (swapper.GetRoleClass() as EvilSwapper).SwapList.Remove(target.PlayerId);
                reason = string.Format(GetString("UnChooseTarget"), target.GetRealName());
            }
            else if ((swapper.GetRoleClass() as EvilSwapper).SwapList.Count < 2 && !(swapper.GetRoleClass() as EvilSwapper).SwapList.Contains(target.PlayerId))
            {
                (swapper.GetRoleClass() as EvilSwapper).SwapList.Add(target.PlayerId);
                reason = string.Format(GetString("ChooseTarget"), target.GetRealName());
            }
            else
            {
                reason = GetString("ChooseMax");
                return false;
            }
        }
        Logger.Info($"{swapper.GetNameWithRole()} 添加了 {target.GetNameWithRole()}", "Swapper");



        CustomSoundsManager.RPCPlayCustomSoundAll("Gunfire");

        return true;
    }
    private static bool MsgToPlayer(string msg, out byte id, out string error)
    {
        id = byte.MaxValue;
        

        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        mc.Do(m => result += m);

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //FIXME: 指令中包含颜色后无法正确匹配职业名
            //并不是玩家编号，判断是否颜色
            int color = GetColorFromMsg(msg);
            List<PlayerControl> list = Main.AllAlivePlayerControls.Where(p => p.cosmetics.ColorId == color).ToList();
            if (list.Count < 1)
            {
                error = GetString("SwapNull");
                return false;
            }
            else if (list.Count != 1)
            {
                error = GetString("SwapMultipleColor");
                return false;
            }
            id = list.FirstOrDefault().PlayerId;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("SwapNull");
            return false;
        }

        

        error = string.Empty;
        return true;
    }
}//*/