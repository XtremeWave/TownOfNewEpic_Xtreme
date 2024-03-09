using AmongUs.HTTP;
using Hazel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Media;
using static TONEX.AudioManager;


namespace TONEX.Modules;

public static class CustomSoundsManager
{
    public static Dictionary<string, string> formatMap = new()
    {
    { ".wav", ".flac" },
    { ".flac", ".aiff" },
    { ".aiff", ".mp3" },
    { ".mp3", ".aac" },
    { ".aac", ".ogg" },
    { ".ogg", ".m4a" }
};
    public static void RPCPlayCustomSound(this PlayerControl pc , string sound, int playmode=0, bool force = false)
    {
        if (pc == null || pc.AmOwner)
        {
            Play(sound, playmode);
            return;
        }
        if (!force) if (!AmongUsClient.Instance.AmHost || !pc.IsModClient()) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlayCustomSound, SendOption.Reliable, pc.GetClientId());
        writer.Write(sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RPCPlayCustomSoundAll(string sound)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlayCustomSound, SendOption.Reliable, -1);
        writer.Write(sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        Play(sound, 0);
    }
    public static void ReceiveRPC(MessageReader reader) => Play(reader.ReadString(), 0);


    public static readonly string SOUNDS_PATH = @$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_Data/Sounds/";
    public static readonly string PLAY_PATH = @$"{Environment.CurrentDirectory.Replace(@"\", "/")}/TONEX_Data/Sounds/";
    public static void Play(string sound, int playmode = 0)
    {
        if (!Constants.ShouldPlaySfx() || !Main.EnableCustomSoundEffect.Value) return;
        var path = SOUNDS_PATH + sound + ".wav";
        
        if (!Directory.Exists(SOUNDS_PATH)) Directory.CreateDirectory(SOUNDS_PATH);
        DirectoryInfo folder = new(SOUNDS_PATH);
        if ((folder.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            folder.Attributes = FileAttributes.Hidden;
        var i = 0;
        while (!File.Exists(path))
        {
            i++;
            Logger.Error($"{path} No Found", "CustomSoundsManager.Play");
            string matchingKey = formatMap.Keys.FirstOrDefault(key => path.Contains(key));
            if (matchingKey != null)
            {
                string newFormat = formatMap[matchingKey];
                path = path.Replace(matchingKey, newFormat);
                Logger.Warn($"Try To Find {path} ", "CustomSoundsManager.Play");
            }
            if (i == formatMap.Count)
            {
                Logger.Error($"{path} Cannot Be Finded", "CustomSoundsManager.Play");
                break;
            }
        }
        if (File.Exists(path))
        {
            path = path.Replace(SOUNDS_PATH, PLAY_PATH);
            Logger.Warn($"{path} Finded", "CustomSoundsManager.Play");
        }
            /*if (!File.Exists(path))
            {
                Logger.Warn($"未找到{path}", "CustomSoundsManager.Play");
                string originalFormat = Path.GetExtension(path);
                if (formatMap.ContainsKey(originalFormat))
                {
                    string newFormat = formatMap[originalFormat];
                    path = path.Replace(originalFormat, newFormat);

                    goto Retry;
                }

                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TONEX.Resources.Sounds." + sound + ".wav");
                if (stream == null)
                {
                    Logger.Warn($"声音文件缺失：{sound}", "CustomSounds");
                    return;
                }
                var fs = File.Create(path);
                stream.CopyTo(fs);
                fs.Close();
            }*/
            switch (playmode)
        {
            case 0:
                StartPlay(path);
                break;
            case 1:
                StartPlayLoop(path);
                break;
            
        }
        
        Logger.Msg($"播放声音：{sound}", "CustomSounds");
    }

    [DllImport("winmm.dll")]
    public static extern bool PlaySound(string Filename, int Mod, int Flags);
    public static void StartPlay(string path) => PlaySound(@$"{path}", 0, 1); //第3个形参，换为9，连续播放
    public static void StartPlayOnce(string path) => PlaySound(@$"{path}", 0, 1); //第3个形参，换为9，连续播放
    public static void StopPlay() => PlaySound(null, 0, 0);
    public static void StartPlayLoop(string path)
    {

        
      //  if (@$"{path}".Contains(".mp3"))
        //    PlaySoundInMedia(path);
        PlaySound(@$"{path}", 0, 9);
    }

}
