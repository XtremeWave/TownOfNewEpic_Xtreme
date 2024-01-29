using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using TONEX.Modules;
using TONEX.Roles.Core;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Crewmate;
public sealed class Transporter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Transporter),
            player => new Transporter(player),
            CustomRoles.Transporter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21500,
            SetupOptionItem,
            "tr|傳送師|传送",
            "#42D1FF"
        );
    public Transporter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionTeleportNums;
    enum OptionName
    {
        TransporterTeleportMax
    }
    public override bool GetGameStartSound(out string sound)
    {
        sound = "Teleport";
        return true;
    }
    private static void SetupOptionItem()
    {
        OptionTeleportNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TransporterTeleportMax, new(1, 99, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override bool OnCompleteTask(out bool cancel)
    {
        cancel = false;
        if (!Player.IsAlive() || MyTaskState.CompletedTasksCount + 1 > OptionTeleportNums.GetInt()) return false;

        Logger.Info("传送师触发传送:" + Player.GetNameWithRole(), "Transporter");

        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != Player.PlayerId && x.IsAlive() && !x.inVent).ToList();
        var SelectedTarget = pcList[IRandom.Instance.Next(0, pcList.Count)];
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.IsEaten()) continue;
            if (SelectedTarget == null) continue;
               pc.RpcTeleport(SelectedTarget.GetTruePosition());
        }
        return false;
    }
}