using UnityEngine;
using UnityEngine.UI;
using MyGame.Events;
using MyGame.UI.SaveLoad.Model;
using MyGame.UI.SaveLoad.Controller;
using MyGame.Data;
using Logger;
using TMPro;
using MyGame.UI.SaveLoad.View.Components;
using System.Collections.Generic;
using MyGame.UI.SaveLoadMenu.View.Components;

namespace MyGame.UI.SaveLoad.View
{
    /// <summary>
    /// 存档菜单视图类
    /// 负责显示UI元素和响应用户输入
    /// </summary>
    public class SaveLoadMenuView : BaseView<SaveLoadMenuController>, IConfirm
    {
        #region 生命周期
    /// <summary>
    /// 初始化视图组件
    /// </summary>
    protected override void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 当对象被销毁时
    /// </summary>
    protected override void OnDestroy()
    {
        Cleanup();
    }
    #endregion

    #region UI元素
    [Header("基础UI元素")]
    [Tooltip("画布组")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [Tooltip("返回按钮")]
    [SerializeField] private Button _backButton;
    [Tooltip("确认对话框")]
    [SerializeField] private ConfirmDialog _confirmDialog;

    [Header("模式切换")]
    [Tooltip("保存模式按钮")]
    [SerializeField] private Button _saveModeButton;
    [Tooltip("加载模式按钮")]
    [SerializeField] private Button _loadModeButton;
    [Tooltip("模式标题文本")]
    [SerializeField] private TextMeshProUGUI _modeTitleText;

    [Header("存档槽区域")]
    [Tooltip("存档槽容器")]
    [SerializeField] private Transform _saveSlotsContainer;
    [Tooltip("存档槽预制体")]
    [SerializeField] private GameObject _saveSlotPrefab;
    
    [Header("编辑器配置的存档槽")]
    [Tooltip("存档槽1")]
    [SerializeField] private SaveSlot _saveSlot1;
    [Tooltip("存档槽2")]
    [SerializeField] private SaveSlot _saveSlot2;
    [Tooltip("存档槽3")]
    [SerializeField] private SaveSlot _saveSlot3;
    [Tooltip("存档槽4")]
    [SerializeField] private SaveSlot _saveSlot4;

    [Header("分页控制")]
    [Tooltip("上一页按钮")]
    [SerializeField] private Button _prevPageButton;
    [Tooltip("下一页按钮")]
    [SerializeField] private Button _nextPageButton;
    [Tooltip("分页指示器文本")]
    [SerializeField] private TextMeshProUGUI _pageIndicatorText;
    #endregion

        #region 成员变量
        private static readonly string LOG_MODULE = LogModules.SAVE;
        private readonly List<SaveSlot> _saveSlots = new();

        #endregion

        #region 初始化和清理方法
        /// <summary>
        /// 初始化面板
        /// 重写IUIPanel接口的Initialize方法
        /// </summary>
        public override void Initialize()
        {
            // 初始化CanvasGroup
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // 初始化存档槽列表
            _saveSlots.AddRange(new[] { _saveSlot1, _saveSlot2, _saveSlot3, _saveSlot4 });

            // 绑定事件监听器
            BindButtonEvents();

            // 初始状态设置为隐藏
            Hide();

            base.Initialize();
        }


        /// <summary>
        /// 清理面板资源
        /// 重写IUIPanel接口的Cleanup方法
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
        }
        #endregion

        #region 显示和隐藏控制
        /// <summary>
        /// 显示视图
        /// </summary>
        public override void Show()
        {
            // 设置CanvasGroup可见性
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            // 刷新UI内容
            RefreshUI();
        }

        /// <summary>
        /// 隐藏视图
        /// </summary>
        public override void Hide()
        {
            // 设置CanvasGroup不可见
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
        }
        #endregion

        #region 事件绑定和处理
        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        private void BindButtonEvents()
        {
            // 返回按钮
            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(HandleBackButtonClicked);
            }

            // 模式切换按钮
            if (_saveModeButton != null)
            {
                _saveModeButton.onClick.RemoveAllListeners();
                _saveModeButton.onClick.AddListener(HandleSaveModeButtonClicked);
            }

            if (_loadModeButton != null)
            {
                _loadModeButton.onClick.RemoveAllListeners();
                _loadModeButton.onClick.AddListener(HandleLoadModeButtonClicked);
            }

            // 分页控制按钮
            if (_prevPageButton != null)
            {
                _prevPageButton.onClick.RemoveAllListeners();
                _prevPageButton.onClick.AddListener(HandlePrevPageButtonClicked);
            }

            if (_nextPageButton != null)
            {
                _nextPageButton.onClick.RemoveAllListeners();
                _nextPageButton.onClick.AddListener(HandleNextPageButtonClicked);
            }
        }

        /// <summary>
        /// 处理返回按钮点击
        /// </summary>
        private void HandleBackButtonClicked()
        {
            // 触发隐藏保存加载菜单事件
            GameEvents.TriggerMenuShow(UIType.SaveLoadMenu, false);
        }

        /// <summary>
        /// 处理保存模式按钮点击
        /// </summary>
        private void HandleSaveModeButtonClicked()
        {
            // 通知控制器切换到保存模式
            if(m_controller != null)
            {
                m_controller.ShowSaveMenu();
            }
        }

        /// <summary>
        /// 处理读取模式按钮点击
        /// </summary>
        private void HandleLoadModeButtonClicked()
        {
            // 通知控制器切换到加载模式
            if(m_controller != null)
            {
                m_controller.ShowLoadMenu();
            }
        }

        /// <summary>
        /// 处理上一页按钮点击
        /// </summary>
        private void HandlePrevPageButtonClicked()
        {
            // 通过控制器请求上一页
            if(m_controller != null)
            {
                m_controller.HandlePrevPage();
            }

        }

        /// <summary>
        /// 处理下一页按钮点击
        /// </summary>
        private void HandleNextPageButtonClicked()
        {
            // 通过控制器请求下一页
            if(m_controller != null)
            {
                m_controller.HandleNextPage();
            }
        }
        #endregion

        #region 模型变更事件处理
        /// <summary>
        /// 处理菜单模式变更
        /// </summary>
        /// <param name="mode">新的菜单模式</param>
        public void HandleMenuModeChanged(SaveLoadMenuModel.MenuMode mode)
        {
            // 更新标题文本
            if (_modeTitleText != null)
            {
                _modeTitleText.text = mode == SaveLoadMenuModel.MenuMode.Save ? "保存游戏" : "加载游戏";
            }

            // 刷新存档槽显示
            RefreshSaveSlots();
        }

        /// <summary>
        /// 处理页码变更
        /// </summary>
        public void HandlePageChanged(int currentPage, int totalPages)
        {
            // 更新页码指示器
            if (_pageIndicatorText != null)
            {
                _pageIndicatorText.text = string.Format("{0}/{1}", currentPage + 1, totalPages);
            }

            // 更新分页按钮状态
            UpdatePageButtonStates(currentPage, totalPages);

            // 刷新存档槽显示
            RefreshSaveSlots();
        }
        #endregion

        #region UI刷新方法
        /// <summary>
        /// 刷新整个UI
        /// </summary>
        private void RefreshUI()
        {
            // 通过控制器刷新UI数据
            m_controller.RefreshViewData();
        }

        /// <summary>
        /// 处理存档槽点击事件
        /// </summary>
        /// <param name="slotName">点击的存档槽名称</param>
        /// <param name="saveData">存档数据</param>
        public void OnSaveSlotClick(string slotName, SaveData saveData)
        {
            if (m_controller != null)
            {
                // 通知控制器存档槽被点击
                m_controller.HandleSaveSlotClick(slotName, saveData);
            }
        }

        /// <summary>
        /// 刷新存档槽显示
        /// </summary>
        private void RefreshSaveSlots()
        {
            // 检查控制器是否可用
            if (m_controller == null)
                return;

            // 通过控制器获取当前页的存档槽数据
            var currentPageSlots = m_controller.GetCurrentPageSaveSlots();
            if (currentPageSlots == null)
                return;

            // 更新存档槽显示
            for (int i = 0; i < _saveSlots.Count; i++)
            {
                var saveSlot = _saveSlots[i];
                if (saveSlot != null)
                {
                    // 检查是否有对应的存档数据
                    if (i < currentPageSlots.Count)
                    {
                        var saveSlotInfo = currentPageSlots[i];
                        saveSlot.gameObject.SetActive(true);
                        saveSlot.Initialize(saveSlotInfo, this); // 正确传递两个参数
                    }
                    else
                    {
                        // 如果没有更多的数据，则隐藏多余的存档槽
                        saveSlot.gameObject.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// 更新分页按钮状态
        /// </summary>
        private void UpdatePageButtonStates(int currentPage, int totalPages)
        {
            if (_prevPageButton != null && _nextPageButton != null)
            {
                _prevPageButton.interactable = currentPage > 0;
                _nextPageButton.interactable = currentPage < totalPages - 1;
            }
        }
        #endregion

        #region IConfirm接口实现
        /// <summary>
        /// 获取确认对话框消息文本
        /// </summary>
        /// <param name="state">对话框状态</param>
        public void GetConfirmMessage(ConfirmDialogState state)
        {
            if (_confirmDialog != null)
            {
                _confirmDialog.GetConfirmMessage(state);
            }
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="state">对话框状态，决定显示的文本内容</param>
        /// <param name="onConfirm">确认按钮点击时的回调</param>
        /// <param name="onCancel">取消按钮点击时的回调</param>
        public void ShowConfirmDialog(ConfirmDialogState state, System.Action onConfirm, System.Action onCancel = null)
        {
            if (_confirmDialog != null)
            {
                _confirmDialog.ShowConfirmDialog(state, onConfirm, onCancel);
            }
            else
            {
                Log.Warning(LOG_MODULE, "确认对话框未注册至视图");
                // 如果没有确认对话框组件，直接执行确认回调
                onConfirm?.Invoke();
            }
        }

        /// <summary>
        /// 隐藏确认对话框
        /// </summary>
        public void HideConfirmDialog()
        {
            if (_confirmDialog != null)
            {
                _confirmDialog.HideConfirmDialog();
            }
        }
        #endregion

        #region 控制器绑定回调
        /// <summary>
        /// 控制器绑定后的回调
        /// 在这里可以注册模型事件监听
        /// </summary>
        protected override void OnControllerBound()
        {
            base.OnControllerBound();
            // 控制器绑定后刷新UI
            RefreshUI();
        }

        /// <summary>
        /// 控制器解绑后的回调
        /// 在这里可以清理事件监听
        /// </summary>
        protected override void OnControllerUnbound()
        {
            base.OnControllerUnbound();
            // 清理资源
        }
        #endregion
    }
}