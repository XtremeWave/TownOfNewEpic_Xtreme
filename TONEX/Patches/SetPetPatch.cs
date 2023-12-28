using TONEX.Roles.Core;
using TONEX;
using static UnityEngine.GraphicsBuffer;

public static class PetsPatch
{
    public static void SetPet(PlayerControl player, string petId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!player.Is(CustomRoles.GM))
        {
            var sender = CustomRpcSender.Create(name: $"PetsPatch.RpcSetPet)");
            player.SetPet(petId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetPetStr)
            .Write(petId)
            .EndRpc();
            return;
        }
    }
}
