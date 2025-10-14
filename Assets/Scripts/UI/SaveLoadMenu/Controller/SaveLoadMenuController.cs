using System.Collections.Generic;
using UnityEngine;
using MyGame.Data;
using MyGame.UI.SaveLoad.Events;
using MyGame.Events;
using MyGame.UI.SaveLoad.View;
using MyGame.Managers;
using System;
using Logger;
using MyGame.UI.SaveLoadMenu.View;

namespace MyGame.UI.SaveLoad.Controller
{
    /// <summary>
    /// 存档菜单控制器类
    /// 负责处理存档菜单的用户交互和业务逻辑
    /// 继承自BaseController以保持架构一致性
    /// </summary>
    public class SaveLoadMenuController : BaseController<SaveLoadMenuView, SaveLoadMenuModel>
    {
        [Header("MVC Components")]
        [SerializeField] private SaveLoadMenuModel _model;
        [SerializeField] private SaveLoadMenuView _view;
        
        [Header("配置文件")]
        [Tooltip("存档菜单配置文件，包含存档设置、UI配置、文本配置等")]
        [SerializeField] private SaveLoadMenuConfig _config;

        private const string Log_MODULE = LogModules.SAVELOADMENU;
        
        /// <summary>
        /// 存档菜单配置文件
        /// </summary>
        public SaveLoadMenuConfig Config
        {
            get { return _config; }
            set { _config = value; }
        }
        
        /// <summary>
        /// 模型组件
        /// 提供对具体类型的访问
        /// </summary>
        public SaveLoadMenuModel Model
        {
            get { return m_model; }
            set { base.SetModel(value); }
        }
        
        /// <summary>
        /// 视图组件
        /// 提供对具体类型的访问
        /// </summary>
        public SaveLoadMenuView View
        {
            get { return m_view; }
            set { base.SetView(value); }
        }
        
        /// <summary>
        /// 初始化MVC组件
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
        }
        
        /// <summary>
        /// 禁用组件时注销事件
        /// </summary>
        private void OnDisable()
        {
            UnregisterEvents();
        }
        
        /// <summary>
        /// 初始化逻辑
        /// 重写基类OnInitialize方法
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            // 初始化逻辑已在InitializeMVC中实现
        }
        
        /// <summary>
        /// 清理逻辑
        /// 重写基类OnCleanup方法
        /// </summary>
        protected override void OnCleanup()
        {
            base.OnCleanup();
            UnregisterEvents();
            if (m_view != null)
            {
                m_view.Cleanup();
            }
        }
        
        /// <summary>
        /// 设置模型组件
        /// 重写基类SetModel方法
        /// </summary>
        /// <param name="model">模型实例</param>
        public override void SetModel(SaveLoadMenuModel model)
        {
            if (m_model != null)
            {
                // 移除旧模型的事件监听
                m_model.OnSaveSlotsUpdated -= HandleSaveSlotsUpdated;
                m_model.OnSelectedSaveSlotChanged -= HandleSelectedSaveSlotChanged;
            }
            
            base.SetModel(model);
            
            if (m_model != null)
            {
                m_model.Initialize();
                // 添加新模型的事件监听
                m_model.OnSaveSlotsUpdated += HandleSaveSlotsUpdated;
                m_model.OnSelectedSaveSlotChanged += HandleSelectedSaveSlotChanged;
                
                // 如果视图已设置，同步模型引用
                if (m_view != null)
                {
                    m_view.Model = m_model;
                }
            }
        }
        
        /// <summary>
        /// 设置视图组件
        /// 重写基类SetView方法
        /// </summary>
        /// <param name="view">视图实例</param>
        public override void SetView(SaveLoadMenuView view)
        {
            if (m_view != null)
            {
                // 移除旧视图的引用
                m_view.Controller = null;
            }
            
            base.SetView(view);
            
            if (m_view != null)
            {
                m_view.Initialize();
                m_view.Controller = this;
                
                if (m_model != null)
                {
                    m_view.SetModel(m_model);
                }
            }
        }
        
        /// <summary>
        /// 初始化MVC组件关系
        /// </summary>
        private void InitializeMVC()
        {
            // 确保模型和视图不为空
            _model ??= new SaveLoadMenuModel();
            
            if (_view == null)
            {
                _view = GetComponentInChildren<SaveLoadMenuView>();
            }
            
            // 设置MVC关系
            SetModel(_model);
            SetView(_view);
            
            // 初始化存档槽
            InitializeSaveSlots();
        }
        
        /// <summary>
        /// 注册事件
        /// </summary>
        private void RegisterEvents()
        {
            // 注册存档菜单相关事件
            SaveLoadMenuEvents.OnSaveGame += HandleSaveGame;
            SaveLoadMenuEvents.OnLoadGame += HandleLoadGame;
            SaveLoadMenuEvents.OnDeleteSave += HandleDeleteSave;
            SaveLoadMenuEvents.OnCreateNewGame += HandleCreateNewGame;
            SaveLoadMenuEvents.OnBackToMainMenu += HandleBackToMainMenu;
            SaveLoadMenuEvents.OnSaveSlotSelected += HandleSaveSlotSelected;
        }
        
        /// <summary>
        /// 注销事件
        /// </summary>
        private void UnregisterEvents()
        {
            // 注销存档菜单相关事件
            SaveLoadMenuEvents.OnSaveGame -= HandleSaveGame;
            SaveLoadMenuEvents.OnLoadGame -= HandleLoadGame;
            SaveLoadMenuEvents.OnDeleteSave -= HandleDeleteSave;
            SaveLoadMenuEvents.OnCreateNewGame -= HandleCreateNewGame;
            SaveLoadMenuEvents.OnBackToMainMenu -= HandleBackToMainMenu;
            SaveLoadMenuEvents.OnSaveSlotSelected -= HandleSaveSlotSelected;
        }
        
        /// <summary>
        /// 初始化存档槽
        /// </summary>
        private void InitializeSaveSlots()
        {
            if (_model == null)
                return;
            
            List<SaveSlotInfo> slots = new();
            
            // 从配置中获取自动存档槽名称
            string autoSaveSlotName = "AutoSave";
            
            // 添加自动存档槽
            if (_config != null)
            {
                autoSaveSlotName = _config.AutoSaveSlotName;
                
                slots.Add(new SaveSlotInfo
                {
                    SlotName = autoSaveSlotName,
                    DisplayName = "自动存档",
                    IsAutoSave = true,
                    NotEmpty = SaveManager.Instance.DoesSaveExist(autoSaveSlotName)
                });
            }
            int saveSlotCount = 4;

            // 优先使用配置中的存档槽数量
            if (_config != null)
            {
                saveSlotCount = _config.SlotsPerPage;
                
                // 如果添加了自动存档槽，则减少一个手动存档槽的数量
                if (slots.Count > 0 && slots[0].IsAutoSave)
                {
                    saveSlotCount = Math.Max(0, saveSlotCount - 1);
                }
            }
            
            for (int i = 1; i <= saveSlotCount; i++)
            {
                string slotName = string.Format("save_{0}", i);
                
                slots.Add(new SaveSlotInfo
                {
                    SlotName = slotName,
                    DisplayName = string.Format("存档槽 {0}", i),
                    IsAutoSave = false,
                    NotEmpty = SaveManager.Instance.DoesSaveExist(slotName)
                });
            }
            
            // 更新存档数据
            foreach (var slot in slots)
            {
                if (slot.NotEmpty)
                {
                    SaveData saveData = SaveManager.Instance.LoadSaveData(slot.SlotName);
                    if (saveData != null)
                    {
                        slot.SaveData = saveData;
                        // 使用正确的属性名saveTime而不是LastModified
                        slot.LastModified = saveData.saveTime;
                        // 使用正确的属性名version而不是Version
                        slot.Version = saveData.version;
                        // 从gameProgress中构建进度文本
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
            
            // 更新模型中的存档槽列表
            _model.UpdateSaveSlots(slots);
        }
        
        /// <summary>
        /// 处理存档槽选中事件
        /// 根据当前游戏状态处理空存档槽的点击行为
        /// - 在主菜单状态下点击空存档槽：创建新游戏
        /// - 在游戏中点击空存档槽：直接进行存档操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <param name="saveData">存档数据</param>
        public void HandleSaveSlotSelected(string slotName, SaveData saveData = null)
        {
            if (_model == null)
                return;
            
            // 设置选中的存档槽
            _model.SetSelectedSaveSlot(slotName, saveData);
            
            // 处理空存档槽的特殊逻辑
            if (saveData == null)
            {
                // 安全检查：确保GameManager实例存在
                if (GameManager.Instance != null)
                {
                    // 获取当前游戏状态
                    GameState currentState = GameManager.Instance.State;
                    
                    if (currentState == GameState.Menu)
                    {
                        // 在主菜单状态下点击空存档槽，触发创建新游戏操作

                        m_view.ShowConfirmPanel(ConfirmActionType.Create, slotName);
                    }
                    else if (currentState == GameState.Playing || currentState == GameState.Paused)
                    {
                        // 在游戏中点击空存档槽，直接进行存档操作
                        HandleSaveGame(slotName);
                    }
                }
            }
            Log.Info(Log_MODULE, $"存档槽{slotName}被点击");
        }
        
        /// <summary>
        /// 处理存档操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        public void HandleSaveGame(string slotName)
        {
            // 通过GameEvents触发存档操作
            GameEvents.TriggerSaveGame(slotName);
        }
        
        /// <summary>
        /// 处理加载游戏操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        public void HandleLoadGame(string slotName)
        {
            // 通过GameEvents触发加载游戏操作
            GameEvents.TriggerLoadGame(slotName);
        }
        
        /// <summary>
        /// 处理删除存档操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        public void HandleDeleteSave(string slotName)
        {
            // 通过GameEvents触发删除存档操作
            GameEvents.TriggerDeleteSave(slotName);
        }
        
        /// <summary>
        /// 处理创建新游戏操作
        /// </summary>
        public void HandleCreateNewGame()
        {
            // 通过GameEvents触发创建新游戏操作
            GameEvents.TriggerCreateNewGame();
        }
        
        /// <summary>
        /// 处理返回主菜单操作
        /// </summary>
        public void HandleBackToMainMenu()
        {
            // 处理返回主菜单逻辑
            if (_view != null)
            {
                _view.Hide();
            }
        }
        
        /// <summary>
        /// 处理存档槽更新事件
        /// </summary>
        private void HandleSaveSlotsUpdated()
        {
            if (_view != null)
            {
                _view.UpdateView();
            }
        }
        
        /// <summary>
        /// 处理选中存档槽变更事件
        /// </summary>
        private void HandleSelectedSaveSlotChanged()
        {
            if (_view != null)
            {
                _view.UpdateView();
            }
        }
        
        /// <summary>
        /// 显示存档菜单
        /// </summary>
        public void Show()
        {
            if (_view != null)
            {
                _view.Show();
                // 显示前刷新存档数据
                InitializeSaveSlots();
            }
        }
        
        /// <summary>
        /// 隐藏存档菜单
        /// </summary>
        public void Hide()
        {
            if (_view != null)
            {
                _view.Hide();
            }
        }
    }
}