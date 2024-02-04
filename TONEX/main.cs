using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TONEX.Attributes;
using TONEX.Roles.Core;
using UnityEngine;

[assembly: AssemblyFileVersion(TONEX.Main.PluginVersion)]
[assembly: AssemblyInformationalVersion(TONEX.Main.PluginVersion)]
[assembly: AssemblyVersion(TONEX.Main.PluginVersion)]
namespace TONEX;

[BepInPlugin(PluginGuid, "TONEX", PluginVersion)]
[BepInIncompatibility("jp.ykundesu.supernewroles")]
[BepInProcess("Among Us.exe")]
public class Main : BasePlugin
{
    // == 程序基本设定 / Program Config ==
    public static readonly string ModName = "TONEX";
    public static readonly string ModColor = "#cdfffd";
    public static readonly Color32 ModColor32 = new(205, 255, 253, 255);
    public static readonly bool AllowPublicRoom = true;
    public static readonly string ForkId = "TONEX";
    public const string OriginalForkId = "OriginalTOH";
    public const string PluginGuid = "cn.tonex.xtremewave";
    // == 认证设定 / Authentication Config ==
    public static HashAuth DebugKeyAuth { get; private set; }
    public const string DebugKeyHash = "c0fd562955ba56af3ae20d7ec9e64c664f0facecef4b3e366e109306adeae29d";
    public const string DebugKeySalt = "59687b";
    public static ConfigEntry<string> DebugKeyInput { get; private set; }
    // == 版本相关设定 / Version Config ==
    public const string LowestSupportedVersion = "2023.10.24";
    public static readonly bool IsPublicAvailableOnThisVersion = false;
    public const string PluginVersion = "0.9.7";
    public const string PluginShowVersion = "1.0_20240204_Preview_3";
    public const int PluginCreation = 1;
    // == 链接相关设定 / Link Config ==
    public static readonly bool ShowWebsiteButton = true;
    public static readonly string WebsiteUrl = Translator.IsChineseLanguageUser ? "https://tonex.cc" : "https://tonex.cc/En";
    public static readonly bool ShowQQButton = true;
    public static readonly string QQInviteUrl = "http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=4ojpzbUU42giZZeQ-DTaal-tC5RIpL46&authKey=49OYQwsCza2x5eHGdXDHXD1M%2FvYvQcEhJBNL5h8Gq7AxOu5eMfTc6g2edtlsMuCm&noverify=0&group_code=733425569";
    public static readonly bool ShowDiscordButton = true;
    public static readonly string DiscordInviteUrl = "https://discord.gg/kz787Zg7h8";
    public static readonly bool ShowGithubUrl = true;
    public static readonly string GithubRepoUrl = "https://github.com/XtremeWave/TownOfNewEpic_Xtreme";
    // ==========

    public Harmony Harmony { get; } = new Harmony(PluginGuid);
    public static Version version = Version.Parse(PluginVersion);
    public static BepInEx.Logging.ManualLogSource Logger;
    public static bool hasArgumentException = false;
    public static string ExceptionMessage;
    public static bool ExceptionMessageIsShown = false;
    public static string CredentialsText;
    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    //Client Options
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<int> MessageWait { get; private set; }
    public static ConfigEntry<bool> ShowResults { get; private set; }
    public static ConfigEntry<bool> UnlockFPS { get; private set; }
    //public static ConfigEntry<bool> CanPublic { get; private set; }
    public static ConfigEntry<bool> HorseMode { get; private set; }
    public static ConfigEntry<bool> AutoStartGame { get; private set; }
    public static ConfigEntry<bool> AutoEndGame { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguage { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguageRoleName { get; private set; }
    public static ConfigEntry<bool> EnableCustomButton { get; private set; }
    public static ConfigEntry<bool> EnableCustomSoundEffect { get; private set; }
    public static ConfigEntry<bool> VersionCheat { get; private set; }
    public static ConfigEntry<bool> GodMode { get; private set; }


    public static Dictionary<byte, PlayerVersion> playerVersion = new();
    //Preset Name Options
    public static ConfigEntry<string> Preset1 { get; private set; }
    public static ConfigEntry<string> Preset2 { get; private set; }
    public static ConfigEntry<string> Preset3 { get; private set; }
    public static ConfigEntry<string> Preset4 { get; private set; }
    public static ConfigEntry<string> Preset5 { get; private set; }
    //Other Configs
    public static ConfigEntry<string> WebhookURL { get; private set; }
    public static ConfigEntry<float> LastKillCooldown { get; private set; }
    public static ConfigEntry<float> LastShapeshifterCooldown { get; private set; }
    public static OptionBackupData RealOptionsData;
    public static Dictionary<byte, string> AllPlayerNames = new();
    public static Dictionary<(byte, byte), string> LastNotifyNames;
    public static Dictionary<byte, Color32> PlayerColors = new();
    public static Dictionary<byte, CustomDeathReason> AfterMeetingDeathPlayers = new();
    public static Dictionary<CustomRoles, string> roleColors;
    public static List<byte> winnerList = new();
    public static List<string> winnerNameList = new();
    public static List<int> clientIdList = new();
    public static List<(string, byte, string)> MessagesToSend = new();
    public static List<PlayerControl> LoversPlayers = new();
    public static bool isLoversDead = true;
    public static Dictionary<byte, float> AllPlayerKillCooldown = new();
    public static List<(string, PlayerControl)> SetRolesList = new List<(string, PlayerControl)>();
    public static List<byte> CantUseSkillList = new();
    public static List<byte> CantDoActList = new();
    /// <summary>
    /// 基本的に速度の代入は禁止.スピードは増減で対応してください.
    /// </summary>
    public static Dictionary<byte, float> AllPlayerSpeed = new();
    public const float MinSpeed = 0.0001f;
    public static int AliveImpostorCount;
    public static Dictionary<byte, bool> CheckShapeshift = new();
    public static Dictionary<byte, byte> ShapeshiftTarget = new();
    public static bool VisibleTasksCount = false;
    public static string HostNickName = "";
    public static bool introDestroyed = false;
    public static float DefaultCrewmateVision;
    public static float DefaultImpostorVision;
    public static bool IsInitialRelease = DateTime.Now.Month == 1 && DateTime.Now.Day is 17;
    public static bool IsAprilFools = DateTime.Now.Month == 4 && DateTime.Now.Day is 1;
    public const float RoleTextSize = 2f;

    public static Dictionary<byte, CustomRoles> DevRole = new();

    public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
    public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && !p.Data.Disconnected && !p.IsEaten());

    public static Main Instance;

    //TONX

    public static Dictionary<byte, Vent> LastEnteredVent = new();
    public static Dictionary<byte, Vector2> LastEnteredVentLocation = new();
    public static Dictionary<int, int> SayStartTimes = new();
    public static Dictionary<int, int> SayBanwordsTimes = new();

    public static string OverrideWelcomeMsg = "";
    public static bool DoBlockNameChange = false;
    public static bool NewLobby = false;
    public static byte FirstDied = byte.MaxValue;
    public static byte ShieldPlayer = byte.MaxValue;

    public static List<string> TName_Snacks_CN = new() { "冰激凌", "奶茶", "巧克力", "蛋糕", "甜甜圈", "可乐", "柠檬水", "冰糖葫芦", "果冻", "糖果", "牛奶", "抹茶", "烧仙草", "菠萝包", "布丁", "椰子冻", "曲奇", "红豆土司", "三彩团子", "艾草团子", "泡芙", "可丽饼", "桃酥", "麻薯", "鸡蛋仔", "马卡龙", "雪梅娘", "炒酸奶", "蛋挞", "松饼", "西米露", "奶冻", "奶酥", "可颂", "奶糖" };
    public static List<string> TName_Snacks_EN = new() { "Ice cream", "Milk tea", "Chocolate", "Cake", "Donut", "Coke", "Lemonade", "Candied haws", "Jelly", "Candy", "Milk", "Matcha", "Burning Grass Jelly", "Pineapple Bun", "Pudding", "Coconut Jelly", "Cookies", "Red Bean Toast", "Three Color Dumplings", "Wormwood Dumplings", "Puffs", "Can be Crepe", "Peach Crisp", "Mochi", "Egg Waffle", "Macaron", "Snow Plum Niang", "Fried Yogurt", "Egg Tart", "Muffin", "Sago Dew", "panna cotta", "soufflé", "croissant", "toffee" };
    public static string Get_TName_Snacks => TranslationController.Instance.currentLanguage.languageID is SupportedLangs.SChinese or SupportedLangs.TChinese ?
        TName_Snacks_CN[IRandom.Instance.Next(0, TName_Snacks_CN.Count)] :
        TName_Snacks_EN[IRandom.Instance.Next(0, TName_Snacks_EN.Count)];

    public override void Load()
    {
        Instance = this;

        //Client Options
        HideName = Config.Bind("Client Options", "Hide Game Code Name", "TONEX");
        HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
        DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");
        ShowResults = Config.Bind("Result", "Show Results", true);
        UnlockFPS = Config.Bind("Client Options", "UnlockFPS", false);
        //CanPublic = Config.Bind("Client Options", "CanPublic", true);
        HorseMode = Config.Bind("Client Options", "HorseMode", false);
        AutoStartGame = Config.Bind("Client Options", "AutoStartGame", false);
        AutoEndGame = Config.Bind("Client Options", "AutoEndGame", false);
        ForceOwnLanguage = Config.Bind("Client Options", "ForceOwnLanguage", false);
        ForceOwnLanguageRoleName = Config.Bind("Client Options", "ForceOwnLanguageRoleName", false);
        EnableCustomButton = Config.Bind("Client Options", "EnableCustomButton", true);
        EnableCustomSoundEffect = Config.Bind("Client Options", "EnableCustomSoundEffect", true);
        VersionCheat = Config.Bind("Client Options", "VersionCheat", false);
        GodMode = Config.Bind("Client Options", "GodMode", false);

        Logger = BepInEx.Logging.Logger.CreateLogSource("TONEX");
        TONEX.Logger.Enable();
        TONEX.Logger.Disable("NotifyRoles");
        TONEX.Logger.Disable("SwitchSystem");
        TONEX.Logger.Disable("ModNews");
        TONEX.Logger.Disable("CustomRpcSender");
        if (!DebugModeManager.AmDebugger)
        {
            TONEX.Logger.Disable("CheckRelease");
            TONEX.Logger.Disable("CustomRpcSender");
            //TONEX.Logger.Disable("ReceiveRPC");
            TONEX.Logger.Disable("SendRPC");
            TONEX.Logger.Disable("SetRole");
            TONEX.Logger.Disable("Info.Role");
            TONEX.Logger.Disable("TaskState.Init");
            //TONEX.Logger.Disable("Vote");
            TONEX.Logger.Disable("RpcSetNamePrivate");
            //TONEX.Logger.Disable("SendChat");
            TONEX.Logger.Disable("SetName");
            //TONEX.Logger.Disable("AssignRoles");
            //TONEX.Logger.Disable("RepairSystem");
            //TONEX.Logger.Disable("MurderPlayer");
            //TONEX.Logger.Disable("CheckMurder");
            TONEX.Logger.Disable("PlayerControl.RpcSetRole");
            TONEX.Logger.Disable("SyncCustomSettings");
            TONEX.Logger.Disable("CancelPet");
            TONEX.Logger.Disable("Pet");
            //TONEX.Logger.Disable("SetScanner");
            TONEX.Logger.Disable("test");
            TONEX.Logger.Disable("ForNVBeKilled");
            TONEX.Logger.Disable("ForNVCAAList");
            TONEX.Logger.Disable("ForNVDFList");
            TONEX.Logger.Disable("ForNvFarAheadList");
            TONEX.Logger.Disable("ForNVMoney");
            TONEX.Logger.Disable("Pet");
        }
        //TONEX.Logger.isDetail = true;

        // 認証関連-初期化
        DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

        // 認証関連-認証
        DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

        Preset1 = Config.Bind("Preset Name Options", "Preset1", "Preset_1");
        Preset2 = Config.Bind("Preset Name Options", "Preset2", "Preset_2");
        Preset3 = Config.Bind("Preset Name Options", "Preset3", "Preset_3");
        Preset4 = Config.Bind("Preset Name Options", "Preset4", "Preset_4");
        Preset5 = Config.Bind("Preset Name Options", "Preset5", "Preset_5");
        WebhookURL = Config.Bind("Other", "WebhookURL", "none");
        MessageWait = Config.Bind("Other", "MessageWait", 1);

        LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);
        LastShapeshifterCooldown = Config.Bind("Other", "LastShapeshifterCooldown", (float)30);

        hasArgumentException = false;
        ExceptionMessage = "";
        try
        {
            roleColors = new Dictionary<CustomRoles, string>()
            {
                //GM
                {CustomRoles.GM, "#ff5b70"},

                //Vanilla
                {CustomRoles.Crewmate, "#ffffff"},
                {CustomRoles.Engineer, "#8cffff"},
                {CustomRoles.Scientist, "#8cffff"},
                {CustomRoles.GuardianAngel, "#ffffff"},
                {CustomRoles.Impostor, "#ff1919"},
                {CustomRoles.Shapeshifter, "#ff1919"},

                //Add-Ons
                {CustomRoles.NotAssigned, "#ffffff"},
                {CustomRoles.LastImpostor, "#ff1919"},
                {CustomRoles.Lovers, "#ff9ace"},
                {CustomRoles.Neptune, "#00a4ff"},
                {CustomRoles.Madmate, "#ff1919"},
                {CustomRoles.Watcher, "#800080"},
                {CustomRoles.Flashman, "#ff8400"},
                {CustomRoles.Lighter, "#eee5be"},
                {CustomRoles.Seer, "#61b26c"},
                {CustomRoles.Tiebreaker, "#1447af"},
                {CustomRoles.Oblivious, "#424242"},
                {CustomRoles.Bewilder, "#c894f5"},
                {CustomRoles.Workhorse, "#00ffff"},
                {CustomRoles.Fool, "#e6e7ff"},
                {CustomRoles.Avenger, "#ffab1b"},
                {CustomRoles.YouTuber, "#fb749b"},
                {CustomRoles.Egoist, "#5600ff"},
                {CustomRoles.TicketsStealer, "#ff1919"},
                {CustomRoles.Schizophrenic, "#3a648f"},
                {CustomRoles.Mimic, "#ff1919"},
                {CustomRoles.Reach, "#74ba43"},
                {CustomRoles.Charmed, "#ff00ff"},
                {CustomRoles.Bait, "#00f7ff"},
                {CustomRoles.Beartrap, "#5a8fd0"},
                {CustomRoles.Wolfmate,"#00b4eb" },
                {CustomRoles.Rambler,"#ccffff" },
                {CustomRoles.Chameleon,"#8cffff" },
                {CustomRoles.Mini,"ffffff" },
                {CustomRoles.Libertarian,"#33CC99" },
            };
            var type = typeof(RoleBase);
            var roleClassArray =
            CustomRoleManager.AllRolesClassType = Assembly.GetAssembly(type)
                .GetTypes()
                .Where(x => x.IsSubclassOf(type)).ToArray();

            foreach (var roleClassType in roleClassArray)
                roleClassType.GetField("RoleInfo")?.GetValue(type);
        }
        catch (ArgumentException ex)
        {
            TONEX.Logger.Error("错误：字典出现重复项", "LoadDictionary");
            TONEX.Logger.Exception(ex, "LoadDictionary");
            hasArgumentException = true;
            ExceptionMessage = ex.Message;
            ExceptionMessageIsShown = false;
        }

        RegistryManager.Init(); // 这是优先级最高的模块初始化方法，不能使用模块初始化属性

        PluginModuleInitializerAttribute.InitializeAll();

        IRandom.SetInstance(new NetRandomWrapper());

        TONEX.Logger.Info($"{Application.version}", "AmongUs Version");

        var handler = TONEX.Logger.Handler("GitVersion");
        handler.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}");
        handler.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}");
        handler.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}");
        handler.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}");
        handler.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}");
        handler.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}");

        ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();

        Harmony.PatchAll();

        if (!DebugModeManager.AmDebugger) ConsoleManager.DetachConsole();
        else ConsoleManager.CreateConsole();

        TONEX.Logger.Msg("========= TONEX loaded! =========", "Plugin Load");
    }
}
public enum CustomDeathReason
{
    // AmongUs 
    Kill,
    Vote,

    //cTOH
    Suicide,
    Spell,
    FollowingSuicide,
    Bite,
    Bombed,
    Misfire,
    Torched,
    Sniped,
    Revenge,
    Execution,
    Infected,
    Disconnected,
    Fall,

    // TONX
    Gambled,
    Eaten,
    Sacrifice,
    Quantization,
    Overtired,
    Ashamed,
    PissedOff,
    Dismembered,
    LossOfHead,
    Trialed,
    Merger,

    // TONEX

    etc = -1
}
//WinData
public enum CustomWinner
{
    Draw = -1,
    Default = -2,
    None = -3,
    Error = -4,
    Impostor = CustomRoles.Impostor,
    Crewmate = CustomRoles.Crewmate,
    Jester = CustomRoles.Jester,
    Terrorist = CustomRoles.Terrorist,
    Lovers = CustomRoles.Lovers,
    Executioner = CustomRoles.Executioner,
    Arsonist = CustomRoles.Arsonist,
    Revolutionist = CustomRoles.Revolutionist,
    Jackal = CustomRoles.Jackal,
    God = CustomRoles.God,
    Mario = CustomRoles.Mario,
    Innocent = CustomRoles.Innocent,
    Pelican = CustomRoles.Pelican,
    YouTuber = CustomRoles.YouTuber,
    Egoist = CustomRoles.Egoist,
    Demon = CustomRoles.Demon,
    Stalker = CustomRoles.Stalker,
    Workaholic = CustomRoles.Workaholic,
    Collector = CustomRoles.Collector,
    BloodKnight = CustomRoles.BloodKnight,
    Succubus = CustomRoles.Succubus,
    PlagueDoctor = CustomRoles.PlagueDoctor,
    Vulture = CustomRoles.Vulture,
    Despair = CustomRoles.Despair,
    RewardOfficer = CustomRoles.RewardOfficer,
    ColdPotato = CustomRoles.ColdPotato,
    FAFL = CustomRoles.Vagor_FAFL,
    Congu = CustomRoles.Non_Villain,
    Lawyer = CustomRoles.Lawyer,
}
public enum SuffixModes
{
    None = 0,
    TONEX,
    Streaming,
    Recording,
    RoomHost,
    OriginalName,
    DoNotKillMe,
    NoAndroidPlz
}
public enum VoteMode
{
    Default,
    Suicide,
    SelfVote,
    Skip
}
public enum TieMode
{
    Default,
    All,
    Random
}