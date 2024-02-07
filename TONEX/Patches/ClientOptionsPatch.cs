using HarmonyLib;
using TONEX.Modules.ClientOptions;
using TONEX.Modules.MoreOptions;
using TONEX.Modules.NameTagInterface;
using TONEX.Modules.SoundInterface;
using UnityEngine;

namespace TONEX;

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class OptionsMenuBehaviourStartPatch
{
    private static ClientOptionItem UnlockFPS;
  //  private static ClientOptionItem CanPublic;
    private static ClientOptionItem HorseMode;
    private static ClientOptionItem AutoStartGame;
    private static ClientOptionItem AutoEndGame;
    private static ClientOptionItem ForceOwnLanguage;
    private static ClientOptionItem ForceOwnLanguageRoleName;
    private static ClientOptionItem EnableCustomButton;
    private static ClientOptionItem EnableCustomSoundEffect;
    private static ClientActionItem UnloadMod;
    private static ClientActionItem DumpLog;
    private static ClientOptionItem VersionCheat;
    private static ClientOptionItem GodMode;
    public static MoreActionItem NameTag;
    public static MoreActionItem Sound;
    public static MoreActionItem SoundManager;

    private static bool reseted = false;
    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        if (__instance.DisableMouseMovement == null) return;

        

        if (!reseted || !DebugModeManager.AmDebugger)
        {
            reseted = true;
            Main.VersionCheat.Value = false;
            Main.GodMode.Value = false;
        }

      
        
        if (UnlockFPS == null || UnlockFPS.ToggleButton == null)
        {
            UnlockFPS = ClientOptionItem.Create("UnlockFPS", Main.UnlockFPS, __instance, UnlockFPSButtonToggle);
            static void UnlockFPSButtonToggle()
            {
                Application.targetFrameRate = Main.UnlockFPS.Value ? 240 : 60;
                Logger.SendInGame(string.Format(Translator.GetString("FPSSetTo"), Application.targetFrameRate));
            }
        }
       // if (CanPublic == null || CanPublic.ToggleButton == null)
        //{
            //CanPublic = ClientOptionItem.Create("CanPublic", Main.CanPublic, __instance);
       // }
       
        if (HorseMode == null || HorseMode.ToggleButton == null)
        {
            HorseMode = ClientOptionItem.Create("HorseMode", Main.HorseMode, __instance);
        }
        if (AutoStartGame == null || AutoStartGame.ToggleButton == null)
        {
            AutoStartGame = ClientOptionItem.Create("AutoStartGame", Main.AutoStartGame, __instance, AutoStartButtonToggle);
            static void AutoStartButtonToggle()
            {
                if (Main.AutoStartGame.Value == false && GameStates.IsCountDown)
                {
                    GameStartManager.Instance.ResetStartState();
                }
            }
        }
        if (AutoEndGame == null || AutoEndGame.ToggleButton == null)
        {
            AutoEndGame = ClientOptionItem.Create("AutoEndGame", Main.AutoEndGame, __instance);
        }
        if (ForceOwnLanguage == null || ForceOwnLanguage.ToggleButton == null)
        {
            ForceOwnLanguage = ClientOptionItem.Create("ForceOwnLanguage", Main.ForceOwnLanguage, __instance);
        }
        if (ForceOwnLanguageRoleName == null || ForceOwnLanguageRoleName.ToggleButton == null)
        {
            ForceOwnLanguageRoleName = ClientOptionItem.Create("ForceOwnLanguageRoleName", Main.ForceOwnLanguageRoleName, __instance);
        }
        if (EnableCustomButton == null || EnableCustomButton.ToggleButton == null)
        {
            EnableCustomButton = ClientOptionItem.Create("EnableCustomButton", Main.EnableCustomButton, __instance);
        }
        if (EnableCustomSoundEffect == null || EnableCustomSoundEffect.ToggleButton == null)
        {
            EnableCustomSoundEffect = ClientOptionItem.Create("EnableCustomSoundEffect", Main.EnableCustomSoundEffect, __instance);
        }
        if (UnloadMod == null || UnloadMod.ToggleButton == null)
        {
            UnloadMod = ClientActionItem.Create("UnloadMod", ModUnloaderScreen.Show, __instance);
        }
        if (DumpLog == null || DumpLog.ToggleButton == null)
        {
            DumpLog = ClientActionItem.Create("DumpLog", () => Utils.DumpLog(), __instance);
        }
        if ((VersionCheat == null || VersionCheat.ToggleButton == null) && DebugModeManager.AmDebugger)
        {
            VersionCheat = ClientOptionItem.Create("VersionCheat", Main.VersionCheat, __instance);
        }
        if ((GodMode == null || GodMode.ToggleButton == null) && DebugModeManager.AmDebugger)
        {
            GodMode = ClientOptionItem.Create("GodMode", Main.GodMode, __instance);
        }
        
        var mouseMoveToggle = __instance.DisableMouseMovement;
        if ((NameTag == null || NameTag.ToggleButton == null))
        {
            NameTag = MoreActionItem.Create("NameTag", () =>
            {
                NameTagPanel.CustomBackground?.gameObject?.SetActive(true);
            }, __instance);
        }
        if ((Sound == null || Sound.ToggleButton == null))
        {
            Sound = MoreActionItem.Create("SoundOption", () =>
            {
                if (SoundPanel.CustomBackground == null)
                    Logger.Info("1_1", "test");
                if (SoundPanel.CustomBackground.gameObject == null)
                    Logger.Info("1_2", "test");
                try
                {
                    SoundPanel.CustomBackground.gameObject.SetActive(true);
                }

                catch (System.Exception ex)
                {
                    Logger.Exception(ex, "Sounds");
                }
            }, __instance);
        }

        if ((SoundManager == null || SoundManager.ToggleButton == null))
        {
            SoundManager = MoreActionItem.Create("SoundManager", () =>
            {


                SoundManagerPanel.CustomBackground?.gameObject?.SetActive(true);


            }, __instance);
        }
        if (!GameStates.IsNotJoined)
        {
            NameTag.ToggleButton.Text.text = Translator.GetString("OnlyAvailableInMainMenu");
            NameTag.ToggleButton.GetComponent<PassiveButton>().enabled = false;
            NameTag.ToggleButton.Background.color = Palette.DisabledGrey;//*/
            /*NameTag.ToggleButton.Text.text = Translator.GetString("NameTagOptions");
            NameTag.ToggleButton.GetComponent<PassiveButton>().enabled = true;
            NameTag.ToggleButton.Background.color = Main.ModColor32;//*/
            Sound.ToggleButton.Text.text = Translator.GetString("SoundOptions");
            Sound.ToggleButton.GetComponent<PassiveButton>().enabled = true;
            Sound.ToggleButton.Background.color = Main.ModColor32;
            SoundManager.ToggleButton.Text.text = Translator.GetString("OnlyAvailableInMainMenu");
            SoundManager.ToggleButton.GetComponent<PassiveButton>().enabled = false;
            SoundManager.ToggleButton.Background.color = Palette.DisabledGrey;
        }
        else
        {
            NameTag.ToggleButton.Text.text = Translator.GetString("NameTagOptions");
            NameTag.ToggleButton.GetComponent<PassiveButton>().enabled = true;
            NameTag.ToggleButton.Background.color = Main.ModColor32;
            Sound.ToggleButton.Text.text = Translator.GetString("SoundOptions");
            Sound.ToggleButton.GetComponent<PassiveButton>().enabled = true;
            Sound.ToggleButton.Background.color = Main.ModColor32;
            SoundManager.ToggleButton.Text.text = Translator.GetString("SoundManagerOptions");
            SoundManager.ToggleButton.GetComponent<PassiveButton>().enabled = true;
            SoundManager.ToggleButton.Background.color = Main.ModColor32;
        }
        NameTagPanel.Init(__instance);
        SoundPanel.Init(__instance);
        SoundManagerPanel.Init(__instance);

        if (ModUnloaderScreen.Popup == null)
        ModUnloaderScreen.Init(__instance);

    }
}

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
public static class OptionsMenuBehaviourClosePatch
{
    public static void Postfix()
    {
        Logger.Info("1", "test");
        ClientActionItem.CustomBackground?.gameObject?.SetActive(false);
        MoreActionItem.CustomBackground?.gameObject?.SetActive(false);
        Logger.Info("2", "test");
        NameTagPanel.Hide();
        Logger.Info("3", "test");
        NameTagEditMenu.Hide();
        Logger.Info("4", "test");
        ModUnloaderScreen.Hide();
        Logger.Info("5", "test");
        SoundPanel.Hide();
        Logger.Info("6", "test");
        SoundManagerPanel.Hide();
        Logger.Info("7", "test");
    }
}
