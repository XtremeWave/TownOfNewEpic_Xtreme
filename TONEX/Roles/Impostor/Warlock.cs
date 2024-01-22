using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TONEX.Modules;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using UnityEngine;
using static TONEX.Translator;

namespace TONEX.Roles.Impostor;
public sealed class Warlock : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Warlock),
            player => new Warlock(player),
            CustomRoles.Warlock,
       () => Options.UsePets.GetBool() ? RoleTypes.Impostor : RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1500,
            SetupOptionItem,
            "wa|術士"
        );
    public Warlock(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    public override void OnDestroy()
    {
        CursedPlayer = null;
    }
    static OptionItem OptionCanKillAllies;
    static OptionItem OptionCanKillSelf;

    PlayerControl CursedPlayer;
    bool IsCursed;
    bool Shapeshifting;
    public int UsePetCooldown;
    private static void SetupOptionItem()
    {
        OptionCanKillAllies = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanKillAllies, false, false);
        OptionCanKillSelf = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanKillSelf, false, false);
    }
    public override void Add()
    {
        CursedPlayer = null;
        IsCursed = false;
        Shapeshifting = false;
    }
    public override void OnGameStart() => UsePetCooldown = (int)Options.DefaultKillCooldown;
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SyncWarlock);
        sender.Writer.Write(IsCursed);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SyncWarlock) return;
        IsCursed = reader.ReadBoolean();
    }
    public bool OverrideKillButtonText(out string text)
    {
        if (Options.UsePets.GetBool())
        {
            text = GetString("WarlockCurseButtonText");
            return true;
        }
        text = GetString("WarlockCurseButtonText");
        return !Shapeshifting;
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        if (Options.UsePets.GetBool())
        {
            buttonName = "Curse";
            return true;
        }
        buttonName = "Curse";
        return !Shapeshifting;
    }
    public override bool GetGameStartSound(out string sound)
    {
        sound = "Line";
        return true;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("WarlockShapeshiftButtonText");
        return !Shapeshifting && IsCursed;
    }
    public override bool GetAbilityButtonSprite(out string buttonName)
    {
        buttonName = "CurseKill";
        return !Shapeshifting && IsCursed;
    }
    public override bool GetPetButtonText(out string text)
    {
        text = GetString("WarlockShapeshiftButtonText");
        return IsCursed && !(UsePetCooldown != 0);
    }
    public override bool GetPetButtonSprite(out string buttonName)
    {
        buttonName = "CurseKill";
        return IsCursed && !(UsePetCooldown != 0);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = IsCursed ? 1f : Options.DefaultKillCooldown;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        //自殺なら関係ない
        if (info.IsSuicide) return true;

        var (killer, target) = info.AttemptTuple;
        if (!Shapeshifting)
        {//変身してない
            if (!IsCursed)
            {//まだ呪っていない
                IsCursed = true;
                SendRPC();
                CursedPlayer = target;  
                UsePetCooldown = 1;
                //呪える相手は一人だけなのでキルボタン無効化
                killer.SetKillCooldownV2(255f);
                killer.RpcResetAbilityCooldown();
                killer.RPCPlayCustomSound("Line");
              
            }
            //どちらにしてもキルは無効
            return false;
        }
        //変身中は通常キル
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
    public override void OnUsePet()
    {
        if (!Options.UsePets.GetBool()) return;
        if (UsePetCooldown != 0)
        {
            Player.Notify(string.Format(GetString("ShowUsePetCooldown"), UsePetCooldown, 1f));
            return;
        }
        if (CursedPlayer != null && CursedPlayer.IsAlive())
        {//呪っていて対象がまだ生きていたら
            Vector2 cpPos = CursedPlayer.transform.position;
            Dictionary<PlayerControl, float> candidateList = new();
            float distance;
            foreach (PlayerControl candidatePC in Main.AllAlivePlayerControls)
            {
                if (candidatePC.PlayerId == CursedPlayer.PlayerId) continue;
                if (Is(candidatePC) && !OptionCanKillSelf.GetBool()) continue;
                if ((candidatePC.Is(CustomRoleTypes.Impostor) || candidatePC.Is(CustomRoles.Madmate)) && !OptionCanKillAllies.GetBool()) continue;
                distance = Vector2.Distance(cpPos, candidatePC.transform.position);
                candidateList.Add(candidatePC, distance);
                Logger.Info($"{candidatePC?.Data?.PlayerName}の位置{distance}", "Warlock.OnShapeshift");
            }
            if (candidateList.Count >= 1)
            {
                var nearest = candidateList.OrderBy(c => c.Value).FirstOrDefault();
                var killTarget = nearest.Key;

                var killed = false;
                CustomRoleManager.OnCheckMurder(
                    Player, killTarget,
                    CursedPlayer, killTarget,
                    () => killed = true
                    );

                if (killed)
                {
                    killTarget.SetRealKiller(Player);
                    RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                }
                else
                {
                    Player.Notify(GetString("WarlcokKillFaild"));
                }

                Logger.Info($"{killTarget.GetNameWithRole()} 被操控击杀", "Warlock.OnShapeshift");

            }
            else
            {
                Player.Notify(GetString("WarlockNoTarget"));
            }
            Player.SetKillCooldownV2();
            CursedPlayer = null;
            SendRPC();
            Player.SyncSettings();
            Player.RpcResetAbilityCooldown();
        }
        IsCursed = false;
    }
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (Shapeshifting)
        {///変身時
            if (CursedPlayer != null && CursedPlayer.IsAlive())
            {//呪っていて対象がまだ生きていたら
                Vector2 cpPos = CursedPlayer.transform.position;
                Dictionary<PlayerControl, float> candidateList = new();
                float distance;
                foreach (PlayerControl candidatePC in Main.AllAlivePlayerControls)
                {
                    if (candidatePC.PlayerId == CursedPlayer.PlayerId) continue;
                    if (Is(candidatePC) && !OptionCanKillSelf.GetBool()) continue;
                    if ((candidatePC.Is(CustomRoleTypes.Impostor) || candidatePC.Is(CustomRoles.Madmate)) && !OptionCanKillAllies.GetBool()) continue;
                    distance = Vector2.Distance(cpPos, candidatePC.transform.position);
                    candidateList.Add(candidatePC, distance);
                    Logger.Info($"{candidatePC?.Data?.PlayerName}の位置{distance}", "Warlock.OnShapeshift");
                }
                if (candidateList.Count >= 1)
                {
                    var nearest = candidateList.OrderBy(c => c.Value).FirstOrDefault();
                    var killTarget = nearest.Key;

                    var killed = false;
                    CustomRoleManager.OnCheckMurder(
                        Player, killTarget,
                        CursedPlayer, killTarget,
                        () => killed = true
                        );

                    if (killed)
                    {
                        killTarget.SetRealKiller(Player);
                        RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                    }
                    else
                    {
                        Player.Notify(GetString("WarlcokKillFaild"));
                    }

                    Logger.Info($"{killTarget.GetNameWithRole()} 被操控击杀", "Warlock.OnShapeshift");

                }
                else
                {
                    Player.Notify(GetString("WarlockNoTarget"));
                }
                Player.SetKillCooldownV2();
                CursedPlayer = null;
            }
        }
        else
        {
            if (IsCursed)
            {
                //ShapeshifterCooldownを通常に戻す
                IsCursed = false;
                SendRPC();
                Player.SyncSettings();
                Player.RpcResetAbilityCooldown();
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        CursedPlayer = null;
        UsePetCooldown = (int)Options.DefaultKillCooldown;
    }
}