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

internal static class BlessingModeManager
{
    public static Dictionary<byte, int> MoneyCount;
    public static Dictionary<byte, int> WealthAndBrillianceDictionary;
    public static List<byte> ComeAndAwayList;
    public static List<byte> OvercomeList;
    public static List<byte> FarAheadList;
    public static List<byte> HasFarAheadList;
    public static List<byte> EtiquetteList;
    public static List<byte> DigitalLifeList;
    //设置11

    [GameModuleInitializer]
    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.HotPotato) return;
        BoomTimes = Boom.GetInt() + 9;
        HotPotatoMax = HotQuan.GetInt();
        IsAliveCold = 0;
        IsAliveHot = 0;
    }

 
}

