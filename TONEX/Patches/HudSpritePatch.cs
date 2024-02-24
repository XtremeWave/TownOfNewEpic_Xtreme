using HarmonyLib;
using Il2CppInterop.Runtime;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
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
    public static bool IsEnd;
    private static Sprite? Defalt_Kill => DestroyableSingleton<HudManager>.Instance?.KillButton?.graphic?.sprite;
    private static Sprite? Defalt_Ability => DestroyableSingleton<HudManager>.Instance?.AbilityButton?.graphic?.sprite;
    private static Sprite? Defalt_Vent => DestroyableSingleton<HudManager>.Instance?.ImpostorVentButton?.graphic?.sprite;
    private static Sprite? Defalt_Report => DestroyableSingleton<HudManager>.Instance?.ReportButton?.graphic?.sprite;
    private static Sprite? Defalt_Pet => DestroyableSingleton<HudManager>.Instance.PetButton?.graphic?.sprite;
    private static Sprite? Defalt_Use => DestroyableSingleton<HudManager>.Instance.UseButton?.graphic?.sprite;
    private static Sprite? Defalt_Admin => DestroyableSingleton<HudManager>.Instance.AdminButton?.graphic?.sprite;
    private static SpriteRenderer? Defalt_Remain => DestroyableSingleton<HudManager>.Instance.AbilityButton?.usesRemainingSprite;
    private static GameObject? Defalt_Set => DestroyableSingleton<HudManager>.Instance.SettingsButton;
    private static PassiveButton? Defalt_Map => DestroyableSingleton<HudManager>.Instance.MapButton;
    private static GameObject? Defalt_Chat => DestroyableSingleton<HudManager>.Instance.Chat.chatButton;
    public static void Postfix(HudManager __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (__instance == null || player == null || !GameStates.IsModHost) return;
        if (!SetHudActivePatch.IsActive || !player.IsAlive()) return;

        Sprite newKillButton = Defalt_Kill ?? __instance.KillButton.graphic.sprite;
        Sprite newAbilityButton = Defalt_Ability ?? __instance.AbilityButton.graphic.sprite;
        Sprite newVentButton = Defalt_Vent ?? __instance.ImpostorVentButton.graphic.sprite;
        Sprite newReportButton = Defalt_Report ?? __instance.ReportButton.graphic.sprite;
        Sprite newPetButton = Defalt_Pet ?? __instance.PetButton.graphic.sprite;
        SpriteRenderer newRemain = Defalt_Remain ?? __instance.AbilityButton.usesRemainingSprite;
        Sprite newUseButton = Defalt_Use ?? __instance.UseButton.graphic.sprite;
        Sprite newAdminButton = Defalt_Admin ?? __instance.AdminButton.graphic.sprite;
        GameObject newSetting = Defalt_Set ?? __instance.SettingsButton;
        PassiveButton newMap = Defalt_Map ?? __instance.MapButton;
        GameObject newChat = Defalt_Chat ?? __instance.Chat.chatButton;

        if (Main.EnableCustomButton.Value)
        {
            if (player.GetRoleClass() is IKiller killer)
            {
                if (killer.OverrideKillButtonSprite(out var newKillButtonName))
                    newKillButton = CustomButton.GetSprite(newKillButtonName);
                if (killer.OverrideVentButtonSprite(out var newVentButtonName))
                    newVentButton = CustomButton.GetSprite(newVentButtonName);
            }
            if (player.Is(CustomRoles.EvilGuardian)) newAbilityButton = CustomButton.GetSprite("KillButton");
            if (player.GetRoleClass()?.GetAbilityButtonSprite(out var newAbilityButtonName) ?? false)
                newAbilityButton = CustomButton.GetSprite(newAbilityButtonName);
            if (player.GetRoleClass()?.GetReportButtonSprite(out var newReportButtonName) ?? false)
                newReportButton = CustomButton.GetSprite(newReportButtonName);

            if (player.GetRoleClass()?.GetPetButtonSprite(out var newPetButtonName) == true && Options.UsePets.GetBool())
            {
                var PetButton = newPetButtonName;
                newPetButton = CustomButton.GetSprite(PetButton);
                __instance.PetButton?.graphic?.material?.SetFloat("_Desat", 0f);
            }
            else if (player.GetRoleClass()?.GetPetButtonSprite(out var newPetButtonNameV2) == false && Options.UsePets.GetBool() && newPetButtonNameV2 != null)
            {
                var PetButton = newPetButtonNameV2;
                if (PetButton != default)
                {
                    newPetButton = CustomButton.GetSprite(PetButton);
                    __instance.PetButton?.graphic?.material?.SetFloat("_Desat", 1f);
                }
            }

            if (player.GetRoleClass()?.GetUseButtonSprite(out var newUseButtonName) ?? false)
                newUseButton = CustomButton.GetSprite(newUseButtonName);
            if (player.GetRoleClass()?.GetAdminButtonSprite(out var newAdminButtonName) ?? false)
                newAdminButton = CustomButton.GetSprite(newAdminButtonName);


            Sprite newUseNumSprite = CustomButton.GetSprite("UseNum");
            newRemain.sprite = newUseNumSprite;

            Sprite newSettingButton = CustomButton.GetSprite("SettingButton");
            SpriteRenderer spritesetting = newSetting.GetComponent<SpriteRenderer>();
            spritesetting.sprite = newSettingButton;

            Sprite newChatButton = CustomButton.GetSprite("ChatLobby");
            
            if (!GameStates.IsNotJoined)
            {
                switch (player.GetCustomRole().GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Crewmate:
                        newChatButton = CustomButton.GetSprite("ChatCrew");
                        break;
                    case CustomRoleTypes.Impostor:
                        newChatButton = CustomButton.GetSprite("ChatImp");
                        break;
                    case CustomRoleTypes.Neutral:
                        if (player.GetRoleClass() is not IIndependent && !player.GetCustomRole().IsNeutralKilling())
                            newChatButton = CustomButton.GetSprite("ChatN");
                        else
                            newChatButton = CustomButton.GetSprite("ChatEvilN");
                        break;
                }
            }
            if (IsEnd)
                newChatButton = CustomButton.GetSprite("ChatLobby");
            SpriteRenderer spritechat = newChat.GetComponent<SpriteRenderer>();
            spritechat.sprite = newChatButton;

            #region 地图
            Sprite newmap = CustomButton.GetSprite("mapJourne");
            switch (Main.NormalOptions.MapId)
            {
                case 0:
                    newmap = CustomButton.GetSprite("mapJourne");
                    break;
                case 1:
                    newmap = CustomButton.GetSprite("mapMIRA");
                    break;
                case 2:
                    newmap = CustomButton.GetSprite("mapPolus");
                    break;
                case 4:
                    newmap = CustomButton.GetSprite("mapAirship");
                    break;
                case 5:
                    newmap = CustomButton.GetSprite("theFungle");
                    break;

            }
            SpriteRenderer spritemap = newMap.GetComponent<SpriteRenderer>();
            spritemap.sprite = newmap;
            #endregion
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
            if (__instance.SettingsButton != newSetting && newSetting != null)
            {
                __instance.SettingsButton = newSetting;
            }
            if (__instance.Chat.chatButton != newChat && newChat != null)
            {
                __instance.Chat.chatButton = newChat;
            }
            if (__instance.MapButton != newMap && newMap != null)
            {
                __instance.MapButton = newMap;
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

    
}
#nullable disable