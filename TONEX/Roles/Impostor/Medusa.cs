using AmongUs.GameOptions;
using TONEX.Roles.Core;
using static TONEX.Translator;
using System.Collections.Generic;
using Hazel;
using UnityEngine;
using System.Linq;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Impostor;
public sealed class Medusa : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Medusa),
            player => new Medusa(player),
            CustomRoles.Medusa,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            15384319,
            SetupOptionItem,
            "me|蛇",
            experimental: true
        );
    public Medusa(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ForMedusa = new();
        CustomRoleManager.MarkOthers.Add(MarkOthers);
    }

    static OptionItem OptionShapeshiftCooldown;
    static OptionItem OptionStone;
    public static List<byte> ForMedusa;
    enum OptionName
    {
        MedusaCooldown,
        MedusaStone,
    }
    private static void SetupOptionItem()
    {
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.MedusaCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionStone = FloatOptionItem.Create(RoleInfo, 11, OptionName.MedusaStone, new(2.5f, 180f, 2.5f), 10f, false)
    .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        ForMedusa = new();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterLeaveSkin = true;
        AURoleOptions.ShapeshifterCooldown = OptionShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = Translator.GetString("MedusaButtonText");
        return !Shapeshifting;
    }
    private bool Shapeshifting;
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (Shapeshifting)
        {
            if (!target.IsAlive())
            {
                ForMedusa.Add(target.PlayerId);
                Player.Notify(GetString("TargetIsDead"));
                new LateTask(() =>
                {
                    ForMedusa.Remove(target.PlayerId);
                    Utils.NotifyRoles();
                }, OptionStone.GetFloat(), "Bomber Suiscide");
            }
            else
            {                 
                var tmpSpeed1 = Main.AllPlayerSpeed[target.PlayerId];
                    Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
                    ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
                ForMedusa.Add(target.PlayerId);
                new LateTask(() =>
                {
                    Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed1;
                    ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
                    target.MarkDirtySettings();
                    RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
                    ForMedusa.Remove(target.PlayerId);
                    Utils.NotifyRoles();
                }, OptionStone.GetFloat(), "Bomber Suiscide");
            }
               
        }
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner) => ForMedusa.Clear();
    public override void OnStartMeeting() => ForMedusa.Clear();
    public static string MarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return (ForMedusa.Contains(seen.PlayerId) && isForMeeting == false) ? Utils.ColorString(Color.black, "⬛") : "";
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (ForMedusa.Contains(target.PlayerId))
        {
            reporter.Notify(Utils.ColorString(RoleInfo.RoleColor, Translator.GetString("NotReport")));
            Logger.Info($"{target.Object.GetNameWithRole()} 的尸体已被吞噬，无法被报告", "Cleaner.OnCheckReportDeadBody");
            return false;
        }
     return true;
    }
 };

