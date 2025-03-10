using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TONEX.Attributes;
using TONEX.Roles.Core;
using TONEX.Modules;
using UnityEngine;
using TONEX.OptionUI;

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
    public const string OriginalForkId = "OriginalTONEX";
    public const string PluginGuid = "cn.tonex.xtremewave";
    // == 认证设定 / Authentication Config ==
    public static HashAuth DebugKeyAuth { get; private set; }
    public const string DebugKeyHash = "c0fd562955ba56af3ae20d7ec9e64c664f0facecef4b3e366e109306adeae29d";
    public const string DebugKeySalt = "59687b";
    public static ConfigEntry<string> DebugKeyInput { get; private set; }
    // == 版本相关设定 / Version Config ==
    public const string LowestSupportedVersion = "2024.10.29";
    public static readonly bool IsPublicAvailableOnThisVersion = true;
    public const string PluginVersion = "1.4";
    public const string ShowVersion_Head = "1.4_20250131";
    public const string ShowVersion_TestText = "_Scrapter";
    public const string ShowVersion = ShowVersion_Head + ShowVersion_TestText;
    public const int PluginCreation = 1;
    // == 链接相关设定 / Link Config ==
    public static readonly bool ShowWebsiteButton = true;
    public static readonly string WebsiteUrl = Translator.IsChineseLanguageUser ? "https://www.xtreme.net.cn/project/TONEX/" : "https://www.xtreme.net.cn/en/project/TONEX/";
    public static readonly bool ShowQQButton = true;
    public static readonly string QQInviteUrl = "https://qm.qq.com/q/1HnCuIcFow";
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
    public CreateUIElements UI;
    public static NormalGameOptionsV08 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    //Client Options
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<int> MessageWait { get; private set; }
    public static ConfigEntry<bool> ShowResults { get; private set; }
    public static ConfigEntry<bool> UnlockFPS { get; private set; }
    //public static ConfigEntry<bool> CanPublic { get; private set; }
    public static ConfigEntry<bool> AssistivePluginMode { get; private set; }
    public static ConfigEntry<bool> HorseMode { get; private set; }
    public static ConfigEntry<bool> LongMode { get; private set; }
    public static ConfigEntry<bool> AutoStartGame { get; private set; }
    public static ConfigEntry<bool> AutoEndGame { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguage { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguageRoleName { get; private set; }
    public static ConfigEntry<bool> EnableCustomButton { get; private set; }
    public static ConfigEntry<bool> EnableCustomSoundEffect { get; private set; }
    public static ConfigEntry<bool> EnableMapBackGround { get; private set; }
    public static ConfigEntry<bool> EnableRoleBackGround { get; private set; }
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

    public static Dictionary<byte, float> AllPlayerKillCooldown = new();
    public static Dictionary<byte, float> AllPlayerVision = new();
    public static Dictionary<byte, List<string>> SetRolesList = new();
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
    public static bool IsTONEXEInitialRelease = DateTime.Now.Month == 1 && DateTime.Now.Day is 17;
    public static bool IsTONEXEXInitialRelease = DateTime.Now.Month == 5 && DateTime.Now.Day is 21;
    public static bool IsTONEXInitialRelease = DateTime.Now.Month == 2 && DateTime.Now.Day is 9;
    public static bool IsAprilFools = DateTime.Now.Month == 4 && DateTime.Now.Day is 1;
    public const float RoleTextSize = 2f;

    public static Dictionary<byte, CustomRoles> DevRole = new();

    static bool LoadEnd = false;
    public static IEnumerable<PlayerControl> AllPlayerControls => 
        //(PlayerControl.AllPlayerControls == null || PlayerControl.AllPlayerControls.Count == 0) && LoadEnd
        //? AllPlayerControls : 
        PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
    public static IEnumerable<PlayerControl> AllAlivePlayerControls => 
        //(PlayerControl.AllPlayerControls == null || PlayerControl.AllPlayerControls.Count == 0) && LoadEnd
        //? AllAlivePlayerControls :
        PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && !p.Data.Disconnected && !p.IsEaten());

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

        LoadEnd = false;
        //Client Options
        HideName = Config.Bind("Client Options", "Hide Game Code Name", "TONEX");
        HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
        DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");
        ShowResults = Config.Bind("Result", "Show Results", true);
        UnlockFPS = Config.Bind("Client Options", "UnlockFPS", false);
        //CanPublic = Config.Bind("Client Options", "CanPublic", true);
        AssistivePluginMode = Config.Bind("Client Options", "AssistivePluginMode", false);
        HorseMode = Config.Bind("Client Options", "HorseMode", false);
        LongMode = Config.Bind("Client Options", "LongMode", false);
        AutoStartGame = Config.Bind("Client Options", "AutoStartGame", false);
        AutoEndGame = Config.Bind("Client Options", "AutoEndGame", false);
        ForceOwnLanguage = Config.Bind("Client Options", "ForceOwnLanguage", false);
        ForceOwnLanguageRoleName = Config.Bind("Client Options", "ForceOwnLanguageRoleName", false);
        EnableCustomButton = Config.Bind("Client Options", "EnableCustomButton", true);
        EnableCustomSoundEffect = Config.Bind("Client Options", "EnableCustomSoundEffect", true);
        EnableMapBackGround = Config.Bind("Client Options", "EnableMapBackGround", true);
        EnableRoleBackGround = Config.Bind("Client Options", "EnableRoleBackGround", true);
        VersionCheat = Config.Bind("Client Options", "VersionCheat", false);
        GodMode = Config.Bind("Client Options", "GodMode", false);

        Logger = BepInEx.Logging.Logger.CreateLogSource("TONEX");
        TONEX.Logger.Enable();
        TONEX.Logger.Disable("NotifyRoles");
        TONEX.Logger.Disable("SwitchSystem");
        TONEX.Logger.Disable("ModNews");
        TONEX.Logger.Disable("CustomRpcSender");
        TONEX.Logger.Disable("CoBegin");
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
            TONEX.Logger.Disable("ver");
            TONEX.Logger.Disable("RpcTeleport");
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
                //Vanilla
                {CustomRoles.CrewmateGhost, "#8cffff"},
                {CustomRoles.ImpostorGhost, "#ff1919"},

                //GM
                {CustomRoles.GM, "#ff5b70"},

                //Add-Ons
                {CustomRoles.NotAssigned, "#ffffff"},
                {CustomRoles.LastImpostor, "#ff1919"},
                {CustomRoles.Madmate, "#ff1919"},
                {CustomRoles.Charmed, "#ff00ff"},
                {CustomRoles.Wolfmate,"#00b4eb" },
            };
            var roletype = typeof(RoleBase);
            var roleClassArray = Assembly.GetAssembly(roletype)
                .GetTypes()
                .Where(x => x.IsSubclassOf(roletype)).ToArray();

            foreach (var roleClassType in roleClassArray)
                roleClassType.GetField("RoleInfo")?.GetValue(roletype);

            var addontype = typeof(AddonBase);
            var addonClassArray = Assembly.GetAssembly(addontype)
                .GetTypes()
                .Where(x => x.IsSubclassOf(addontype)).ToArray();

            foreach (var addonClassType in addonClassArray)
                addonClassType.GetField("RoleInfo")?.GetValue(addontype);

            CustomRoleManager.AllRolesClassType = roleClassArray.Concat(addonClassArray).ToArray();
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
        //   AddComponent<CanvasManager>();
        //   AddComponent<MainUIManager>();
        //   AddComponent<OpenButtonManager>();
        //   AddComponent<SettingItemManager>();
        //   AddComponent<SidebarManager>();
        //   AddComponent<TabGroupManager>();
        //   AddComponent<UIBase>();
        //   UI = AddComponent<CreateUIElements>();


        SystemEnvironment.SetEnvironmentVariables();

        Harmony.PatchAll();

        if (!DebugModeManager.AmDebugger) ConsoleManager.DetachConsole();
        else ConsoleManager.CreateConsole();

        TONEX.Logger.Msg("========= TONEX loaded! =========", "Plugin Load");
        LoadEnd = true;
    }
}
public enum CustomDeathReason
{
    // AmongUs 
    Kill,
    Vote,

    // TONEX
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
    Redemption,


    // TONEX
    Merger,

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
    Vagator = CustomRoles.Vagator,
    Congu = CustomRoles.Non_Villain,
    Lawyer = CustomRoles.Lawyer,
    Rebels = CustomRoles.Rebels,
    Mini = CustomRoles.Mini,
    Martyr = CustomRoles.Martyr,
    NightWolf = CustomRoles.NightWolf,
    GodOfPlagues = CustomRoles.GodOfPlagues,
    Puppeteer = CustomRoles.Puppeteer,
    MeteorArbiter = CustomRoles.MeteorArbiter,
    MeteorMurderer = CustomRoles.MeteorMurderer,
    SharpShooter = CustomRoles.SharpShooter,
    AdmirerLovers = CustomRoles.AdmirerLovers,
    AkujoLovers = CustomRoles.AkujoLovers,
    CupidLovers = CustomRoles.CupidLovers,
    Specterraid = CustomRoles.Specterraid,
    Yandere = CustomRoles.Yandere,
    Infector = CustomRoles.Infector,
    Survivor = CustomRoles.Survivor,
    Killer = CustomRoles.Killer,
    Ranger = CustomRoles.Ranger,
    King = CustomRoles.King,
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
