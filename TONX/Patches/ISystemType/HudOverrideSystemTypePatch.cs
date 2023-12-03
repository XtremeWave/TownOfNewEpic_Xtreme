using HarmonyLib;
using Hazel;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;

namespace TONEX.Patches.ISystemType;

[HarmonyPatch(typeof(HudOverrideSystemType), nameof(HudOverrideSystemType.UpdateSystem))]
public static class HudOverrideSystemTypeUpdateSystemPatch
{
    public static bool Prefix(HudOverrideSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }

        var playerRole = player.GetRoleClass();

        if (playerRole is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateHudOverrideSystem(__instance, amount))
        {
            return false;
        }
        return true;
    }
    public static void Postfix()
    {
        Camouflage.CheckCamouflage();
        Utils.NotifyRoles();
    }
}