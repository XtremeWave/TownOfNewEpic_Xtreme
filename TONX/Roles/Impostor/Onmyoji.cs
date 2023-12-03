using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using MS.Internal.Xml.XPath;
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

    private static Dictionary<byte, byte> Yang;
    private static Dictionary<byte, byte> Yin;

    static OptionItem OptionKillCooldown;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 40f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetYinYangPlayer);
        sender.Writer.Write(Yang.Count);
        Yang.Do(x => { sender.Writer.Write(x.Key); sender.Writer.Write(x.Value); });
        sender.Writer.Write(Yin.Count);
        Yin.Do(x => { sender.Writer.Write(x.Key); sender.Writer.Write(x.Value); });
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetYinYangPlayer) return;
        Yin = new();
        Yang = new();
        for (int i = 0; i < reader.ReadInt32(); i++)
            Yin.Add(reader.ReadByte(), reader.ReadByte());
        for (int i = 0; i < reader.ReadInt32(); i++)
            Yang.Add(reader.ReadByte(), reader.ReadByte());
    }
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.RpcProtectedMurderPlayer(target);
        if (!Yang.ContainsKey(target.PlayerId))
        {
            if (Yin.Count == 0)
            {
                Yang.TryAdd(target.PlayerId, killer.PlayerId);  
NameColorManager.Add(killer.PlayerId, target.PlayerId, "#333333");
            }
            else  
            {
     Yin.TryAdd(target.PlayerId, killer.PlayerId);
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#99FFFF");
            }
           
        }
         else if (Yang.ContainsKey(target.PlayerId))
        {
            Yang.Remove(target.PlayerId);
            NameColorManager.Add(killer.PlayerId, target.PlayerId, "#ffffff");
            foreach (var x in Main.AllAlivePlayerControls)
            {
                if (Yin.ContainsKey(x.PlayerId))
                    Yin.Remove(x.PlayerId);
                NameColorManager.Add(killer.PlayerId, x.PlayerId, "#ffffff");
            }
        }
         else if (Yin.ContainsKey(target.PlayerId))
        {
            Yin.Remove(target.PlayerId);
            NameColorManager.Add(killer.PlayerId, target.PlayerId, "#ffffff");
        }
        SendRPC();
        info.DoKill = false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var ghost in Yang)
        {
            var gs = Utils.GetPlayerById(ghost.Key);
            var killer = Utils.GetPlayerById(ghost.Value);
            if (!gs.IsAlive()) continue;
            foreach (var pk in Yin)
            {
                var pc = Utils.GetPlayerById(pk.Key);
                if (!pc.IsAlive() || pc == killer) continue;
                var pos = gs.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > 0.3f) continue;
                gs.SetDeathReason(CustomDeathReason.Quantization);
                pc.SetDeathReason(CustomDeathReason.Quantization);
                gs.SetRealKiller(killer);
                gs.RpcMurderPlayerV2(gs);
                pc.RpcMurderPlayerV2(pc);

                break;
            }
        }
    }
}
