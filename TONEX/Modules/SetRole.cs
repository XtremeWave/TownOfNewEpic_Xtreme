/*using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AmongUs.Data;
using AmongUs.GameOptions;
using AmongUs.QuickChat;
using Assets.CoreScripts;
using Beebyte.Obfuscator;
using Hazel;
using InnerNet;
using Innersloth.Assets;
using PowerTools;
using TONEX;
using HarmonyLib;
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetRole))]
public static class RoleManagerSetRole
{
    
    public static void Prefix(PlayerControl __instance, RoleTypes role)
    {
        bool flag = RoleManager.IsGhostRole(role);
        if (!DestroyableSingleton<TutorialManager>.InstanceExists && __instance.roleAssigned && !flag)
        {
            return;
        }
        if (flag)
        {
            DestroyableSingleton<RoleManager>.Instance.SetRole(__instance, role);
            __instance.Data.Role.SpawnTaskHeader(__instance);
            if (__instance.AmOwner)
            {
                DestroyableSingleton<HudManager>.Instance.ReportButton.gameObject.SetActive(false);
                return;
            }
        }
        else
        {
            __instance.RemainingEmergencies = GameManager.Instance.LogicOptions.GetNumEmergencyMeetings();
            DestroyableSingleton<RoleManager>.Instance.SetRole(__instance, role);
            __instance.Data.Role.SpawnTaskHeader(__instance);
            __instance.MyPhysics.SetBodyType(__instance.BodyType);
            if (__instance.AmOwner)
            {
                if (__instance.Data.Role.IsImpostor)
                {
                    StatsManager.Instance.IncrementStat(StringNames.StatsGamesImpostor);
                    StatsManager.Instance.ResetStat(StringNames.StatsCrewmateStreak);
                }
                else
                {
                    StatsManager.Instance.IncrementStat(StringNames.StatsGamesCrewmate);
                    StatsManager.Instance.IncrementStat(StringNames.StatsCrewmateStreak);
                }
                DestroyableSingleton<HudManager>.Instance.MapButton.gameObject.SetActive(true);
                DestroyableSingleton<HudManager>.Instance.ReportButton.gameObject.SetActive(true);
                DestroyableSingleton<HudManager>.Instance.UseButton.gameObject.SetActive(true);
            }
            if (!DestroyableSingleton<TutorialManager>.InstanceExists)
            {
                if (Enumerable.All<PlayerControl>(Main.AllPlayerControls, (PlayerControl pc) => pc.roleAssigned || pc.Data.Disconnected))
                {
                    System.Action<PlayerControl> action = new(pc => PlayerNameColor.Set(pc));
                    PlayerControl.AllPlayerControls.ForEach(action);
                    __instance.StopAllCoroutines();
                    DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
                    DestroyableSingleton<HudManager>.Instance.HideGameLoader();
                }
            }
        }
        return;
    }
}*/