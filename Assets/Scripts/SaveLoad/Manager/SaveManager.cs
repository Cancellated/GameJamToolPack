using MyGame.Events;
using MyGame.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger;

namespace MyGame.Data
{
    /// <summary>
    /// 游戏存档管理器，负责处理游戏数据的保存、加载和删除操作。
    /// 作为单例类提供全局访问点，并与游戏事件系统集成。
    ///
    /// 中立化契约：本管理器不再内置任何玩法进度字段（关卡/任务/数值等）。
    /// 玩法系统实现 IGameplaySaveProvider 并在进入玩法场景时注册，
    /// 由本管理器在保存时捕获玩法 JSON、加载时通过 GameEvents.OnGameplayDataLoaded 回传。
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

        /// <summary>当前注册的玩法存档提供者；场景卸载时由提供者主动注销。</summary>
        private IGameplaySaveProvider m_gameplaySaveProvider;

        /// <summary>读档后待应用玩法数据的标记：等待游戏场景加载完成后再广播加载事件。</summary>
        private bool m_pendingGameplayDataToApply;

        [Header("自动存档")]
        [SerializeField, Tooltip("是否启用定时自动存档（仅游戏进行中生效）")]
        private bool m_enableAutoSave = true;

        [SerializeField, Tooltip("自动存档间隔（秒）")]
        private float m_autoSaveInterval = 300f;

        /// <summary>自动存档计时器（秒）</summary>
        private float m_autoSaveTimer;
        
        #region 属性
        
        /// <summary>
        /// 当前加载的存档数据（含设置、元数据与不透明的玩法 JSON）。
        /// </summary>
        public SaveData CurrentSaveData
        {
            get { return m_currentSaveData; }
        }

        /// <summary>
        /// 是否已注册可用的玩法存档提供者。
        /// 在菜单等没有玩法系统的场景中为 false；此时保存仍会写入设置与元数据。
        /// </summary>
        public bool HasRegisteredGameplaySaveProvider
        {
            get { return TryGetAliveProvider(out _); }
        }
        
        /// <summary>
        /// 获取所有可用的存档槽名称。
        /// </summary>
        public List<string> AvailableSaves
        {
            get { return m_saveSystem?.GetAvailableSaves() ?? new List<string>(); }
        }
        
        #endregion

        #region 玩法存档提供者注册（框架与具体游戏的唯一写入桥梁）

        /// <summary>
        /// 注册玩法存档提供者。通常在玩法场景的引导对象 Start 时调用；
        /// 同一对象须在 OnDestroy/OnDisable 时调用 UnregisterGameplaySaveProvider。
        /// </summary>
        public void RegisterGameplaySaveProvider(IGameplaySaveProvider provider)
        {
            if (provider == null)
            {
                Log.Error(LOG_MODULE, "注册玩法存档提供者失败：提供者为空");
                return;
            }

            if (ReferenceEquals(m_gameplaySaveProvider, provider))
            {
                Log.InfoWithCooldown(LOG_MODULE, "玩法存档提供者重复注册，已忽略", "register-same-provider");
                return;
            }

            if (TryGetAliveProvider(out var existingProvider))
            {
                Log.Warning(LOG_MODULE,
                    $"已存在玩法存档提供者，将被替换：{existingProvider.GetType().Name} -> {provider.GetType().Name}");
            }

            m_gameplaySaveProvider = provider;
            Log.Info(LOG_MODULE, $"已注册玩法存档提供者: {provider.GetType().Name}");
        }

        /// <summary>
        /// 注销玩法存档提供者。场景卸载前必须调用，避免管理器持有失效引用。
        /// </summary>
        public void UnregisterGameplaySaveProvider(IGameplaySaveProvider provider)
        {
            if (provider == null || !ReferenceEquals(m_gameplaySaveProvider, provider))
            {
                return;
            }

            m_gameplaySaveProvider = null;
            Log.Info(LOG_MODULE, $"已注销玩法存档提供者: {provider.GetType().Name}");
        }

        /// <summary>
        /// 获取当前存活的提供者；若其宿主 MonoBehaviour 已随场景销毁则自动注销。
        /// </summary>
        private bool TryGetAliveProvider(out IGameplaySaveProvider provider)
        {
            provider = m_gameplaySaveProvider;
            if (provider == null)
            {
                return false;
            }

            if (provider is UnityEngine.Object unityObject && unityObject == null)
            {
                Log.Warning(LOG_MODULE, "玩法存档提供者的宿主对象已随场景销毁，自动注销");
                m_gameplaySaveProvider = null;
                provider = null;
                return false;
            }

            return true;
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
        /// 存在玩法存档提供者时捕获其玩法 JSON 与摘要；不存在（如菜单场景）时
        /// 保留内存中已有的玩法数据不变，仅同步设置与元数据。
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

            // 确保当前存档对象已初始化，避免空引用
            EnsureCurrentSaveData();

            // 存在玩法提供者时捕获新玩法数据；不存在（如主菜单）时：
            // - 当前内存已有玩法数据则保留（例如刚读档后换槽保存/复制存档）；
            // - 当前内存为空且目标槽已有存档时，回读目标槽玩法数据，
            //   避免"菜单里改设置后顺手保存"把原槽位的玩法数据清空。
            if (TryGetAliveProvider(out var provider))
            {
                CaptureGameplayDataFromProvider(provider);
            }
            else
            {
                Log.InfoWithCooldown(LOG_MODULE,
                    "未注册玩法存档提供者，本次保存仅写入设置与元数据（玩法数据按规则保留）",
                    "save-without-provider");
                PreserveExistingGameplayData(saveSlot);
            }

            // 保存前同步当前设置：运行时设置统一持久化在 PlayerPrefs（由 SettingsModel 写入），
            // 这里从 PlayerPrefs 读取同步进存档，保证存档中的设置与玩家当前设置一致
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
            m_pendingGameplayDataToApply = false;

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

                // 若读档发生在主菜单（随后会切场景），先把玩法数据标记为待应用，
                // 待游戏场景加载完成后再广播；若已在游戏场景内，则立即广播
                bool willLoadGameScene = GameManager.Instance != null
                                         && GameManager.Instance.State == GameState.Menu;
                string gameplayDataJson = m_currentSaveData.gameplayDataJson ?? string.Empty;
                if (willLoadGameScene)
                {
                    m_pendingGameplayDataToApply = true;
                }
                else
                {
                    GameEvents.TriggerGameplayDataLoaded(gameplayDataJson);
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
                m_currentSaveData.EnsureInitialized();
                
                Log.Info(LOG_MODULE, "存档系统初始化完成");
            }
        }
        
        /// <summary>
        /// 创建一个新的游戏存档（仅内存，不写盘）。
        /// 玩法数据字段重置为空；具体玩法系统仍通过 GameStart 事件自行初始化世界。
        /// </summary>
        public void NewGame()
        {
            // 初始化新的存档数据
            m_currentSaveData = new SaveData();
            m_pendingGameplayDataToApply = false;
            
            Log.Info(LOG_MODULE, "创建了新游戏存档");

            // 通知玩法系统：当前局内数据已重置
            GameEvents.TriggerGameplayDataChanged();
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
        /// 从玩法存档提供者捕获玩法 JSON 与摘要。
        /// 捕获失败时保留内存中已有玩法数据，避免一次异常清空玩家进度。
        /// </summary>
        private void CaptureGameplayDataFromProvider(IGameplaySaveProvider provider)
        {
            try
            {
                string gameplayJson = provider.CaptureGameplayDataJson();
                string summary = provider.GetProgressSummary();
                m_currentSaveData.SetGameplayData(gameplayJson, summary);
            }
            catch (Exception ex)
            {
                Log.Error(LOG_MODULE,
                    $"玩法存档提供者捕获数据失败，已保留内存中已有玩法数据: {ex.Message}");
            }
        }

        /// <summary>
        /// 无玩法提供者（典型：主菜单场景）保存时的防护：
        /// 当前内存玩法数据为空且目标槽已有存档时，回读目标槽的玩法数据与摘要，
        /// 防止"仅改设置后保存"把原槽位玩法进度清空。
        /// </summary>
        private void PreserveExistingGameplayData(string saveSlot)
        {
            if (!string.IsNullOrEmpty(m_currentSaveData.gameplayDataJson))
            {
                return;
            }

            SaveData existingData = m_saveSystem.LoadGame(saveSlot);
            if (existingData == null)
            {
                return;
            }

            m_currentSaveData.SetGameplayData(existingData.gameplayDataJson, existingData.progressSummary);
            Log.Info(LOG_MODULE,
                $"当前无玩法存档提供者且内存玩法数据为空，已回读目标槽 {saveSlot} 的玩法数据以避免覆盖丢失");
        }

        /// <summary>
        /// 归一化加载出的存档数据：补齐字符串字段。
        /// 主要防御旧版本存档缺少字段时 UI/玩法层出现空引用。
        /// </summary>
        private void NormalizeLoadedSaveData(SaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.EnsureInitialized();
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
            m_currentSaveData.EnsureInitialized();
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
        /// 场景加载完成回调：读档触发切场景时，在新场景初始化后再广播玩法数据加载事件。
        /// </summary>
        private void HandleSceneLoadComplete(string sceneName)
        {
            if (!m_pendingGameplayDataToApply)
            {
                return;
            }

            // 无论加载到哪个场景都清空标记，避免之后误广播
            m_pendingGameplayDataToApply = false;

            if (string.IsNullOrEmpty(sceneName) || sceneName == "MainMenu")
            {
                return;
            }

            Log.Info(LOG_MODULE, $"游戏场景 {sceneName} 加载完成，准备应用玩法存档数据");
            StartCoroutine(NotifyLoadedGameplayDataNextFrame());
        }

        /// <summary>
        /// 延迟一帧广播玩法数据加载事件：确保新场景对象的 Awake/OnEnable/Start 已执行完毕。
        /// </summary>
        private IEnumerator NotifyLoadedGameplayDataNextFrame()
        {
            yield return null;

            if (m_currentSaveData != null)
            {
                Log.Info(LOG_MODULE, "向玩法系统广播已加载的玩法存档数据");
                GameEvents.TriggerGameplayDataLoaded(m_currentSaveData.gameplayDataJson ?? string.Empty);
            }
        }

        #endregion
    }
}
