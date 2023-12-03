using TONEX.Roles.Core;
using TONEX;

public static class PetsPatch
{
    public static void SetPet(PlayerControl player, string petId, bool applyNow = false)
    {
        if (player.Is(CustomRoles.GM)) return;
        if (player.AmOwner)
        {
            player.SetPet(petId);
            return;
        }
        var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin(awa)");
        var outfit = player.Data.Outfits[PlayerOutfitType.Default];
        outfit.PetId = petId;
       player.SetPet(outfit.PetId);
        sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetPetStr)
            .Write(outfit.PetId)
            .EndRpc();
    }
}
