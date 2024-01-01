using HarmonyLib;
using Il2CppInterop.Runtime;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONEX;

public static class CustomButton
{
    public static Sprite GetSprite(string name) => Utils.LoadSprite($"TONEX.Resources.Images.Skills.{name}.png", 115f);
}

#nullable enable
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update)), HarmonyPriority(Priority.LowerThanNormal)]
class HudSpritePatch
{
    private static Sprite? Defalt_Kill => DestroyableSingleton<HudManager>.Instance?.KillButton?.graphic?.sprite;
    private static Sprite? Defalt_Ability => DestroyableSingleton<HudManager>.Instance?.AbilityButton?.graphic?.sprite;
    private static Sprite? Defalt_Vent => DestroyableSingleton<HudManager>.Instance?.ImpostorVentButton?.graphic?.sprite;
    private static Sprite? Defalt_Report => DestroyableSingleton<HudManager>.Instance?.ReportButton?.graphic?.sprite;
    private static Sprite? Defalt_Pet => DestroyableSingleton<HudManager>.Instance.PetButton?.graphic?.sprite;
    private static Sprite? Defalt_Use => DestroyableSingleton<HudManager>.Instance.UseButton?.graphic?.sprite;
    private static Sprite? Defalt_Admin => DestroyableSingleton<HudManager>.Instance.AdminButton?.graphic?.sprite;
    private static SpriteRenderer? Defalt_Remain => DestroyableSingleton<HudManager>.Instance.AbilityButton?.usesRemainingSprite;
    public static void Postfix(HudManager __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (__instance == null || player == null || !GameStates.IsModHost || !GameStates.IsInTask) return;
        if (!SetHudActivePatch.IsActive || !player.IsAlive()) return;

        Sprite newKillButton = Defalt_Kill ?? __instance.KillButton.graphic.sprite;
        Sprite newAbilityButton = Defalt_Ability ?? __instance.AbilityButton.graphic.sprite;
        Sprite newVentButton = Defalt_Vent ?? __instance.ImpostorVentButton.graphic.sprite;
        Sprite newReportButton = Defalt_Report ?? __instance.ReportButton.graphic.sprite;
        Sprite newPetButton = Defalt_Pet ?? __instance.PetButton.graphic.sprite;
        SpriteRenderer newRemain = Defalt_Remain ?? __instance.AbilityButton.usesRemainingSprite;
        Sprite newUseButton = Defalt_Use ?? __instance.UseButton.graphic.sprite;
        Sprite newAdminButton = Defalt_Admin ?? __instance.AdminButton.graphic.sprite;


        if (Main.EnableCustomButton.Value)
        {
            if (player.GetRoleClass() is IKiller killer)
            {
                if (killer.OverrideKillButtonSprite(out var newKillButtonName))
                    newKillButton = CustomButton.GetSprite(newKillButtonName);
                if (killer.OverrideVentButtonSprite(out var newVentButtonName))
                    newVentButton = CustomButton.GetSprite(newVentButtonName);
            }
            if(player.Is(CustomRoles.EvilGuardian)) newAbilityButton = CustomButton.GetSprite("KillButton");
            if (player.GetRoleClass()?.GetAbilityButtonSprite(out var newAbilityButtonName) ?? false)
                newAbilityButton = CustomButton.GetSprite(newAbilityButtonName);
            if (player.GetRoleClass()?.GetReportButtonSprite(out var newReportButtonName) ?? false)
                newReportButton = CustomButton.GetSprite(newReportButtonName);
            if (player.GetRoleClass()?.GetPetButtonSprite(out var newPetButtonName) ?? false && Options.UsePets.GetBool() && player.GetRoleClass()?.GetPetButtonSprite(out var ButtonName) == true)
                newPetButton = CustomButton.GetSprite(newPetButtonName);
            if (player.GetRoleClass()?.GetUseButtonSprite(out var newUseButtonName) ?? false)
                newUseButton = CustomButton.GetSprite(newUseButtonName);
            if (player.GetRoleClass()?.GetAdminButtonSprite(out var newAdminButtonName) ?? false)
                newAdminButton = CustomButton.GetSprite(newAdminButtonName);

            Sprite newSprite = CustomButton.GetSprite("UseNum");
                SpriteRenderer spriteRenderer = __instance.AbilityButton.usesRemainingSprite;// 获取当前对象的 SpriteRenderer 组件
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = newSprite; // 将新的 Sprite 对象赋值给 SpriteRenderer 组件的 sprite 属性
                }
        }

        if (player.GetRoleClass() is IKiller)
        {
            if (__instance.KillButton.graphic.sprite != newKillButton && newKillButton != null)
            {
                __instance.KillButton.graphic.sprite = newKillButton;
                __instance.KillButton.graphic.material = __instance.ReportButton.graphic.material;
            }
            if (__instance.ImpostorVentButton.graphic.sprite != newVentButton && newVentButton != null)
            {
                __instance.ImpostorVentButton.graphic.sprite = newVentButton;
            }
            __instance.KillButton?.graphic?.material?.SetFloat("_Desat", __instance?.KillButton?.isCoolingDown ?? true ? 1f : 0f);
        }
        if (__instance.AbilityButton.graphic.sprite != newAbilityButton && newAbilityButton != null)
        {
            __instance.AbilityButton.graphic.sprite = newAbilityButton;
            __instance.AbilityButton.graphic.material = __instance.ReportButton.graphic.material;
        }
        if (__instance.ReportButton.graphic.sprite != newReportButton && newReportButton != null)
        {
            __instance.ReportButton.graphic.sprite = newReportButton;
        }
        if (__instance.PetButton.graphic.sprite != newPetButton && newPetButton != null)
        {
            __instance.PetButton.graphic.sprite = newPetButton;
        }
        if (__instance.AbilityButton.usesRemainingSprite != newRemain && newRemain != null)
        {
            __instance.AbilityButton.usesRemainingSprite = newRemain;
        }
        if (__instance.UseButton.graphic.sprite != newUseButton && newUseButton != null)
        {
            __instance.UseButton.graphic.sprite = newUseButton;
        }
        if (__instance.AdminButton.graphic.sprite != newAdminButton && newAdminButton != null)
        {
            __instance.AdminButton.graphic.sprite = newAdminButton;
        }
        __instance.AbilityButton?.graphic?.material?.SetFloat("_Desat", __instance?.AbilityButton?.isCoolingDown ?? true ? 1f : 0f);
    }
}
#nullable disable