using AmongUs.GameOptions;
using System;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;

namespace TONEX.Roles.Impostor;
public sealed class Shifters : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Shifters),
            player => new Shifters(player),
            CustomRoles.Shifters,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            199874,
            SetupOptionItem,
            "sh|化形",
           experimental: true
        );
    public Shifters(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TargetSkins = new();
        KillerSkins = new();
        KillerSpeed = new();
        KillerName = "";
        TargetSpeed = new();
        TargetName = "";
    }
    public GameData.PlayerOutfit TargetSkins = new();
    public GameData.PlayerOutfit KillerSkins = new();
    public float KillerSpeed = new();
    public string KillerName = "";
    public float TargetSpeed = new();
    public string TargetName = "";
    public byte TargetPlayerId = new();
    public byte KillerPlayerId = new();
    static OptionItem KillCooldown;
    private static void SetupOptionItem()
    {
       KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 35f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        TargetSkins = new();
        KillerSkins = new();
        KillerSpeed = new();
        KillerName = "";
        TargetSpeed = new();
        TargetName = "";
        TargetPlayerId= new();
        KillerPlayerId= new();
    }
    public float CalculateKillCooldown() => KillCooldown.GetFloat();

    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
           var (killer, target) = info.AttemptTuple;
        KillerSkins = new GameData.PlayerOutfit().Set(killer.GetRealName(), killer.Data.DefaultOutfit.ColorId, killer.Data.DefaultOutfit.HatId, killer.Data.DefaultOutfit.SkinId, killer.Data.DefaultOutfit.VisorId, killer.Data.DefaultOutfit.PetId);

        TargetSkins = new GameData.PlayerOutfit().Set(target.GetRealName(), target.Data.DefaultOutfit.ColorId, target.Data.DefaultOutfit.HatId, target.Data.DefaultOutfit.SkinId, target.Data.DefaultOutfit.VisorId, target.Data.DefaultOutfit.PetId);
        TargetSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        TargetName = Main.AllPlayerNames[killer.PlayerId];
        KillerSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        KillerName = Main.AllPlayerNames[killer.PlayerId];
        TargetPlayerId = target.PlayerId;
        KillerPlayerId = killer.PlayerId;
        if (!killer.Is(CustomRoles.Shifters)) return;
        GameData.PlayerOutfit outfit = new();
        var sender = CustomRpcSender.Create(name: $"RpcSetSkin({target.Data.PlayerName})");
        if (!killer.Is(CustomRoles.Shifters)) return;

        Logger.Info($"Pet={killer.Data.DefaultOutfit.PetId}", "RpcSetSkin");
        new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = TargetSpeed;
                 var outfit = TargetSkins;
        var outfit2 = KillerSkins;
            //凶手变样子
            killer.SetName(outfit.PlayerName);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetName)
                .Write(outfit.PlayerName)
                .EndRpc();
            sender.SendMessage();
            Main.AllPlayerNames[killer.PlayerId] = Main.AllPlayerNames[target.PlayerId];
            killer.RpcSetColor((byte)outfit.ColorId);
            killer.SetHat(outfit.HatId, outfit.ColorId);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetHatStr)
                .Write(outfit.HatId)
                .EndRpc();
            sender.SendMessage();
            killer.SetSkin(outfit.SkinId, outfit.ColorId);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(outfit.SkinId)
                .EndRpc();

            killer.SetVisor(outfit.VisorId, outfit.ColorId);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(outfit.VisorId)
                .EndRpc();
            sender.SendMessage();
            killer.SetPet(outfit.PetId);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetPetStr)
                .Write(outfit.PetId)
                .EndRpc();
            sender.SendMessage();
            Main.AllPlayerSpeed[target.PlayerId] = KillerSpeed;

            //目标变样子
            target.SetName(outfit2.PlayerName);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetName)
                .Write(outfit2.PlayerName)
                .EndRpc();
            sender.SendMessage();
            Main.AllPlayerNames[target.PlayerId] = KillerName;

            target.SetHat(outfit2.HatId, outfit2.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(outfit2.HatId)
                .EndRpc();
            sender.SendMessage();
            killer.RpcSetColor((byte)outfit2.ColorId);
            target.SetSkin(outfit2.SkinId, outfit2.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(outfit2.SkinId)
                .EndRpc();
            sender.SendMessage();
            target.SetVisor(outfit2.VisorId, outfit2.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(outfit2.VisorId)
                .EndRpc();
            sender.SendMessage();
            target.SetPet(outfit2.PetId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                .Write(outfit2.PetId)
                .EndRpc();
            sender.SendMessage();
        }, 0.5f, "Clam");
    }
}
