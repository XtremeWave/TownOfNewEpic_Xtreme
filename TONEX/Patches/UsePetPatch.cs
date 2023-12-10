using AmongUs.GameOptions;
using HarmonyLib;
using MS.Internal.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hazel;
using InnerNet;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using System.Runtime.Intrinsics.X86;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UI;
using UnityEngine.Networking.Types;
using Microsoft.Extensions.Logging;
using Sentry;
using UnityEngine.SocialPlatforms;
using static UnityEngine.ParticleSystem.PlaybackState;
using Cpp2IL.Core.Extensions;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Neutral;
using TONEX;

namespace TONEX;

/*
 * HUGE THANKS TO
 * ImaMapleTree / 단풍잎 / Tealeaf
 * FOR THE CODE
 */


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TryPet))]
class LocalPetPatch
{
    private static readonly Dictionary<byte, long> LastProcess = new();

    public static bool Prefix(PlayerControl __instance)
    {
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return true;
        if (GameStates.IsLobby) return true;

        if (__instance.petting) return true;
        __instance.petting = true;

        if (!LastProcess.ContainsKey(__instance.PlayerId)) LastProcess.TryAdd(__instance.PlayerId, Utils.GetTimeStamp() - 2);
        if (LastProcess[__instance.PlayerId] + 1 >= Utils.GetTimeStamp()) return true;

        ExternalRpcPetPatch.Prefix(__instance.MyPhysics, (byte)RpcCalls.Pet);
        LastProcess[__instance.PlayerId] = Utils.GetTimeStamp();
        return false;
    }

    public static void Postfix(PlayerControl __instance)
    {
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return;
        __instance.petting = false;
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
class ExternalRpcPetPatch
{
    public static Dictionary<byte, float> PetCooldown = new();
    public static Dictionary<byte, bool> SkillReady = new();
    public static void Init()
    {
        PetCooldown = new();
        SkillReady = new();
    }

    private static readonly Dictionary<byte, long> LastProcess = new();
    public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callID)
    {
        if (!AmongUsClient.Instance.AmHost || (RpcCalls)callID != RpcCalls.Pet) return;

        var pc = __instance.myPlayer;
        var physics = __instance;

        if (pc == null || physics == null) return;

        if (pc != null
            && !pc.inVent
            && !pc.inMovingPlat
            && !pc.walkingToVent
            && !pc.onLadder
            && !physics.Animations.IsPlayingEnterVentAnimation()
            && !physics.Animations.IsPlayingClimbAnimation()
            && !physics.Animations.IsPlayingAnyLadderAnimation()
            && !Pelican.IsEaten(pc.PlayerId)
            && GameStates.IsInTask)
            physics.CancelPet();

        if (!LastProcess.ContainsKey(pc.PlayerId)) LastProcess.TryAdd(pc.PlayerId, Utils.GetTimeStamp() - 2);
        if (LastProcess[pc.PlayerId] + 1 >= Utils.GetTimeStamp()) return;
        LastProcess[pc.PlayerId] = Utils.GetTimeStamp();
        __instance.CancelPet();
        physics.RpcCancelPet();
        physics.RpcCancelPet();
        physics.RpcCancelPet();
        physics.RpcCancelPet();
        Logger.Info($"Player {pc.GetNameWithRole().RemoveHtmlTags()} petted their pet", "PetActionTrigger");

        _ = new LateTask(() => { OnPetUse(pc); }, 0.2f, $"OnPetUse: {pc.GetNameWithRole().RemoveHtmlTags()}");
    }
    public static void OnPetUse(PlayerControl pc)
    {

        if (pc == null ||
            pc.inVent ||
            pc.inMovingPlat ||
            pc.onLadder ||
            pc.walkingToVent ||
            pc.MyPhysics.Animations.IsPlayingEnterVentAnimation() ||
            pc.MyPhysics.Animations.IsPlayingClimbAnimation() ||
            pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation() ||
            Pelican.IsEaten(pc.PlayerId))
            return;
        var Pet = pc;
          Pet.GetRoleClass()?.OnUsePet(Pet);
    }
}

