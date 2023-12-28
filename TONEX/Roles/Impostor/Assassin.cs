using AmongUs.GameOptions;
using Hazel;
using Il2CppSystem.Collections.Generic;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;
using static TONEX.Translator;

namespace TONEX.Roles.Impostor;
public sealed class Assassin : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Assassin),
            player => new Assassin(player),
            CustomRoles.Assassin,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1600,
            SetupOptionItem,
            "as|忍者"
        );
    public Assassin(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ForAssassin = new();
    }

    static OptionItem MarkCooldown;
    static OptionItem AssassinateCooldown;
    enum OptionName
    {
        AssassinMarkCooldown,
        AssassinAssassinateCooldown,
    }

    public static List<byte> ForAssassin;
    private static void SetupOptionItem()
    {
        MarkCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.AssassinMarkCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        AssassinateCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.AssassinAssassinateCooldown, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        ForAssassin = new();
        Shapeshifting = false;
    }
    private static void SendRPC_SyncList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMarkedPlayer, SendOption.Reliable, -1);
        writer.Write(ForAssassin.Count);
        for (int i = 0; i < ForAssassin.Count; i++)
            writer.Write(ForAssassin[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ForAssassin = new();
        for (int i = 0; i < count; i++)
            ForAssassin.Add(reader.ReadByte());
    }
    public float CalculateKillCooldown() => MarkCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = AssassinateCooldown.GetFloat();
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        if (Shapeshifting) return true;
        else
        {
            ForAssassin.Add(target.PlayerId);
          SendRPC_SyncList();
            killer.ResetKillCooldown();
            killer.SetKillCooldownV2();
            killer.RPCPlayCustomSound("Clothe");
            return false;
        }
    }
    private bool Shapeshifting;
    private Vector2 dis;
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);
         dis =  Player.GetTruePosition();
        if (!AmongUsClient.Instance.AmHost) return;

        if (!Shapeshifting)
        {
            Player.SetKillCooldownV2();
            return;
        }
        foreach (var pc in ForAssassin)
        {
            target = Utils.GetPlayerById(pc);
            SendRPC_SyncList();
            new LateTask(() =>
            {
                if (!(target == null || !target.IsAlive() || target.IsEaten() || target.inVent || !GameStates.IsInTask))
                {
                    Utils.TP(Player.NetTransform, target.GetTruePosition());
                    CustomRoleManager.OnCheckMurder(Player, target);
                    ForAssassin.Remove(target.PlayerId);
                    SendRPC_SyncList();
                }
            }, 1.5f, "Assassin Assassinate");
        }
        Utils.TP(Player.NetTransform, dis);
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("AssassinMarkButtonText");
        return !Shapeshifting;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("AssassinShapeshiftText");
        return ForAssassin.Count >= 1 && !Shapeshifting;
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = "Mark";
        return !Shapeshifting;
    }
    public override bool GetAbilityButtonSprite(out string buttonName)
    {
        buttonName = "Assassinate";
        return ForAssassin.Count >= 1 && !Shapeshifting;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (ForAssassin.Contains(seen.PlayerId))
            return Utils.ColorString(Color.red, "🔴");
        else
            return "";
    }
}