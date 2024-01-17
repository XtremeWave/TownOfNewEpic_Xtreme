using AmongUs.GameOptions;
using UnityEngine;
using TONEX.Modules;
using TONEX.Roles.Core;
using static TONEX.Translator;
using Hazel;
using static UnityEngine.GraphicsBuffer;

namespace TONEX.Roles.Crewmate;
public sealed class Veteran : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Veteran),
            player => new Veteran(player),
            CustomRoles.Veteran,
         () => Options.UsePets.GetBool() ? RoleTypes.Crewmate : RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            21800,
            SetupOptionItem,
            "ve",
            "#a77738"
        );
    public Veteran(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillDuration;
    static OptionItem OptionSkillNums;
    enum OptionName
    {
        VeteranSkillCooldown,
        VeteranSkillDuration,
        VeteranSkillMaxOfUseage,
    }

    private int SkillLimit;
    private long ProtectStartTime;
    private long UsePetCooldown;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.VeteranSkillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 11, OptionName.VeteranSkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillNums = IntegerOptionItem.Create(RoleInfo, 12, OptionName.VeteranSkillMaxOfUseage, new(1, 99, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        SkillLimit = OptionSkillNums.GetInt();
        ProtectStartTime = -1;
        if (Options.UsePets.GetBool()) UsePetCooldown = Utils.GetTimeStamp();
    }
    public override void OnGameStart()  
    {
        if(Options.UsePets.GetBool())UsePetCooldown = Utils.GetTimeStamp();
}
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown =
            SkillLimit <= 0
            ? 255f
            : OptionSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override int OverrideAbilityButtonUsesRemaining() => SkillLimit;
   
    public override bool GetGameStartSound(out string sound)
 {
 sound = "Gunload";
        return true;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("VeteranVetnButtonText");
        return true;
    }
    public override bool GetAbilityButtonSprite(out string buttonName)
    {
        buttonName = "Veteran";
        return true;
    }
    public override bool CanUseAbilityButton() => SkillLimit >= 1;
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.VeteranKill);
        sender.Writer.Write(SkillLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.VeteranKill) return;
        SkillLimit = reader.ReadInt32();
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (SkillLimit >= 1)
        {
            SkillLimit--;
            SendRPC();
            ProtectStartTime = Utils.GetTimeStamp();
            if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
            Player.RPCPlayCustomSound("Gunload");
            Player.Notify(string.Format(GetString("VeteranOnGuard"), SkillLimit, 2f));
            return true;
        }
        else
        {
            Player.Notify(GetString("SkillMaxUsage"));
            return false;
        }
    }
    public override void OnUsePet()
    {
        if (!Options.UsePets.GetBool()) return;
        if (UsePetCooldown != -1)
        {
            var cooldown = UsePetCooldown + (long)OptionSkillCooldown.GetFloat() - Utils.GetTimeStamp();
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), cooldown, 1f));
            return;
        }
        if (SkillLimit >= 1)
        {
            SkillLimit--;
            SendRPC();
            ProtectStartTime = Utils.GetTimeStamp();
            if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
            Player.RPCPlayCustomSound("Gunload");
            UsePetCooldown = Utils.GetTimeStamp();
            Player.Notify(string.Format(GetString("VeteranOnGuard"), SkillLimit, 2f));
        }
        else
        {
            Player.Notify(GetString("SkillMaxUsage"));
            return;
        }
    }
    public override bool GetPetButtonText(out string text)
    {
        text = GetString("VeteranVetnButtonText");
        return !(UsePetCooldown != -1);
    }
    public override bool GetPetButtonSprite(out string buttonName)
    {
        buttonName = "Veteran";
        return !(UsePetCooldown != -1);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var now = Utils.GetTimeStamp();
        if (ProtectStartTime + (long)OptionSkillDuration.GetFloat() < now && ProtectStartTime != -1)
        {
            ProtectStartTime = -1;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("VeteranOffGuard"), SkillLimit));
        }
        if (UsePetCooldown + (long)OptionSkillCooldown.GetFloat() < now && UsePetCooldown != -1 && Options.UsePets.GetBool())
        {
            UsePetCooldown = -1;
            player.RpcProtectedMurderPlayer();
            player.Notify(string.Format(GetString("PetSkillCanUse")));
        }
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        if (ProtectStartTime != 0 && ProtectStartTime + OptionSkillDuration.GetFloat() >= Utils.GetTimeStamp())
        {
            var (killer, target) = info.AttemptTuple;
            target.RpcMurderPlayerV2(killer);
            Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{killer.GetRealName()}", "Veteran.OnCheckMurderAsTarget");
            return false;
        }
        return true;
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        Player.RpcResetAbilityCooldown();
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(SkillLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Veteran) : Color.gray, $"({SkillLimit})");
}