using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using TONEX.Attributes;
using TONEX.Modules;
using TONEX.Roles.AddOns.Common;
using TONEX.Roles.AddOns.Crewmate;
using TONEX.Roles.Core.Interfaces;
using TONEX.Roles.Core.Interfaces.GroupAndRole;
using static TONEX.Translator;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TONEX.Roles.Core;

public static class CustomRoleManager
{
    public static Type[] AllRolesClassType;
    public static Dictionary<CustomRoles, SimpleRoleInfo> AllRolesInfo = new(CustomRolesHelper.AllStandardRoles.Length);
    public static Dictionary<byte, RoleBase> AllActiveRoles = new(15);
    public static Dictionary<byte, List<AddonBase>> AllActiveAddons = new(15);

    public static SimpleRoleInfo GetRoleInfo(this CustomRoles role) => AllRolesInfo.ContainsKey(role) ? AllRolesInfo[role] : null;
    public static RoleBase GetRoleClass(this PlayerControl player) => GetRoleBaseByPlayerId(player.PlayerId);
    public static RoleBase GetRoleBaseByPlayerId(byte playerId) => AllActiveRoles.TryGetValue(playerId, out var roleBase) ? roleBase : null;
    public static List<AddonBase> GetAddonClasses(this PlayerControl player) => GetAddonBaseByPlayerId(player.PlayerId);
    public static List<AddonBase> GetAddonBaseByPlayerId(byte playerId) => AllActiveAddons.TryGetValue(playerId, out var roleBase) ? roleBase : null;

    public static void Do_Addons(this PlayerControl player, Action<AddonBase> act)
    {
        player.GetAddonClasses()?.Do_Addons(act);
    }
    public static void Do_Addons(this List<AddonBase> @base, Action<AddonBase> act)
    {
        @base?.Where(x => x != null)?.Do(act);
    }
    public static bool Any_Addons(this PlayerControl player, Func<AddonBase, bool> act)
    {
        return player.GetAddonClasses()?.Any(act) ?? false;
    }
    public static bool Any_Addons(this List<AddonBase> @base, Func<AddonBase, bool> act)
    {
        return @base?.Any(act) ?? false;
    }
    public static bool Contains_Addons<T>(this PlayerControl player, out T addon) where T : class
    {
        addon = null;
        var addons = player.GetAddonClasses();
        if (addons != null)
        {
            // 查询并获取第一个符合类型 T 的插件对象
            addon = addons.FirstOrDefault(x => x is T) as T;
            return addons is T; // 返回是否找到符合条件的插件对象
        }
        else
        {
            return false;
        }
    }
    public static bool Contains_Addons<T>(this List<AddonBase> @base, out T addon) where T : class
    {
        addon = null;
        var addons = @base;
        if (addons != null)
        {
            // 查询并获取第一个符合类型 T 的插件对象
            addon = addons.FirstOrDefault(x => x is T) as T;
            return addons is T; // 返回是否找到符合条件的插件对象
        }
        else
        {
            return false;
        }
    }
    public static T As_Addons<T>(this PlayerControl player) where T : class
    {
        T addon = null;
        var addons = player.GetAddonClasses();

        addon = addons?.FirstOrDefault(x => x is T) as T;
        return addon; 
    }
    public static T As_Addons<T>(this List<AddonBase> @base) where T : class
    {
        T addon = null;
        var addons = @base;

        addon = addons?.FirstOrDefault(x => x is T) as T;
        return addon;
    }





    public static void Do<T>(this List<T> list, Action<T> action) => list.ToArray().Do(action);
    // == CheckMurder 相关处理 ==
    public static Dictionary<byte, MurderInfo> CheckMurderInfos = new();
    /// <summary>
    ///
    /// </summary>
    /// <param name="attemptKiller">实际击杀者，不变</param>
    /// <param name="attemptTarget">>实际被击杀的玩家，不变</param>
    public static bool OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget)
        => OnCheckMurder(attemptKiller, attemptTarget, attemptKiller, attemptTarget);
    /// <summary>
    ///
    /// </summary>
    /// <param name="attemptKiller">实际击杀者，不变</param>
    /// <param name="attemptTarget">>实际被击杀的玩家，不变</param>
    /// <param name="appearanceKiller">视觉上的击杀者，可变</param>
    /// <param name="appearanceTarget">视觉上被击杀的玩家，可变</param>
    public static bool OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearanceTarget, Action actionAfterAll = null)
    {
        Logger.Info($"Attempt：{attemptKiller.GetNameWithRole()} => {attemptTarget.GetNameWithRole()}", "CheckMurder");
        if (appearanceKiller != attemptKiller || appearanceTarget != attemptTarget)
            Logger.Info($"Apperance：{appearanceKiller.GetNameWithRole()} => {appearanceTarget.GetNameWithRole()}", "CheckMurder");



        var info = new MurderInfo(attemptKiller, attemptTarget, appearanceKiller, appearanceTarget);

        appearanceKiller.ResetKillCooldown();

       
        // 無効なキルをブロックする処理 必ず最初に実行する
        if (!CheckMurderPatch.CheckForInvalidMurdering(info))
        {
            return false;
        }

        var killerRole = attemptKiller.GetRoleClass();
        var targetRole = attemptTarget.GetRoleClass();
        if (attemptKiller.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Kill)) return false;
        // 首先凶手确实是击杀类型的职业
        if (killerRole is IKiller killer)
        {

            if (attemptKiller.IsDisabledAction(ExtendedPlayerControl.PlayerActionType.Kill, ExtendedPlayerControl.PlayerActionInUse.Skill)&& !killer.IsKiller) return false;
            attemptKiller.DisableAction(attemptTarget);
            attemptTarget.DisableAction(attemptKiller);
            // 其他职业类对击杀事件的事先检查
            if (!killer.IsKiller)
            {
                Nihility.OnCheckMurderPlayerOthers_Nihility(info);
            }
            if (killer.IsKiller)
            {
                if (targetRole != null)
                {
                    // 被害者检查击杀
                    if (!targetRole.OnCheckMurderAsTargetBefore(info))
                    {
                        Logger.Info($"被害者阻塞了击杀", "CheckMurder");
                        return false;
                    }
                }
                foreach (var onCheckMurderPlayer in OnCheckMurderPlayerOthers_Before)
                {
                    if (!onCheckMurderPlayer(info))
                    {
                        Logger.Info($"OtherBefore：{onCheckMurderPlayer.Method.Name} 阻塞了击杀", "CheckMurder");
                        return false;
                    }
                }
            }
            // 凶杀检查击杀
            if (!killer.OnCheckMurderAsKiller(info))
            {
                Logger.Info($"凶手阻塞了击杀", "CheckMurder");
                return false;
            }
            if (killer.IsKiller)
            {
                if (targetRole != null)
                {
                    // 被害者检查击杀
                    if (!targetRole.OnCheckMurderAsTargetAfter(info))
                    {
                        Logger.Info($"被害者阻塞了击杀", "CheckMurder");
                        return false;
                    }
                }
                // 其他职业类对击杀事件的事后检查
                foreach (var onCheckMurderPlayer in OnCheckMurderPlayerOthers_After)
                {
                    if (!onCheckMurderPlayer(info))
                    {
                        Logger.Info($"OtherAfter：{onCheckMurderPlayer.Method.Name} 阻塞了击杀", "CheckMurder");
                        return false;
                    }
                }
                if (Madmate.FirstKill(attemptKiller, attemptTarget))
                {
                    return false;
                }
            }

        }

        //キル可能だった場合のみMurderPlayerに進む
        if (info.CanKill && info.DoKill)
        {
            // 调用职业类对击杀发生前进行预处理如设置冷却等操作
            if (killerRole is IKiller killer2) killer2?.BeforeMurderPlayerAsKiller(info);
            targetRole?.BeforeMurderPlayerAsTarget(info);

            //MurderPlayer用にinfoを保存
            CheckMurderInfos[appearanceKiller.PlayerId] = info;
            appearanceKiller.RpcMurderPlayer(appearanceTarget);
            actionAfterAll?.Invoke();
            return true;
        }
        else
        {
            if (!info.CanKill) Logger.Info($"{appearanceTarget.GetNameWithRole()} 无法被击杀", "CheckMurder");
            if (!info.DoKill) Logger.Info($"{appearanceKiller.GetNameWithRole()} 无法击杀", "CheckMurder");
            return false;
        }
    }
    /// <summary>
    /// MurderPlayer 事件的处理
    /// </summary>
    /// <param name="appearanceKiller">视觉上的击杀者，可变</param>
    /// <param name="appearanceTarget">视觉上被击杀的玩家，可变</param>
    public static void OnMurderPlayer(PlayerControl appearanceKiller, PlayerControl appearanceTarget)
    {
        //MurderInfoの取得
        if (CheckMurderInfos.TryGetValue(appearanceKiller.PlayerId, out var info))
        {
            //参照出来たら削除
            CheckMurderInfos.Remove(appearanceKiller.PlayerId);
        }
        else
        {
            //CheckMurderを経由していない場合はappearanceで処理
            info = new MurderInfo(appearanceKiller, appearanceTarget, appearanceKiller, appearanceTarget);
        }

        (var attemptKiller, var attemptTarget) = info.AttemptTuple;

        Logger.Info($"Real Killer={attemptKiller.GetNameWithRole()}", "MurderPlayer");

        //キラーの処理
        (attemptKiller.GetRoleClass() as IKiller)?.OnMurderPlayerAsKiller(info);
        

        //ターゲットの処理
        var targetRole = attemptTarget.GetRoleClass();
        targetRole?.OnMurderPlayerAsTarget(info);
        attemptTarget.GetAddonClasses().Do(x => x.OnMurderPlayerAsTarget(info));
        
        //その他視点の処理があれば実行
        foreach (var onMurderPlayer in OnMurderPlayerOthers.ToArray())
        {
            onMurderPlayer(info);
        }

        //サブロール処理ができるまではラバーズをここで処理

        //以降共通処理
        var targetState = PlayerState.GetByPlayerId(attemptTarget.PlayerId);
        if (targetState.DeathReason == CustomDeathReason.etc)
        {
            //死因が設定されていない場合は死亡判定
            targetState.DeathReason = CustomDeathReason.Kill;
        }

        targetState.SetDead();
        attemptTarget.SetRealKiller(attemptKiller, true);

        Utils.CountAlivePlayers(true);

        Utils.TargetDies(info);

        Utils.SyncAllSettings();
        Utils.NotifyRoles();
    }
    /// <summary>
    /// 其他玩家视角下的 MurderPlayer 事件
    /// 初始化时使用 OnMurderPlayerOthers+= 注册
    /// </summary>
    public static HashSet<Action<MurderInfo>> OnMurderPlayerOthers = new();
    /// <summary>
    /// 其他玩家视角下的 CheckMurderPlayer 事件
    /// 在击杀事件当时玩家的 CheckMurderPlayer 函数调用前检查
    /// 初始化时使用 OnCheckMurderPlayerOthers_Before+= 注册
    /// 返回 false 以阻止本次击杀事件
    /// </summary>
    public static HashSet<Func<MurderInfo, bool>> OnCheckMurderPlayerOthers_Before = new();
    /// <summary>
    /// 其他玩家视角下的 CheckMurderPlayer 事件
    /// 在击杀事件当时玩家的 CheckMurderPlayer 函数调用后检查
    /// 初始化时使用 OnCheckMurderPlayerOthers_After+= 注册
    /// 返回 false 以阻止本次击杀事件
    /// </summary>
    public static HashSet<Func<MurderInfo, bool>> OnCheckMurderPlayerOthers_After = new();
    private static Dictionary<byte, long> LastSecondsUpdate = new();
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (GameStates.IsInTask)
        {
            var now = Utils.GetTimeStamp();
            LastSecondsUpdate.TryAdd(player.PlayerId, 0);
            if (LastSecondsUpdate[player.PlayerId] != now)
            {
                player.GetRoleClass()?.OnSecondsUpdate(player, now);
                player.Do_Addons(x => x.OnSecondsUpdate(player, now));
                LastSecondsUpdate[player.PlayerId] = now;
            }
            var roleclass = player.GetRoleClass();
            if (player.IsAlive() && roleclass?.CountdownList != null && roleclass?.CooldownList != null)
            {
                for (int i= 0; i< roleclass.CountdownList.Count; i++)
                {
                    if (roleclass.CheckForOffGuard(i))
                    {
                        roleclass.ZeroingCountdown(i);
                        if (roleclass.SetOffGuardProtect(out string notify, out int format_int, out float format_float))
                        player.RpcProtectedMurderPlayer();
                        if (format_int != -255)
                            player.Notify(string.Format(notify, format_int));
                        else if (format_float != -255)
                            player.Notify(string.Format(notify, format_float));
                        else
                            player.Notify(notify);
                        roleclass.AfterOffGuard();
                    }
                }
                roleclass.CD_Update();
            }
            if (player.IsAlive() && Options.UsePets.GetBool() && roleclass?.UsePetCooldown_Timer != null && roleclass?.UsePetCooldown != null)
            {
                if (roleclass.UsePetCooldown_Timer + roleclass.UsePetCooldown < now && roleclass.UsePetCooldown_Timer != -1)
                {
                    roleclass.UsePetCooldown_Timer = -1;
                    player.RpcProtectedMurderPlayer();
                    player.Notify(string.Format(GetString("PetSkillCanUse")));
                }
            }
            

            player.GetRoleClass()?.OnFixedUpdate(player);
            
            //その他視点処理があれば実行
            foreach (var onFixedUpdate in OnFixedUpdateOthers)
            {
                onFixedUpdate(player);
            }
        }
    }
    /// <summary>
    /// 其他玩家视角下的帧 Task 处理事件
    /// 用于干涉其他职业
    /// 请注意：全部模组端都会调用
    /// 初始化时使用 OnFixedUpdateOthers+= 注册
    /// </summary>
    public static HashSet<Action<PlayerControl>> OnFixedUpdateOthers = new();

    public static bool OnSabotage(PlayerControl player, SystemTypes systemType)
    {
        bool cancel = false;
        foreach (var roleClass in AllActiveRoles.Values)
        {
            if (!roleClass.OnSabotage(player, systemType))
            {
                cancel = true;
            }
        }
        return !cancel;
    }
    // ==初始化处理 ==
    [GameModuleInitializer]
    public static void Initialize()
    {
        AllRolesInfo.Do(kvp => kvp.Value.IsEnable = kvp.Key.IsEnable());
        AllActiveRoles.Clear();
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
        ReceiveMessage.Clear();
        CheckMurderInfos.Clear();
        OnMurderPlayerOthers.Clear();
        OnCheckMurderPlayerOthers_Before.Clear();
        OnCheckMurderPlayerOthers_After.Clear();
        OnFixedUpdateOthers.Clear();
    }
    public static void CreateInstance()
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            CreateInstance(pc.GetCustomRole(), pc);

            foreach (var subRole in PlayerState.GetByPlayerId(pc.PlayerId).SubRoles)
                CreateInstance(subRole, pc);
        }
    }
    public static void CreateInstance(CustomRoles role, PlayerControl player)
    {
        if (AllRolesInfo.TryGetValue(role, out var roleInfo))
        {
            if (!role.IsAddon())
                roleInfo.CreateRoleInstance(player).Add();
            else
                roleInfo.CreateAddonInstance(player).Add();
        }
        else
        {
            
            //OtherRolesAdd(player);
        }
        if (player.Data.Role.Role == RoleTypes.Shapeshifter)
        {
            Main.CheckShapeshift.TryAdd(player.PlayerId, false);
        }
    }

    /// <summary>
    /// 从收到的RPC中取得目标并传给职业类
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="rpcType"></param>
    public static void DispatchRpc(MessageReader reader)
    {
        var playerId = reader.ReadByte();
        GetRoleBaseByPlayerId(playerId)?.ReceiveRPC(reader);
    }
    public static void DispatchRpc(MessageReader reader, CustomRPC rpcTypes)
    {
        var playerId = reader.ReadByte();
        GetAddonBaseByPlayerId(playerId)?.Do(x => x?.ReceiveRPC(reader, rpcTypes));
    }
    //NameSystem
    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> MarkOthers = new();
    public static HashSet<Func<PlayerControl, PlayerControl, bool, bool, string>> LowerOthers = new();
    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> SuffixOthers = new();
    /// <summary>
    /// 无论 seer,seen 是否持有职业职业都会触发的 Mark 获取事件
    /// 会默认为全体职业注册
    /// </summary>
    /// <param name="seer">看到的人</param>
    /// <param name="seen">被看到的人</param>
    /// <param name="isForMeeting">是否正在会议中</param>
    /// <returns>组合后的全部 Mark</returns>
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        var sb = new StringBuilder(100);
        foreach (var marker in MarkOthers)
        {
            sb.Append(marker(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }
    /// <summary>
    /// 无论 seer,seen 是否持有职业职业都会触发的 LowerText 获取事件
    /// 会默认为全体职业注册
    /// </summary>
    /// <param name="seer">看到的人</param>
    /// <param name="seen">被看到的人</param>
    /// <param name="isForMeeting">是否正在会议中</param>
    /// <param name="isForHud">是否显示在模组端的HUD</param>
    /// <returns>组合后的全部 LowerText</returns>
    public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        var sb = new StringBuilder(100);
        foreach (var lower in LowerOthers)
        {
            sb.Append(lower(seer, seen, isForMeeting, isForHud));
        }
        return sb.ToString();
    }
    /// <summary>
    /// 无论 seer,seen 是否持有职业职业都会触发的 Suffix 获取事件
    /// 会默认为全体职业注册
    /// </summary>
    /// <param name="seer">看到的人</param>
    /// <param name="seen">被看到的人</param>
    /// <param name="isForMeeting">是否正在会议中</param>
    /// <returns>组合后的全部 Suffix</returns>
    public static string GetSuffixOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        var sb = new StringBuilder(100);
        foreach (var suffix in SuffixOthers)
        {
            sb.Append(suffix(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }
    //ChatMessages
    public static HashSet<Action<MessageControl>> ReceiveMessage = new();
    /// <summary>
    /// 玩家收到消息后调用的函数
    /// 无论您是否发送者都会调用，因此您可能需要判断该消息是否是您自己发送的
    /// </summary>
    /// <param name="msgControl">收到的消息</param>
    /// <param name="recallMode">该消息应该做何处理</param>
    /// <returns>true: 阻塞该消息并停止向下判断</returns>
    public static bool OnReceiveMessage(MessageControl msgControl, out MsgRecallMode recallMode)
    {
        recallMode = MsgRecallMode.None;
        return false;
    }

    /// <summary>
    /// 全部对象的销毁事件
    /// </summary>
    public static void Dispose()
    {
        Logger.Info($"Dispose ActiveRoles", "CustomRoleManager");
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
        ReceiveMessage.Clear();
        CheckMurderInfos.Clear();
        OnMurderPlayerOthers.Clear();
        OnFixedUpdateOthers.Clear();

        AllActiveRoles.Values.ToArray().Do(roleClass => roleClass.Dispose());
        AllActiveAddons.Values.ToArray().Do(x => x.Do(roleClass => roleClass.Dispose()));
    }
}
public class MurderInfo
{
    /// <summary>实际击杀者，不变</summary>
    public PlayerControl AttemptKiller { get; }
    /// <summary>实际被击杀的玩家，不变</summary>
    public PlayerControl AttemptTarget { get; }
    /// <summary>视觉上的击杀者，可变</summary>
    public PlayerControl AppearanceKiller { get; set; }
    /// <summary>视觉上被击杀的玩家，可变</summary>
    public PlayerControl AppearanceTarget { get; set; }

    /// <summary>
    /// 目标是否可以被击杀，由于目标导致的无法击杀将该值赋值为 false
    /// </summary>
    public bool CanKill = true;
    /// <summary>
    /// 击杀者是否真的会进行击杀，由于击杀者导致的无法击杀将该值赋值为 false
    /// </summary>
    public bool DoKill = true;
    /// <summary>
    /// 是否发生从梯子上摔死等意外
    /// </summary>
    public bool IsAccident = false;

    // 使用 (killer, target) = info.AttemptTuple; 即可获得数据
    public (PlayerControl killer, PlayerControl target) AttemptTuple => (AttemptKiller, AttemptTarget);
    public (PlayerControl killer, PlayerControl target) AppearanceTuple => (AppearanceKiller, AppearanceTarget);
    /// <summary>
    /// 真的是自杀
    /// </summary>
    public bool IsSuicide => AttemptKiller.PlayerId == AttemptTarget.PlayerId;
    /// <summary>
    /// 原版视角下的自杀
    /// </summary>
    public bool IsFakeSuicide => AppearanceKiller.PlayerId == AppearanceTarget.PlayerId;
    public MurderInfo(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearancetarget)
    {
        AttemptKiller = attemptKiller;
        AttemptTarget = attemptTarget;
        AppearanceKiller = appearanceKiller;
        AppearanceTarget = appearancetarget;
    }
}

public enum CustomRoles
{
    //Default
    Crewmate = 0,
    //Impostor(Vanilla)
    ImpostorGhost =100,
    Impostor,
    Shapeshifter,
    Phantom,
    //Impostor
    BountyHunter,
    Fireworker,
    Mafia,
    SerialKiller,
    ShapeMaster,
    EvilGuesser,
    KillingMachine,
    Zombie,
    Sniper,
    Vampire,
    Witch,
    Warlock,
    Ninja,
    Hacker,
    Miner,
    Escapist,
    Mare,
    ControlFreak,
    TimeThief,
    EvilTracker,
    AntiAdminer,
    Arrogance,
    Bomber,
    BoobyTrap,
    Scavenger,
    Capitalist,
    Gangster,
    Cleaner,
    BallLightning,
    Greedy,
    CursedWolf,
    SoulCatcher,
    QuickShooter,
    Concealer,
    Eraser,
    Butcher,
    Hangman,
    Bard,
    EvilInvisibler,
    CrewPostor,
    Penguin,
    Stealth,
    Messenger,
    Insider,
    Onmyoji,
    Gamblers,
    DoubleKiller,
    Medusa,
    Skinwalker,
    ViciousSeeker,
    EvilAngel,
    EvilTimePauser,
    MirrorSpirit,//TODO 镜妖
    Assaulter,
    MimicTeam,//TODO 模仿者团队
    MimicKiller,//TODO 模仿者（杀手）
    MimicAssistant,//TODO 模仿者（助手）
    Blackmailer,
    EvilSwapper,
    Disperser,//TODO 分散者
    EvilPianist,//TODO 邪恶的钢琴家
    EvilGrenadier,
    Fisherman,
    Disguiser,
    SuperPower,
    Sorcerer,
    EvilTrapper,
    Proxy,
    AbyssBringer,

    //Crewmate(Vanilla)
    CrewmateGhost = 400,
    Engineer,
    Scientist,
    GuardianAngel,
    Noisemaker,
    Tracker,
    //Crewmate
    Luckey,
    LazyGuy,
    SuperStar,
    Celebrity,
    Mayor,
    Paranoia,
    Psychic,
    Repairman,
    Sheriff,
    Snitch,
    SpeedBooster,
    Dictator,
    MedicalExaminer,
    Vigilante,
    NiceGuesser,
    Transporter,
    TimeManager,
    Veteran,
    Bodyguard,
    Deceiver,
    NiceGrenadier,
    Medic,
    FortuneTeller,
    Glitch,
    Judge,
    Mortician,
    Medium,
    Observer,
    DoveOfPeace,
    NiceTimePauser,
    TimeMaster,
    Prophet,
    Instigator,
    Adventurer,
    Unyielding,
    Perfumer, //TODO 香水师
    Captain,// TODO 舰长
    VirtueGuider, //TODO 善导者，TONEXEX的舰长
    NiceTracker,//TODO 正义的追踪者
    NiceInvisibler,//TODO 影行者（正义隐身）
    NiceSwapper,
    Hunter,
    SpecterSlayer,
    Alien, //TODO 外星人
    Spy,//TODO 卧底
    NicePianist,//TODO 正义的钢琴家
    Sloth,//TODO 树懒
    Bees,//TODO 蜜蜂
    CopyCat,//TODO 效颦者
    Deputy,
    InjusticeSpirit,
    Scout,
    Amber,
    Saint,
    Geneticist,
    Awake,
    Crusader,
    Telegrapher,
    SpeedSeeker,
    Introvert,
    Eggy,

    //Neutral
    Arsonist = 800,
    Jester,
    God,
    Opportunist,
    Mario,
    Terrorist,
    Executioner,
    Jackal,
    Innocent,
    Pelican,
    Revolutionist, 
    Hater,
    Konan, //TODO 柯南
    Demon,
    Stalker, 
    Workaholic,
    Collector,
    Provocateur,
    Sunnyboy, 
    BloodKnight,
    Follower,
    Succubus,
    PlagueDoctor,
    SchrodingerCat,
    Vulture,
    Whoops,
    Sidekick,
    Despair,
    RewardOfficer,
    Vagator,
    Non_Villain,//不演反派 1.0限定
    Lawyer,
    Prosecutors,
    PVPboss,//TODO PvP大佬
    Rebels,
    Admirer,
    Akujo, 
    Cupid,
    Yandere,
    Puppeteer,
    Changger,//TODO 连环交换师
    Amnesiac,//TODO 失忆者
    Plaguebearer,
    GodOfPlagues,

    PoliticalStrategists,//TODO 纵横家

    Challenger,//TODO 挑战者
    Martyr,//先烈 1.1限定
    NightWolf,
    Moonshadow,//TODO 月影,1.4限定
    Specterraid,
    MeteorArbiter,// 陨星判官,1.2限定
    MeteorMurderer,// 陨星戮者,1.2限定
    SharpShooter,
    Alternate,
    SplitPersonality,
    Ranger,
    King,
    //GameMode
    HotPotato,
    ColdPotato,
    Survivor,
    Infector,
    Killer,

    //GM
    GM,

    //Sub-role after 1500
    NotAssigned = 1500,
    LastImpostor,
    Lovers,
    AdmirerLovers,
    AkujoLovers,
    AkujoFakeLovers,
    CupidLovers,
    Neptune,
    Madmate,
    Watcher,
    Flashman,
    Lighter,
    Seer,
    Tiebreaker,
    Oblivious,
    Bewilder,
    Workhorse,
    Fool,
    Avenger,
    YouTuber,
    Egoist,
    TicketsStealer,
    Schizophrenic,
    Mimic,
    Reach,
    Charmed,
    Bait,
    Beartrap,
    Wolfmate,
    Rambler,
    Chameleon,
    Mini,
    Libertarian,
    Signal,
    Spiders,
    Professional,//TODO 专业赌怪
    Luckless,//TODO 倒霉蛋
    FateFavor,//TODO 命运眷顾者
    Nihility,
    Diseased,
    IncorruptibleOfficial,//TODO 清廉之官
    VIP,//TODO VIP
    Believer,
    PublicOpinionShaper,
    Guesser,

}
public enum CustomRoleTypes
{
    Crewmate,
    Impostor,
    Neutral,
    Addon
}
public enum HasTask
{
    True,
    False,
    ForRecompute
}