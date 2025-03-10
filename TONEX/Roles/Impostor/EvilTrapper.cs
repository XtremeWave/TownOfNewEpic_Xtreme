using AmongUs.GameOptions;
using Hazel;
using System.Linq;
using UnityEngine;
using TONEX.Roles.Core;
using static TONEX.Translator;
using System.Collections.Generic;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using static Il2CppSystem.Net.Http.Headers.Parser;
using TMPro;
using static UnityEngine.GraphicsBuffer;
using TONEX.Roles.GameModeRoles;
using YamlDotNet.Core.Tokens;

namespace TONEX.Roles.Impostor;
public sealed class EvilTrapper : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilTrapper),
            player => new EvilTrapper(player),
            CustomRoles.EvilTrapper,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            94_1_4_1600,
            SetupOptionItem,
            "et"
        );
    public EvilTrapper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { ForEvilTrapper = new();
        OriginalSpeed = new();
    }

    static OptionItem OptionCooldown;
    static OptionItem OptionLimit;
    static OptionItem OptionDuration;
    public int Limit;
    public List<byte> ForEvilTrapper;
    public List<Vector2> Snare;
    Dictionary<byte, float> OriginalSpeed;
    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.SkillCooldown, new(0f, 180f, 2.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionLimit = IntegerOptionItem.Create(RoleInfo, 11, GeneralOption.SkillLimit, new(1, 180, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionDuration = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.SkillDuration, new(1f, 10f, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        Limit = OptionLimit.GetInt();
        ForEvilTrapper = new();
        Snare = new();
        OriginalSpeed = new();
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("EvilTrapperButtonText");
        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = OptionCooldown.GetFloat();
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Limit);
    }
    public override void ReceiveRPC(MessageReader reader) => Limit = reader.ReadInt32();

    public override bool OnCheckVanish()
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (Limit <= 0) return false;
        Snare.Add(Player.GetTruePosition());
        Limit--;
        SendRPC();
        Utils.NotifyRoles(Player);
        return false;

    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
         foreach(var V2 in Snare) {
            foreach(var pc in Main.AllAlivePlayerControls) {
                if (Vector2.Distance(V2, pc.GetTruePosition()) < 0.5f && !pc.GetCustomRole().IsImpostor() && !ForEvilTrapper.Contains(pc.PlayerId)) {
                    OriginalSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId];
                    ForEvilTrapper.Add(pc.PlayerId);
                     Main.AllPlayerSpeed[pc.PlayerId] = Main.MinSpeed;
                    pc.RpcTeleport(V2);
                    Snare.Remove(V2);
                    pc.MarkDirtySettings();
                    pc.Notify(GetString("ForEvilTrapper"));
                    new LateTask(() =>
                    {
                        if (ForEvilTrapper.Contains(pc.PlayerId)) {
                            pc.RpcMurderPlayerV2(pc);
                            pc.SetRealKiller(Player);
                            Main.AllPlayerSpeed[pc.PlayerId] = OriginalSpeed[pc.PlayerId];
                            ForEvilTrapper.Remove(pc.PlayerId);
                        }
                        pc.MarkDirtySettings();
                        Utils.NotifyRoles();
                    }, OptionDuration.GetFloat(), "Trapper");
                }
            }
            break;
        }
        foreach (var tg in ForEvilTrapper)
        {
            var target = Utils.GetPlayerById(tg);
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (Vector2.Distance(target.GetTruePosition(), pc.GetTruePosition()) < 0.3f && pc!=target)
                {
                    ForEvilTrapper.Remove(target.PlayerId);
                    Main.AllPlayerSpeed[pc.PlayerId] = OriginalSpeed[pc.PlayerId];
                    pc.RpcProtectedMurderPlayer(pc);
                    pc.MarkDirtySettings();
                }
            }
        }
         
    }
    public override void OnStartMeeting()
    {
        foreach (var tg in ForEvilTrapper)
        {
            var target = Utils.GetPlayerById(tg);
            ForEvilTrapper.Remove(tg);
            target.SetRealKiller(Player);
            target.RpcMurderPlayerV2(target);
            Main.AllPlayerSpeed[tg] = OriginalSpeed[tg];
            ForEvilTrapper.Remove(tg);
            target.MarkDirtySettings();
        }

    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(Limit>=1 ? Utils.GetRoleColor(CustomRoles.Impostor) : Color.gray, $"({Limit})");
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner) => Player.RpcResetAbilityCooldown();
}
