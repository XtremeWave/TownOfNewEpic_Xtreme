using HarmonyLib;
using Newtonsoft.Json.Linq;
using Sentry.Unity.NativeUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using TMPro;
using TONEX.Modules;
using UnityEngine;
using static TONEX.Translator;
using System.IO.Compression;
using TONEX.Modules.SoundInterface;
using static UnityEngine.PlayerLoop.PreUpdate;

namespace TONEX;

[HarmonyPatch]
public class MusicDownloader
{
    public static string sound = "";
    public static string DownloadFileTempPath = $"TONEX_Data/Sounds/{sound}.wav";
    private static IReadOnlyList<string> URLs => new List<string>
    {
#if DEBUG
        "file:///D:/Desktop/TONEX/info.json",
        "file:///C:/Users/YJNSH/Desktop/info.json",
        "file:///%userprofile%/Desktop/info.json",
        "file:///D:/Desktop/info.json",
#else
        "https://raw.githubusercontent.com/XtremeWave/TownOfNewEpic_Xtreme/TONEX/info.json",
        "https://cdn.jsdelivr.net/gh/XtremeWave/TownOfNewEpic_Xtreme/info.json",
         //"https://tonx-1301425958.cos.ap-shanghai.myqcloud.com/info.json",
        "https://tonex.cc/Resource/info.json",
        "https://gitee.com/TEAM_TONEX/TownOfNewEpic_Xtreme/raw/TONEX/info.json",
         

#endif
    };
    private static IReadOnlyList<string> GetInfoFileUrlList()
    {
        var list = URLs.ToList();
        if (IsChineseUser) list.Reverse();
        return list;
    }
    public static string versionInfoRaw = "";

    public static Version latestVersion = null;
    public static string showVer = "";
    public static string verHead = "";
    public static string verDate = "";
    public static string verTestName = "";
    public static string verTestNum = "";
    public static Version minimumVersion = null;
    public static int creation = 0;

    public static string downloadUrl_github = "";
    public static string downloadUrl_gitee = "";
    public static string downloadUrl_website = "";

    private static int retried = 0;
    private static bool firstLaunch = true;

    public static async Task<bool> GetVersionInfo(string url)
    {
        Logger.Msg(url, "CheckRelease");
        try
        {
            string result;
            if (url.StartsWith("file:///"))
            {
                result = File.ReadAllText(url[8..]);
            }
            else
            {
                using HttpClient client = new();
                client.DefaultRequestHeaders.Add("User-Agent", "TONEX Updater");
                client.DefaultRequestHeaders.Add("Referer", "tonex.cc");
                using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                if (!response.IsSuccessStatusCode || response.Content == null)
                {
                    Logger.Error($"Failed: {response.StatusCode}", "CheckRelease");
                    return false;
                }
                result = await response.Content.ReadAsStringAsync();
                result = result.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
            }

            JObject data = JObject.Parse(result);

            JObject downloadUrl = data["url"].Cast<JObject>();
            downloadUrl_github = downloadUrl["githubmus"]?.ToString().Replace("{{sound}}", $"{sound}");
            downloadUrl_gitee = downloadUrl["giteemus"]?.ToString().Replace("{{sound}}", $"{sound}");

            downloadUrl_website = downloadUrl["websitemus"]?.ToString().Replace("{{sound}}", $"{sound}");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void StartDownload(string url = "waitToSelect", string soundname = "")
    {
        sound = soundname;
        if (sound == "") return;
        if (url == "waitToSelect")
        {
            url = IsChineseUser ? downloadUrl_gitee : downloadUrl_github;
        }
        var task = GetVersionInfo(url);
        var task2 = DownloadMus(url);
        task.ContinueWith(t =>
        {
            task2.Wait();
            IntSoundManager.ReloadTag(soundname);
            SoundPanel.RefreshTagList();
            SoundManagerPanel.RefreshTagList();
        });

    }
    public static async Task<bool> DownloadMus(string url)
    {
        File.Delete(DownloadFileTempPath);
        File.Create(DownloadFileTempPath).Close();

        Logger.Msg("Start Downlaod From: " + url, "DownloadDLL");
        Logger.Msg("Save To: " + DownloadFileTempPath, "DownloadDLL");

        try
        {
            

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                              fileStream = new FileStream(DownloadFileTempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await contentStream.CopyToAsync(fileStream);
                }
            }


            Logger.Info($"下载完成", "DownloadMus", false);
            return true;
            
        }
        catch (Exception ex)
        {
            File.Delete(DownloadFileTempPath);
            Logger.Error($"更新失败\n{ex.Message}", "DownloadMus", false);
            return false;
        }
    }
    private static void OnDownloadProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
    {
        string msg = $"{GetString("updateInProgress")}\n{totalFileSize / 1000}KB / {totalBytesDownloaded / 1000}KB  -  {(int)progressPercentage}%";
        Logger.Info(msg, "DownloadDLL");
        CustomPopup.UpdateTextLater(msg);
    }
}