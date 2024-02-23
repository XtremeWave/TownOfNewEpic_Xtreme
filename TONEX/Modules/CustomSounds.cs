using Hazel;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TONEX.Modules;

public static class CustomSoundsManager
{
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


    private static readonly string SOUNDS_PATH = @$"{Environment.CurrentDirectory.Replace(@"\", "/")}./TONEX_DATA/Sounds/";
    public static void Play(string sound, int playmode = 0)
    {
        if (!Constants.ShouldPlaySfx() || !Main.EnableCustomSoundEffect.Value) return;
        var path = SOUNDS_PATH + sound + ".wav";
        if (!Directory.Exists(SOUNDS_PATH)) Directory.CreateDirectory(SOUNDS_PATH);
        DirectoryInfo folder = new(SOUNDS_PATH);
        if ((folder.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            folder.Attributes = FileAttributes.Hidden;
        if (!File.Exists(path))
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TONEX.Resources.Sounds." + sound + ".wav");
            if (stream == null)
            {
                Logger.Warn($"声音文件缺失：{sound}", "CustomSounds");
                return;
            }
            var fs = File.Create(path);
            stream.CopyTo(fs);
            fs.Close();
        }
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
    public static void StartPlayLoop(string path) => PlaySound(@$"{path}", 0, 9); //第3个形参，把1换为9，连续播放
}
