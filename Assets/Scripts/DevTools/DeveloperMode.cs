using System;
using System.IO;
using UnityEngine;

namespace MyGame.DevTools
{
    /// <summary>
    /// 开发者模式开关（对 modder 友好）。
    ///
    /// 调试控制台等开发者功能在编辑器 / Development Build 中默认可用；
    /// 正式发布版默认关闭，但可以通过以下任一方式显式开启，方便 mod 开发者调试：
    ///
    /// 1. 启动参数（任选其一）：
    ///    -devMode
    ///    -developerMode
    ///    -devConsole
    ///    -enableDevConsole
    ///
    /// 2. 在 Application.persistentDataPath 下放置 dev_config.json：
    ///    {
    ///        "developerModeEnabled": true,   // 开启全部开发者功能（含控制台）
    ///        "debugConsoleEnabled": true     // 只开启调试控制台
    ///    }
    ///
    /// 3. mod 代码调用：
    ///    DeveloperMode.SetDeveloperModeEnabled(true);   // 开启开发者模式并持久化
    ///    DeveloperMode.SetDebugConsoleEnabled(true);    // 只开启控制台并持久化
    ///
    /// 4. 游戏内设置面板中的“开发者模式（保存后生效）”开关；
    ///
    /// 5. 控制台内执行 devmode on / devmode off（写入 PlayerPrefs，跨会话生效）。
    ///
    /// 优先级：编辑器/Development Build 恒可用；发布版以 PlayerPrefs、
    /// 启动参数、dev_config.json 任一命中为准。
    /// </summary>
    public static class DeveloperMode
    {
        public const string DEVELOPER_MODE_PLAYERPREFS_KEY = "dev.mode.enabled";
        public const string DEBUG_CONSOLE_PLAYERPREFS_KEY = "dev.debugConsole.enabled";

        private const string CONFIG_FILE_NAME = "dev_config.json";

        private static bool s_initialized;
        private static bool s_developerModeEnabled;
        private static bool s_debugConsoleEnabled;

        /// <summary>
        /// 是否开启开发者模式。
        /// 编辑器与 Development Build 恒为 true；发布版由 PlayerPrefs / 启动参数 / dev_config.json 决定。
        /// </summary>
        public static bool IsDeveloperModeEnabled
        {
            get
            {
                if (Debug.isDebugBuild)
                {
                    return true;
                }

                EnsureInitialized();
                return s_developerModeEnabled;
            }
        }

        /// <summary>
        /// 已保存的开发者模式状态（不含编辑器/Debug 构建的自动开启）。
        /// 供设置面板读取/回滚使用。
        /// </summary>
        public static bool GetSavedDeveloperModeEnabled()
        {
            EnsureInitialized();
            return s_developerModeEnabled;
        }

        /// <summary>
        /// 调试控制台是否可用。
        /// 编辑器与 Development Build 恒为 true；发布版需开启开发者模式或单独开启控制台开关。
        /// </summary>
        public static bool IsDebugConsoleEnabled
        {
            get
            {
                if (Debug.isDebugBuild)
                {
                    return true;
                }

                EnsureInitialized();
                return s_developerModeEnabled || s_debugConsoleEnabled;
            }
        }

        /// <summary>
        /// 设置开发者模式开关并持久化到 PlayerPrefs。
        /// 供 mod 代码、控制台命令或设置面板“保存”流程调用。
        /// </summary>
        public static void SetDeveloperModeEnabled(bool enabled)
        {
            s_developerModeEnabled = enabled;
            PersistFlag(DEVELOPER_MODE_PLAYERPREFS_KEY, enabled);
        }

        /// <summary>
        /// 设置调试控制台开关并持久化到 PlayerPrefs（不开启其他开发者功能）。
        /// </summary>
        public static void SetDebugConsoleEnabled(bool enabled)
        {
            s_debugConsoleEnabled = enabled;
            PersistFlag(DEBUG_CONSOLE_PLAYERPREFS_KEY, enabled);
        }

        /// <summary>
        /// 丢弃内存缓存并重新读取 PlayerPrefs / 启动参数 / dev_config.json。
        /// mod 在运行中修改 dev_config.json 后调用本方法即可热刷新。
        /// </summary>
        public static void Reload()
        {
            s_initialized = false;
            EnsureInitialized();
        }

        private static void PersistFlag(string key, bool enabled)
        {
            try
            {
                PlayerPrefs.SetInt(key, enabled ? 1 : 0);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DeveloperMode] 保存开关失败: {ex.Message}");
            }
        }

        private static void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            s_initialized = true;

            try
            {
                s_developerModeEnabled = PlayerPrefs.GetInt(DEVELOPER_MODE_PLAYERPREFS_KEY, 0) == 1;
                s_debugConsoleEnabled = PlayerPrefs.GetInt(DEBUG_CONSOLE_PLAYERPREFS_KEY, 0) == 1;

                if (HasCommandLineArg("-devMode", "-developerMode"))
                {
                    s_developerModeEnabled = true;
                }

                if (HasCommandLineArg("-devConsole", "-enableDevConsole"))
                {
                    s_debugConsoleEnabled = true;
                }

                LoadConfigFile();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DeveloperMode] 初始化失败，保持默认关闭: {ex.Message}");
            }
        }

        private static void LoadConfigFile()
        {
            try
            {
                string configPath = Path.Combine(Application.persistentDataPath, CONFIG_FILE_NAME);
                if (!File.Exists(configPath))
                {
                    return;
                }

                string json = File.ReadAllText(configPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                DevConfigFile config = JsonUtility.FromJson<DevConfigFile>(json);
                if (config == null)
                {
                    Debug.LogWarning($"[DeveloperMode] dev_config.json 格式无效，已忽略: {configPath}");
                    return;
                }

                if (config.developerModeEnabled)
                {
                    s_developerModeEnabled = true;
                }

                if (config.debugConsoleEnabled)
                {
                    s_debugConsoleEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DeveloperMode] 读取 dev_config.json 失败: {ex.Message}");
            }
        }

        private static bool HasCommandLineArg(params string[] candidates)
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            foreach (string arg in commandLineArgs)
            {
                foreach (string candidate in candidates)
                {
                    if (string.Equals(arg, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// dev_config.json 的结构。两个开关任一为 true 都会启用调试控制台。
    /// </summary>
    [Serializable]
    public sealed class DevConfigFile
    {
        public bool developerModeEnabled;
        public bool debugConsoleEnabled;
    }
}
