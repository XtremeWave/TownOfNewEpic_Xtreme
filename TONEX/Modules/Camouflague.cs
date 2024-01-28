using System.Collections.Generic;
using TONEX.Attributes;
using TONEX.Roles.Core;
using TONEX.Roles.Impostor;
using Hazel;

namespace TONEX;

static class PlayerOutfitExtension
{
    public static GameData.PlayerOutfit Set(this GameData.PlayerOutfit instance, string playerName, int colorId, string hatId, string skinId, string visorId, string petId)
    {
        instance.PlayerName = playerName;
        instance.ColorId = colorId;
        instance.HatId = hatId;
        instance.SkinId = skinId;
        instance.VisorId = visorId;
        instance.PetId = petId;
        return instance;
    }
    public static bool Compare(this GameData.PlayerOutfit instance, GameData.PlayerOutfit targetOutfit)
    {
        return instance.ColorId == targetOutfit.ColorId &&
                instance.HatId == targetOutfit.HatId &&
                instance.SkinId == targetOutfit.SkinId &&
                instance.VisorId == targetOutfit.VisorId &&
                instance.PetId == targetOutfit.PetId;

    }
    public static string GetString(this GameData.PlayerOutfit instance)
    {
        return $"{instance.PlayerName} Color:{instance.ColorId} {instance.HatId} {instance.SkinId} {instance.VisorId} {instance.PetId}";
    }
}
public static class Camouflage
{
    public static GameData.PlayerOutfit CamouflageOutfit_Default = new GameData.PlayerOutfit().Set("", 15, "", "", "", "");
    public static GameData.PlayerOutfit CamouflageOutfit_KPD = new GameData.PlayerOutfit().Set("", 13, "hat_pk05_Plant", "", "visor_BubbleBumVisor", "");

    public static bool IsCamouflage;
    public static Dictionary<byte, GameData.PlayerOutfit> PlayerSkins = new();

    [GameModuleInitializer]
    public static void Init()
    {
        IsCamouflage = false;
        PlayerSkins.Clear();
    }
    public static void CheckCamouflage()
    {
        if (!(AmongUsClient.Instance.AmHost && (Options.CommsCamouflage.GetBool() || CustomRoles.Concealer.IsExist(true)))) return;

        var oldIsCamouflage = IsCamouflage;

        IsCamouflage = Utils.IsActive(SystemTypes.Comms) || Concealer.IsHidding;

        if (oldIsCamouflage != IsCamouflage)
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                RpcSetSkin(pc);

                // The code is intended to remove pets at dead players to combat a vanilla bug
                if (!IsCamouflage && !pc.IsAlive())
                {

                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetPet, SendOption.Reliable, -1);
                    writer.Write("");
                    writer.Write(pc);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);

                }
            }
            Utils.NotifyRoles(NoCache: true);
        }
    }
    public static void RpcSetSkin(PlayerControl target, bool ForceRevert = false, bool RevertToDefault = false)
    {
        if (!(AmongUsClient.Instance.AmHost && (Options.CommsCamouflage.GetBool() || CustomRoles.Concealer.IsExist(true)))) return;
        if (target == null) return;

        var id = target.PlayerId;

        if (IsCamouflage)
        {
            //コミュサボ中

            //死んでいたら処理しない
            if (PlayerState.GetByPlayerId(id).IsDead) return;
        }

        var newOutfit = Options.KPDCamouflageMode.GetBool()
            ? CamouflageOutfit_KPD
            : CamouflageOutfit_Default;

        if (!IsCamouflage || ForceRevert)
        {
            //コミュサボ解除または強制解除

            if (Main.CheckShapeshift.TryGetValue(id, out var shapeshifting) && shapeshifting && !RevertToDefault)
            {
                //シェイプシフターなら今の姿のidに変更
                id = Main.ShapeshiftTarget[id];
            }

            newOutfit = PlayerSkins[id];
        }


        if (newOutfit.Compare(target.Data.DefaultOutfit)) return;

        Logger.Info($"newOutfit={newOutfit.GetString()}", "RpcSetSkin");

        target.SetOutFitStatic( newOutfit.ColorId, newOutfit.HatId, newOutfit.SkinId, newOutfit.VisorId, newOutfit.PetId);
    }
   
}