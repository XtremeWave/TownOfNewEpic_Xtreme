using AmongUs.Data;
using HarmonyLib;
using Hazel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static TONEX.Translator;

namespace TONEX;

#nullable enable
public static class IntSoundManager
{
    public static readonly string TAGS_DIRECTORY_PATH = @"./TONEX_Data/SoundNames/";
    private static List<string> CustomMusicList = new();
    public static IReadOnlyList<string> TONEXMusic => TONEXMusicList;
    public static IReadOnlyList<string> AllMusic => CustomMusicList.Concat(TONEXMusicList).ToList();
    public static IReadOnlyList<string> AllSounds => TONEXSoundList;
    public static IReadOnlyList<string> AllFiles => AllSounds.Concat(AllMusic).ToList();

    private static List<string> TONEXMusicList = new()
    {
        "Spring Rejoices in Parallel Universes",
    
    
    
    
    
    };
    private static List<string> TONEXSoundList = new()
    {
        "AWP",
        "Bet",
        "Bite",
        "Boom",
        "Clothe",
        "Congrats",
        "Curse",
        "Dove",
        "Eat",
        "ElementSkill1",
        "ElementSkill2",
        "ElementSkill3",
        "ElementMaxi1",
        "ElementMaxi2",
        "ElementMaxi3",
        "FlashBang",
        "GongXiFaCai",
        "Gunfire",
        "Gunload",
        "Join1",
        "Join2",
        "Join3",
        "Line",
        "MarioCoin",
        "MarioJump",
        "Onichian",
        "Shapeshifter",
        "Shield",
        "Teleport",
        "TheWorld",
    };
    public static void ReloadTag(string? sound)
    {
        if (sound == null)
        {
            Init();
            return;
        }

        CustomMusicList.Remove(sound);

        string path = $"{TAGS_DIRECTORY_PATH}{sound}.json";
        if (File.Exists(path))
        {
            try { ReadTagsFromFile(path); }
            catch (Exception ex)
            {
                Logger.Error($"Load Tag From: {path} Failed\n" + ex.ToString(), "SoundManager", false);
            }
        }

        if (!TONEXSoundList.Contains(sound) && !CustomMusicList.Contains(sound))
        {
            CustomMusicList.Add(sound);
        }
    }
    public static void Init()
    {
        CustomMusicList = new();

        if (!Directory.Exists(TAGS_DIRECTORY_PATH)) Directory.CreateDirectory(TAGS_DIRECTORY_PATH);
        var files = Directory.EnumerateFiles(TAGS_DIRECTORY_PATH, "*.json", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            try { ReadTagsFromFile(file); }
            catch (Exception ex)
            {
                Logger.Error($"Load Tag From: {file} Failed\n" + ex.ToString(), "SoundManager", false);
            }
        }

        Logger.Msg($"{CustomMusicList.Count} Name Tags Loaded", "SoundManager");
    }
    public static void ReadTagsFromFile(string path)
    {
        if (path.ToLower().Contains("template")) return;
        string sound = Path.GetFileNameWithoutExtension(path);
        if (sound != null && !AllSounds.Contains(sound))
        {
            CustomMusicList.Add(sound);
            Logger.Info($"Sound Loaded: {sound}", "SoundManager");
        }
    }
}
#nullable disable