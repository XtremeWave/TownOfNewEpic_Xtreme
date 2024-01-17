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
using TONEX.Modules;
using static TONEX.Roles.Neutral.Non_Villain;
using Rewired.Utils.Platforms.Windows;
using YamlDotNet.Core.Tokens;
using System.Linq;
using static Il2CppSystem.Xml.Schema.FacetsChecker.FacetsCompiler;
using System.Text;

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
#if DEBUG
            SetupOptionItem,
#else
            null,
#endif
            "恭喜发财|刘德华|商场|Non_Villain|不演反派",
             "#FF0000",
            true,
            countType: CountTypes.None
        );
    public Non_Villain(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.OnCheckMurderPlayerOthers_Before.Add(OnCheckMurderPlayerOthers_Before);
        DigitalLifeList = new();
        MoneyCount = new();
        BlessingCode = new();
        FarAheadYet = new();
    }
    public override void Add()
    {
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            Dictionary<Blessing, int> dic2 = new();
            dic2.Add((Blessing)0, 1);
            BlessingCode.Add(pc, dic2);
            MoneyCount.Add(pc, 0);
        }
    }
    public enum Blessing
    {
        Non_Blessing = 0,
        WealthAndBrilliance = 1,
        ComeAndAway = 2,
        Overcome = 3,
        FarAhead = 4,
        Etiquette = 5,
    }

    public static List<PlayerControl> DigitalLifeList;
    public static Dictionary<PlayerControl, int> MoneyCount;
    public static Dictionary<PlayerControl, Dictionary<Blessing, int>> BlessingCode;
    public static Dictionary<PlayerControl, bool> FarAheadYet;
    private float KillCooldown;
    private static void SetupOptionItem()
    {
    }
    private void SendRPC(PlayerControl bl = null, PlayerControl df = null, PlayerControl mone = null, PlayerControl FAY = null, int Int1 = 2555555, int Int2 = 2555555, Blessing blessing = Blessing.Non_Blessing, bool Bool = false)
    {
        using var sender = CreateSender(CustomRPC.ForNV);
        if (bl != null)
            sender.Writer.Write(bl);
        if (df != null)
            sender.Writer.Write(df);
        if (mone != null)
            sender.Writer.Write(mone);
        if (FAY != null)
            sender.Writer.Write(FAY);
        if (Int1 != 2555555)
            sender.Writer.Write(Int1);
        if (Int2 != 2555555)
            sender.Writer.Write(Int2);
        if (blessing != Blessing.Non_Blessing)
            sender.Writer.Write((int)blessing);
        if (Bool)
            sender.Writer.Write(Bool);
    }
    private static void StaticSendRPC(PlayerControl bl = null, PlayerControl df = null, PlayerControl mone = null, PlayerControl FAY = null, int Int1 = 2555555, int Int2 = 2555555, Blessing blessing = Blessing.Non_Blessing, bool Bool = false)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ForNV, SendOption.Reliable, -1);
        if (bl != null)
            writer.Write(bl);
        if (df != null)
            writer.Write(df);
        if (mone != null)
            writer.Write(mone);
        if (FAY != null)
            writer.Write(FAY);
        if (Int1 != 2555555)
            writer.Write(Int1);
        if (Int2 != 2555555)
            writer.Write(Int2);
        if (blessing != Blessing.Non_Blessing)
            writer.Write((int)blessing);
        if (Bool)
            writer.Write(Bool);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.ForNV) return;
        byte bl;
        byte df;
        byte mone;
        byte FAY;
        int Int1 ;
        int Int2;
        Blessing blessing;
        bool Bool;
        bl = reader.ReadByte();
        df = reader.ReadByte();
        mone = reader.ReadByte();
        FAY = reader.ReadByte();
        Int1 = reader.ReadInt32();
        Int2 = reader.ReadInt32();
        int blessingValue = reader.ReadInt32();
        blessing = (Blessing)blessingValue;
        Bool = reader.ReadBoolean();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.PlayerId == df)
            {
                DigitalLifeList.Add(pc);
            }
                
            if (pc.PlayerId == bl)
            {
                Dictionary<Blessing, int> a = new();
                
                if (!BlessingCode.ContainsKey(pc))
                {
                    
                    a.Add(blessing, Int1);
                    BlessingCode.Add(pc, a);
                }
                else if ((!BlessingCode[pc].ContainsKey(blessing)))
                {
                    BlessingCode[pc].Add(blessing, Int1);
                }
                else
                {
                    BlessingCode[pc][blessing] += Int1;
                }
              
            }
            if (pc.PlayerId == mone)
            {
                if (!MoneyCount.ContainsKey(pc))
                    MoneyCount.Add(pc, Int2);
                else MoneyCount[pc] += Int2;
            }
            if (pc.PlayerId == FAY && !FarAheadYet.ContainsKey(pc))
                FarAheadYet.Add(pc, Bool);
        }
    }
    public bool CanUseKillButton() => true;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public float CalculateKillCooldown() => KillCooldown;
    private static bool OnCheckMurderPlayerOthers_Before(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (target.Is(CustomRoles.Non_Villain))
        {
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc != target)
                {
                    var posi = target.transform.position;
                    var diss = Vector2.Distance(posi, pc.transform.position);
                    if (diss < 3f)
                    {
                        BlessingCode[pc].Add(Blessing.Etiquette, 1);
                        StaticSendRPC(bl: pc, blessing: Blessing.Etiquette, Int1: 1);
                        Main.CantUseSkillList.Add(target);
                    }
                }
            }
        }
        else if (BlessingCode.ContainsKey(target) && BlessingCode[target].ContainsKey((Blessing)3))
        {
            BlessingCode[target][(Blessing)3] -= 1;
            StaticSendRPC(bl: target, blessing: (Blessing)3, Int1: -1);
            return false;
        }
        else if (BlessingCode.ContainsKey(target) && BlessingCode[target].ContainsKey((Blessing)4))
        {
            BlessingCode[target].Remove((Blessing)4);
            StaticSendRPC(bl: target, blessing: (Blessing)4, Int1: -1);
            Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] * (10 / Main.AllPlayerSpeed[target.PlayerId]);
            return false;
        }
        return true;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        var blessing = Random.Range(1, 3);
        var Money = Random.Range(100, 1000);
        
        new LateTask(() =>
        {
            if (BlessingCode[target].ContainsKey((Blessing)0))
                BlessingCode[target].Remove((Blessing)0);
            if (BlessingCode[target].ContainsKey((Blessing)blessing))
            {
                if (blessing == 1 && BlessingCode[target][(Blessing)blessing] == 3)
                    blessing = Random.Range(2, 3);
                BlessingCode[target][(Blessing)blessing]++;

            }
            else BlessingCode[target].Add((Blessing)blessing, 1);
            SendRPC(bl: target, blessing: (Blessing)blessing, Int1: 1);
            MoneyCount[target] += Money;
            SendRPC(mone: target, Int2: Money);

        },2f); 
        return false;
    }
    public bool IsKiller { get; private set; } = true;
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (!MoneyCount.ContainsKey(pc))
            {
                MoneyCount.Add(pc, 0);
                SendRPC(mone: pc, Int2: 0);
            }
            MoneyCount[pc] += 25;
            SendRPC(mone: pc, Int2: 25);
            if (BlessingCode.ContainsKey(pc))
            {
                if (BlessingCode[pc].ContainsKey((Blessing)1))
                {
                    var Multiply = BlessingCode[pc][(Blessing)1];
                    MoneyCount[pc] += 20 * Multiply;
                    SendRPC(mone: pc, Int2: 20 * Multiply);
                }
                else if (BlessingCode.ContainsKey(pc) && BlessingCode[pc].ContainsKey((Blessing)4))
                {
                    MoneyCount[pc] += 25;
                    SendRPC(mone: pc, Int2: 25);
                }
            }
            if (MoneyCount[pc] >= 7500 && BlessingCode[pc].ContainsKey((Blessing)4))
            {
                MoneyCount[pc] -= 7500;
                SendRPC(mone: pc, Int2: -7500);
                BlessingCode[pc].Add((Blessing)4, 1);
                SendRPC(bl: pc, blessing: (Blessing)4, Int1: 1);
                Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] + Main.AllPlayerSpeed[pc.PlayerId] * 0.1f;
                if (!FarAheadYet[pc])
                {
                    FarAheadYet[pc] = true;
                    SendRPC(FAY: pc, Bool: true);
                }

            }
            else if (MoneyCount[pc] >= 5000 && FarAheadYet[pc] && !DigitalLifeList.Contains(pc))
            {
                MoneyCount[pc] -= 5000;
                SendRPC(mone: pc, Int2: -5000);
                DigitalLifeList.Add(pc);
                SendRPC(df: pc);
            }
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
        string blessings;

        if (!BlessingCode.ContainsKey(seen) || BlessingCode[seen].Count <= 0)
        {
            blessings = $"({GetString("Non_Blessing")}";
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in BlessingCode[seen])
            {
                sb.Append($"{GetString("Blessing")}{kvp.Key}: {kvp.Value}, ");

            }
            blessings = sb.ToString().TrimEnd(',', ' ');
        }
        if (!MoneyCount.ContainsKey(seen))
        {
            money = 0;
        }
        else
        {
            money = MoneyCount[seen];
        }
        return (seer == seen || seer.Is(CustomRoles.Non_Villain)) ? $"({GetString("MoneyCount")}: {money}, {blessings})" : "";
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
                countTypeMapping.Add(pc.GetCountTypes(), MoneyCount[pc]);
                else countTypeMapping[pc.GetCountTypes()]+= MoneyCount[pc];
                
            }
            refe.Add(countTypeMapping.OrderByDescending(kvp => kvp.Value).First().Key);
        }
        else
        {
            foreach (var pc in DigitalLifeList)
            {
                if (!refe.Contains(pc.GetCountTypes()))
                    refe.Add(pc.GetCountTypes() );
            }
        }
        return refe.Contains(winnerCountType);
    }
}