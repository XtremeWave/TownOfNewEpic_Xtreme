using AmongUs.GameOptions;
using static TONEX.Translator;
using TONEX.Roles.Core;
using UnityEngine;
using MS.Internal.Xml.XPath;
using static UnityEngine.GraphicsBuffer;
using TONEX.Roles.Neutral;
using System.Collections.Generic;
using Hazel;
using static Il2CppSystem.Net.Http.Headers.Parser;
using TONEX.Modules;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using TONEX.Roles.Core.Interfaces;
using System.Linq;

namespace TONEX.Roles.Neutral;

public sealed class MeteorArbiter : RoleBase, INeutralKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MeteorArbiter),
            player => new MeteorArbiter(player),
            CustomRoles.MeteorArbiter,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            75_1_2_0100,
            null,
            "Sans|MeteorArbiter|SFBF!Sans",
             "#C0EAFF",
            true,
            true,
#if RELEASE
            ctop:true,
#endif
            countType: CountTypes.MeteorArbiter
        );
    public MeteorArbiter(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
    }
    public bool IsNK { get; private set; } = false;
    public bool IsNE { get; private set; } = false;
    #region 全局变量
    public bool Murderer;
    public bool Dust;
    public int LOVE;
    public int Tired;
    public int LVOverFlow;
    #endregion
    #region RPC相关
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetMeteorArbiter);
        sender.Writer.Write(Murderer);
        sender.Writer.Write(Dust);
        sender.Writer.Write(LOVE);
        sender.Writer.Write(Tired);
        sender.Writer.Write(LVOverFlow);
        
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType == CustomRPC.SetMeteorArbiter)
        {
            Murderer = reader.ReadBoolean();
            Dust = reader.ReadBoolean();
            LOVE = reader.ReadInt32();
            Tired = reader.ReadInt32();
            LVOverFlow = reader.ReadInt32();
        }
    }
    #endregion
    public bool CanUseKillButton() => true;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public float CalculateKillCooldown() => 25f;
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        var misspercent = Random.Range(0, 100);
        float misssucceed = 100;
        for (int i = 0; i < Tired; i++)
            misssucceed -= 4f;
        Tired++;
        if (Murderer)
            misssucceed = misssucceed + misssucceed * 0.5f;
        if (Dust)
            misssucceed = 1;
        if (misspercent < misssucceed)
        {
            killer.RpcProtectedMurderPlayer(target);
            killer.SetKillCooldown();
            target.Notify($"<color=#666666>MISS</color>");
            return false;

        }
        SendRPC();
        return true;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        var killpercent = Random.Range(0, 100);
        float killsucceed = 10;
        for (int i = 0; i < Tired;i++)
           killsucceed += 2.5f;
        Tired++;
        if (Murderer)
            killsucceed = killsucceed + killsucceed * 0.5f;
        if (Dust)
            killsucceed = 99;
        if (killpercent < killsucceed)
        {
            int lv = LOVE;
            var player = target;
            if (player.IsImp() || player.IsNeutralKiller())
            {
                lv = System.Math.Min(20, lv + 10);
            }
            else if (player.IsNeutralNonKiller())
            {
                lv = System.Math.Min(20, lv + 5);
            }
            else if (player.IsCrew())
            {
                lv = System.Math.Min(20, lv + 1);
            }
            if (lv > 20)
            {
                LVOverFlow += lv - 20;
                lv = 20;
            }
            LOVE = lv;
            return true;
        }
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (LOVE > 5 && !Murderer)
        {
            Murderer = true;
            SendRPC();
        }
        if (LOVE == 20 && !Dust)
        {
            Dust = true;
            SendRPC();
        }
        if (LOVE > 1 && !(IsNK || IsNE))
        {
            IsNK = true;
        }
    }
    
    public override void AfterMeetingTasks()
    {
        Tired -= 2;
    }
    public override string GetProgressText(bool comms = false)
    {
        Color color = Utils.GetRoleColor(CustomRoles.MeteorArbiter);
        if (Murderer)
            color = Color.red;
        if (Dust)
            color = Palette.Purple;
        return Utils.ColorString(color, $"(LV{LOVE})" + GetString("Tired")+ $" {Tired}");
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = "RainOfGeo";
        return true;
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (Is(reporter) && target == null)
        {
            var player = Utils.GetPlayerById(target.PlayerId);
            int lv = LOVE;
            if (player.IsImp() || player.IsNeutralKiller())
            {
                lv = System.Math.Min(20, lv + 10);
            }
            else if (player.IsNeutralNonKiller())
            {
                lv = System.Math.Min(20, lv + 5);
            }
            else if (player.IsCrew())
            {
                lv = System.Math.Min(20, lv + 1);
            }
            if (lv > 20)
            {
                LVOverFlow += lv - 20;
                lv = 20;
            }
            LOVE = lv;
            SendRPC();
        }
            return true;
    }
    public bool CheckWin(ref CustomRoles winnerRole, ref CountTypes winnerCountType)
    {
        return Player.IsAlive() && (LOVE == 1 || LOVE < 5 && CustomRoles.MeteorMurder.IsExistCountDeath());
    }
}
