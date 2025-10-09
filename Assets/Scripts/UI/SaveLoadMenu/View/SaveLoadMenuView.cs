using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using TMPro;
using MyGame.Data;
using Logger;
using MyGame.UI.SaveLoad.Controller;
using System.Collections;
using UnityEditor.EditorTools;

namespace MyGame.UI.SaveLoad.View
{
    /// <summary>
    /// 存档菜单视图类
    /// 负责显示存档菜单的UI元素并处理用户交互
    /// 继承自BaseView以保持架构一致性
    /// </summary>
    public class SaveLoadMenuView : BaseView<SaveLoadMenuController>
    {
        [Header("存档槽")]
        [Tooltip("存档槽容器，用于放置所有存档槽UI")]
        [SerializeField] protected Transform saveSlotsContainer;
        [Tooltip("存档槽预制件引用")]
        [SerializeField] protected AssetReference saveSlotPrefabRef;
        [Tooltip("最大存档槽数量")]
        [SerializeField] protected int maxSaveSlots = 10;

        
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
        
        // 确认操作类型枚举
        protected enum ConfirmActionType
        {
            Save,
            Load,
            Delete
        }
        
        // 当前确认操作类型
        private ConfirmActionType _currentConfirmActionType;
        
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
        

        private const string LOG_MODULE = LogModules.SAVE;
        
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
                _menuTitleText.text = "存档/读档菜单";
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
        /// 控制器绑定后的回调
        /// </summary>
        protected override void OnControllerBound()
        {
            // When controller is bound, set model reference
            if (m_controller != null && m_controller.Model != null)
            {
                Model = m_controller.Model;
            }
        }
        
        /// <summary>
        /// 控制器解绑后的回调
        /// </summary>
        protected override void OnControllerUnbound()
        {
            // When controller is unbound, clear model reference
            if (_model != null)
            {
                UnsubscribeFromModelEvents();
                _model = null;
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
        
        /// <summary>
        /// 更新视图显示
        /// </summary>
        public virtual void UpdateView()
        {
            CreateSaveSlotUIs();
            UpdateSaveOptionsButtonStates();
        }

        
        // 用于跟踪异步加载操作
        private readonly List<AsyncOperationHandle<GameObject>> _prefabLoadHandles = new();
        
        /// <summary>
        /// 创建存档槽UI
        /// 使用Addressable Assets异步加载预制件
        /// </summary>
        protected virtual async void CreateSaveSlotUIs()
        {
            if (_model == null || saveSlotsContainer == null)
                return;
            
            // 清除现有的存档槽UI
            ClearSaveSlotUIs();
            
            if (_model.SaveSlots == null || _model.SaveSlots.Count == 0)
                return;
            
            // 创建存档槽UI
            for (int i = 0; i < _model.SaveSlots.Count && i < maxSaveSlots; i++)
            {
                var slotInfo = _model.SaveSlots[i];
                
                // 异步加载存档槽预制件
                var handle = saveSlotPrefabRef.LoadAssetAsync<GameObject>();
                _prefabLoadHandles.Add(handle);
                await handle.Task;
                
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    // 实例化存档槽UI
                    var saveSlotGO = Instantiate(handle.Result, saveSlotsContainer);
                    
                    // 获取ISaveSlotUI组件
                    if (saveSlotGO.TryGetComponent<ISaveSlotUI>(out var saveSlotUI))
                    {
                        // 初始化存档槽UI
                        saveSlotUI.Initialize(slotInfo, this);
                        
                        // 添加到存档槽UI列表
                        _saveSlotUIs.Add(saveSlotUI);
                        
                        // 添加点击事件
                        saveSlotGO.GetComponent<Button>().onClick.AddListener(() => OnSaveSlotClick(saveSlotUI.SlotName, slotInfo.SaveData));
                    }
                }
            }
        }
        
        /// <summary>
        /// 清除存档槽UI
        /// </summary>
        protected virtual void ClearSaveSlotUIs()
        {
            // 清除所有存档槽UI对象
            foreach (var saveSlotUI in _saveSlotUIs)
            {
                if (saveSlotUI != null && saveSlotUI is MonoBehaviour monoBehaviour)
                {
                    Destroy(monoBehaviour.gameObject);
                }
            }
            
            _saveSlotUIs.Clear();
            
            // 清除加载句柄
            ClearLoadHandles();
        }
        
        /// <summary>
        /// 清除加载句柄
        /// </summary>
        protected virtual void ClearLoadHandles()
        {
            foreach (var handle in _prefabLoadHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            
            _prefabLoadHandles.Clear();
        }
        
        /// <summary>
        /// 处理选中存档槽变更事件
        /// </summary>
        protected virtual void OnSelectedSaveSlotChanged()
        {
            // 不再需要更新按钮状态，由二级菜单控制器处理
        }
        
        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        protected virtual void BindButtonEvents()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBackButtonClick);
            }
            
            // 绑定二级菜单按钮事件
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(HandleSaveButtonClick);
            }
            
            if (loadButton != null)
            {
                loadButton.onClick.AddListener(HandleLoadButtonClick);
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(HandleDeleteButtonClick);
            }
            
            // 绑定确认面板按钮事件
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirmButtonClick);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HandleCancelButtonClick);
            }
        }
        
        /// <summary>
        /// 解绑按钮事件
        /// </summary>
        protected virtual void UnbindButtonEvents()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackButtonClick);
            }
            
            // 解绑二级菜单按钮事件
            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(HandleSaveButtonClick);
            }
            
            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(HandleLoadButtonClick);
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(HandleDeleteButtonClick);
            }
            
            // 解绑确认面板按钮事件
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmButtonClick);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelButtonClick);
            }
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
        /// 处理存档槽点击事件
        /// - 如果是非空存档槽，显示二级菜单
        /// - 如果是空存档槽，直接选中但不显示二级菜单
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <param name="saveData">存档数据，如果为null表示空存档槽</param>
        public virtual void OnSaveSlotClick(string slotName, SaveData saveData = null)
        {
            if (m_controller != null)
            {
                m_controller.HandleSaveSlotSelected(slotName, saveData);
            }
            
            // 点击非空存档槽时显示二级菜单
            if (saveData != null)
            {
                // 查找对应的存档槽信息
                var slotInfo = _model.SaveSlots.Find(slot => slot.SlotName == slotName);
                if (slotInfo != null)
                {
                    // 使用整合后的二级菜单
                    ShowSaveOptionsMenu(slotInfo);
                }
            }
            else
            {
                // 隐藏二级菜单
                HideSaveOptionsMenu();
            }
        }
        
        /// <summary>
        /// 显示保存选项菜单
        /// </summary>
        /// <param name="slotInfo">存档槽信息</param>
        public virtual void ShowSaveOptionsMenu(SaveSlotInfo slotInfo)
        {
            currentSelectedSlotInfo = slotInfo;
            
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
        /// 处理保存按钮点击事件
        /// </summary>
        public virtual void HandleSaveButtonClick()
        {
            if (m_controller != null && currentSelectedSlotInfo != null)
            {
                // 显示保存确认面板
                ShowConfirmPanel(ConfirmActionType.Save, currentSelectedSlotInfo.SlotName);
            }
        }
        
        /// <summary>
        /// 处理加载按钮点击事件
        /// </summary>
        public virtual void HandleLoadButtonClick()
        {
            if (m_controller != null && currentSelectedSlotInfo != null)
            {
                // 显示加载确认面板
                ShowConfirmPanel(ConfirmActionType.Load, currentSelectedSlotInfo.SlotName);
            }
        }
        
        /// <summary>
        /// 处理删除按钮点击事件
        /// </summary>
        public virtual void HandleDeleteButtonClick()
        {
            if (m_controller != null && currentSelectedSlotInfo != null)
            {
                // 调用确认删除方法显示确认面板
                ConfirmDeleteSave(currentSelectedSlotInfo.SlotName);
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
            ShowConfirmPanel(ConfirmActionType.Delete, slotName);
        }
        
        /// <summary>
        /// 显示确认面板
        /// </summary>
        /// <param name="actionType">确认操作类型</param>
        /// <param name="slotName">存档槽名称</param>
        protected virtual void ShowConfirmPanel(ConfirmActionType actionType, string slotName)
        {
            // 设置当前确认操作类型和存档槽名称
            _currentConfirmActionType = actionType;
            _currentConfirmSlotName = slotName;
            
            // 设置确认信息文本
            if (confirmDirectionText != null && m_controller != null && m_controller.Config != null)
            {
                switch (actionType)
                {
                    case ConfirmActionType.Save:
                        // 保存操作通常不需要确认文本或使用通用文本
                        confirmDirectionText.text = "确定要保存到该存档槽吗？这将覆盖该存档的内容。";
                        break;
                    case ConfirmActionType.Load:
                        confirmDirectionText.text = m_controller.Config.LoadSaveConfirmText;
                        break;
                    case ConfirmActionType.Delete:
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
                    case ConfirmActionType.Save:
                        m_controller.HandleSaveGame(_currentConfirmSlotName);
                        break;
                    case ConfirmActionType.Load:
                        m_controller.HandleLoadGame(_currentConfirmSlotName);
                        break;
                    case ConfirmActionType.Delete:
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
            
            if (saveButton != null)
            {
                saveButton.interactable = true; // 保存按钮始终可用
            }
            
            if (loadButton != null)
            {
                loadButton.interactable = hasSaveData; // 加载按钮仅在有存档数据时可用
            }
            
            if (deleteButton != null)
            {
                deleteButton.interactable = hasSaveData; // 删除按钮仅在有存档数据时可用
            }
        }
        

        
        /// <summary>
        /// 处理存档槽更新事件
        /// </summary>
        protected virtual void OnSaveSlotsUpdated()
        {
            UpdateView();
        }
        

        
        /// <summary>
        /// 显示视图
        /// </summary>
        public override void Show()
        {
            gameObject.SetActive(true);
            HideSaveOptionsMenu();
        }

        /// <summary>
        /// 隐藏保存选项菜单
        /// </summary>
        protected virtual void HideSaveOptionsMenu()
        {
            // 隐藏整合后的二级菜单面板
            if (saveOptionsPanel != null)
            {
                saveOptionsPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// 隐藏视图
        /// </summary>
        public override void Hide()
        {
            gameObject.SetActive(false);
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
    }
}