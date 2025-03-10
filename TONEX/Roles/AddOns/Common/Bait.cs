using System;
using TONEX.Modules.SoundInterface;
using TONEX.Roles.Core;
using UnityEngine;
using static TONEX.Translator;

namespace TONEX.Roles.AddOns.Common;
public sealed class Bait : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    SimpleRoleInfo.Create(
    typeof(Bait),
    player => new Bait(player),
    CustomRoles.Bait,
    81700,
    SetupOptionItem,
    "ba|大奖|头奖|誘餌",
    "#00f7ff"
    );
    public Bait(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }


    public static OptionItem OptionReportDelayMin;
    public static OptionItem OptionReportDelayMax;
    public static OptionItem OptionDelayNotifyForKiller;
    public static OptionItem OptionCanSeePlayerInVent;

    enum OptionName
    {
        BaitDelayMin,
        BaitDelayMax,
        BaitDelayNotify,
        BaitanSeePlayerInVent,
    }

    static void SetupOptionItem()
    {
        OptionReportDelayMin = FloatOptionItem.Create(RoleInfo, 20, OptionName.BaitDelayMin, new(0f, 5f, 1f), 0f, false)
.SetValueFormat(OptionFormat.Seconds);
        OptionReportDelayMax = FloatOptionItem.Create(RoleInfo, 21, OptionName.BaitDelayMax, new(0f, 10f, 1f), 0f, false)
.SetValueFormat(OptionFormat.Seconds);
        OptionDelayNotifyForKiller = BooleanOptionItem.Create(RoleInfo, 22, OptionName.BaitDelayNotify, true, false);
        OptionCanSeePlayerInVent = BooleanOptionItem.Create(RoleInfo, 23, OptionName.BaitanSeePlayerInVent, true, false);
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide) return;

        killer.RPCPlayCustomSound("Congrats");
        target.RPCPlayCustomSound("Congrats");
        float delay;
        if (OptionReportDelayMax.GetFloat() < OptionReportDelayMin.GetFloat()) delay = 0f;
        else delay = IRandom.Instance.Next((int)OptionReportDelayMin.GetFloat(), (int)OptionReportDelayMax.GetFloat() + 1);
        delay = Math.Max(delay, 0.15f);
        if (delay > 0.15f && OptionDelayNotifyForKiller.GetBool()) killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), string.Format(Translator.GetString("KillBaitNotify"), (int)delay)), delay);
        Logger.Info($"{killer.GetNameWithRole()} Killed Bait => {target.GetNameWithRole()}", "Bait.OnMurderPlayerAsTarget");
        _ = new LateTask(() => { if (GameStates.IsInTask) killer.CmdReportDeadBody(target.Data); }, delay, "Bait Self Report");
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (OptionCanSeePlayerInVent.GetBool())
        {

            if (player.PlayerId == Player.PlayerId) return;
            if (Vector2.Distance(Player.transform.position, player.transform.position) <= 3f && player.inVent)
            {
                Player.Notify(GetString("BaitSeeVentPlayer"));

            }
        }
    }
}

