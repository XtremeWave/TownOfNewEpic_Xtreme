using Hazel;
using System.Collections.Generic;
using System.Linq;
using TONEX.Attributes;
using TONEX.Modules;
using TONEX.Roles.Core;
using UnityEngine;
using static TONEX.Options;

namespace TONEX.Roles.AddOns.Common;
public static class Mini
{
    private static readonly int Id = 828893;
    private static List<byte> playerIdList = new();
    public static OptionItem OptionAgeTime;
    public static int Age;
    public static int UpTime;
    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Mini);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Mini, true, true, true);
        OptionAgeTime = FloatOptionItem.Create(Id + 20, "MiniUpTime", new(60f, 360f, 2.5f), 180f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mini])
.SetValueFormat(OptionFormat.Seconds);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
        Age = 0;
        UpTime = -8;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
   public static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MiniAge, SendOption.Reliable, -1);
        writer.Write(Age);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.MiniAge) return;
        Age = reader.ReadInt32();
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static void OnSecondsUpdate(PlayerControl player,long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player.Is(CustomRoles.Mini))
        {
            if (Age < 18 && player.IsAlive())
            {
                UpTime++;
                if (UpTime >= OptionAgeTime.GetInt() / 18)
                {
                    Age += 1;
                    UpTime = 0;
                    SendRPC();
                    player.RpcProtectedMurderPlayer(player);
                }
            }
        }
    }
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Age < 18 && target.Is(CustomRoles.Mini))
        {
            killer.Notify(Translator.GetString("CantKillKid"));
            return;
        }
        
    }
    public static string GetProgressText(byte playerId, bool comms = false)
    {
        if (!playerIdList.Contains(playerId)) return "";
        return Age < 18 ? Utils.ColorString(Color.yellow, $"({Age})") : "";
    }
}
