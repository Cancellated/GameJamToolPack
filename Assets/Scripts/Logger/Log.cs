using System.Collections.Generic;
using UnityEngine;

namespace Logger
{
    public static class Log
    {
        // 日志级别控制
        public enum LogLevel { None, Error, Warning, Info, Debug }
        public static LogLevel currentLogLevel = LogLevel.Info;

        /// <summary>冷却日志的最近输出时间记录</summary>
        private static readonly Dictionary<string, float> s_lastLogTimes = new();

        // 基础日志方法
        public static void Info(string module, string message, UnityEngine.Object context = null)
        {
            if (currentLogLevel < LogLevel.Info) return;

            string formatted = $"[{module}] {message}";

            if (context != null)
                Debug.Log(formatted, context);
            else
                Debug.Log(formatted);
        }

        // 警告日志
        public static void Warning(string module, string message, UnityEngine.Object context = null)

        {
            if (currentLogLevel < LogLevel.Warning) return;

            string formatted = $"[{module}] ⚠️ {message}";

            if (context != null)
                Debug.LogWarning(formatted, context);
            else
                Debug.LogWarning(formatted);
        }

        // 错误日志
        public static void Error(string module, string message, UnityEngine.Object context = null)

        {
            if (currentLogLevel < LogLevel.Error) return;

            string formatted = $"[{module}] ❌ {message}";

            if (context != null)
                Debug.LogError(formatted, context);
            else
                Debug.LogError(formatted);
        }

        // 带冷却的信息日志：同一 key 在 cooldownSeconds 内只输出一次。
        // 适合滑块拖动、每帧状态变化等高频调用，避免刷屏。
        public static void InfoWithCooldown(string module, string message,
            string key = null, float cooldownSeconds = 1f, UnityEngine.Object context = null)
        {
            if (!CanLog(key ?? message, cooldownSeconds))
                return;
            Info(module, message, context);
        }

        public static void WarningWithCooldown(string module, string message,
            string key = null, float cooldownSeconds = 1f, UnityEngine.Object context = null)
        {
            if (!CanLog(key ?? message, cooldownSeconds))
                return;
            Warning(module, message, context);
        }

        public static void ErrorWithCooldown(string module, string message,
            string key = null, float cooldownSeconds = 1f, UnityEngine.Object context = null)
        {
            if (!CanLog(key ?? message, cooldownSeconds))
                return;
            Error(module, message, context);
        }

        private static bool CanLog(string key, float cooldownSeconds)
        {
            if (string.IsNullOrEmpty(key))
            {
                return true;
            }

            float now = Time.realtimeSinceStartup;
            if (s_lastLogTimes.TryGetValue(key, out float lastTime) && now - lastTime < cooldownSeconds)
            {
                return false;
            }

            s_lastLogTimes[key] = now;
            return true;
        }

        // 带颜色的日志
        public static void LogColor(string module, string message, Color color, UnityEngine.Object context = null)

        {
            if (currentLogLevel < LogLevel.Info) return;

            string hexColor = ColorUtility.ToHtmlStringRGBA(color);
            string formatted = $"<color=#{hexColor}>[{module}] {message}</color>";

            if (context != null)
                Debug.Log(formatted, context);
            else
                Debug.Log(formatted);
        }

        // 调试专用日志（只在开发版本显示）
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void DebugLog(string module, string message, UnityEngine.Object context = null)

        {
            string formatted = $"[{module}] 🐞 {message}";

            if (context != null)
                Debug.Log(formatted, context);
            else
                Debug.Log(formatted);
        }
    }
}