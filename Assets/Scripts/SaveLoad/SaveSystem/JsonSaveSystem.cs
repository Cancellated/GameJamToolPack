using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Logger;

namespace MyGame.Data
{
    /// <summary>
    /// 基于JSON文件的存档系统实现。
    /// 提供游戏数据的JSON序列化和文件存储功能。
    ///
    /// 健壮性策略：
    ///  1. 写入：临时文件 + File.Replace（支持时）原子替换；不支持时降级为 Copy+Delete，并保留 .bak 备份。
    ///  2. 读取：主存档损坏时自动隔离为 .corrupt 文件，并尝试从 .bak 恢复。
    ///  3. 版本：dataVersion 负责结构迁移，version 负责游戏发布版本提示。
    /// </summary>
    public class JsonSaveSystem : ISaveSystem
    {
        private const string LOG_MODULE = LogModules.SAVESYSTEM;
        private const string SAVE_FILE_EXTENSION = ".json";
        private const string BACKUP_FILE_EXTENSION = ".json.bak";
        private const string TEMP_FILE_EXTENSION = ".json.tmp";
        private const string CORRUPT_FILE_SUFFIX = ".corrupt";
        private const string SAVE_FOLDER_NAME = "Saves";
        private const int MAX_SANITIZED_FILE_NAME_LENGTH = 180;

        private static readonly HashSet<string> s_windowsReservedNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
            };

        private readonly string m_saveDirectoryPath;

        /// <summary>
        /// 初始化JSON存档系统。
        /// </summary>
        public JsonSaveSystem()
        {
            // 使用Unity持久化数据路径作为存档根目录
            m_saveDirectoryPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER_NAME);
            EnsureDirectoryExists();
        }

        #region 路径与文件名处理

        /// <summary>
        /// 获取指定存档槽的完整文件路径。
        /// </summary>
        private string GetSaveFilePath(string slotName)
        {
            if (string.IsNullOrEmpty(slotName))
            {
                throw new ArgumentException("存档名称不能为空", nameof(slotName));
            }

            string sanitizedName = SanitizeFileName(slotName);
            return Path.Combine(m_saveDirectoryPath, sanitizedName + SAVE_FILE_EXTENSION);
        }

        private string GetBackupFilePath(string filePath)
        {
            return filePath + ".bak";
        }

        private string GetTempFilePath(string filePath)
        {
            return filePath + ".tmp";
        }

        /// <summary>
        /// 清理文件名，移除非法字符并处理跨平台边界情况：
        /// - 非法字符编码为其 Unicode 码点（_xxxx_），避免不同槽名清洗后碰撞；
        /// - 结尾的 '.' / 空格在 Windows 上会导致文件名不可用，编码为 _2e_ / _20_；
        /// - Windows 保留设备名（CON/NUL/COM1 等）加前缀避免创建失败；
        /// - 超长文件名截断后追加稳定哈希，避免碰撞。
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder sb = new(fileName.Length + 8);
            foreach (char c in fileName)
            {
                if (Array.IndexOf(invalidChars, c) >= 0)
                {
                    sb.Append('_').Append(((int)c).ToString("x4")).Append('_');
                }
                else
                {
                    sb.Append(c);
                }
            }

            string sanitized = sb.ToString();

            // 编码结尾的点与空格（Windows 不允许）
            int trailingCount = 0;
            for (int i = sanitized.Length - 1; i >= 0; i--)
            {
                if (sanitized[i] == '.')
                {
                    trailingCount++;
                }
                else if (sanitized[i] == ' ')
                {
                    trailingCount++;
                }
                else
                {
                    break;
                }
            }

            if (trailingCount > 0)
            {
                StringBuilder trailingEncoded = new(trailingCount * 4);
                for (int i = sanitized.Length - trailingCount; i < sanitized.Length; i++)
                {
                    trailingEncoded.Append(sanitized[i] == '.'
                        ? "_2e_"
                        : "_20_");
                }
                sanitized = sanitized.Substring(0, sanitized.Length - trailingCount) + trailingEncoded;
            }

            if (string.IsNullOrEmpty(sanitized))
            {
                sanitized = "_empty_";
            }

            // Windows 保留设备名
            string stem = sanitized;
            int firstDot = sanitized.IndexOf('.');
            if (firstDot > 0)
            {
                stem = sanitized.Substring(0, firstDot);
            }
            if (s_windowsReservedNames.Contains(stem))
            {
                sanitized = "_" + sanitized + "_";
            }

            // 超长文件名：截断 + 稳定哈希
            if (sanitized.Length > MAX_SANITIZED_FILE_NAME_LENGTH)
            {
                sanitized = TruncateWithStableHash(sanitized, MAX_SANITIZED_FILE_NAME_LENGTH);
            }

            return sanitized;
        }

        private static string TruncateWithStableHash(string value, int keepLength)
        {
            if (value.Length <= keepLength)
            {
                return value;
            }

            ulong hash = 14695981039346656037UL; // FNV-1a 64
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            foreach (byte b in bytes)
            {
                hash ^= b;
                hash *= 1099511628211UL;
            }

            return value.Substring(0, keepLength) + "_" + hash.ToString("x16");
        }

        #endregion

        #region 保存

        /// <summary>
        /// 同步保存游戏数据到文件。
        /// 写入流程：写临时文件 → 备份旧文件 → 替换/拷贝到正式文件。
        /// </summary>
        /// <returns>保存操作是否成功。</returns>
        public bool SaveGame(SaveData saveData, string slotName)
        {
            string filePath = null;
            string tempPath = null;

            try
            {
                if (saveData == null)
                {
                    Log.Error(LOG_MODULE, "保存失败：存档数据为空");
                    return false;
                }

                EnsureDirectoryExists();

                // 更新存档元数据
                saveData.SetSaveTimeMetadata();
                saveData.version = Application.version;
                saveData.dataVersion = SaveData.CURRENT_DATA_VERSION;
                saveData.EnsureInitialized();

                // 序列化数据为JSON
                string jsonData = JsonUtility.ToJson(saveData, true); // true表示格式化输出

                filePath = GetSaveFilePath(slotName);
                tempPath = GetTempFilePath(filePath);

                // 清理可能残留的旧临时文件
                TryDeleteFile(tempPath);

                File.WriteAllText(tempPath, jsonData, Encoding.UTF8);

                // 原子写入：优先 File.Replace（可同时生成备份），失败时降级 Copy+Delete
                CommitSave(tempPath, filePath);

                // 保存成功后清理该槽位的历史损坏文件，避免目录堆积
                CleanupCorruptFiles(filePath);

                Log.Info(LOG_MODULE, $"游戏数据已保存到: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(LOG_MODULE, $"保存游戏失败: {ex.Message}");
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    TryDeleteFile(tempPath);
                }
                return false;
            }
        }

        /// <summary>
        /// 将临时文件提交为正式存档。
        /// 目标已存在时：先备份旧文件，再原子替换；平台不支持 File.Replace 时降级为 Copy+Delete。
        /// 目标不存在时：优先 Move，失败则 Copy+Delete。
        /// </summary>
        private void CommitSave(string tempPath, string filePath)
        {
            string backupPath = GetBackupFilePath(filePath);

            if (File.Exists(filePath))
            {
                TryDeleteFile(backupPath);

                try
                {
                    // 第三个参数让系统在替换的同时生成 .bak 备份
                    File.Replace(tempPath, filePath, backupPath);
                    return;
                }
                catch (Exception replaceException)
                {
                    Log.Warning(LOG_MODULE,
                        $"File.Replace 不可用或执行失败，降级为 Copy+Delete 提交: {replaceException.Message}");
                }

                // 降级路径：旧文件 → 备份，临时文件 → 正式文件
                File.Copy(filePath, backupPath, true);
                File.Copy(tempPath, filePath, true);
                TryDeleteFile(tempPath);
                return;
            }

            try
            {
                File.Move(tempPath, filePath);
            }
            catch (Exception moveException)
            {
                Log.Warning(LOG_MODULE, $"File.Move 不可用，降级为 Copy+Delete 提交: {moveException.Message}");
                File.Copy(tempPath, filePath, false);
                TryDeleteFile(tempPath);
            }
        }

        #endregion

        #region 加载

        /// <summary>
        /// 同步加载游戏数据。
        /// 主存档损坏时自动隔离并尝试从 .bak 恢复。
        /// </summary>
        /// <returns>加载的游戏数据，如果失败则返回null。</returns>
        public SaveData LoadGame(string slotName)
        {
            try
            {
                string filePath = GetSaveFilePath(slotName);

                SaveData saveData = LoadMainOrRecoverFromBackup(filePath);
                if (saveData == null)
                {
                    return null;
                }

                // 结构迁移：dataVersion 负责字段补齐/迁移，version 负责游戏发布版本提示
                MigrateSaveData(saveData);

                if (!string.IsNullOrEmpty(saveData.version) && saveData.version != Application.version)
                {
                    Log.Warning(LOG_MODULE,
                        $"存档游戏版本({saveData.version})与当前游戏版本({Application.version})不一致，" +
                        "缺失字段已按默认值处理");
                }

                Log.Info(LOG_MODULE, $"游戏数据已从: {filePath} 加载");
                return saveData;
            }
            catch (Exception ex)
            {
                Log.Error(LOG_MODULE, $"加载游戏失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 优先读取主存档；主存档缺失/损坏时尝试从备份恢复。
        /// </summary>
        private SaveData LoadMainOrRecoverFromBackup(string filePath)
        {
            string backupPath = GetBackupFilePath(filePath);

            if (!File.Exists(filePath))
            {
                if (!File.Exists(backupPath))
                {
                    Log.Warning(LOG_MODULE, $"存档文件不存在: {filePath}");
                    return null;
                }

                Log.Warning(LOG_MODULE, $"主存档缺失，尝试从备份恢复: {backupPath}");
                return TryRestoreFromBackup(backupPath, filePath);
            }

            SaveData mainData = TryReadAndParse(filePath);
            if (mainData != null)
            {
                return mainData;
            }

            // 主存档损坏：隔离坏文件，防止被 UI 当成"空槽"直接覆盖
            string corruptPath = QuarantineCorruptFile(filePath);
            Log.Error(LOG_MODULE, $"主存档损坏，已隔离到: {corruptPath}");

            if (File.Exists(backupPath))
            {
                Log.Warning(LOG_MODULE, $"尝试从备份恢复存档: {backupPath}");
                return TryRestoreFromBackup(backupPath, filePath);
            }

            return null;
        }

        private SaveData TryRestoreFromBackup(string backupPath, string filePath)
        {
            SaveData backupData = TryReadAndParse(backupPath);
            if (backupData == null)
            {
                Log.Error(LOG_MODULE, $"备份文件同样损坏或无法解析: {backupPath}");
                return null;
            }

            try
            {
                File.Copy(backupPath, filePath, true);
                Log.Info(LOG_MODULE, $"已从备份恢复主存档: {filePath}");
            }
            catch (Exception ex)
            {
                // 恢复主文件失败但备份仍可读，继续返回数据，下次保存会重新写入主文件
                Log.Error(LOG_MODULE, $"从备份恢复主存档失败（数据仍已加载）: {ex.Message}");
            }

            return backupData;
        }

        /// <summary>
        /// 读取并反序列化指定文件；任何异常都返回 null，由调用方决定隔离/备份策略。
        /// </summary>
        private SaveData TryReadAndParse(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                string jsonData = File.ReadAllText(filePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    Log.Warning(LOG_MODULE, $"存档文件为空: {filePath}");
                    return null;
                }

                // JsonUtility.FromJson 对部分损坏 JSON 不抛异常而返回 null，必须显式判空
                SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);
                if (saveData == null)
                {
                    Log.Error(LOG_MODULE, $"存档文件损坏或格式不兼容，无法解析: {filePath}");
                    return null;
                }

                return saveData;
            }
            catch (Exception ex)
            {
                Log.Warning(LOG_MODULE, $"读取存档失败: {filePath}, 原因: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将损坏的主存档移动到带时间戳的 .corrupt 文件，避免被当作空槽覆盖。
        /// </summary>
        private string QuarantineCorruptFile(string filePath)
        {
            string corruptPath = filePath + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + CORRUPT_FILE_SUFFIX;

            try
            {
                if (File.Exists(filePath))
                {
                    File.Move(filePath, corruptPath);
                }
            }
            catch (Exception moveException)
            {
                Log.Warning(LOG_MODULE, $"隔离损坏存档失败，降级为 Copy+Delete: {moveException.Message}");
                File.Copy(filePath, corruptPath, true);
                TryDeleteFile(filePath);
            }

            return corruptPath;
        }

        /// <summary>
        /// 按存档结构版本执行归一化。
        /// 已废弃的 v1（内嵌 gameProgress）结构不再支持，该版本存档无需保留；
        /// 这里只负责把低于当前版本但仍可解析的存档补齐必要字段并归一化字符串。
        /// </summary>
        private void MigrateSaveData(SaveData saveData)
        {
            int sourceVersion = saveData.dataVersion;

            if (sourceVersion < SaveData.CURRENT_DATA_VERSION)
            {
                Log.Info(LOG_MODULE,
                    $"迁移存档结构：dataVersion {sourceVersion} -> {SaveData.CURRENT_DATA_VERSION}");
                if (string.IsNullOrEmpty(saveData.saveTimeUtc))
                {
                    saveData.saveTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }
                saveData.dataVersion = SaveData.CURRENT_DATA_VERSION;
                sourceVersion = SaveData.CURRENT_DATA_VERSION;
            }

            if (sourceVersion > SaveData.CURRENT_DATA_VERSION)
            {
                Log.Warning(LOG_MODULE,
                    $"存档结构版本({sourceVersion})高于当前支持版本({SaveData.CURRENT_DATA_VERSION})，" +
                    "将尽量保留数据加载，未识别字段可能丢失");
            }

            saveData.EnsureInitialized();
        }

        #endregion

        #region 删除与查询

        /// <summary>
        /// 删除指定存档及其所有关联文件（主文件、备份、隔离文件、临时文件）。
        /// </summary>
        public bool DeleteGame(string slotName)
        {
            try
            {
                string filePath = GetSaveFilePath(slotName);
                string fileBase = Path.GetFileName(filePath);

                // 删除主文件、.bak、.corrupt、.tmp 等所有关联文件
                bool deletedAny = false;
                string[] relatedFiles = Directory.GetFiles(m_saveDirectoryPath, fileBase + "*");
                foreach (string relatedFile in relatedFiles)
                {
                    if (TryDeleteFile(relatedFile))
                    {
                        deletedAny = true;
                    }
                }

                if (!deletedAny)
                {
                    Log.Warning(LOG_MODULE, $"要删除的存档文件不存在: {filePath}");
                    return false;
                }

                Log.Info(LOG_MODULE, $"存档已删除: {slotName}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(LOG_MODULE, $"删除存档失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查指定存档是否存在（主文件或可恢复的备份文件均视为存在）。
        /// </summary>
        public bool DoesSaveExist(string slotName)
        {
            try
            {
                string filePath = GetSaveFilePath(slotName);
                return File.Exists(filePath) || File.Exists(GetBackupFilePath(filePath));
            }
            catch (Exception ex)
            {
                Log.Error(LOG_MODULE, $"检查存档存在性失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取指定存档槽文件的最后写入时间（UTC）；存档不存在时返回 DateTime.MinValue。
        /// 主文件缺失但备份存在时，以备份时间参与 UI 缓存判断。
        /// </summary>
        public DateTime GetSaveLastWriteTime(string slotName)
        {
            try
            {
                string filePath = GetSaveFilePath(slotName);
                DateTime mainTime = File.Exists(filePath)
                    ? File.GetLastWriteTimeUtc(filePath)
                    : DateTime.MinValue;

                string backupPath = GetBackupFilePath(filePath);
                DateTime backupTime = File.Exists(backupPath)
                    ? File.GetLastWriteTimeUtc(backupPath)
                    : DateTime.MinValue;

                return mainTime > backupTime ? mainTime : backupTime;
            }
            catch (Exception ex)
            {
                Log.Error(LOG_MODULE, $"获取存档写入时间失败: {ex.Message}");
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// 获取所有可用的存档槽列表（主文件与可恢复备份文件都会计入，去重）。
        /// </summary>
        public List<string> GetAvailableSaves()
        {
            List<string> saveSlots = new();

            try
            {
                if (!Directory.Exists(m_saveDirectoryPath))
                {
                    return saveSlots;
                }

                HashSet<string> uniqueSlots = new(StringComparer.OrdinalIgnoreCase);

                foreach (string file in Directory.GetFiles(m_saveDirectoryPath, "*" + SAVE_FILE_EXTENSION))
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.EndsWith(BACKUP_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // 备份文件单独处理，避免重复统计
                    }
                    uniqueSlots.Add(Path.GetFileNameWithoutExtension(file));
                }

                foreach (string file in Directory.GetFiles(m_saveDirectoryPath, "*" + BACKUP_FILE_EXTENSION))
                {
                    // save_1.json.bak -> save_1.json -> save_1
                    string withoutBackup = Path.GetFileNameWithoutExtension(file);
                    uniqueSlots.Add(Path.GetFileNameWithoutExtension(withoutBackup));
                }

                saveSlots.AddRange(uniqueSlots);
            }
            catch (Exception ex)
            {
                Log.Error(LOG_MODULE, $"获取可用存档列表失败: {ex.Message}");
            }

            return saveSlots;
        }

        #endregion

        #region 工具方法

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(m_saveDirectoryPath))
            {
                Directory.CreateDirectory(m_saveDirectoryPath);
                Log.Info(LOG_MODULE, $"创建存档文件夹: {m_saveDirectoryPath}");
            }
        }

        private static bool TryDeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(LOG_MODULE, $"删除文件失败: {filePath}, 原因: {ex.Message}");
                return false;
            }
        }

        private void CleanupCorruptFiles(string filePath)
        {
            try
            {
                string fileBase = Path.GetFileName(filePath);
                foreach (string corruptFile in Directory.GetFiles(m_saveDirectoryPath, fileBase + "*" + CORRUPT_FILE_SUFFIX))
                {
                    TryDeleteFile(corruptFile);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(LOG_MODULE, $"清理损坏存档文件失败: {ex.Message}");
            }
        }

        #endregion
    }
}
