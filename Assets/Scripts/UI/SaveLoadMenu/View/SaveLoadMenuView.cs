using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using TMPro;
using MyGame.Data;
using Logger;
using MyGame.UI.SaveLoad.Controller;
using System.Collections;
using UnityEngine.InputSystem;
using MyGame.UI.SaveLoad.Utils;
using MyGame.UI.SaveLoad.View.Components;
using MyGame.UI.SaveLoad.View;

namespace MyGame.UI.SaveLoad
{
    /// <summary>
    /// 存档菜单视图类
    /// 负责显示存档菜单的UI元素并处理用户交互
    /// 继承自BaseView以保持架构一致性
    /// </summary>
    public class SaveLoadMenuView : BaseView<SaveLoadMenuController>, ISaveSlotMenuHandler
    {
        #region 成员变量
        [Header("存档槽")]
        [Tooltip("存档槽容器，用于放置所有存档槽UI")]
        [SerializeField] protected Transform saveSlotsContainer;
        [Tooltip("存档槽预制件引用")]
        [SerializeField] protected AssetReference saveSlotPrefabRef;
        [Tooltip("最大存档槽数量")]
        [SerializeField] protected int maxSaveSlots = 10;
        
        [Header("滚动与选择设置")]
        [Tooltip("存档槽滚动区域")]
        [SerializeField] protected ScrollRect saveScrollRect;
        
        // 辅助类
        private SaveSlotNavigationHelper _navigationHelper;
        private SaveLoadAsyncOperationHelper _asyncOperationHelper;
        private SaveLoadUIEventHandler _uiEventHandler;

        
        // 二级菜单UI元素 - 直接整合到父视图中
        [Header("二级菜单UI元素")]
        [Tooltip("二级菜单面板")]
        [SerializeField] protected GameObject saveOptionsPanel;
        
        [Tooltip("保存按钮")]
        [SerializeField] protected Button saveButton;
        
        [Tooltip("加载按钮")]
        [SerializeField] protected Button loadButton;
        
        [Tooltip("删除按钮")]
        [SerializeField] protected Button deleteButton;
        
        
        [Header("主要选项")]  // 一级菜单：仅保留返回按钮
        [Tooltip("返回按钮")]
        [SerializeField] protected Button backButton;

        [Header("确认面板UI元素")]
        [Tooltip("确认面板游戏对象")]
        [SerializeField] private GameObject confirmPanel;
        
        [Tooltip("确认按钮")]
        [SerializeField] private Button confirmButton;
        
        [Tooltip("取消按钮")]
        [SerializeField] private Button cancelButton;
        
        [Tooltip("确认信息文本")]
        [SerializeField] private TextMeshProUGUI confirmDirectionText;
        
        // 当前确认操作类型
        private ConfirmType _currentConfirmActionType;
        
        // 当前确认操作的存档槽名称
        private string _currentConfirmSlotName;

        [Header("自定义UI元素")]
        [Tooltip("菜单标题文本")]
        [SerializeField]
        private TextMeshProUGUI _menuTitleText;

        protected SaveLoadMenuModel _model;
        protected List<ISaveSlotUI> _saveSlotUIs = new();
        
        // 当前选中的存档槽信息
        private SaveSlotInfo currentSelectedSlotInfo;
        // private const string LOG_MODULE = LogModules.SAVE;
        #endregion
        
        #region 属性
        /// <summary>
        /// 控制器组件
        /// 重写基类Controller属性以提供对具体类型的访问
        /// </summary>
        public SaveLoadMenuController Controller
        {
            get { return m_controller; }
            set { BindController(value); }
        }
        
        /// <summary>
        /// 模型组件
        /// </summary>
        public SaveLoadMenuModel Model
        {
            get { return _model; }
            set { SetModel(value); }
        }
        #endregion
        
        #region 初始化和设置方法
        /// <summary>
        /// 初始化视图
        /// 重写基类Initialize方法
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            BindButtonEvents();
            
            // 隐藏二级菜单
            if (saveOptionsPanel != null)
            {
                saveOptionsPanel.SetActive(false);
            }
            
            // Set initial title
            if (_menuTitleText != null)
            {
                _menuTitleText.text = "选择存档";
            }
            
            // 初始化辅助类
            InitializeHelpers();
            
            // 设置滚动和遮罩组件
            SetupScrollAndMask();
        }
        
        /// <summary>
        /// 初始化辅助类
        /// </summary>
        protected virtual void InitializeHelpers()
        {
            // 初始化导航辅助类
            _navigationHelper = new SaveSlotNavigationHelper();
            _navigationHelper.Initialize(saveScrollRect, this);
            _navigationHelper.OnSlotSelected = OnNavigationSlotSelected;
            
            // 初始化异步操作辅助类
            _asyncOperationHelper = new SaveLoadAsyncOperationHelper();
            
            // 初始化UI事件处理辅助类
            _uiEventHandler = new SaveLoadUIEventHandler();
            _uiEventHandler.Initialize(backButton, saveButton, loadButton, deleteButton, 
                confirmButton, cancelButton);
            
            // 注册事件回调处理方法
            _uiEventHandler.RegisterBackButtonCallback(HandleBackButtonClick);
            _uiEventHandler.RegisterSaveButtonCallback(slotInfo => HandleSaveButtonClick(slotInfo));
            _uiEventHandler.RegisterLoadButtonCallback(slotInfo => HandleLoadButtonClick(slotInfo));
            _uiEventHandler.RegisterDeleteButtonCallback(slotInfo => HandleDeleteButtonClick(slotInfo));
            _uiEventHandler.RegisterConfirmButtonCallback(HandleConfirmButtonClick);
            _uiEventHandler.RegisterCancelButtonCallback(HandleCancelButtonClick);
        }

        /// <summary>
        /// 设置滚动和遮罩组件
        /// </summary>
        protected virtual void SetupScrollAndMask()
        {
            _navigationHelper?.SetupScrollAndMask(saveSlotsContainer);
            
            // 确保ScrollRect的viewport和content设置正确
            if (saveScrollRect != null)
            {
                // 确保有Viewport设置
                if (saveScrollRect.viewport == null)
                {
                    // 如果没有设置Viewport，尝试使用父对象作为Viewport
                    Transform parent = saveScrollRect.transform.parent;
                    if (parent != null)
                    {
                        saveScrollRect.viewport = parent.GetComponent<RectTransform>();
                    }
                }
                
                // 确保有Content设置
                if (saveScrollRect.content == null && saveSlotsContainer != null)
                {
                    saveScrollRect.content = saveSlotsContainer.GetComponent<RectTransform>();
                }
                
                // 添加Mask组件以实现遮罩功能
                if (saveScrollRect.viewport != null && saveScrollRect.viewport.GetComponent<Mask>() == null)
                {
                    Mask mask = saveScrollRect.viewport.gameObject.AddComponent<Mask>();
                    mask.showMaskGraphic = true; // 显示遮罩图形
                }
            }
        }

        /// <summary>
        /// 尝试自动绑定控制器
        /// </summary>
        protected override void TryBindController()
        {
            // Try to find controller component
            var controller = GetComponentInParent<SaveLoadMenuController>();
            if (controller != null)
            {
                BindController(controller);
            }
        }

        /// <summary>
        /// 设置模型组件
        /// </summary>
        /// <param name="model">模型实例</param>
        public virtual void SetModel(SaveLoadMenuModel model)
        {
            if (_model != null)
            {
                UnsubscribeFromModelEvents();
            }
            
            _model = model;
            
            if (_model != null)
            {
                SubscribeToModelEvents();
            }
            
            UpdateView();
        }
        #endregion
        
        #region 存档槽UI相关方法
        // 用于跟踪创建存档槽的协程
        private Coroutine _createSaveSlotCoroutine;
        
        /// <summary>
        /// 视图更新类型枚举
        /// </summary>
        public enum UpdateViewType
        {
            Full,        // 完整更新 - 重新创建存档槽UI
            Selection,   // 仅更新选择状态
            SpecificSlot // 仅更新特定存档槽的数据
        }

        /// <summary>
        /// 更新视图显示
        /// 根据更新原因区分处理 - 避免点击存档槽时重复创建UI
        /// </summary>
        /// <param name="updateType">更新类型，默认为完整更新</param>
        public virtual void UpdateView(UpdateViewType updateType = UpdateViewType.Full)
        {
            // 根据更新类型执行不同的更新操作
            switch (updateType)
            {
                case UpdateViewType.Full:
                    // 完整更新 - 重新创建存档槽UI
                    if (_createSaveSlotCoroutine != null)
                    {
                        StopCoroutine(_createSaveSlotCoroutine);
                    }
                    _createSaveSlotCoroutine = StartCoroutine(CreateSaveSlotUIs());
                    break;
                case UpdateViewType.Selection:
                    // 仅更新选择状态 - 不重新创建存档槽UI
                    _navigationHelper?.UpdateSelectionUI();
                    break;
                case UpdateViewType.SpecificSlot:
                    // 仅更新特定存档槽的数据
                    // 使用当前选中的存档槽信息更新UI显示
                    if (currentSelectedSlotInfo != null && !string.IsNullOrEmpty(currentSelectedSlotInfo.SlotName))
                    {
                        UpdateSpecificSaveSlotUI(currentSelectedSlotInfo.SlotName);
                    }
                    break;
            }
            
            // 无论何种更新类型，都更新按钮状态
            UpdateSaveOptionsButtonStates();
        }
        
        /// <summary>
        /// 更新特定存档槽的UI显示
        /// 只更新单个存档槽的数据而不重建所有UI，提高性能和用户体验
        /// </summary>
        /// <param name="slotName">要更新的存档槽名称</param>
        public virtual void UpdateSpecificSaveSlotUI(string slotName)
        {
            if (_model == null || string.IsNullOrEmpty(slotName) || _saveSlotUIs == null)
                return;
            
            // 查找对应的存档槽UI
            ISaveSlotUI slotUI = _saveSlotUIs.Find(ui => ui.SlotName == slotName);
            if (slotUI == null)
                return;
            
            // 查找对应的存档槽数据
            SaveSlotInfo slotInfo = _model.SaveSlots.Find(slot => slot.SlotName == slotName);
            if (slotInfo == null)
                return;
            
            // 更新存档槽UI的显示
            slotUI.UpdateDisplay();
            
            // 更新按钮状态
            UpdateSaveOptionsButtonStates();
        }

        
        /// <summary>
        /// 创建存档槽UI
        /// 使用Addressable Assets异步加载预制件
        /// </summary>
        protected virtual IEnumerator CreateSaveSlotUIs()
        {
            if (_model == null || saveSlotsContainer == null || _asyncOperationHelper == null)
                yield break;
            
            // 清除现有的存档槽UI
            ClearSaveSlotUIs();
            
            if (_model.SaveSlots == null || _model.SaveSlots.Count == 0)
                yield break;
            
            // 调用异步操作助手创建存档槽UI
            IEnumerator createUIsCoroutine = _asyncOperationHelper.CreateSaveSlotUIs(
                saveSlotPrefabRef,        // 预制件引用
                saveSlotsContainer,       // 容器变换
                maxSaveSlots,             // 创建数量
                _model.SaveSlots,         // 存档槽信息列表
                this,                     // 当前视图引用
                uis =>
                {
                    _saveSlotUIs = uis;
                },
                null                      // 进度回调（可选）
            );
            
            // 运行协程
            yield return StartCoroutine(createUIsCoroutine);
        
        // 初始化选中状态
        if (_navigationHelper != null && _saveSlotUIs.Count > 0)
        {
            _navigationHelper.InitializeSelection(_saveSlotUIs);
        }
        }
        
        /// <summary>
        /// 清除存档槽UI
        /// </summary>
        protected virtual void ClearSaveSlotUIs()
        {
            // 使用异步操作辅助类清除存档槽UI
            _asyncOperationHelper?.CleanupSaveSlotUIs(_saveSlotUIs);

            // 重置导航状态
            _navigationHelper?.ResetSelection();
        }
        
        /// <summary>
        /// 处理键盘导航
        /// </summary>
        private void Update()
        {
            _navigationHelper?.HandleKeyboardNavigation();
        }
        #endregion
        
        #region 事件处理方法
        /// <summary>
        /// 处理导航辅助类触发的存档槽选中事件
        /// </summary>
        /// <param name="index">选中的存档槽索引</param>
        private void OnNavigationSlotSelected(int index)
        {
            if (_saveSlotUIs != null && index >= 0 && index < _saveSlotUIs.Count)
            {
                var slotUI = _saveSlotUIs[index];
                var slotInfo = _model?.SaveSlots.Find(slot => slot.SlotName == slotUI.SlotName);
                if (slotInfo != null && slotInfo.SaveData != null)
                {
                    ShowSaveOptionsMenu(slotInfo);
                }
                else
                {
                    HideSaveOptionsMenu();
                }
            }
        }
        
        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        protected virtual void BindButtonEvents()
        {
            // 使用UI事件处理辅助类绑定按钮事件
            _uiEventHandler?.BindEvents();
        }
        
        /// <summary>
        /// 解绑按钮事件
        /// </summary>
        protected virtual void UnbindButtonEvents()
        {
            // 使用UI事件处理辅助类解绑按钮事件
            _uiEventHandler?.UnbindEvents();
        }
        
        /// <summary>
        /// 订阅模型事件
        /// </summary>
        protected virtual void SubscribeToModelEvents()
        {
            if (_model != null)
            {
                _model.OnSaveSlotsUpdated += OnSaveSlotsUpdated;
                _model.OnSelectedSaveSlotChanged += OnSelectedSaveSlotChanged;
            }
        }
        
        /// <summary>
        /// 取消订阅模型事件
        /// </summary>
        protected virtual void UnsubscribeFromModelEvents()
        {
            if (_model != null)
            {
                _model.OnSaveSlotsUpdated -= OnSaveSlotsUpdated;
                _model.OnSelectedSaveSlotChanged -= OnSelectedSaveSlotChanged;
            }
        }
        
        /// <summary>
        /// 处理选中存档槽变更事件
        /// 更新UI上的存档槽选中指示
        /// 确保导航辅助类中的选中索引与模型保持同步
        /// </summary>
        protected virtual void OnSelectedSaveSlotChanged()
        {
            if (_model != null && _navigationHelper != null && _saveSlotUIs != null && _saveSlotUIs.Count > 0)
            {
                // 获取模型中选中的存档槽名称
                string selectedSlotName = _model.SelectedSaveSlotName;
                
                if (!string.IsNullOrEmpty(selectedSlotName))
                {
                    // 遍历存档槽UI，找到与选中名称匹配的存档槽
                    for (int i = 0; i < _saveSlotUIs.Count; i++)
                    {
                        if (_saveSlotUIs[i] != null && _saveSlotUIs[i].SlotName == selectedSlotName)
                        {
                            // 更新导航辅助类中的选中索引
                            _navigationHelper.SelectAndScrollToSlot(i);
                            break;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理存档槽点击事件
        /// 仅负责与控制器通信，二级菜单的显示/隐藏由SaveSlot类自行处理
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <param name="saveData">存档数据，如果为null表示空存档槽</param>
        public virtual void OnSaveSlotClick(string slotName, SaveData saveData = null)
        {
            if (m_controller != null)
            {
                m_controller.HandleSaveSlotSelected(slotName, saveData);
            }
        }
        
        /// <summary>
        /// 处理存档槽更新事件
        /// </summary>
        protected virtual void OnSaveSlotsUpdated()
        {
            UpdateView();
        }
        #endregion
        
        #region 二级菜单相关方法
        /// <summary>
        /// 显示保存选项菜单
        /// </summary>
        /// <param name="slotInfo">存档槽信息</param>
        public virtual void ShowSaveOptionsMenu(SaveSlotInfo slotInfo)
        {
            currentSelectedSlotInfo = slotInfo;
            
            // 更新事件处理器中的当前选中存档槽信息
            _uiEventHandler?.SetCurrentSelectedSlotInfo(slotInfo);
            
            // 更新按钮状态
            UpdateSaveOptionsButtonStates();
            
            // 显示二级菜单面板
            if (saveOptionsPanel != null)
            {
                saveOptionsPanel.SetActive(true);
                // 添加点击面板外部区域隐藏菜单的检测
                StartCoroutine(CheckForClickOutsidePanel());
            }
        }
        
        /// <summary>
        /// 隐藏保存选项菜单
        /// </summary>
        public virtual void HideSaveOptionsMenu()
        {
            // 隐藏整合后的二级菜单面板
            if (saveOptionsPanel != null)
            {
                saveOptionsPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// 检查点击是否在面板外部，如果是则隐藏菜单
        /// </summary>
        private IEnumerator CheckForClickOutsidePanel()
        {
            while (saveOptionsPanel != null && saveOptionsPanel.activeSelf)
            {
                // 等待下一个帧
                yield return null;
                
                // 检查是否有鼠标点击
                if (Input.GetMouseButtonDown(0))
                {
                    // 获取鼠标点击位置
                    Vector2 clickPosition = Input.mousePosition;
                    
                    // 检查点击是否在面板内部或任何确认面板上
                    if (saveOptionsPanel.TryGetComponent<RectTransform>(out var panelRectTransform))
                    {
                        // 将面板矩形转换为屏幕空间
                        Rect panelRect = new(
                            panelRectTransform.position.x - panelRectTransform.sizeDelta.x * panelRectTransform.pivot.x,
                            panelRectTransform.position.y - panelRectTransform.sizeDelta.y * panelRectTransform.pivot.y,
                            panelRectTransform.sizeDelta.x,
                            panelRectTransform.sizeDelta.y
                        );
                        
                        // 检查点击是否在面板外部，并且不在任何确认面板上
                        if (!panelRect.Contains(clickPosition) && !IsClickOnAnyConfirmationPanel(clickPosition))
                        {
                            HideSaveOptionsMenu();
                            break;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 检查点击是否在任何确认面板上
        /// </summary>
        /// <param name="clickPosition">点击位置</param>
        /// <returns>点击是否在确认面板上</returns>
        private bool IsClickOnAnyConfirmationPanel(Vector2 clickPosition)
        {
            // 检查确认面板是否存在且激活，并判断点击是否在面板上
            if (confirmPanel != null && confirmPanel.activeSelf && confirmPanel.TryGetComponent<RectTransform>(out var panelRectTransform))
            {
                // 将面板矩形转换为屏幕空间
                Rect panelRect = new(
                    panelRectTransform.position.x - panelRectTransform.sizeDelta.x * panelRectTransform.pivot.x,
                    panelRectTransform.position.y - panelRectTransform.sizeDelta.y * panelRectTransform.pivot.y,
                    panelRectTransform.sizeDelta.x,
                    panelRectTransform.sizeDelta.y
                );
                
                return panelRect.Contains(clickPosition);
            }
            
            return false;
        }
        
        /// <summary>
        /// 更新保存选项菜单的按钮状态
        /// </summary>
        protected virtual void UpdateSaveOptionsButtonStates()
        {
            // 检查存档数据是否存在来更新按钮状态
            bool hasSaveData = currentSelectedSlotInfo != null && currentSelectedSlotInfo.SaveData != null;
            
            // 使用事件处理器统一更新按钮状态
            _uiEventHandler?.UpdateButtonStates(currentSelectedSlotInfo != null, hasSaveData);
        }
        #endregion
        
        #region 确认面板相关方法
        /// <summary>
        /// 处理保存按钮点击事件
        /// </summary>
        /// <param name="slotInfo">存档槽信息，如果为null则使用当前选中的存档槽</param>
        public virtual void HandleSaveButtonClick(SaveSlotInfo slotInfo = null)
        {
            // 如果提供了slotInfo参数，则使用它；否则使用当前选中的存档槽
            var targetSlotInfo = slotInfo ?? currentSelectedSlotInfo;
            
            if (m_controller != null && targetSlotInfo != null)
            {
                // 显示保存确认面板
                ShowConfirmPanel(ConfirmType.Overwrite, targetSlotInfo.SlotName);
            }
        }
        
        /// <summary>
        /// 处理加载按钮点击事件
        /// </summary>
        /// <param name="slotInfo">存档槽信息，如果为null则使用当前选中的存档槽</param>
        public virtual void HandleLoadButtonClick(SaveSlotInfo slotInfo = null)
        {
            // 如果提供了slotInfo参数，则使用它；否则使用当前选中的存档槽
            var targetSlotInfo = slotInfo ?? currentSelectedSlotInfo;
            
            if (m_controller != null && targetSlotInfo != null)
            {
                // 显示加载确认面板
                ShowConfirmPanel(ConfirmType.Load, targetSlotInfo.SlotName);
            }
        }
        
        /// <summary>
        /// 处理删除按钮点击事件
        /// </summary>
        /// <param name="slotInfo">存档槽信息，如果为null则使用当前选中的存档槽</param>
        public virtual void HandleDeleteButtonClick(SaveSlotInfo slotInfo = null)
        {
            // 如果提供了slotInfo参数，则使用它；否则使用当前选中的存档槽
            var targetSlotInfo = slotInfo ?? currentSelectedSlotInfo;
            
            if (m_controller != null && targetSlotInfo != null)
            {
                // 调用确认删除方法显示确认面板
                ConfirmDeleteSave(targetSlotInfo.SlotName);
            }
        }
        
        /// <summary>
        /// 确认删除存档
        /// 显示删除确认面板
        /// </summary>
        /// <param name="slotName">要删除的存档槽名称</param>
        protected virtual void ConfirmDeleteSave(string slotName)
        {
            // 显示删除确认面板
            ShowConfirmPanel(ConfirmType.Delete, slotName);
        }
        
        /// <summary>
        /// 显示确认面板
        /// </summary>
        /// <param name="actionType">确认操作类型</param>
        /// <param name="slotName">存档槽名称</param>
        protected virtual void ShowConfirmPanel(ConfirmType actionType, string slotName)
        {
            // 设置当前确认操作类型和存档槽名称
            _currentConfirmActionType = actionType;
            _currentConfirmSlotName = slotName;
            
            // 设置确认信息文本
            if (confirmDirectionText != null && m_controller != null && m_controller.Config != null)
            {
                switch (actionType)
                {
                    case ConfirmType.Overwrite:
                        // 保存操作通常不需要确认文本或使用通用文本
                        confirmDirectionText.text = m_controller.Config.OverwriteSaveConfirmText;
                        break;
                    case ConfirmType.Load:
                        confirmDirectionText.text = m_controller.Config.LoadSaveConfirmText;
                        break;
                    case ConfirmType.Delete:
                        confirmDirectionText.text = m_controller.Config.DeleteSaveConfirmText;
                        break;
                }
            }
            
            // 显示确认面板
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(true);
                // 显示确认面板时可以隐藏二级菜单或保持显示，根据设计需求决定
                // HideSaveOptionsMenu();
            }
        }
        
        /// <summary>
        /// ISaveSlotMenuHandler接口实现：显示确认面板
        /// </summary>
        /// <param name="actionType">确认操作类型</param>
        /// <param name="slotName">存档槽名称</param>
        void ISaveSlotMenuHandler.ShowConfirmPanel(ConfirmType actionType, string slotName)
        {
            // 直接调用内部的ShowConfirmPanel方法，无需类型转换
            ShowConfirmPanel(actionType, slotName);
        }
        
        /// <summary>
        /// ISaveSlotMenuHandler接口实现：隐藏确认面板
        /// </summary>
        void ISaveSlotMenuHandler.HideConfirmPanel()
        {
            // 调用内部的HideConfirmPanel方法
            HideConfirmPanel();
        }
        
        /// <summary>
        /// ISaveSlotMenuHandler接口实现：处理保存游戏操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        void ISaveSlotMenuHandler.HandleSaveGame(string slotName)
        {
            if (m_controller != null)
            {
                m_controller.HandleSaveGame(slotName);
            }
        }
        
        /// <summary>
        /// ISaveSlotMenuHandler接口实现：处理加载游戏操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        void ISaveSlotMenuHandler.HandleLoadGame(string slotName)
        {
            if (m_controller != null)
            {
                m_controller.HandleLoadGame(slotName);
            }
        }
        
        /// <summary>
        /// ISaveSlotMenuHandler接口实现：处理删除存档操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        void ISaveSlotMenuHandler.HandleDeleteSave(string slotName)
        {
            if (m_controller != null)
            {
                m_controller.HandleDeleteSave(slotName);
            }
        }
        
        /// <summary>
        /// 隐藏确认面板
        /// </summary>
        protected virtual void HideConfirmPanel()
        {
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// 处理确认按钮点击事件
        /// 根据当前确认操作类型执行相应的操作
        /// </summary>
        protected virtual void HandleConfirmButtonClick()
        {
            if (m_controller != null)
            {
                switch (_currentConfirmActionType)
                {
                    case ConfirmType.Overwrite:
                        m_controller.HandleSaveGame(_currentConfirmSlotName);
                        // 保存完成后更新特定存档槽的视图显示
                        UpdateView(UpdateViewType.SpecificSlot);
                        break;
                    case ConfirmType.Load:
                        m_controller.HandleLoadGame(_currentConfirmSlotName);
                        break;
                    case ConfirmType.Delete:
                        m_controller.HandleDeleteSave(_currentConfirmSlotName);
                        break;
                }
            }
            // 隐藏确认面板
            HideConfirmPanel();
        }
        
        /// <summary>
        /// 处理取消按钮点击事件
        /// </summary>
        protected virtual void HandleCancelButtonClick()
        {
            // 隐藏确认面板
            HideConfirmPanel();
        }
        #endregion
        
        #region 视图控制方法
        /// <summary>
        /// 处理返回按钮点击事件
        /// </summary>
        public virtual void HandleBackButtonClick()
        {
            HideSaveOptionsMenu();
            if (m_controller != null)
            {
                m_controller.HandleBackToMainMenu();
            }
        }
        
        /// <summary>
        /// 显示视图
        /// </summary>
        public override void Show()
        {
            m_canvasGroup.alpha = 1;
            m_canvasGroup.interactable = true;
            m_canvasGroup.blocksRaycasts = true;
            HideConfirmPanel();
            HideSaveOptionsMenu();
        }
        
        /// <summary>
        /// 隐藏视图
        /// </summary>
        public override void Hide()
        {
            m_canvasGroup.alpha = 0;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            UnbindButtonEvents();
            UnsubscribeFromModelEvents();
            ClearSaveSlotUIs();
            
            // 重置整合后的二级菜单状态
            currentSelectedSlotInfo = null;
        }
        #endregion
    }
}