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

namespace TONEX;

[HarmonyPatch]
public class MusicDownloader
{
    public static string DownloadFileTempPath = "TONEX_Data/Sounds/{{sound}}.wav.temp";

    public static string Url_github = "";
    public static string Url_gitee = "";
    public static string downloadUrl_github = "";
    public static string downloadUrl_gitee = "";
    public static string downloadUrl_website = "";

    public static void StartDownload(string sound, string url = "waitToSelect")
    {
        DownloadFileTempPath = "TONEX_Data/Sounds/{{sound}}.wav.temp";
        if (url == "waitToSelect")
        {
            downloadUrl_github = Url_github.Replace("{{sound}}", $"{sound}");
            downloadUrl_gitee = Url_gitee.Replace("{{sound}}", $"{sound}");
            url = IsChineseUser ? downloadUrl_gitee : downloadUrl_github;
        }

        if (!IsValidUrl(url))
        {
            Logger.Error($"Invalid URL: {url}", "DownloadDLL", false);
            return;
        }
        File.Delete(DownloadFileTempPath);
        File.Create(DownloadFileTempPath).Close();
        DownloadFileTempPath = DownloadFileTempPath.Replace("{{sound}}", $"{sound}");

        Logger.Msg("Start Downloading from: " + url, "DownloadDLL");
        Logger.Msg("Saving file to: " + DownloadFileTempPath, "DownloadDLL");

        try
        {

            string filePath = @$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/{sound}.wav";

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
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to update\n{ex.Message}", "DownloadDLL", false);
            File.Delete(DownloadFileTempPath);
        }
    }

    private static bool IsValidUrl(string url)
    {
        string pattern = @"^(https?|ftp)://[^\s/$.?#].[^\s]*$";
        return Regex.IsMatch(url, pattern);
    }

}