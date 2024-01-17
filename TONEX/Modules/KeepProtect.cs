using HarmonyLib;
using Hazel;
using System.Linq;
using InnerNet;

namespace TONEX.Modules
{
    public static class KeepProtect
    {
        public static long LastFixUpdate = 0;
        public static void SendKeepProtect(this PlayerControl target)
        {
            if (target == null) return;
            if (!target.Data.IsDead)
            {
                //Host side
                if (!target.AmOwner)
                    target.ProtectPlayer(target, 11);

                //Client side
                /*
                MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, -1);
                messageWriter.WriteNetObject(target);
                messageWriter.Write(18);
                AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
                */
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                    (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, -1);
                writer.WriteNetObject(target);
                writer.Write(11);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

            }
            //Host ignore this rpc so ability cooldown wont get reset
        }
        public static void OnFixedUpdate()
        {
            if (!Main.CanPublic.Value) return;
            if (LastFixUpdate + 10 < Utils.GetTimeStamp())
            {
                LastFixUpdate = Utils.GetTimeStamp();
                Main.AllAlivePlayerControls.ToArray()
                    .Where(x => !x.AmOwner && !x.IsProtected())
                    .Do(x => x.SendKeepProtect());
                PlayerControl.LocalPlayer.SendKeepProtect();
            }
        }

        public static void SendToAll()
        {
            if (!Main.CanPublic.Value) return;
            LastFixUpdate = Utils.GetTimeStamp();
            Main.AllAlivePlayerControls.ToArray().Do(x => x.SendKeepProtect());
        }

    }
}