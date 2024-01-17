using AmongUs.GameOptions;
using Hazel;
using Il2CppSystem.Collections.Generic;
using MS.Internal.Xml.XPath;
using TONEX.Modules;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Impostor;
public sealed class Ninja : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Ninja),
            player => new Ninja(player),
            CustomRoles.Ninja,
       () => Options.UsePets.GetBool() ? RoleTypes.Impostor : RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1600,
            SetupOptionItem,
            "as|忍者"
        );
    public Ninja(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ForNinja = new();
    }

    static OptionItem MarkCooldown;
    static OptionItem NinjaateCooldown;
    enum OptionName
    {
        NinjaMarkCooldown,
        NinjaNinjaateCooldown,
    }

    public static List<byte> ForNinja;
    public int UsePetCooldown;
    private static void SetupOptionItem()
    {
        MarkCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.NinjaMarkCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        NinjaateCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.NinjaNinjaateCooldown, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        ForNinja = new();
        Shapeshifting = false;
    }
    public override void OnGameStart() => UsePetCooldown = NinjaateCooldown.GetInt();
    private static void SendRPC_SyncList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMarkedPlayer, SendOption.Reliable, -1);
        writer.Write(ForNinja.Count);
        for (int i = 0; i < ForNinja.Count; i++)
            writer.Write(ForNinja[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_SyncList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ForNinja = new();
        for (int i = 0; i < count; i++)
            ForNinja.Add(reader.ReadByte());
    }
    public float CalculateKillCooldown() => MarkCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = NinjaateCooldown.GetFloat();
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        if (Shapeshifting) return true;
        else
        {
            ForNinja.Add(target.PlayerId);
          SendRPC_SyncList();
            killer.ResetKillCooldown();
            killer.SetKillCooldownV2();
            killer.RPCPlayCustomSound("Clothe");
            TargetArrow.Add(Player.PlayerId, target.PlayerId);
            Utils.NotifyRoles();
            return false;
        }
    }
    public override void OnUsePet()
    {
        if (!Options.UsePets.GetBool()) return;
        if (UsePetCooldown != 0)
        {
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), UsePetCooldown, 1f));
            return;
        }
        Player.SetKillCooldownV2();
       foreach (var pc in ForNinja)
        {
         var target = Utils.GetPlayerById(pc);
            SendRPC_SyncList();
                if (!(target == null || !target.IsAlive() || target.IsEaten() || target.inVent || !GameStates.IsInTask))
                {
                    target.RpcMurderPlayerV2(target);
                    target.SetRealKiller(Player);
                    ForNinja.Remove(target.PlayerId);
                    SendRPC_SyncList();
                }
        }
            return;
    }
    private bool Shapeshifting;
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Shapeshifting)
        {
            Player.SetKillCooldownV2();
            return;
        }
        foreach (var pc in ForNinja)
        {
         var ps = Utils.GetPlayerById(pc);
            SendRPC_SyncList();
            new LateTask(() =>
            {
                if (!(target == null || !target.IsAlive() || target.IsEaten() || target.inVent || !GameStates.IsInTask))
                {
                    Player.RpcMurderPlayerV2(ps);
                    ForNinja.Remove(ps.PlayerId);
                    SendRPC_SyncList();
                }
            }, 1.5f, "Ninja Ninjaate");
        }
    }
    public override bool GetGameStartSound(out string sound)
    {
        sound = "Clothe";
        return true;
    }
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (UsePetCooldown == 0 || !Options.UsePets.GetBool()) return;
        if (UsePetCooldown >= 1 && Player.IsAlive() && !GameStates.IsMeeting) UsePetCooldown -= 1;
        if (UsePetCooldown <= 0 && Player.IsAlive())
        {
            player.Notify(string.Format(GetString("PetSkillCanUse")), 2f);
        }
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("NinjaMarkButtonText");
        return !Shapeshifting;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("NinjaShapeshiftText");
        return ForNinja.Count >= 1 && !Shapeshifting;
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = "Mark";
        return !Shapeshifting;
    }
    public override bool GetAbilityButtonSprite(out string buttonName)
    {
        buttonName = "Ninjaate";
        return ForNinja.Count >= 1 && !Shapeshifting;
    }
        public override bool GetPetButtonText(out string text)
    {
                text = GetString("NinjaShapeshiftText");
        return ForNinja.Count >= 1 && !(UsePetCooldown != 0);
    }
    public override bool GetPetButtonSprite(out string buttonName)
    {
             buttonName = "Mark";
        return ForNinja.Count >= 1 && !(UsePetCooldown != 0);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (ForNinja.Contains(seen.PlayerId))
            return Utils.ColorString(Color.red, "🔴");
        else
            return "";
    }
   public override void AfterMeetingTasks()
    {
        UsePetCooldown = (int)NinjaateCooldown.GetInt();
    }
}