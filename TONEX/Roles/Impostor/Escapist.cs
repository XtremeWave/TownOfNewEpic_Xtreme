using AmongUs.GameOptions;
using Hazel;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using UnityEngine;
using static TONEX.Translator;

namespace TONEX.Roles.Impostor;
public sealed class Escapist : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Escapist),
            player => new Escapist(player),
            CustomRoles.Escapist,
       () => Options.UsePets.GetBool() ? RoleTypes.Impostor : RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            2000,
            null,
            "ec|逃逸"
        );
    public Escapist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Marked = false;
    }

    private bool Shapeshifting;
    private bool Marked;
    private Vector2 MarkedPosition;
    private int UsePetCooldown;
    public override void Add()
    {
        Marked = false;
        Shapeshifting = false;
    }
        public override void OnGameStart() => UsePetCooldown = (int)Options.DefaultKillCooldown;
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SyncEscapist);
        sender.Writer.Write(Marked);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SyncEscapist) return;
        Marked = reader.ReadBoolean();
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = Marked ? Translator.GetString("EscapistTeleportButtonText") : Translator.GetString("EscapistMarkButtonText");
        return !Shapeshifting;
    }
    public override bool GetPetButtonText(out string text)
    {
        text = Marked ? Translator.GetString("EscapistTeleportButtonText") : Translator.GetString("EscapistMarkButtonText");
        return !(UsePetCooldown != 0);
    }
    public override bool GetPetButtonSprite(out string buttonName)
    {
        buttonName = "Telport";
        return !(UsePetCooldown != 0);
    }
    public override void OnUsePet()
    {
        if (!Options.UsePets.GetBool()) return;
        if (UsePetCooldown != 0)
        {
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), UsePetCooldown, 1f));
            return;
        }
                if (Marked)
        {
            Marked = false;
            Player.RPCPlayCustomSound("Teleport");
            Utils.TP(Player.NetTransform, MarkedPosition);
            Logger.Msg($"{Player.GetNameWithRole()}：{MarkedPosition}", "Escapist.OnShapeshift");
        }
        else
        {
            MarkedPosition = Player.GetTruePosition();
            Marked = true;
            SendRPC();
        }
        return;
    }
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (!Shapeshifting) return;

        if (Marked)
        {
            Marked = false;
            Player.RPCPlayCustomSound("Teleport");
            Utils.TP(Player.NetTransform, MarkedPosition);
            Logger.Msg($"{Player.GetNameWithRole()}：{MarkedPosition}", "Escapist.OnShapeshift");
        }
        else
        {
            MarkedPosition = Player.GetTruePosition();
            Marked = true;
            SendRPC();
        }
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
  public override void AfterMeetingTasks()
    {
        UsePetCooldown = (int)Options.DefaultKillCooldown;
    }
}