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
using TONEX.Roles.Core.Interfaces;
using Il2CppInterop.Generator.Extensions;

namespace TONEX;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TryPet))]
class TryPetPatch
{
    public static void Prefix(PlayerControl __instance)
    {
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return;
        var cancel = Options.CurrentGameMode == CustomGameMode.Standard;
        if (cancel)
            __instance.petting = true;
        ExternalRpcPetPatch.Prefix(__instance.MyPhysics, 51, new MessageReader());
    }

    public static void Postfix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var cancel = Options.CurrentGameMode == CustomGameMode.Standard;
        if (cancel)
        {
            __instance.petting = false;
            if (__instance.AmOwner)
                __instance.MyPhysics.RpcCancelPet();
        }
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
class ExternalRpcPetPatch
{
    public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var cancel = Options.CurrentGameMode == CustomGameMode.Standard;
        var rpcType = callId == 51 ? RpcCalls.Pet : (RpcCalls)callId;
        if (rpcType != RpcCalls.Pet) return;

        PlayerControl pc = __instance.myPlayer;

        if (callId == 51 && cancel)
            __instance.CancelPet();
        if (callId != 51 && cancel)
        {
            __instance.CancelPet();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                AmongUsClient.Instance.FinishRpcImmediately(AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, 50, SendOption.None, player.GetClientId()));
        }
        if (!pc.CanUseSkill()) return;
        pc.GetRoleClass()?.OnUsePet();
    }
}

