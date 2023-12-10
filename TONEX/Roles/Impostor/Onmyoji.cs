using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using static TONEX.Translator;
using System.Collections.Generic;
using System.Linq;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;
using static Il2CppSystem.Net.Http.Headers.Parser;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Impostor;
public sealed class Onmyoji : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Onmyoji),
            player => new Onmyoji(player),
            CustomRoles.Onmyoji,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            269854,
            SetupOptionItem,
            "on|阴阳|"
        );
    public Onmyoji(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Yang = new();
        Yin = new();
    }

    private static List<byte> Yang;
    private static List<byte> Yin;

    static OptionItem OptionKillCooldown;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 40f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        Yang = new();
        Yin = new();
    }
    private static void SendRPC_SyncYangList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetYangPlayer, SendOption.Reliable, -1);
        writer.Write(Yang.Count);
        for (int i = 0; i < Yang.Count; i++)
            writer.Write(Yang[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncYangList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        Yang = new();
        for (int i = 0; i < count; i++)
            Yang.Add(reader.ReadByte());
    }
    private static void SendRPC_SyncYinList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetYinPlayer, SendOption.Reliable, -1);
        writer.Write(Yin.Count);
        for (int i = 0; i < Yin.Count; i++)
            writer.Write(Yin[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncYinList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        Yin = new();
        for (int i = 0; i < count; i++)
            Yin.Add(reader.ReadByte());
    }
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        if (!Yang.Contains(target.PlayerId) && Yang.Count <= 0)
        {
            Logger.Info($"{target.GetNameWithRole()} 阳", "YinYang.OnFixedUpdate");
            Yang.Add(target.PlayerId);
            SendRPC_SyncYangList();
        }
        else if (!Yin.Contains(target.PlayerId) && Yang.Count >= 1)
        {
            Logger.Info($"{target.GetNameWithRole()} 阴", "YinYang.OnFixedUpdate");
            Yin.Add(target.PlayerId);
            SendRPC_SyncYinList();
        }
        else if (Yang.Contains(target.PlayerId))
        {
            Yang.Clear();
            Yin.Clear();
            SendRPC_SyncYangList();
            SendRPC_SyncYinList();
        }
        else if (Yin.Contains(target.PlayerId))
        {
            SendRPC_SyncYinList();
            Yin.Clear();
        }
        else if (Yang.Count >= 1 && Yin.Count >= 1) return true;
        else Logger.Info($"{target.GetNameWithRole()} 未知错误", "YinYang.OnFixedUpdate");
        return false;
    } 
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var ghost in Yang)
        {
            var gs = Utils.GetPlayerById(ghost);
            if (!gs.IsAlive()) continue;
            foreach (var pk in Yin)
            {
                var pc = Utils.GetPlayerById(pk);
                if (!pc.IsAlive()) continue;
                var pos = gs.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > 0.3f) continue;
                gs.SetRealKiller(Player);
                pc.SetRealKiller(Player);
                gs.SetDeathReason(CustomDeathReason.Merger);
                pc.SetDeathReason(CustomDeathReason.Merger);
                gs.RpcMurderPlayerV2(gs);
                pc.RpcMurderPlayerV2(pc);
                Yang.Clear();
                Yin.Clear();
                SendRPC_SyncYangList();
                SendRPC_SyncYinList();
                break;
            }
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (Yang.Contains(seen.PlayerId) && seen.IsAlive())
            return Utils.ColorString(Color.white, "🔴");
        else if (Yin.Contains(seen.PlayerId) && seen.IsAlive())
            return Utils.ColorString(Color.black, "🔴");
        else
           return "";
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("YinYangButtonText");
            return !(Yang.Count >= 1 && Yin.Count >= 1);
    }
}
