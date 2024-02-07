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
using static TONEX.Translator;
using TONEX.Modules;

namespace TONEX;

[HarmonyPatch]
public class MusicDownloader
{
    public static string DownloadFileTempPath = "TONEX_Data/Sounds/{{sound}}.wav";

    public static string Url_github = "";
    public static string Url_gitee = ""; 
    public static string Url_website = "";
    public static string Url_website2 = "";
    public static string downloadUrl_github = "";
    public static string downloadUrl_gitee = "";
    public static string downloadUrl_website = "";
    public static string downloadUrl_website2 = "";
    public static bool succeed = false;

    public static void StartDownload(string sound, string url = "waitToSelect")
    {
        succeed = false;
        if (!Directory.Exists(@"./TONEX_Data/Sounds"))
        {
            Directory.CreateDirectory(@"./TONEX_Data/Sounds");
        }
        DownloadFileTempPath = "TONEX_Data/Sounds/{{sound}}.wav";
        if (url == "waitToSelect")
        {
            downloadUrl_github = Url_github.Replace("{{sound}}", $"{sound}");
            downloadUrl_gitee = Url_gitee.Replace("{{sound}}", $"{sound}");
            downloadUrl_website = Url_website.Replace("{{sound}}", $"{sound}");
            downloadUrl_website2 = Url_website2.Replace("{{sound}}", $"{sound}");
            url = IsChineseUser ? downloadUrl_website : downloadUrl_github;
        }

        if (!IsValidUrl(url))
        {
            Logger.Error($"Invalid URL: {url}", "DownloadSound", false);
            return;
        }
        retry:
        DownloadFileTempPath = DownloadFileTempPath.Replace("{{sound}}", $"{sound}");
        File.Create(DownloadFileTempPath).Close();
       

        Logger.Msg("Start Downloading from: " + url, "DownloadSound");
        Logger.Msg("Saving file to: " + DownloadFileTempPath, "DownloadSound");

        try
        {

            string filePath = DownloadFileTempPath;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream streamResponse = response.GetResponseStream())
            using (Stream streamFile = File.Create(filePath))
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = streamResponse.Read(buffer, 0, buffer.Length)) > 0)
                {
                    streamFile.Write(buffer, 0, bytesRead);
                }
            }
            succeed = true;
            Logger.Info($"Succeed in {url}", "DownloadSound");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to download\n{ex.Message}", "DownloadSound", false);
            File.Delete(DownloadFileTempPath);
        }
        if (!succeed)
        {
            if (url == downloadUrl_website)
            {
                url = downloadUrl_website2;
                goto retry;
            }
            else if (url == downloadUrl_website2)
            {
                url = downloadUrl_gitee;
                goto retry;
            }
            
        }
        
    }
    private static bool IsValidUrl(string url)
    {
        string pattern = @"^(https?|ftp)://[^\s/$.?#].[^\s]*$";
        return Regex.IsMatch(url, pattern);
    }

}