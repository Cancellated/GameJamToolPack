using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using MyGame.Data;
using Logger;
using MyGame.UI.SaveLoad.Controller;
using System;
using System.Collections;
using MyGame.UI.SaveLoadMenu.View;

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
        [Tooltip("存档槽预制件")]
        [SerializeField] protected GameObject saveSlotPrefab;
        [Tooltip("最大存档槽数量")]
        [SerializeField] protected int maxSaveSlots;

        
        [Tooltip("保存选项菜单组件")]
        [SerializeField] protected SaveOptions saveOptions;
        
        [Tooltip("返回按钮")]
        [SerializeField] protected Button backButton;

        [Header("确认面板")]
        [Tooltip("确认对话框组件")]
        [SerializeField] private ConfirmDialog confirmDialog;
        
        // 当前确认操作的存档槽名称
        private string _currentConfirmSlotName;


        protected SaveLoadMenuModel _model;
        protected List<SaveSlot> _saveSlots = new();
        
        // 当前选中的存档槽信息
        private SaveSlotInfo currentSelectedSlotInfo;
        

        private const string LOG_MODULE = LogModules.SAVELOADMENU;
        
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
            SetModel(m_controller.Model);
            BindButtonEvents();
            UpdateView();
        }
        
        /// <summary>
        /// 尝试自动绑定控制器
        /// </summary>
        protected override void TryBindController()
        {
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
            
            // 初始化SaveOptions组件，设置控制器引用
            if (saveOptions != null)
            {
                saveOptions.SetController(m_controller);
                Log.Info(LOG_MODULE, "SaveOptions组件初始化完成");
            }
        }
        
        /// <summary>
        /// 控制器解绑后的回调
        /// </summary>
        protected override void OnControllerUnbound()
        {

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
        /// 优先使用UpdateSaveSlotUIs更新现有槽位，只有在必要时才创建新槽位
        /// </summary>
        public virtual void UpdateView()
        {
            // 优先尝试更新现有存档槽
            if (_saveSlots.Count > 0 && _model != null && _model.SaveSlots != null && _model.SaveSlots.Count > 0)
            {
                UpdateSaveSlotUIs();
            }
            else
            {

                CreateSaveSlotUIs();
            }

        }

        
        // 用于跟踪异步加载操作
        /// <summary>
        /// 创建存档槽UI
        /// 仅在需要时创建新的存档槽
        /// </summary>
        protected virtual void CreateSaveSlotUIs()
        {
            // 参数校验
            if (_model == null || saveSlotsContainer == null || saveSlotPrefab == null)
            {
                Log.Error(LOG_MODULE, "CreateSaveSlotUIs: 缺少必要组件，无法创建存档槽UI");
                return;
            }
            
            // 检查存档槽数据
            if (_model.SaveSlots == null || _model.SaveSlots.Count == 0)
            {
                Log.Info(LOG_MODULE, "CreateSaveSlotUIs: 没有可用的存档槽数据，将创建空存档槽");
                
                // 创建一个默认的存档槽列表，用于显示空存档槽
                List<SaveSlotInfo> emptySlots = new();
                int slotCount = 4; // 默认创建4个空存档槽
                
                // 从配置中获取存档槽数量
                if (m_controller.Config != null)
                {
                    slotCount = m_controller.Config.SlotsPerPage;
                }
                
                // 创建空存档槽信息
                for (int i = 1; i <= slotCount; i++)
                {
                    emptySlots.Add(new SaveSlotInfo
                    {
                        SlotName = string.Format("save_{0}", i),
                        DisplayName = string.Format("存档槽 {0}", i),
                        IsAutoSave = false,
                        NotEmpty = false,
                        SaveData = null
                    });
                }
                
                // 设置临时的空存档槽数据
                int actualTargetCount = slotCount;
                
                // 如果需要，创建新的存档槽
                if (_saveSlots.Count < actualTargetCount)
                {
                    for (int i = _saveSlots.Count; i < actualTargetCount; i++)
                    {
                        var slotInfo = emptySlots[i];
                        try
                        {
                            // 实例化存档槽UI
                            var saveSlotGO = Instantiate(saveSlotPrefab, saveSlotsContainer);
                            
                            // 获取SaveSlot组件
                            if (saveSlotGO.TryGetComponent(out SaveSlot saveSlotUI))
                            {
                                // 初始化存档槽UI
                                saveSlotUI.Initialize(slotInfo, this);
                                
                                // 添加到存档槽UI列表
                                _saveSlots.Add(saveSlotUI);
                                
                                // 添加点击事件
                                saveSlotGO.GetComponent<Button>().onClick.AddListener(() => OnSaveSlotClick(saveSlotUI.SlotName, slotInfo.SaveData));
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(LOG_MODULE, "创建存档槽UI失败 (槽索引: " + i + "): " + ex.Message);
                            // 继续创建其他存档槽
                            continue;
                        }
                    }
                }
                
                // 确保所有创建的存档槽都是激活状态
                for (int i = 0; i < _saveSlots.Count && i < actualTargetCount; i++)
                {
                    var saveSlotUI = _saveSlots[i];
                    if (saveSlotUI != null && saveSlotUI is MonoBehaviour monoBehaviour)
                    {
                        monoBehaviour.gameObject.SetActive(true);
                        // 更新显示为空存档槽
                        var slotInfo = emptySlots[i];
                        saveSlotUI.Initialize(slotInfo, this);
                    }
                }
                
                // 如果存档槽数量过多，隐藏多余的存档槽
                if (_saveSlots.Count > actualTargetCount)
                {
                    for (int i = actualTargetCount; i < _saveSlots.Count; i++)
                    {
                        var saveSlotUI = _saveSlots[i];
                        if (saveSlotUI != null && saveSlotUI is MonoBehaviour monoBehaviour)
                        {
                            monoBehaviour.gameObject.SetActive(false);
                        }
                    }
                }
                
                return;
            }
            
            int targetCount = Math.Min(_model.SaveSlots.Count, maxSaveSlots);
            
            // 如果需要，创建新的存档槽
            if (_saveSlots.Count < targetCount)
            {
                for (int i = _saveSlots.Count; i < targetCount; i++)
                {
                    var slotInfo = _model.SaveSlots[i];
                    try
                    {
                        // 实例化存档槽UI
                        var saveSlotGO = Instantiate(saveSlotPrefab, saveSlotsContainer);
                        
                        // 获取SaveSlot组件
                        if (saveSlotGO.TryGetComponent(out SaveSlot saveSlotUI))
                        {
                            // 初始化存档槽UI
                            saveSlotUI.Initialize(slotInfo, this);
                            
                            // 添加到存档槽UI列表
                            _saveSlots.Add(saveSlotUI);
                            
                            // 添加点击事件
                            saveSlotGO.GetComponent<Button>().onClick.AddListener(() => OnSaveSlotClick(saveSlotUI.SlotName, slotInfo.SaveData));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(LOG_MODULE, "创建存档槽UI失败 (槽索引: " + i + "): " + ex.Message);
                        // 继续创建其他存档槽
                        continue;
                    }
                }
            }
            
            // 如果存档槽数量过多，隐藏或移除多余的存档槽
            if (_saveSlots.Count > targetCount)
            {
                // 注意：这里我们不删除多余的存档槽，而是保留它们以备后续使用
                // 只需要确保它们不显示即可
                for (int i = targetCount; i < _saveSlots.Count; i++)
                {
                    var saveSlotUI = _saveSlots[i];
                    if (saveSlotUI != null && saveSlotUI is MonoBehaviour monoBehaviour)
                    {
                        monoBehaviour.gameObject.SetActive(false);
                    }
                }
            }
            
            Log.Info(LOG_MODULE, "存档槽UI创建完成");
        }

        /// <summary>
        /// 更新现有存档槽的数据
        /// 单独的方法用于更新槽位内容
        /// </summary>
        protected virtual void UpdateSaveSlotUIs()
        {
            // 参数校验
            if (_model == null || _saveSlots.Count == 0)
            {
                Log.Error(LOG_MODULE, "UpdateSaveSlotUIs: 缺少必要数据，无法更新存档槽UI");
                return;
            }
            
            // 检查存档槽数据
            if (_model.SaveSlots == null || _model.SaveSlots.Count == 0)
            {
                Log.Info(LOG_MODULE, "UpdateSaveSlotUIs: 没有可用的存档槽数据，将显示空存档槽");
                
                // 创建一个默认的存档槽列表，用于显示空存档槽
                List<SaveSlotInfo> emptySlots = new();
                int slotCount = Math.Min(_saveSlots.Count, m_controller.Config != null ? m_controller.Config.SlotsPerPage : 4); 
                
                // 从配置中获取存档槽数量
                if (m_controller.Config != null)
                {
                    slotCount = Math.Min(_saveSlots.Count, m_controller.Config.SlotsPerPage);
                }
                
                // 创建空存档槽信息
                for (int i = 1; i <= slotCount; i++)
                {
                    emptySlots.Add(new SaveSlotInfo
                    {
                        SlotName = string.Format("save_{0}", i),
                        DisplayName = string.Format("存档槽 {0}", i),
                        IsAutoSave = false,
                        NotEmpty = false,
                        SaveData = null
                    });
                }
                
                // 确保所有存档槽都是激活状态，并更新为显示空存档槽
                for (int i = 0; i < _saveSlots.Count; i++)
                {
                    var saveSlotUI = _saveSlots[i];
                    if (saveSlotUI != null && saveSlotUI is MonoBehaviour monoBehaviour)
                    {
                        // 根据索引获取对应的空存档槽信息
                        SaveSlotInfo slotInfo = null;
                        if (i < emptySlots.Count)
                        {
                            slotInfo = emptySlots[i];
                        }
                        else
                        {
                            // 为超出默认数量的槽位创建新的空存档槽信息
                            slotInfo = new SaveSlotInfo
                            {
                                SlotName = string.Format("save_{0}", i + 1),
                                DisplayName = string.Format("存档槽 {0}", i + 1),
                                IsAutoSave = false,
                                NotEmpty = false,
                                SaveData = null
                            };
                        }
                        
                        monoBehaviour.gameObject.SetActive(true);
                        saveSlotUI.Initialize(slotInfo, this);
                        saveSlotUI.UpdateDisplay();
                    }
                }
                
                return;
            }
            
            int targetCount = Math.Min(_model.SaveSlots.Count, maxSaveSlots);
            
            // 更新现有存档槽的数据
            for (int i = 0; i < _saveSlots.Count && i < targetCount; i++)
            {
                var saveSlotUI = _saveSlots[i];
                var slotInfo = _model.SaveSlots[i];
                
                if (saveSlotUI != null && slotInfo != null)
                {
                    // 更新存档槽数据
                    saveSlotUI.Initialize(slotInfo, this);
                    saveSlotUI.UpdateDisplay();
                    
                    // 确保正在显示的存档槽是激活状态
                    if (saveSlotUI is MonoBehaviour monoBehaviour && !monoBehaviour.gameObject.activeSelf)
                    {
                        monoBehaviour.gameObject.SetActive(true);
                    }
                }
            }
            
            Log.Info(LOG_MODULE, "存档槽UI更新完成");
        }
        
        /// <summary>
        /// 清除存档槽UI
        /// </summary>
        protected virtual void ClearSaveSlotUIs()
        {
            // 清除所有存档槽UI对象
            foreach (var saveSlotUI in _saveSlots)
            {
                if (saveSlotUI != null && saveSlotUI is MonoBehaviour monoBehaviour)
                {
                    Destroy(monoBehaviour.gameObject);
                }
            }
            
            _saveSlots.Clear();
        }
        
        /// <summary>
        /// 处理选中存档槽变更事件
        /// 当模型中的选中存档槽发生变化时，更新UI以高亮显示当前选中的存档槽并更新操作菜单
        /// </summary>
        protected virtual void OnSelectedSaveSlotChanged()
        {
            // 如果模型为空，不执行任何操作
            if (_model == null)
            {
                Log.Info(LOG_MODULE, "OnSelectedSaveSlotChanged - 模型为空，不执行操作");
                return;
            }
            
            Log.Info(LOG_MODULE, string.Format("OnSelectedSaveSlotChanged - 触发事件，尝试更新UI显示"));
            
            // 获取当前选中的存档槽信息
            SaveSlotInfo selectedSlotInfo = null;
            string selectedSlotName = _model.SelectedSaveSlotName;
            
            // 更新存档槽的选中状态显示
            if (_saveSlots != null && _saveSlots.Count > 0)
            {
                // 遍历所有存档槽，设置它们的选中状态
                foreach (var saveSlot in _saveSlots)
                {
                    if (saveSlot != null)
                    {
                        // 根据存档槽名称判断是否为当前选中的存档槽
                        bool isSelected = !string.IsNullOrEmpty(selectedSlotName) && 
                                         saveSlot.SlotName == selectedSlotName;
                        
                        // 设置存档槽的选中状态，这会触发背景颜色的变化
                        saveSlot.SetSelected(isSelected);
                        
                        // 保存当前选中的存档槽信息
                        if (isSelected)
                        {
                            selectedSlotInfo = _model.SaveSlots.Find(slot => slot.SlotName == selectedSlotName);
                            Log.Info(LOG_MODULE, string.Format("OnSelectedSaveSlotChanged - 获取到的存档信息: {0}", selectedSlotInfo == null ? "空" : selectedSlotInfo.SlotName));
                        }
                    }
                }
            }
            
            if (saveOptions != null)
            {
                saveOptions.UpdateSaveOptionsMenu(selectedSlotInfo);
            }
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
            
            // 绑定确认对话框事件
            if (confirmDialog != null)
            {
                confirmDialog.OnConfirmAction += HandleConfirmAction;
                confirmDialog.OnCancelAction += HandleCancelAction;
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
            
            // 解绑确认对话框事件
            if (confirmDialog != null)
            {
                confirmDialog.OnConfirmAction -= HandleConfirmAction;
                confirmDialog.OnCancelAction -= HandleCancelAction;
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
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        public virtual void OnSaveSlotClick(string slotName, SaveData saveData = null)
        {
            if (m_controller != null)
            {
                m_controller.HandleSaveSlotSelected(slotName, saveData);
            }
        }
        
        /// <summary>
        /// 处理返回按钮点击事件
        /// </summary>
        public virtual void HandleBackButtonClick()
        {
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
            if (confirmDialog != null && m_controller != null && m_controller.Config != null)
            {
                confirmDialog.ShowConfirmPanel(ConfirmActionType.Delete, slotName);
            }
        }
        
        /// <summary>
        /// 处理确认操作事件
        /// </summary>
        /// <param name="actionType">确认操作类型</param>
        /// <param name="slotName">存档槽名称</param>
        protected virtual void HandleConfirmAction(ConfirmActionType actionType, string slotName)
        {
            if (m_controller != null)
            {
                switch (actionType)
                {
                    case ConfirmActionType.Save:
                        m_controller.HandleSaveGame(slotName);
                        break;
                    case ConfirmActionType.Load:
                        m_controller.HandleLoadGame(slotName);
                        break;
                    case ConfirmActionType.Delete:
                        m_controller.HandleDeleteSave(slotName);
                        break;
                    case ConfirmActionType.Create:
                        m_controller.HandleCreateNewGame(slotName);
                        break;
                }
            }
        }
        
        /// <summary>
        /// 处理取消操作事件
        /// </summary>
        protected virtual void HandleCancelAction()
        {
            // 取消操作不需要额外处理
        }
        
        
        

        
        /// <summary>
        /// 处理存档槽更新事件
        /// </summary>
        protected virtual void OnSaveSlotsUpdated()
        {
            UpdateView();
        }
        
        #region 视图操作
        /// <summary>
        /// 显示视图
        /// </summary>
        public override void Show()
        {
            m_canvasGroup.alpha = 1;
            m_canvasGroup.interactable = true;
            m_canvasGroup.blocksRaycasts = true;

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
        /// 显示确认面板
        /// </summary>
        /// <param name="actionType">确认操作类型</param>
        /// <param name="slotName">存档槽名称</param>
        public void ShowConfirmPanel(ConfirmActionType actionType, string slotName)
        {
            // 设置当前确认操作的存档槽名称
            _currentConfirmSlotName = slotName;
            
            if (confirmDialog != null)
            {
                confirmDialog.ShowConfirmPanel(actionType, slotName);
            }
        }
        
        /// <summary>
        /// 隐藏确认面板
        /// </summary>
        protected virtual void HideConfirmPanel()
        {
            if (confirmDialog != null)
            {
                confirmDialog.HideConfirmPanel();
            }
        }
        #endregion

        
        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            UnbindButtonEvents();
            UnsubscribeFromModelEvents();
            ClearSaveSlotUIs();
        }

        /// <summary>
        /// 当对象被销毁时
        /// 重写基类方法，确保正确释放资源
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Cleanup();
        }
    
    }
}