﻿using HarmonyLib;
using Newtonsoft.Json;
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

public static class SoundPanel
{
    public static SpriteRenderer CustomBackground { get; private set; }
    public static GameObject Slider { get; private set; }
    public static Dictionary<string, GameObject> Items { get; private set; }

    public static int PlayMode = 0;
    private static int numItems = 0;
    public static void Hide()
    {
        if (CustomBackground != null)
            CustomBackground?.gameObject?.SetActive(false);
    }
    public static void Init(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

        UiElement[] selectableButtons = optionsMenuBehaviour.ControllerSelectable.ToArray();
        PassiveButton leaveButton = null;
        PassiveButton returnButton = null;
        for (int i = 0; i < selectableButtons.Length; i++)
        {
            var button = selectableButtons[i];
            if (button == null) continue;
            if (button.name == "LeaveGameButton") leaveButton = button.GetComponent<PassiveButton>();
            else if (button.name == "ReturnToGameButton") returnButton = button.GetComponent<PassiveButton>();
        }
        var generalTab = mouseMoveToggle.transform.parent.parent.parent;
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
            var changeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            changeButton.transform.localPosition = new(0.65f, -1.88f, -15f);
            var changeButtonScale  = changeButton.transform.localScale;
            changeButtonScale.x *= 0.4f;
            changeButton.transform.localScale = changeButtonScale;

            changeButton.name = "ChangeButton";
            changeButton.Text.text = GetString($"PlayMode{PlayMode}");
            changeButton.Background.color = Palette.DisabledGrey;
            var changePassiveButton = changeButton.GetComponent<PassiveButton>();
            changePassiveButton.OnClick = new();
            changePassiveButton.OnClick.AddListener(new Action(() =>
            {
                if (PlayMode != 4)
                    PlayMode += 1;
                else
                    PlayMode = 0;
                Init(optionsMenuBehaviour);
            }));

            var stopButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            stopButton.transform.localPosition = new(1.95f, -1.88f, -16f);
            var stopButtonScale = stopButton.transform.localScale;
            stopButtonScale.x *= 0.4f;
            stopButton.transform.localScale = stopButtonScale;
            stopButton.name = "stopButton";
            stopButton.Text.text = GetString("Stop");
            stopButton.Background.color = Palette.DisabledGrey;
            var stopPassiveButton = stopButton.GetComponent<PassiveButton>();
            stopPassiveButton.OnClick = new();
            stopPassiveButton.OnClick.AddListener(new Action(() =>
            {
               
            }));

            var helpText = Object.Instantiate(CustomPopup.InfoTMP.gameObject, CustomBackground.transform);
            helpText.name = "Help Text";
            helpText.transform.localPosition = new(-1.25f, -2.15f, -15f);
            helpText.transform.localScale = new(1f, 1f, 1f);
            var helpTextTMP = helpText.GetComponent<TextMeshPro>();
            helpTextTMP.text = GetString("CustomNameTagHelp");
            helpText.gameObject.GetComponent<RectTransform>().sizeDelta = new(2.45f, 1f);

            var sliderTemplate = AccountManager.Instance.transform.FindChild("MainSignInWindow/SignIn/AccountsMenu/Accounts/Slider").gameObject;
            if (sliderTemplate != null && Slider == null)
            {
                Slider = Object.Instantiate(sliderTemplate, CustomBackground.transform);
                Slider.name = "Name Tags Slider";
                Slider.transform.localPosition = new Vector3(0f, 0.5f, -12f);
                Slider.transform.localScale = new Vector3(1f, 1f, 1f);
                Slider.GetComponent<SpriteRenderer>().size = new(5f, 4f);
                var scroller = Slider.GetComponent<Scroller>();
                scroller.ScrollWheelSpeed = 0.3f;
                var mask = Slider.transform.FindChild("Mask");
                mask.transform.localScale = new Vector3(4.9f, 3.92f, 0f);
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

        foreach (var sound in AllMusic)
        {
            numItems++;
            var button = Object.Instantiate(buttonPrefab, scroller.Inner);
            button.transform.localPosition = new(-1f, 1.6f - 0.6f * numItems, -10.5f);
            button.transform.localScale = new(1.2f, 1.2f, 1.2f);
            button.name = "Name Tag Item For " + sound;
            Object.Destroy(button.GetComponent<UIScrollbarHelper>());
            Object.Destroy(button.GetComponent<NumberButton>());   
            button.transform.GetChild(0).GetComponent<TextMeshPro>().text = AllSounds.Contains(sound)? GetString($"{sound}"):sound;
            var renderer = button.GetComponent<SpriteRenderer>();
            renderer.color = File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav") ? (TONEXMusic.Contains(sound) ? Color.cyan : Color.green) : Palette.DisabledGrey ;

            var rollover = button.GetComponent<ButtonRolloverHandler>();
            rollover.OutColor = File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}/TONEX_DATA/Sounds/{sound}.wav") ? (TONEXMusic.Contains(sound) ? Color.cyan : Color.green) : Palette.DisabledGrey;
            var passiveButton = button.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();
            passiveButton.OnClick.AddListener(new Action(() =>
            {

                if (File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav"))
                {
                    CustomSoundsManager.Play(sound);
                }

            }));
            var previewText = Object.Instantiate(button.transform.GetChild(0).GetComponent<TextMeshPro>(), button.transform);
            previewText.transform.SetLocalX(1.9f);
            previewText.fontSize = 1f;
            string preview = GetString("NoFound");
            if (File.Exists(@$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav"))
                preview = GetString("CanPlay");
            previewText.text = preview;
            Items.Add(sound, button);
        }
        scroller.SetYBoundsMin(0f);
        scroller.SetYBoundsMax(0.6f * numItems);
    }
}
