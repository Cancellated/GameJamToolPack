using System.Collections.Generic;
using MyGame.Data;
using MyGame.Events;
using MyGame.UI.SaveLoad.Model;
using MyGame.UI.SaveLoad.View;
using MyGame.UI.SaveLoad;
using MyGame.UI.SaveLoadMenu.View.Components;

namespace MyGame.UI.SaveLoad.Controller
{
    /// <summary>
    /// 存档菜单控制器类
    /// 负责连接模型和视图，处理用户交互
    /// </summary>
    public class SaveLoadMenuController : BaseController<SaveLoadMenuView, SaveLoadMenuModel>
    {
        #region 生命周期
    /// <summary>
    /// 初始化控制器和MVC组件
    /// </summary>
    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 当对象启用时
    /// </summary>
    private void OnEnable()
    {
        // 确保MVC组件正确初始化
        if (m_model == null)
        {
            Initialize();
        }
    }

    /// <summary>
    /// 当对象被销毁时
    /// </summary>
    private void OnDestroy()
    {
        Cleanup();
    }
    #endregion



        #region 初始化和清理方法
        /// <summary>
        /// 控制器初始化
        /// 调用基类Initialize并执行初始化逻辑
        /// </summary>
        public override void Initialize()
        {
            // 创建并初始化模型
            if (m_model == null)
            {
                m_model = new SaveLoadMenuModel();
                m_model.Initialize();
                SetModel(m_model);
            }

            base.Initialize();
        }

        /// <summary>
        /// 初始化控制器逻辑
        /// </summary>
        protected override void OnInitialize()
        {
            // 注册事件监听
            RegisterEvents();
        }

        /// <summary>
        /// 清理控制器资源
        /// </summary>
        public override void Cleanup()
        {
            // 取消注册事件
            UnregisterEvents();

            base.Cleanup();
        }

        /// <summary>
        /// 清理控制器逻辑
        /// </summary>
        protected override void OnCleanup()
        {
            // 清理模型资源
            m_model?.Cleanup();
        }
        #endregion

        #region 事件注册
        /// <summary>
        /// 注册事件
        /// </summary>
        protected void RegisterEvents()
        {
            if (m_model != null)
            {
                // 注册模型事件
                m_model.OnMenuModeChanged += HandleMenuModeChanged;
                m_model.OnPageChanged += HandlePageChanged;
            }
        }

        /// <summary>
        /// 解除注册事件
        /// </summary>
        protected void UnregisterEvents()
        {
            if (m_model != null)
            {
                // 解除注册模型事件
                m_model.OnMenuModeChanged -= HandleMenuModeChanged;
                m_model.OnPageChanged -= HandlePageChanged;
            }
        }
        #endregion

        #region 菜单显示控制
        /// <summary>
        /// 显示存档菜单（默认保存模式）
        /// </summary>
        public virtual void Show()
        {
            ShowSaveMenu();
        }

        /// <summary>
        /// 显示保存模式的菜单
        /// </summary>
        public virtual void ShowSaveMenu()
        {
            m_model?.SetMenuMode(SaveLoadMenuModel.MenuMode.Save);

            if (m_view != null)
            {
                m_view.Show();
            }
        }

        /// <summary>
        /// 显示读取模式的菜单
        /// </summary>
        public virtual void ShowLoadMenu()
        {
            m_model?.SetMenuMode(SaveLoadMenuModel.MenuMode.Load);

            if (m_view != null)
            {
                m_view.Show();
            }
        }

        /// <summary>
        /// 隐藏菜单
        /// </summary>
        public virtual void Hide()
        {
            if (m_view != null)
            {
                m_view.Hide();
            }
        }
        #endregion

        #region 分页操作
        /// <summary>
        /// 处理上一页请求
        /// </summary>
        public void HandlePrevPage()
        {
            m_model?.GoToPreviousPage();
        }

        /// <summary>
        /// 处理下一页请求
        /// </summary>
        public void HandleNextPage()
        {
            m_model?.GoToNextPage();
        }
        #endregion

        #region 存档操作
        /// <summary>
        /// 获取当前页的存档槽数据
        /// </summary>
        /// <returns>当前页的存档槽信息列表</returns>
        public List<SaveSlotInfo> GetCurrentPageSaveSlots()
        {
            return m_model?.GetCurrentPageSaveSlots;
        }

        /// <summary>
        /// 处理存档槽点击事件
        /// </summary>
        /// <param name="slotName">点击的存档槽名称</param>
        /// <param name="saveData">存档数据</param>
        public void HandleSaveSlotClick(string slotName, SaveData saveData)
        {
            // 设置选中的存档槽
            m_model?.SetSelectedSaveSlot(slotName, saveData);
        }
        #endregion

        #region 视图数据刷新
        /// <summary>
        /// 刷新视图数据
        /// </summary>
        public void RefreshViewData()
        {
            if (m_model != null && m_view != null)
            {
                // 刷新菜单模式
                m_view.HandleMenuModeChanged(m_model.CurrentMenuMode);
                // 刷新页码
                m_view.HandlePageChanged(m_model.CurrentPage, m_model.TotalPages);
            }
        }
        #endregion

        #region 存档操作处理
        /// <summary>
        /// 处理保存游戏操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        private void HandleSaveGame(string slotName)
        {
            if (string.IsNullOrEmpty(slotName))
                return;

            // 检查是否需要显示覆盖确认（如果是已有存档的槽位）
            if (m_model != null && m_model.SaveSlots != null)
            {
                var slotInfo = m_model.SaveSlots.Find(slot => slot.SlotName == slotName);
                if (slotInfo != null && slotInfo.HasSave)
                {
                    // 显示覆盖存档确认对话框
                    m_view.ShowConfirmDialog(
                        ConfirmDialogState.Save,
                        () => GameEvents.TriggerSaveGame(slotName),
                        null);
                    return;
                }
            }

            // 直接触发保存游戏事件（如果是新存档槽位）
            GameEvents.TriggerSaveGame(slotName);
        }

        /// <summary>
        /// 处理加载游戏操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        private void HandleLoadGame(string slotName)
        {
            if (string.IsNullOrEmpty(slotName))
                return;

            // 显示加载存档确认对话框
            m_view.ShowConfirmDialog(
                ConfirmDialogState.Load,
                () =>
                {
                    // 触发加载游戏事件
                    GameEvents.TriggerLoadGame(slotName);
                    // 隐藏菜单
                    Hide();
                },
                null);
        }

        /// <summary>
        /// 处理删除存档操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        private void HandleDeleteGame(string slotName)
        {
            if (string.IsNullOrEmpty(slotName))
                return;

            // 显示删除存档确认对话框
            m_view.ShowConfirmDialog(
                ConfirmDialogState.Delete,
                () =>
                {
                    // 触发删除存档事件
                    GameEvents.TriggerDeleteSave(slotName);
                },
                null);
        }

        /// <summary>
        /// 处理返回按钮点击事件
        /// </summary>
        private void HandleBackButtonClicked()
        {
            Hide();
        }

        /// <summary>
        /// 处理菜单模式变更
        /// </summary>
        /// <param name="mode">新的菜单模式</param>
        private void HandleMenuModeChanged(SaveLoadMenuModel.MenuMode mode)
        {
            if (m_view != null)
            {
                m_view.HandleMenuModeChanged(mode);
            }
        }

        /// <summary>
        /// 处理页码变更
        /// </summary>
        private void HandlePageChanged()
        {
            if (m_model != null && m_view != null)
            {
                m_view.HandlePageChanged(m_model.CurrentPage, m_model.TotalPages);
            }
        }
        #endregion
    }
}