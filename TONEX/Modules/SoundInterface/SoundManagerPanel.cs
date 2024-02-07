using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static TONEX.IntSoundManager;
using static TONEX.Translator;
using Object = UnityEngine.Object;
using System.IO;

namespace TONEX.Modules.SoundInterface;

public static class SoundManagerPanel
{
    public static SpriteRenderer CustomBackground { get; private set; }
    public static GameObject Slider { get; private set; }
    public static Dictionary<string, GameObject> Items { get; private set; }
    private static bool IsDownloading = false;

    private static int numItems = 0;
    public static void Hide()
    {
        if (CustomBackground != null)
            CustomBackground?.gameObject?.SetActive(false);
    }
    public static void Init(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

       
        if (!GameStates.IsNotJoined)
            return;
        if (CustomBackground == null)
        {
            numItems = 0;
            CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
            CustomBackground.name = "Name Tag Panel Background";
            CustomBackground.transform.localScale = new(0.9f, 0.9f, 1f);
            CustomBackground.transform.localPosition += Vector3.back * 18;
            CustomBackground.gameObject.SetActive(false);

            var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            closeButton.transform.localPosition = new(1.3f, -2.43f, -16f);
            closeButton.name = "Close";
            closeButton.Text.text = GetString("Close");
            closeButton.Background.color = Palette.DisabledGrey;
            var closePassiveButton = closeButton.GetComponent<PassiveButton>();
            closePassiveButton.OnClick = new();
            closePassiveButton.OnClick.AddListener(new Action(() =>
            {
                CustomBackground.gameObject.SetActive(false);
            }));

            var newButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            newButton.transform.localPosition = new(1.3f, -1.88f, -16f);
            newButton.name = "New Tag";
            newButton.Text.text = GetString("NewSound");
            newButton.Background.color = Palette.White;
            var newPassiveButton = newButton.GetComponent<PassiveButton>();
            newPassiveButton.OnClick = new();
            newPassiveButton.OnClick.AddListener(new Action(SoundManagerNewWindow.Open));

            var helpText = Object.Instantiate(CustomPopup.InfoTMP.gameObject, CustomBackground.transform);
            helpText.name = "Help Text";
            helpText.transform.localPosition = new(-1.25f, -2.15f, -15f);
            helpText.transform.localScale = new(1f, 1f, 1f);
            var helpTextTMP = helpText.GetComponent<TextMeshPro>();
            helpTextTMP.text = GetString("CustomSoundHelp");
            helpText.gameObject.GetComponent<RectTransform>().sizeDelta = new(2.45f, 1f);

            var sliderTemplate = AccountManager.Instance.transform.FindChild("MainSignInWindow/SignIn/AccountsMenu/Accounts/Slider").gameObject;
            if (sliderTemplate != null && Slider == null)
            {
                Slider = Object.Instantiate(sliderTemplate, CustomBackground.transform);
                Slider.name = "Name Tags Slider";
                Slider.transform.localPosition = new Vector3(0f, 0.5f, -11f);
                Slider.transform.localScale = new Vector3(1f, 1f, 1f);
                Slider.GetComponent<SpriteRenderer>().size = new(5f, 4f);
                var scroller = Slider.GetComponent<Scroller>();
                scroller.ScrollWheelSpeed = 0.3f;
                var mask = Slider.transform.FindChild("Mask");
                mask.transform.localScale = new Vector3(4.9f, 3.92f, 1f);
            }
        }

        ReloadTag(null);
        RefreshTagList();
    }
    public static void RefreshTagList()
    {
        var scroller = Slider.GetComponent<Scroller>();
        scroller.Inner.gameObject.ForEachChild((Action<GameObject>)(DestroyObj));
        static void DestroyObj(GameObject obj)
        {
            if (obj.name.StartsWith("AccountButton")) Object.Destroy(obj);
        }

        var numberSetter = AccountManager.Instance.transform.FindChild("DOBEnterScreen/EnterAgePage/MonthMenu/Months").GetComponent<NumberSetter>();
        var buttonPrefab = numberSetter.ButtonPrefab.gameObject;

        Items?.Values?.Do(Object.Destroy);
        Items = new();

        foreach (var sound in AllFiles)
        {
            numItems++;
            var button = Object.Instantiate(buttonPrefab, scroller.Inner);
            button.transform.localPosition = new(-1f, 1.6f - 0.6f * numItems, -10.5f);
            button.transform.localScale = new(1.2f, 1.2f, 1.2f);
            button.name = "Name Tag Item For " + sound;
            Object.Destroy(button.GetComponent<UIScrollbarHelper>());
            Object.Destroy(button.GetComponent<NumberButton>());
            if (File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav") || File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/SoundNames/{sound}.json"))
                button.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("delete");
            else if (AllTONEX.Contains(sound) && !File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav"))
            button.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("download");
            else if (!AllTONEX.Contains(sound) && !File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav"))
            {
                button.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("NoFound");
            }
                if (IsDownloading)
                button.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("cancel");
            var renderer = button.GetComponent<SpriteRenderer>();
            renderer.color =( File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav") || File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/SoundNames/{sound}.json")) ? (AllTONEX.Contains(sound)?Color.red:Palette.DisabledGrey) : Color.green;

            var rollover = button.GetComponent<ButtonRolloverHandler>();
            rollover.OutColor = (File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav") || File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/SoundNames/{sound}.json")) ? (AllTONEX.Contains(sound) ? Color.red : Palette.DisabledGrey) : Color.green;
            if (IsDownloading)
            {
                renderer.color = rollover.OutColor = Color.yellow;
            }
            var passiveButton = button.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();
            passiveButton.OnClick.AddListener(new Action(() =>
            {
                if (IsDownloading)
                {
                    renderer.color = rollover.OutColor = Color.red;
                }
                else
                { 
                    if (File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav") || File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/SoundNames/{sound}.json"))
                    {
                        DeleteSoundInName(sound);
                        DeleteSoundInFile(sound);
                        ReloadTag(sound);
                        RefreshTagList();
                        SoundPanel.RefreshTagList();
                    }
                    else
                    {
                        renderer.color = rollover.OutColor = Color.yellow;
                        MusicDownloader.StartDownload(sound);
                        ReloadTag(sound);
                        RefreshTagList();
                        SoundPanel.RefreshTagList();
                    }
                }
            }));
            var previewText = Object.Instantiate(button.transform.GetChild(0).GetComponent<TextMeshPro>(), button.transform);
            previewText.transform.SetLocalX(1.9f);
            previewText.fontSize = 1f;
            string preview ="???";
            if (sound != null)
                preview = sound + ".wav";
            previewText.text = preview;
            Items.Add(sound, button);
        }

        scroller.SetYBoundsMin(0f);
        scroller.SetYBoundsMax(0.6f * numItems);
    }
    public static void DeleteSoundInName(string soundname)
    {
        if (AllTONEX.Contains(soundname)) return;
        try
        {

            var path = @$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/SoundNames/{soundname}.json";
            Logger.Info($"{soundname} Deleted", "DeleteSound");
                File.Delete(path);
            
        }
        catch (Exception e)
        {
            Logger.Error($"清除文件名称失败\n{e}", "DeleteOldFiles");
        }
        return;
    }
    public static void DeleteSoundInFile(string sound)
    {
        try
        {
            var path2 = @$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav";
            Logger.Info($"{Path.GetFileName(path2)} Deleted", "DeleteSound");
            File.Delete(path2);
        }
        catch (Exception e)
        {
            Logger.Error($"清除文件失败\n{e}", "DeleteSound");
        }
        return;
    }
}
