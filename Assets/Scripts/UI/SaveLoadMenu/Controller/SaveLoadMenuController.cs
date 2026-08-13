using System.Collections.Generic;
using UnityEngine;
using MyGame.Data;
using MyGame.UI.SaveLoad.Events;
using MyGame.Events;
using MyGame.UI.SaveLoad.View;
using MyGame.UI;

namespace MyGame.UI.SaveLoad.Controller
{
    /// <summary>
    /// 存档菜单控制器
    /// 负责处理存档菜单的用户交互和业务逻辑
    /// 通过订阅 Model 的 OnPropertyChanged 事件驱动 View 更新
    /// </summary>
    public class SaveLoadMenuController : BaseController<SaveLoadMenuView, SaveLoadMenuModel>
    {
        [Header("配置文件")]
        [Tooltip("存档菜单配置文件，包含存档设置、UI配置、文本配置等")]
        [SerializeField] private SaveLoadMenuConfig _config;

        /// <summary>
        /// 存档菜单配置文件
        /// </summary>
        public SaveLoadMenuConfig Config
        {
            get { return _config; }
            set { _config = value; }
        }

        /// <summary>
        /// 初始化MVC组件关系
        /// 创建Model实例并查找/绑定View（二者挂在同一GameObject上）
        /// </summary>
        private void Awake()
        {
            InitializeMVC();
            Initialize();
        }

        /// <summary>
        /// 启用组件时注册事件
        /// </summary>
        private void OnEnable()
        {
            RegisterEvents();
            BindModelEvents();
        }

        /// <summary>
        /// 禁用组件时注销事件
        /// </summary>
        private void OnDisable()
        {
            UnregisterEvents();
            UnbindModelEvents();
        }

        /// <summary>
        /// 初始化逻辑
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
        }

        /// <summary>
        /// 清理逻辑
        /// </summary>
        protected override void OnCleanup()
        {
            base.OnCleanup();
            UnregisterEvents();
            UnbindModelEvents();
            if (m_view != null)
            {
                m_view.Cleanup();
            }
        }

        /// <summary>
        /// 初始化MVC组件关系
        /// 创建Model实例并查找/绑定View（二者挂在同一GameObject上）
        /// </summary>
        private void InitializeMVC()
        {
            // 使用基类方法创建并初始化Model实例
            CreateAndInitializeModel();

            // 查找或获取View引用（同GameObject上的组件）
            if (m_view == null)
            {
                m_view = GetComponent<SaveLoadMenuView>();
            }

            if (m_view != null)
            {
                m_view.Initialize();
            }

            // 初始化存档槽
            InitializeSaveSlots();
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
        /// Model属性变更回调，统一刷新View
        /// </summary>
        /// <param name="propertyName">变更的属性名称</param>
        private void HandleModelPropertyChanged(string propertyName)
        {
            if (m_view != null)
            {
                m_view.UpdateView();
            }
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

        #region 事件注册

        /// <summary>
        /// 注册存档菜单相关事件
        /// </summary>
        private void RegisterEvents()
        {
            SaveLoadMenuEvents.OnSaveGame += HandleSaveGame;
            SaveLoadMenuEvents.OnLoadGame += HandleLoadGame;
            SaveLoadMenuEvents.OnDeleteSave += HandleDeleteSave;
            SaveLoadMenuEvents.OnCreateNewGame += HandleCreateNewGame;
            SaveLoadMenuEvents.OnBackToMainMenu += HandleBackToMainMenu;
            SaveLoadMenuEvents.OnSaveSlotSelected += HandleSaveSlotSelected;
        }

        /// <summary>
        /// 注销存档菜单相关事件
        /// </summary>
        private void UnregisterEvents()
        {
            SaveLoadMenuEvents.OnSaveGame -= HandleSaveGame;
            SaveLoadMenuEvents.OnLoadGame -= HandleLoadGame;
            SaveLoadMenuEvents.OnDeleteSave -= HandleDeleteSave;
            SaveLoadMenuEvents.OnCreateNewGame -= HandleCreateNewGame;
            SaveLoadMenuEvents.OnBackToMainMenu -= HandleBackToMainMenu;
            SaveLoadMenuEvents.OnSaveSlotSelected -= HandleSaveSlotSelected;
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
                    SaveData saveData = SaveManager.Instance.LoadSaveData(slot.SlotName);
                    if (saveData != null)
                    {
                        slot.SaveData = saveData;
                        slot.LastModified = saveData.saveTime;
                        slot.Version = saveData.version;
                        string progress = "无进度信息";
                        if (saveData.gameProgress != null)
                        {
                            progress = string.Format("关卡: {0}, 完成: {1}个",
                                                   saveData.gameProgress.currentLevel,
                                                   saveData.gameProgress.completedLevels.Count);
                        }
                        slot.ProgressText = progress;
                    }
                }
            }

            m_model.UpdateSaveSlots(slots);
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
        /// 处理存档操作
        /// </summary>
        public void HandleSaveGame(string slotName)
        {
            GameEvents.TriggerSaveGame(slotName);
        }

        /// <summary>
        /// 处理加载游戏操作
        /// </summary>
        public void HandleLoadGame(string slotName)
        {
            GameEvents.TriggerLoadGame(slotName);
        }

        /// <summary>
        /// 处理删除存档操作
        /// </summary>
        public void HandleDeleteSave(string slotName)
        {
            GameEvents.TriggerDeleteSave(slotName);
        }

        /// <summary>
        /// 处理创建新游戏操作
        /// </summary>
        public void HandleCreateNewGame()
        {
            GameEvents.TriggerCreateNewGame();
        }

        /// <summary>
        /// 处理返回主菜单操作
        /// </summary>
        public void HandleBackToMainMenu()
        {
            if (m_view != null)
            {
                m_view.Hide();
            }
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
