using Logger;
using MyGame.UI.MainMenu.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame.UI.MainMenu.View
{
    /// <summary>
    /// 主菜单视图，负责显示主菜单UI和处理用户输入
    /// </summary>
    public class MainMenuView : BaseView<MainMenuController>
    {
        #region 字段

        [Header("按钮")]
        [Tooltip("开始游戏按钮")]
        [SerializeField] private Button m_startGameButton;

        [Tooltip("加载游戏按钮")]
        [SerializeField] private Button m_loadGameButton;
        
        [Tooltip("设置按钮")]
        [SerializeField] private Button m_settingsButton;
        
        [Tooltip("关于按钮")]
        [SerializeField] private Button m_aboutButton;
        
        [Tooltip("退出游戏按钮")]
        [SerializeField] private Button m_exitGameButton;

        private const string LOG_MODULE = LogModules.MAINMENU;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化面板
        /// </summary>
        protected override void Awake()
        {
            // 设置面板类型
            m_panelType = UIType.MainMenu;
            base.Awake();
        }

        /// <summary>
        /// 尝试自动绑定控制器（标准模式：同物体查找，初始化并注入视图）
        /// </summary>
        protected override void TryBindController()
        {
            if (TryGetComponent<MainMenuController>(out var controller))
            {
                // 找到控制器：初始化、注入视图并绑定
                controller.Initialize();
                controller.SetView(this);
                BindController(controller);
                return;
            }

            // 如果没有找到控制器，则创建一个新的
            controller = gameObject.AddComponent<MainMenuController>();
            controller.Initialize();
            controller.SetView(this);

            // 绑定控制器到视图
            BindController(controller);
        }


        #endregion

        #region 公共方法

        /// <summary>
        /// 显示面板
        /// </summary>
        public override void Show()
        {
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 1f;
                m_canvasGroup.interactable = true;
                m_canvasGroup.blocksRaycasts = true;
            }
            IsVisible = true;
            Log.Info(LOG_MODULE, "显示主菜单面板");
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public override void Hide()
        {
            // 主菜单直接显隐
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 0f;
                m_canvasGroup.interactable = false;
                m_canvasGroup.blocksRaycasts = false;
            }
            IsVisible = false;
        }

        /// <summary>
        /// 初始化面板：仅绑定按钮事件。
        /// 注意：不在 Initialize 中显示面板——显示/隐藏由 UIManager.SetUIState 统一调度
        /// （启动时 UIManager.Start 调 SetUIState(MainMenu, true)；
        ///   返回主菜单时 MainMenuController.OnSceneLoadComplete 调 TriggerMenuShow）。
        /// 若在此 Show()，场景切换后 RefreshPanelMap 补注册重建的面板会立即显示，
        /// 覆盖在其他场景界面上（选关界面被主菜单挡住）。
        /// </summary>
        public override void Initialize()
        {
            // 绑定按钮事件（遵循基类规范：在 Initialize 中绑定，与 Cleanup 中的解绑成对）
            BindButtonEvents();
        }

        /// <summary>
        /// 清理面板资源
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
            
            // 清理资源
            UnbindButtonEvents();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        private void BindButtonEvents()
        {
            if (m_startGameButton != null)
                m_startGameButton.onClick.AddListener(OnStartGameButtonClick);
            
            if (m_settingsButton != null)
                m_settingsButton.onClick.AddListener(OnSettingsButtonClick);
            
            if (m_aboutButton != null)
                m_aboutButton.onClick.AddListener(OnAboutButtonClick);
            
            if (m_exitGameButton != null)
                m_exitGameButton.onClick.AddListener(OnExitGameButtonClick);
        }

        /// <summary>
        /// 解绑按钮事件
        /// </summary>
        private void UnbindButtonEvents()
        {
            if (m_startGameButton != null)
                m_startGameButton.onClick.RemoveListener(OnStartGameButtonClick);
            
            if (m_settingsButton != null)
                m_settingsButton.onClick.RemoveListener(OnSettingsButtonClick);
            
            if (m_aboutButton != null)
                m_aboutButton.onClick.RemoveListener(OnAboutButtonClick);
            
            if (m_exitGameButton != null)
                m_exitGameButton.onClick.RemoveListener(OnExitGameButtonClick);
        }

        #endregion

        #region 事件响应

        /// <summary>
        /// 开始游戏按钮点击事件
        /// </summary>
        private void OnStartGameButtonClick()
        {
            if (m_controller != null)
            {
                m_controller.OnStartGame();
            }
        }

        /// <summary>
        /// 设置按钮点击事件
        /// </summary>
        private void OnSettingsButtonClick()
        {
            if (m_controller != null)
            {
                m_controller.OnShowSettings();
            }
        }

        /// <summary>
        /// 关于按钮点击事件
        /// </summary>
        private void OnAboutButtonClick()
        {
            if (m_controller != null)
            {
                m_controller.OnShowAbout();
            }
        }

        /// <summary>
        /// 退出游戏按钮点击事件
        /// </summary>
        private void OnExitGameButtonClick()
        {
            if (m_controller != null)
            {
                m_controller.OnExitGame();
            }
        }

        #endregion
    }
}