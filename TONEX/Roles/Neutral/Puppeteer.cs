using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using System.Linq;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Neutral;
public sealed class Puppeteer : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Puppeteer),
            player => new Puppeteer(player),
            CustomRoles.Puppeteer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            94_1_1_0120,
            SetupOptionItem,
            "pu|傀偶",
            "#cc6666",
            true
        );
    public Puppeteer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {

    }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionBeKillLimit;
    static OptionItem OptionDuration;
    enum OptionName
    {
       PuooeteerKillCooldown,
        BeKillLimit,
        SetPlayerDuration
    }
    Vector2 MyLastPos;
    public int BeKillLimit;
    public PlayerControl MarkedPlayer = new();
    public bool CanKill;
    public GameData.PlayerOutfit Skins;
    public string Name;
    public string NameV2;
    public long Timer;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.PuooeteerKillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionBeKillLimit = IntegerOptionItem.Create(RoleInfo, 11, OptionName.BeKillLimit, new(1, 99, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionDuration = FloatOptionItem.Create(RoleInfo, 12, OptionName.SetPlayerDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        Timer = -1;
        CanKill = true;
        MarkedPlayer = null;
        BeKillLimit = OptionBeKillLimit.GetInt();
        Name = Main.AllPlayerNames[Player.PlayerId];
        NameV2 = null;
    }
    public bool IsKiller => false;
    public float CalculateKillCooldown()
    {
        if (!CanUseKillButton()) return 255f;
        return OptionKillCooldown.GetFloat();
    }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool CanUseSabotageButton() => false;
    public bool CanUseKillButton() => Player.IsAlive();
    private void SendRPC_SyncLimit()
    {
        using var sender = CreateSender(CustomRPC.SetBeKillLimit);
        sender.Writer.Write(BeKillLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetBeKillLimit) return;
        BeKillLimit = reader.ReadInt32();
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(Utils.ShadeColor(RoleInfo.RoleColor), $"({BeKillLimit})");
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if(!CanKill) return false;
        MarkedPlayer = target;
        killer.SetKillCooldownV2();
        Skins = new GameData.PlayerOutfit().Set(Player.GetRealName(), Player.Data.DefaultOutfit.ColorId, Player.Data.DefaultOutfit.HatId, Player.Data.DefaultOutfit.SkinId, Player.Data.DefaultOutfit.VisorId, Player.Data.DefaultOutfit.PetId);
        return false;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        if (!CanKill)
        {
            var (killer, target) = info.AttemptTuple;
            killer.SetKillCooldownV2();
            BeKillLimit--;
            SendRPC_SyncLimit();
            if (BeKillLimit <= 0) Win();
            return false;
        }
        return true;
    }
    public void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Puppeteer);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    public override void OnUsePet()
    {
        if (MarkedPlayer != null)
        {
            CanKill = false;
            Timer = Utils.GetTimeStamp();
            GameData.PlayerOutfit TargetSkins = new GameData.PlayerOutfit().Set(MarkedPlayer.GetRealName(), MarkedPlayer.Data.DefaultOutfit.ColorId, MarkedPlayer.Data.DefaultOutfit.HatId, MarkedPlayer.Data.DefaultOutfit.SkinId, MarkedPlayer.Data.DefaultOutfit.VisorId, MarkedPlayer.Data.DefaultOutfit.PetId);
            var outfit = TargetSkins;
            Player.SetOutFitStatic(outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
            MarkedPlayer.SetOutFitStatic(outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
            NameV2 = Main.AllPlayerNames[MarkedPlayer.PlayerId];
            Main.AllPlayerNames[Player.PlayerId] = NameV2;
            Main.AllPlayerNames[MarkedPlayer.PlayerId] = NameV2;
            Player.MarkDirtySettings(); 
            MyLastPos = Player.GetTruePosition();
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var now = Utils.GetTimeStamp();
        if (Player.IsAlive() && Timer + (long)OptionDuration.GetFloat() < now && Timer != -1)
        {
            CanKill = true;
            Timer = -1;  
            Player.RpcTeleport(MyLastPos);
            Player.MarkDirtySettings();
            Main.AllPlayerNames[Player.PlayerId] = Name;
            var outfit = Skins;
            Player.SetOutFitStatic(outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
            Player.MarkDirtySettings();
        }
    }
}
