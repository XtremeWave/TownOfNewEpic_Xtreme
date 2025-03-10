using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using TONEX.Modules;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;
using UnityEngine.UIElements.UIR;
using static TONEX.Translator;
using System;
using System.Collections.Generic;
using TONEX.Modules.SoundInterface;
using static UnityEngine.GraphicsBuffer;
using System.Linq;

namespace TONEX.Roles.Neutral;
public sealed class Alternate : RoleBase, IAdditionalWinner, INeutralKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Alternate),
            player => new Alternate(player),
            CustomRoles.Alternate,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            94_1_5_0100,
            SetupOptionItem,
            "alt|伪人",
            "#663366",
            true,
            true
#if RELEASE
            , 
            ctop: true
#endif
        );
    public Alternate(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {

    }
    public bool IsNE { get; private set; } = false;
    public bool IsKiller => false;
    private static OptionItem OptionSubstituteCooldown;
    public static OptionItem OptionCanVent;
    private static OptionItem OptionSubstituteLimit;
    enum OptionName
    {
        SubstituteCooldown,
        NeedSubstituteLimit,
    }
    public static bool CanVent;
    private string Name;
    public  int SubstituteLimit;
    public byte Id = new();
    public  byte SubstituteId = new();
    public bool InSubstitute;
    public bool EndSubstitute;

    private static void SetupOptionItem()
    {
        OptionSubstituteCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.SubstituteCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSubstituteLimit = IntegerOptionItem.Create(RoleInfo, 11, OptionName.NeedSubstituteLimit, new(1, 20, 1), 5, false)
          .SetValueFormat(OptionFormat.Times);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, true, false);
    }
    public override void Add()
    {
        SubstituteLimit = 1; 
        Name = "";
        Id = byte.MaxValue;
        SubstituteId = byte.MaxValue;
        InSubstitute = false;
        EndSubstitute = false;
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Name);
        sender.Writer.Write(SubstituteLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        Name = reader.ReadString();
        SubstituteLimit = reader.ReadInt32();
    }
    public bool CanUseKillButton() => Player.IsAlive() && SubstituteLimit > 0 &&!EndSubstitute;
    public bool CanUseSabotageButton() => false;
    public float CalculateKillCooldown() => OptionSubstituteCooldown.GetFloat();
    public bool CanUseImpostorVentButton() => CanVent;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool CheckWin(ref CustomRoles winnerRole, ref CountTypes winnerCountType)
    {
        if (Utils.GetPlayerById(SubstituteId).GetCountTypes() == winnerCountType)
        {
            CustomWinnerHolder.WinnerIds.Remove(SubstituteId);
            return true;
        }
        return false;
    }
    public override string GetProgressText(bool comms = false)
    {
        if (Name != ""){
            if (SubstituteLimit > 0) {
                return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Alternate), $"({Name} {SubstituteLimit})"); 
            }
            else
                 return "";
        }
        else
            return "";
    }
    public NetworkedPlayerInfo.PlayerOutfit TargetSkins = new();
    public NetworkedPlayerInfo.PlayerOutfit KillerSkins = new();
    public float KillerSpeed = new();
    public string KillerName = "";
    public float TargetSpeed = new();
    public string TargetName = "";
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Name != ""){
            if (SubstituteLimit >= 1 && Id==target.PlayerId){
                SubstituteLimit--;
                SendRPC();
                if(SubstituteLimit == 0) {
                    KillerSkins = new NetworkedPlayerInfo.PlayerOutfit().Set(killer.GetRealName(), killer.Data.DefaultOutfit.ColorId, killer.Data.DefaultOutfit.HatId, killer.Data.DefaultOutfit.SkinId, killer.Data.DefaultOutfit.VisorId, killer.Data.DefaultOutfit.PetId, killer.Data.DefaultOutfit.NamePlateId);

                    TargetSkins = new NetworkedPlayerInfo.PlayerOutfit().Set(target.GetRealName(), target.Data.DefaultOutfit.ColorId, target.Data.DefaultOutfit.HatId, target.Data.DefaultOutfit.SkinId, target.Data.DefaultOutfit.VisorId, target.Data.DefaultOutfit.PetId, target.Data.DefaultOutfit.NamePlateId);
                    TargetSpeed = Main.AllPlayerSpeed[target.PlayerId];
                    TargetName = Main.AllPlayerNames[target.PlayerId];
                    KillerSpeed = Main.AllPlayerSpeed[killer.PlayerId];
                    KillerName = Main.AllPlayerNames[killer.PlayerId];
                    target.SetOutFit(killer.Data.DefaultOutfit.ColorId);
                    var sender = CustomRpcSender.Create(name: $"RpcSetSkin({target.Data.PlayerName})");

                    Logger.Info($"Pet={killer.Data.DefaultOutfit.PetId}", "RpcSetSkin");
                    new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[killer.PlayerId] = TargetSpeed;
                        var outfit = TargetSkins;
                        var outfit2 = KillerSkins;
                        //凶手变样子
                        killer.SetOutFit(outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                        Main.AllPlayerNames[killer.PlayerId] = TargetName;
                        Main.AllPlayerNames[target.PlayerId] = KillerName;
                        killer.RpcSetName(TargetName);
                        target.RpcSetName(KillerName);
                        Main.AllPlayerSpeed[target.PlayerId] = KillerSpeed;
                        target.SetOutFit(outfit2.ColorId, outfit2.HatId, outfit2.SkinId, outfit2.VisorId, outfit2.PetId);
                    }, 0.2f, "Alternate");
                    new LateTask(() =>
                    {
                        Utils.NotifyRoles(target);
                        Utils.NotifyRoles(killer);
                        Utils.NotifyRoles();
                    }, 0.5f, "Alternate");
                    killer.RpcTeleport(target.GetTruePosition());
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                    target.RpcTeleport(Utils.GetBlackRoomPS());
                    target.SetRealKiller(killer);
                    target.RpcMurderPlayerV2(target);
                    killer.SetKillCooldownV2();
                    EndSubstitute = true;
                    SubstituteId= target.PlayerId;
                    //变成目标的模版职业
                    //var pc = target.GetCustomRole().GetRoleInfo();
                    //killer.SetRole(pc.BaseRoleType.Invoke());
                }
                else {
                  if(IRandom.Instance.Next(0, 100) >=50) target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Alternate), GetString("NothingIsWorthTheRisk")),0.3f);
                    killer.SetKillCooldownV2(target: target, forceAnime: true);
                Utils.NotifyRoles(killer);
                }
            }
        }
        else if (Id == byte.MaxValue){
            Name =ExtendedPlayerControl.GetTrueName(target);
            SubstituteLimit = OptionSubstituteLimit.GetInt();
            SendRPC();
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            Utils.NotifyRoles(killer);
            Utils.NotifyRoles(killer);
            Id = target.PlayerId;
            InSubstitute = true;
        }
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var target =  Utils.GetPlayerById(Id);
        if ((target == null || !target.IsAlive()) && InSubstitute)   {
            Player.SetKillCooldownV2();
            Name = "";
            SubstituteLimit = 1;
            SendRPC();
            Utils.NotifyRoles(Player);
            Utils.NotifyRoles(Player);
            Id = byte.MaxValue;
            InSubstitute = false;
        }
    }
}
