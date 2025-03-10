using Hazel;
using System.Collections.Generic;
using TONEX.Attributes;
using TONEX.Roles.Core;
using UnityEngine;
using UnityEngine.UIElements.UIR;

namespace TONEX.Roles.AddOns.Common;
public sealed class Signal : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    SimpleRoleInfo.Create(
    typeof(Signal),
    player => new Signal(player),
    CustomRoles.Signal,
    75_1_0_0300,
    null,
    "si|通讯",
    "#F39C12"
    );
    public Signal(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Signalbacktrack = new Vector2(9999, 9999);
    }
    public Vector2 Signalbacktrack = new();
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Signalbacktrack= Player.GetTruePosition();
        SendRPC();
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (Signalbacktrack != new Vector2(9999, 9999))
            Player.RpcTeleport(Signalbacktrack);
        Signalbacktrack = new Vector2(9999, 9999);
    }
    public void SendRPC()
    {
        var sender = CreateSender(CustomRPC.SignalPosition);

        sender.Writer.Write(Signalbacktrack.x);
        sender.Writer.Write(Signalbacktrack.y);

    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SignalPosition) return;
        var x = reader.ReadSingle();
        var y = reader.ReadSingle();
        Signalbacktrack = new();
        Signalbacktrack=new(x, y);
    }
}
