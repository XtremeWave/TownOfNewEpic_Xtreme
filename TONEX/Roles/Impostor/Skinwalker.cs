using AmongUs.GameOptions;
using System;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces.GroupAndRole;

namespace TONEX.Roles.Impostor;
public sealed class Skinwalker : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Skinwalker),
            player => new Skinwalker(player),
            CustomRoles.Skinwalker,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            199874,
            SetupOptionItem,
            "sh|化形",
           experimental: true
        );
    public Skinwalker(PlayerControl player)
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
        if (!killer.Is(CustomRoles.Skinwalker)) return;
        GameData.PlayerOutfit outfit = new();
        var sender = CustomRpcSender.Create(name: $"RpcSetSkin({target.Data.PlayerName})");
        if (!killer.Is(CustomRoles.Skinwalker)) return;

        Logger.Info($"Pet={killer.Data.DefaultOutfit.PetId}", "RpcSetSkin");
        new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = TargetSpeed;
                 var outfit = TargetSkins;
        var outfit2 = KillerSkins;
            //凶手变样子
            killer.SetOutFitStatic(outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
           
            Main.AllPlayerNames[killer.PlayerId] = Main.AllPlayerNames[target.PlayerId];
            Main.AllPlayerSpeed[target.PlayerId] = KillerSpeed;
            target.SetOutFitStatic(outfit2.ColorId, outfit2.HatId, outfit2.SkinId, outfit2.VisorId, outfit2.PetId);

            //目标变样子
            Main.AllPlayerNames[target.PlayerId] = KillerName;
        }, 0.5f, "Clam");
    }
}
