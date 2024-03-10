using System;

namespace TONEX.Modules;

public static class SystemEnvironment
{
    public static void SetEnvironmentVariables()
    {
        // ユーザ環境変数に最近開かれたTOHアモアスフォルダのパスを設定
        Environment.SetEnvironmentVariable("TOWN_OF_NEW_EPIC_DIR_ROOT", Environment.CurrentDirectory, EnvironmentVariableTarget.User);
    }
}