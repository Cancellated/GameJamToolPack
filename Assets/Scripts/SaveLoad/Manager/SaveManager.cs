using MyGame.Events;
using MyGame.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Logger;

namespace MyGame.Data
{
    /// <summary>
    /// 游戏存档管理器，负责处理游戏数据的保存、加载和删除操作。
    /// 作为单例类提供全局访问点，并与游戏事件系统集成。
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        private const string LOG_MODULE = LogModules.SAVEMANAGER;

        /// <summary>
        /// 默认存档槽（自动存档）名称。
        /// 注意：必须与存档菜单的自动存档槽（SaveLoadMenuConstants.AUTO_SAVE_SLOT）保持一致，
        /// 否则游戏结束等自动保存的存档在菜单中不可见、无法加载。
        /// </summary>
        public const string DEFAULT_SAVE_SLOT = "auto_save";
        
        private ISaveSystem m_saveSystem;
        private SaveData m_currentSaveData;

        /// <summary>读档后待应用进度的标记：等待游戏场景加载完成后广播进度加载事件。</summary>
        private bool m_pendingProgressToApply;

        [Header("自动存档")]
        [SerializeField, Tooltip("是否启用定时自动存档（仅游戏进行中生效）")]
        private bool m_enableAutoSave = true;

        [SerializeField, Tooltip("自动存档间隔（秒）")]
        private float m_autoSaveInterval = 300f;

        /// <summary>自动存档计时器（秒）</summary>
        private float m_autoSaveTimer;
        
        #region 属性
        
        /// <summary>
        /// 当前加载的存档数据。
        /// </summary>
        public SaveData CurrentSaveData
        {
            get { return m_currentSaveData; }
        }

        /// <summary>
        /// 当前存档中的游戏进度（只读入口）。
        /// 游戏玩法系统如需主动写入进度，请使用本类提供的 UpdateXxx 进度 API。
        /// </summary>
        public GameProgress CurrentGameProgress
        {
            get { return m_currentSaveData != null ? m_currentSaveData.gameProgress : null; }
        }
        
        /// <summary>
        /// 获取所有可用的存档槽名称。
        /// </summary>
        public List<string> AvailableSaves
        {
            get { return m_saveSystem?.GetAvailableSaves() ?? new List<string>(); }
        }
        
        #endregion

        #region 进度写入 API（游戏玩法系统与存档数据的唯一桥梁）

        /// <summary>
        /// 获取当前可写的游戏进度对象；尚未初始化时会补齐默认进度。
        /// </summary>
        public GameProgress GetOrCreateGameProgress()
        {
            EnsureCurrentSaveData();
            if (m_currentSaveData.gameProgress == null)
            {
                m_currentSaveData.gameProgress = new GameProgress();
            }
            m_currentSaveData.gameProgress.EnsureInitialized();
            return m_currentSaveData.gameProgress;
        }

        /// <summary>
        /// 更新玩家位置。
        /// 该值可能每帧变化，因此默认不广播进度变更事件。
        /// </summary>
        public void UpdatePlayerPosition(Vector3 position, bool notify = false)
        {
            GetOrCreateGameProgress().UpdatePlayerPosition(position);
            if (notify)
            {
                NotifyProgressChanged();
            }
        }

        /// <summary>
        /// 更新当前关卡并广播进度变更。
        /// </summary>
        public void UpdateCurrentLevel(int level)
        {
            GetOrCreateGameProgress().UpdateCurrentLevel(level);
            NotifyProgressChanged();
        }

        /// <summary>
        /// 标记关卡完成并广播进度变更。
        /// </summary>
        public void MarkLevelCompleted(int levelId)
        {
            GetOrCreateGameProgress().MarkLevelAsCompleted(levelId);
            NotifyProgressChanged();
        }

        /// <summary>
        /// 更新任务进度并广播进度变更。
        /// </summary>
        public void UpdateQuestProgress(string questId, int step)
        {
            GetOrCreateGameProgress().UpdateQuestProgress(questId, step);
            NotifyProgressChanged();
        }

        /// <summary>
        /// 更新玩家统计并广播进度变更。
        /// </summary>
        public void SetPlayerStat(string statName, int value)
        {
            GetOrCreateGameProgress().SetPlayerStat(statName, value);
            NotifyProgressChanged();
        }

        /// <summary>
        /// 整体替换当前游戏进度（例如场景初始化器从关卡数据构建进度）并广播变更。
        /// </summary>
        public void ReplaceGameProgress(GameProgress progress)
        {
            if (progress == null)
            {
                Log.Error(LOG_MODULE, "替换游戏进度失败：进度为空");
                return;
            }

            EnsureCurrentSaveData();
            m_currentSaveData.gameProgress = progress;
            m_currentSaveData.gameProgress.EnsureInitialized();
            NotifyProgressChanged();
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化存档管理器。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            
            // 初始化存档系统实现（使用JSON文件存储）
            m_saveSystem = new JsonSaveSystem();
            
            // 初始化当前存档数据为默认值
            m_currentSaveData = new SaveData();
            
            Log.Info(LOG_MODULE, "存档管理器已初始化");
        }

        /// <summary>
        /// 懒创建回调：SaveManager 本应显式挂载在场景 Managers 根对象上。
        /// 此处记录明确错误日志，避免配置被静默应用默认值而不自知。
        /// </summary>
        protected override void OnLazyInstanceCreated()
        {
            Log.Error(LOG_MODULE,
                "SaveManager 未在场景 Managers 上显式挂载，已由 Singleton 自动补挂。" +
                "自动创建会使用代码默认配置，请在 MainMenu 场景的 Managers 对象上手动挂载 SaveManager 组件");
        }
        
        private void OnEnable()
        {
            // 注册游戏事件监听器
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnCreateNewGame += HandleCreateNewGame;
            GameEvents.OnGameRestart += HandleGameRestart;
            GameEvents.OnSaveGame += HandleSaveGame;
            GameEvents.OnLoadGame += HandleLoadGame;
            GameEvents.OnDeleteSave += HandleDeleteSave;
            GameEvents.OnAutoSave += HandleAutoSave;
            GameEvents.OnSceneLoadComplete += HandleSceneLoadComplete;
        }
        
        private void OnDisable()
        {
            // 注销游戏事件监听器
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnCreateNewGame -= HandleCreateNewGame;
            GameEvents.OnGameRestart -= HandleGameRestart;
            GameEvents.OnSaveGame -= HandleSaveGame;
            GameEvents.OnLoadGame -= HandleLoadGame;
            GameEvents.OnDeleteSave -= HandleDeleteSave;
            GameEvents.OnAutoSave -= HandleAutoSave;
            GameEvents.OnSceneLoadComplete -= HandleSceneLoadComplete;
        }

        /// <summary>
        /// 定时自动存档：仅游戏进行中累积计时并触发，
        /// 非游戏状态（菜单/结算/暂停）计时清零，避免进入游戏后立即触发一次存档
        /// </summary>
        private void Update()
        {
            if (!m_enableAutoSave || m_autoSaveInterval <= 0f)
            {
                return;
            }

            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                m_autoSaveTimer = 0f;
                return;
            }

            m_autoSaveTimer += Time.unscaledDeltaTime;
            if (m_autoSaveTimer >= m_autoSaveInterval)
            {
                m_autoSaveTimer = 0f;
                Log.Info(LOG_MODULE, "定时自动存档触发");
                SaveCurrentGame(DEFAULT_SAVE_SLOT);
            }
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 加载存档数据但不应用游戏设置，仅用于预览存档信息。
        /// 包含延迟初始化逻辑，确保在第一次访问时初始化存档系统。
        /// </summary>
        /// <param name="slotName">存档槽名称，如果为空则使用默认存档槽。</param>
        /// <returns>加载的存档数据，如果失败则返回null。</returns>
        public SaveData LoadSaveData(string slotName = null)
        {
            // 延迟初始化存档系统
            if (m_saveSystem == null)
            {
                InitializeSaveSystem();
            }
            
            // 使用默认存档槽如果未指定
            string saveSlot = string.IsNullOrEmpty(slotName) ? DEFAULT_SAVE_SLOT : slotName;
            
            Log.Info(LOG_MODULE, $"开始加载存档数据: {saveSlot}");
            
            // 直接从存档系统加载数据但不更新当前游戏数据
            SaveData loadedData = m_saveSystem.LoadGame(saveSlot);
            
            if (loadedData != null)
            {
                NormalizeLoadedSaveData(loadedData);
                Log.Info(LOG_MODULE, "存档数据加载成功");
            }
            else
            {
                Log.Warning(LOG_MODULE, "存档数据加载失败或存档不存在");
            }
            
            return loadedData;
        }
        
        /// <summary>
        /// 保存当前游戏数据到指定存档槽。
        /// 包含延迟初始化逻辑，确保在第一次访问时初始化存档系统。
        /// </summary>
        /// <param name="slotName">存档槽名称，如果为空则使用默认存档槽。</param>
        /// <returns>保存操作是否成功。</returns>
        public bool SaveCurrentGame(string slotName = null)
        {
            // 延迟初始化存档系统
            if (m_saveSystem == null)
            {
                InitializeSaveSystem();
            }
            
            // 使用默认存档槽如果未指定
            string saveSlot = string.IsNullOrEmpty(slotName) ? DEFAULT_SAVE_SLOT : slotName;
            
            Log.Info(LOG_MODULE, $"开始保存游戏到存档槽: {saveSlot}");

            // 确保当前存档与进度对象已初始化，避免空引用或写入空进度
            EnsureCurrentSaveData();
            if (m_currentSaveData.gameProgress == null)
            {
                m_currentSaveData.gameProgress = new GameProgress();
            }
            m_currentSaveData.gameProgress.EnsureInitialized();

            // 保存前同步当前设置：运行时设置统一持久化在 PlayerPrefs（由 SettingsModel 写入），
            // 这里从 PlayerPrefs 读取同步进存档，保证存档中的设置与玩家当前设置一致
            // （修复：此前存档里的设置字段永远是默认值，读档时会覆盖玩家设置）
            GameSettings currentSettings = new GameSettings();
            currentSettings.LoadFromPlayerPrefs();
            m_currentSaveData.UpdateSettings(currentSettings);

            // 保存数据
            bool success = m_saveSystem.SaveGame(m_currentSaveData, saveSlot);
            
            if (success)
            {
                Log.Info(LOG_MODULE, "游戏保存成功");
            }
            else
            {
                Log.Error(LOG_MODULE, "游戏保存失败");
            }
            
            return success;
        }
        
        
        /// <summary>
        /// 从指定存档槽加载游戏数据。
        /// 包含延迟初始化逻辑，确保在第一次访问时初始化存档系统。
        /// </summary>
        /// <param name="slotName">存档槽名称，如果为空则使用默认存档槽。</param>
        /// <returns>加载操作是否成功。</returns>
        public bool LoadGame(string slotName = null)
        {
            // 清理上一次可能残留的待应用标记，避免场景加载失败后误广播
            m_pendingProgressToApply = false;

            // 延迟初始化存档系统
            if (m_saveSystem == null)
            {
                InitializeSaveSystem();
            }
            
            // 使用默认存档槽如果未指定
            string saveSlot = string.IsNullOrEmpty(slotName) ? DEFAULT_SAVE_SLOT : slotName;
            
            Log.Info(LOG_MODULE, $"开始加载游戏存档: {saveSlot}");
            
            // 加载数据
            SaveData loadedData = m_saveSystem.LoadGame(saveSlot);
            
            // 检查加载结果
            if (loadedData != null)
            {
                // 更新当前存档数据
                m_currentSaveData = loadedData;
                NormalizeLoadedSaveData(m_currentSaveData);

                // 应用加载的设置
                ApplyLoadedSettings();

                // 若读档发生在主菜单（随后会切场景），先把进度标记为待应用，
                // 待游戏场景加载完成后再广播；若已在游戏场景内，则立即广播
                bool willLoadGameScene = GameManager.Instance != null
                                         && GameManager.Instance.State == GameState.Menu;
                if (willLoadGameScene)
                {
                    m_pendingProgressToApply = true;
                }
                else
                {
                    GameEvents.TriggerGameProgressLoaded(m_currentSaveData.gameProgress);
                }

                Log.Info(LOG_MODULE, "游戏加载成功");
                return true;
            }
            else
            {
                Log.Error(LOG_MODULE, "游戏加载失败或存档不存在");
                return false;
            }
        }
        
        
        /// <summary>
        /// 删除指定存档槽的游戏数据。
        /// 包含延迟初始化逻辑，确保在第一次访问时初始化存档系统。
        /// </summary>
        /// <param name="slotName">存档槽名称，如果为空则使用默认存档槽。</param>
        /// <returns>删除操作是否成功。</returns>
        public bool DeleteSave(string slotName = null)
        {
            // 延迟初始化存档系统
            if (m_saveSystem == null)
            {
                InitializeSaveSystem();
            }
            
            // 使用默认存档槽如果未指定
            string saveSlot = string.IsNullOrEmpty(slotName) ? DEFAULT_SAVE_SLOT : slotName;
            
            Log.Info(LOG_MODULE, $"开始删除游戏存档: {saveSlot}");
            
            // 删除存档
            bool success = m_saveSystem.DeleteGame(saveSlot);
            
            // 触发删除事件
            if (success)
            {
                Log.Info(LOG_MODULE, "存档删除成功");
            }
            else
            {
                Log.Error(LOG_MODULE, "存档删除失败");
            }
            
            return success;
        }
        
        /// <summary>
        /// 检查指定存档槽是否存在游戏数据。
        /// 包含延迟初始化逻辑，确保在第一次访问时初始化存档系统。
        /// </summary>
        /// <param name="slotName">存档槽名称，如果为空则使用默认存档槽。</param>
        /// <returns>存档是否存在。</returns>
        public bool DoesSaveExist(string slotName = null)
        {
            // 延迟初始化存档系统（防止Awake顺序问题导致的未初始化错误）
            if (m_saveSystem == null)
            {
                InitializeSaveSystem();
            }
            
            // 使用默认存档槽如果未指定
            string saveSlot = string.IsNullOrEmpty(slotName) ? DEFAULT_SAVE_SLOT : slotName;
            
            return m_saveSystem.DoesSaveExist(saveSlot);
        }

        /// <summary>
        /// 获取指定槽位存档文件的最后写入时间（UTC）；存档不存在时返回 DateTime.MinValue。
        /// 仅做文件状态查询（廉价），供 UI 层做缓存刷新判断，
        /// 避免每次打开存档菜单都全量读取全部存档文件。
        /// </summary>
        /// <param name="slotName">存档槽名称，如果为空则使用默认存档槽。</param>
        public DateTime GetSaveLastWriteTime(string slotName = null)
        {
            // 延迟初始化存档系统
            if (m_saveSystem == null)
            {
                InitializeSaveSystem();
            }

            // 使用默认存档槽如果未指定
            string saveSlot = string.IsNullOrEmpty(slotName) ? DEFAULT_SAVE_SLOT : slotName;

            return m_saveSystem.GetSaveLastWriteTime(saveSlot);
        }
        
        /// <summary>
        /// 初始化存档系统实现
        /// 确保与Awake方法中的初始化逻辑保持一致
        /// </summary>
        private void InitializeSaveSystem()
        {
            if (m_saveSystem == null)
            {
                m_saveSystem = new JsonSaveSystem();
                
                // 确保当前存档数据已初始化
                m_currentSaveData ??= new SaveData();
                m_currentSaveData.gameProgress ??= new GameProgress();
                m_currentSaveData.gameProgress.EnsureInitialized();
                
                Log.Info(LOG_MODULE, "存档系统初始化完成");
            }
        }
        
        /// <summary>
        /// 创建一个新的游戏存档（仅内存，不写盘）。
        /// </summary>
        public void NewGame()
        {
            // 初始化新的存档数据
            m_currentSaveData = new SaveData();
            m_pendingProgressToApply = false;
            
            Log.Info(LOG_MODULE, "创建了新游戏存档");

            // 通知玩法系统：当前进度已重置
            GameEvents.TriggerGameProgressChanged(m_currentSaveData.gameProgress);
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 应用加载的游戏设置。
        /// 应用到游戏后同步写回 PlayerPrefs：运行时设置的唯一来源是 PlayerPrefs
        /// （SettingsModel 初始化时从 PlayerPrefs 读取），写回后设置面板与存档保持一致
        /// </summary>
        private void ApplyLoadedSettings()
        {
            if (m_currentSaveData == null)
            {
                return;
            }
            
            // 从存档创建游戏设置对象并应用到游戏中
            GameSettings settings = m_currentSaveData.CreateGameSettings();
            settings.ApplyToGame();
            settings.SaveToPlayerPrefs();

            // 通知设置系统：读档带来的设置变更已应用
            GameEvents.TriggerSettingsApplied();
            Log.Info(LOG_MODULE, "已应用加载的游戏设置");
        }
        
        /// <summary>
        /// 归一化加载出的存档数据：补齐缺失的进度对象与集合字段。
        /// 主要防御旧版本存档缺少字段时 UI/玩法层出现空引用。
        /// </summary>
        private void NormalizeLoadedSaveData(SaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.gameProgress ??= new GameProgress();
            saveData.gameProgress.EnsureInitialized();
        }

        /// <summary>
        /// 确保当前内存中的存档数据已初始化。
        /// </summary>
        private void EnsureCurrentSaveData()
        {
            if (m_saveSystem == null)
            {
                InitializeSaveSystem();
            }

            m_currentSaveData ??= new SaveData();
            m_currentSaveData.gameProgress ??= new GameProgress();
            m_currentSaveData.gameProgress.EnsureInitialized();
        }

        /// <summary>
        /// 广播当前进度变更事件。
        /// </summary>
        private void NotifyProgressChanged()
        {
            if (m_currentSaveData != null && m_currentSaveData.gameProgress != null)
            {
                GameEvents.TriggerGameProgressChanged(m_currentSaveData.gameProgress);
            }
        }

        /// <summary>
        /// 处理游戏结束事件。
        /// </summary>   
        private void HandleGameOver(bool isWin)
        {
            // 游戏结束时自动保存
            Log.Info(LOG_MODULE, $"游戏结束（胜利：{isWin}），触发自动保存");
            SaveCurrentGame();
        }
        
        #endregion
        
        #region 存档事件触发方法
        
        /// <summary>
        /// 处理新游戏创建事件。
        /// 重置当前存档数据并触发游戏开始。
        /// 注意：不立即落盘——立即保存会把空白存档写入默认槽位，覆盖此前
        /// 游戏结束自动保存的进度；待新一局结束（GameOver 自动存档）或
        /// 玩家手动保存时才写入磁盘。
        /// </summary>
        private void HandleCreateNewGame()
        {
            Log.Info(LOG_MODULE, "创建新游戏：重置存档数据并开始游戏");

            // 重置当前存档数据（内存中，不写盘）
            NewGame();

            // 触发游戏开始：GameManager 切换状态，
            // MainMenuController 等监听者负责加载默认游戏场景
            GameEvents.TriggerGameStart();
        }

        /// <summary>
        /// 处理重新开始事件：结算面板点击“重新开始”时重置内存中的本局进度。
        /// 与 NewGame 一致，不立即落盘，避免覆盖上一次自动存档。
        /// </summary>
        private void HandleGameRestart()
        {
            Log.Info(LOG_MODULE, "重新开始：重置当前局进度");
            NewGame();
        }

        private void HandleSaveGame(string slotName)
        {
            bool success = SaveCurrentGame(slotName);
            if (success)
            {
                Log.Info(LOG_MODULE, $"游戏存档已保存到槽位: {slotName}");
            }
            else
            {
                Log.Error(LOG_MODULE, $"游戏存档保存失败: {slotName}");
            }
        }

        private void HandleLoadGame(string slotName)
        {
            if(!DoesSaveExist(slotName))
            {
                Log.Error(LOG_MODULE, $"要加载的存档文件不存在: {slotName}");
                return;
            }

            if (!LoadGame(slotName))
            {
                Log.Error(LOG_MODULE, $"从槽位 {slotName} 加载游戏失败");
                return;
            }

            Log.Info(LOG_MODULE, $"游戏从槽位 {slotName} 加载完成");

            // 加载成功后恢复游戏世界：触发游戏开始事件，
            // GameManager 切换状态、MainMenuController 等监听者加载游戏场景
            GameEvents.TriggerGameStart();
        }
        
        private void HandleDeleteSave(string slotName)
        {
            if (DeleteSave(slotName))
            {
                Log.Info(LOG_MODULE, $"游戏存档已删除槽位: {slotName}");
            }
            else
            {
                Log.Error(LOG_MODULE, $"游戏存档删除失败: {slotName}");
            }
        }
        
        private void HandleAutoSave(string slotName = DEFAULT_SAVE_SLOT)
        {
            SaveCurrentGame(slotName);
            Log.Info(LOG_MODULE, $"游戏自动保存到槽位: {slotName}");
        }

        /// <summary>
        /// 场景加载完成回调：读档触发切场景时，在新场景初始化后再广播进度加载事件。
        /// </summary>
        private void HandleSceneLoadComplete(string sceneName)
        {
            if (!m_pendingProgressToApply)
            {
                return;
            }

            // 无论加载到哪个场景都清空标记，避免之后误广播
            m_pendingProgressToApply = false;

            if (string.IsNullOrEmpty(sceneName) || sceneName == "MainMenu")
            {
                return;
            }

            Log.Info(LOG_MODULE, $"游戏场景 {sceneName} 加载完成，准备应用存档进度");
            StartCoroutine(NotifyLoadedProgressNextFrame());
        }

        /// <summary>
        /// 延迟一帧广播进度加载事件：确保新场景对象的 Awake/OnEnable/Start 已执行完毕。
        /// </summary>
        private IEnumerator NotifyLoadedProgressNextFrame()
        {
            yield return null;

            if (m_currentSaveData != null && m_currentSaveData.gameProgress != null)
            {
                Log.Info(LOG_MODULE, "向玩法系统广播已加载的存档进度");
                GameEvents.TriggerGameProgressLoaded(m_currentSaveData.gameProgress);
            }
        }

        #endregion
    }
}