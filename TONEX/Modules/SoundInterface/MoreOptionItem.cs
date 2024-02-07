using BepInEx.Configuration;
using System;
using UnityEngine;
using TONEX.Modules.SoundInterface;
/*
namespace TONEX.Modules.MoreOptions;

public sealed class MoreOptionItem : MoreActionItem
{
    public ConfigEntry<bool> Config { get; private set; }

    private MoreOptionItem(
        string name,
        Action additionalOnClickAction,
        OptionsMenuBehaviour optionsMenuBehaviour)
    : base(
        name,
        optionsMenuBehaviour)
    {
        UpdateToggle();
    }

    /// <summary>
    /// Modオプション画面にconfigのトグルを追加します
    /// </summary>
    /// <param name="name">ボタンラベルの翻訳キーとボタンのオブジェクト名</param>
    /// <param name="config">対応するconfig</param>
    /// <param name="optionsMenuBehaviour">OptionsMenuBehaviourのインスタンス</param>
    /// <param name="additionalOnClickAction">クリック時に追加で発火するアクション．configが変更されたあとに呼ばれる</param>
    /// <returns>作成したアイテム</returns>
    public static MoreOptionItem Create(
        string name,
        Action additionalOnClickAction
        ,
        OptionsMenuBehaviour optionsMenuBehaviour,
        ConfigEntry<bool> config=  null)
    {
        var item = new MoreOptionItem(name, additionalOnClickAction, optionsMenuBehaviour);
        item.OnClickAction = () =>
        {
            item.UpdateToggle();
            additionalOnClickAction?.Invoke();
        };
        return item;
    }

    public void UpdateToggle()
    {
        if (ToggleButton == null) return;

        var color = Config.Value ? new Color32(158, 239, 255, byte.MaxValue) : new Color32(77, 77, 77, byte.MaxValue);
        ToggleButton.Background.color = color;
        ToggleButton.Rollover?.ChangeOutColor(color);
    }
}*/