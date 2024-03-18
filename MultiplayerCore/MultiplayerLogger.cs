using System;
using System.Collections.Generic;
using UnityEngine;
using static Mono.Security.X509.X520;

namespace TootTallyMultiplayer.MultiplayerCore
{
    public static class MultiplayerLogger
    {
        public static Queue<string> logList = new Queue<string>();
        public static Action<string> OnLogReceivedUpdate;

        public static void LogInfo(string message)
        {
            logList.Enqueue(message);
            OnLogReceivedUpdate?.Invoke(GetFormattedLogs());
            if (logList.Count > 69)
                logList.Dequeue();
        }
        
        public static void UserLog(string user, string message) =>
            LogInfo($"[{ColorText("User", Color.yellow)}] {user}: {message}");

        public static void HostLog(string name, string message) =>
            LogInfo($"[{ColorText("Host", new Color(.95f, .2f, .95f))}] {name}: {message}");

        public static void ServerLog(string message) =>
            LogInfo($"[{ColorText("Server", Color.green)}]: {message}");

        public static string GetFormattedLogs() => logList.Count > 0 ? string.Join("\n", logList) : "-";

        public static void ClearLogs() => logList.Clear();

        public static string ColorText(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }
}
