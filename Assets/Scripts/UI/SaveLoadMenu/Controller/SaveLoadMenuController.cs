using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Data;
using MyGame.Events;
using MyGame.UI.SaveLoad.View;
using MyGame.UI;
using Logger;

namespace MyGame.UI.SaveLoad.Controller
{
    /// <summary>
    /// 存档菜单控制器
    /// 负责处理存档菜单的用户交互和业务逻辑
    /// 通过订阅 Model 的 OnPropertyChanged 事件驱动 View 更新
    /// </summary>
    public class SaveLoadMenuController : BaseController<SaveLoadMenuView, SaveLoadMenuModel>
    {
        private const string LOG_MODULE = LogModules.SAVELOAD;

        [Header("配置文件")]
        [Tooltip("存档菜单配置文件，包含存档设置、UI配置、文本配置等")]
        [SerializeField] private SaveLoadMenuConfig _config;

        /// <summary>
        /// 槽位存档数据缓存（槽名 → 最后写入时间 + 存档数据）。
        /// 打开菜单时若文件未变化则复用缓存，避免每次全量读取全部存档文件
        /// </summary>
        private readonly Dictionary<string, (DateTime LastWriteUtc, SaveData Data)> _slotDataCache = new();

        /// <summary>
        /// 存档菜单配置文件
        /// </summary>
        public SaveLoadMenuConfig Config
        {
            get { return _config; }
            set { _config = value; }
        }

        /// <summary>
        /// 初始化控制器（由 SaveLoadMenuView.TryBindController 调用）。
        /// 创建 Model、初始化存档槽，再触发基类 OnInitialize。
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                // 使用基类方法创建并初始化Model实例
                CreateAndInitializeModel();

                // 初始化存档槽（依赖 m_model 与 Inspector 配置 _config，二者此时已就绪）
                InitializeSaveSlots();

                // 调用基类初始化（触发 OnInitialize）
                base.Initialize();
            }
        }

        /// <summary>
        /// 初始化逻辑：订阅 Model 变更
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            BindModelEvents();
        }

        /// <summary>
        /// 清理控制器资源（解绑 Model 事件、清理模型）
        /// </summary>
        public override void Cleanup()
        {
            if (IsInitialized)
            {
                UnbindModelEvents();

                if (m_model != null)
                {
                    m_model.Cleanup();
                    m_model = null;
                }

                // 清理存档数据缓存
                _slotDataCache.Clear();

                base.Cleanup();
            }
        }

        #region Model事件绑定

        /// <summary>
        /// 订阅Model的属性变更事件
        /// </summary>
        private void BindModelEvents()
        {
            if (m_model != null)
            {
                m_model.OnPropertyChanged += HandleModelPropertyChanged;
            }
        }

        /// <summary>
        /// 取消订阅Model的属性变更事件
        /// </summary>
        private void UnbindModelEvents()
        {
            if (m_model != null)
            {
                m_model.OnPropertyChanged -= HandleModelPropertyChanged;
            }
        }

        /// <summary>
        /// Model属性变更回调。
        /// 选中相关属性只刷新高亮/详情/按钮状态，不重建槽位列表，
        /// 避免连续三个属性变更触发三次 Addressable 异步重建导致槽位翻倍。
        /// </summary>
        /// <param name="propertyName">变更的属性名称</param>
        private void HandleModelPropertyChanged(string propertyName)
        {
            if (m_view == null)
            {
                return;
            }

            bool isSelectionOnlyChange =
                propertyName == nameof(SaveLoadMenuModel.SelectedSaveSlotName) ||
                propertyName == nameof(SaveLoadMenuModel.SelectedSaveData) ||
                propertyName == nameof(SaveLoadMenuModel.IsAutoSaveSlot);

            if (isSelectionOnlyChange)
            {
                m_view.RefreshSelectedState();
                return;
            }

            m_view.UpdateView();
        }

        #endregion

        #region View查询方法（供View层通过Controller获取Model数据）

        /// <summary>
        /// 获取存档槽列表
        /// </summary>
        public List<SaveSlotInfo> GetSaveSlots()
        {
            return m_model?.SaveSlots;
        }

        /// <summary>
        /// 获取当前选中的存档槽名称
        /// </summary>
        public string GetSelectedSaveSlotName()
        {
            return m_model?.SelectedSaveSlotName;
        }

        /// <summary>
        /// 获取当前选中的存档数据
        /// </summary>
        public SaveData GetSelectedSaveData()
        {
            return m_model?.SelectedSaveData;
        }

        #endregion

        #region 存档槽初始化

        /// <summary>
        /// 初始化存档槽列表
        /// </summary>
        private void InitializeSaveSlots()
        {
            if (m_model == null)
                return;

            List<SaveSlotInfo> slots = new()
            {
                // 添加自动存档槽
                new SaveSlotInfo
                {
                    SlotName = SaveLoadMenuConstants.AUTO_SAVE_SLOT,
                    DisplayName = "自动存档",
                    IsAutoSave = true,
                    HasSave = SaveManager.Instance.DoesSaveExist(SaveLoadMenuConstants.AUTO_SAVE_SLOT)
                }
            };

            // 添加手动存档槽
            int saveSlotCount = SaveLoadMenuConstants.DEFAULT_SAVE_SLOT_COUNT;

            if (_config != null)
            {
                saveSlotCount = _config.MaxManualSaveCount;
            }

            for (int i = 1; i <= saveSlotCount; i++)
            {
                string slotName = string.Format("save_{0}", i);

                slots.Add(new SaveSlotInfo
                {
                    SlotName = slotName,
                    DisplayName = string.Format("存档槽 {0}", i),
                    IsAutoSave = false,
                    HasSave = SaveManager.Instance.DoesSaveExist(slotName)
                });
            }

            // 填充存档数据
            foreach (var slot in slots)
            {
                if (slot.HasSave)
                {
                    SaveData saveData = LoadSlotDataCached(slot.SlotName);
                    if (saveData != null)
                    {
                        slot.SaveData = saveData;
                        slot.LastModified = !string.IsNullOrEmpty(saveData.saveTime)
                            ? saveData.saveTime
                            : (!string.IsNullOrEmpty(saveData.saveTimeUtc) ? saveData.saveTimeUtc : "时间未知");
                        slot.Version = string.IsNullOrEmpty(saveData.version) ? "未知" : saveData.version;
                        string progress = string.IsNullOrEmpty(saveData.progressSummary)
                            ? "无进度信息"
                            : saveData.progressSummary;
                        slot.ProgressText = progress;
                    }
                    else
                    {
                        // 存档文件存在但无法解析：JsonSaveSystem 已将其隔离为 .corrupt，
                        // 该槽应立刻按空槽处理，避免玩家点击后误覆盖坏档
                        Log.Warning(LOG_MODULE, $"槽位 {slot.SlotName} 的存档无法读取，按空槽显示");
                        slot.HasSave = false;
                        _slotDataCache.Remove(slot.SlotName);
                    }
                }
                else
                {
                    // 槽位已空，清除对应缓存
                    _slotDataCache.Remove(slot.SlotName);
                }
            }

            m_model.UpdateSaveSlots(slots);
        }

        /// <summary>
        /// 按槽位加载存档数据（带缓存）：文件最后写入时间未变化时复用缓存，
        /// 避免每次打开存档菜单都全量读取所有存档文件
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <returns>存档数据，加载失败返回 null</returns>
        private SaveData LoadSlotDataCached(string slotName)
        {
            DateTime lastWriteUtc = SaveManager.Instance.GetSaveLastWriteTime(slotName);

            if (_slotDataCache.TryGetValue(slotName, out var cached) && cached.LastWriteUtc == lastWriteUtc)
            {
                return cached.Data;
            }

            SaveData saveData = SaveManager.Instance.LoadSaveData(slotName);
            if (saveData != null)
            {
                _slotDataCache[slotName] = (lastWriteUtc, saveData);
            }
            else
            {
                _slotDataCache.Remove(slotName);
            }
            return saveData;
        }

        /// <summary>
        /// 保存/删除后刷新槽位列表与选中数据。
        /// 批量更新期间暂停 Model 属性通知，只触发一次视图刷新，避免连续多次重建存档槽 UI。
        /// 同时供 View 在 Show 前调用，保证通过 UIManager 直接 panel.Show() 时槽位数据也是最新的。
        /// </summary>
        public void RefreshSaveSlots()
        {
            if (m_model == null)
                return;

            string selectedSlot = m_model.SelectedSaveSlotName;

            // 批量更新期间暂停属性通知
            m_model.OnPropertyChanged -= HandleModelPropertyChanged;
            try
            {
                InitializeSaveSlots();

                // 重新绑定选中槽位：列表已刷新，选中数据同步为最新（删除后 SaveData 为 null，
                // 加载/保存按钮状态随之更新）
                if (!string.IsNullOrEmpty(selectedSlot))
                {
                    SaveSlotInfo slot = m_model.SaveSlots.Find(s => s.SlotName == selectedSlot);
                    if (slot != null)
                    {
                        m_model.SetSelectedSaveSlot(slot.SlotName, slot.SaveData);
                    }
                }
            }
            finally
            {
                m_model.OnPropertyChanged += HandleModelPropertyChanged;
            }

            m_view?.UpdateView();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理存档槽选中事件
        /// </summary>
        public void HandleSaveSlotSelected(string slotName, SaveData saveData = null)
        {
            if (m_model == null)
                return;

            m_model.SetSelectedSaveSlot(slotName, saveData);
        }

        /// <summary>
        /// 处理存档操作（事件同步执行保存后刷新槽位显示）
        /// </summary>
        public void HandleSaveGame(string slotName)
        {
            GameEvents.TriggerSaveGame(slotName);

            // 保存为同步流程：事件返回后立即刷新槽位时间戳/状态与选中数据
            RefreshSaveSlots();
        }

        /// <summary>
        /// 处理加载游戏操作
        /// </summary>
        public void HandleLoadGame(string slotName)
        {
            GameEvents.TriggerLoadGame(slotName);
        }

        /// <summary>
        /// 处理删除存档操作（事件同步执行删除后刷新槽位显示）
        /// </summary>
        public void HandleDeleteSave(string slotName)
        {
            GameEvents.TriggerDeleteSave(slotName);

            // 删除为同步流程：事件返回后立即刷新槽位状态与选中数据
            RefreshSaveSlots();
        }

        /// <summary>
        /// 处理创建新游戏操作
        /// </summary>
        public void HandleCreateNewGame()
        {
            GameEvents.TriggerCreateNewGame();
        }

        /// <summary>
        /// 处理返回主菜单操作。
        /// 通过 UIManager 统一调度，确保 currentState / 输入模式 / 面板显隐一起更新。
        /// </summary>
        public void HandleBackToMainMenu()
        {
            GameEvents.TriggerMenuShow(UIType.SaveLoadMenu, false);
            GameEvents.TriggerMenuShow(UIType.MainMenu, true);
        }

        #endregion

        #region 公共操作方法

        /// <summary>
        /// 显示存档菜单
        /// </summary>
        public void Show()
        {
            if (m_view != null)
            {
                // 显示前刷新存档数据
                InitializeSaveSlots();
                m_view.Show();
            }
        }

        /// <summary>
        /// 隐藏存档菜单
        /// </summary>
        public void Hide()
        {
            if (m_view != null)
            {
                m_view.Hide();
            }
        }

        #endregion
    }
}
