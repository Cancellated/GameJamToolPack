using MyGame.Events;
using MyGame.Managers;
using MyGame.UI.MainMenu.Model;
using MyGame.UI.MainMenu.View;
using UnityEngine;
using Logger;

namespace MyGame.UI.MainMenu.Controller
{
    /// <summary>
    /// 主菜单的控制器，连接模型和视图，处理业务逻辑。
    ///
    /// 【初始化契约】（与 HUD/PauseMenu/ResultPanel 等标准 Controller 对齐）
    ///   - 由 View 侧 TryBindController 调用 Initialize() + SetView()；
    ///   - Model 在 Initialize() 中创建；
    ///   - 全局事件订阅在 OnInitialize() 中注册、Cleanup() 中注销。
    /// </summary>
    public class MainMenuController : BaseController<MainMenuView, MainMenuModel>
    {
        #region 字段

        [Header("菜单配置")]
        [Tooltip("默认启动的游戏场景名称")]
        [SerializeField] private string m_defaultGameScene = "Level Select";

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化控制器（由 MainMenuView.TryBindController 调用）。
        /// 创建 Model、应用默认场景配置，再触发基类 OnInitialize。
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                Log.Info(LogModules.MAINMENU, "初始化主菜单控制器");

                // 创建并初始化模型
                CreateAndInitializeModel();

                // 应用默认游戏场景配置
                if (m_model != null && !string.IsNullOrEmpty(m_defaultGameScene))
                {
                    m_model.DefaultGameScene = m_defaultGameScene;
                }

                // 调用基类初始化（触发 OnInitialize）
                base.Initialize();
            }
        }

        /// <summary>
        /// 初始化逻辑：注册全局事件并订阅 Model 变更
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            RegisterEvents();
            BindModelEvents();
        }

        /// <summary>
        /// 当对象被销毁时（MainMenu 面板随 MainMenu 场景卸载），清理资源
        /// </summary>
        private void OnDestroy()
        {
            // 清理控制器（注销事件、清理模型）
            Cleanup();

            // 解绑视图
            m_view?.UnbindController();
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理控制器资源（注销事件、清理模型）
        /// </summary>
        public override void Cleanup()
        {
            if (IsInitialized)
            {
                Log.Info(LogModules.MAINMENU, "清理主菜单控制器");

                // 注销全局事件
                UnregisterEvents();

                // 取消订阅 Model 事件
                UnbindModelEvents();

                // 清理模型资源
                if (m_model != null)
                {
                    m_model.Cleanup();
                    m_model = null;
                }

                // 调用基类清理
                base.Cleanup();
            }
        }

        #endregion

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
        /// Model属性变更回调
        /// 主菜单暂无View更新需求，但订阅以遵循ObservableModel规范
        /// </summary>
        /// <param name="propertyName">变更的属性名</param>
        private void HandleModelPropertyChanged(string propertyName)
        {
            // 主菜单的Model属性变更（如 IsSettingsVisible）由GameEvents驱动，
            // 暂不需要额外的View更新。保留订阅以满足ObservableModel使用规范。
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始游戏（新游戏）。
        /// 统一走 CreateNewGame 事件：SaveManager 会重置当前内存存档（不落盘），
        /// 再由 HandleCreateNewGame 触发 GameStart 加载游戏场景。
        /// </summary>
        public void OnStartGame()
        {
            GameEvents.TriggerCreateNewGame();
        }

        /// <summary>
        /// 显示存档菜单
        /// </summary>
        public void OnShowSaveLoad()
        {
            // UIConfig 中 SaveLoadMenu 的 hideOnShow 配置会负责隐藏主菜单并切换 UI 输入模式
            GameEvents.TriggerMenuShow(UIType.SaveLoadMenu, true);
        }

        /// <summary>
        /// 显示设置面板
        /// </summary>
        public void OnShowSettings()
        {
            if (m_model != null)
            {
                m_model.IsSettingsVisible = true;
                GameEvents.TriggerMenuShow(UIType.SettingsPanel, true);
            }
        }

        /// <summary>
        /// 显示关于面板
        /// </summary>
        public void OnShowAbout()
        {
            if (m_model != null)
            {
                m_model.IsAboutVisible = true;
                GameEvents.TriggerMenuShow(UIType.AboutPanel, true);
            }
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void OnExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 游戏开始事件响应
        /// </summary>
        private void OnGameStart()
        {
            if (m_model != null)
            {
                SceneSwitcher.RequestLoadScene(m_model.DefaultGameScene);
            }
        }

        /// <summary>
        /// 场景加载完成事件响应
        /// </summary>
        /// <param name="sceneName">加载完成的场景名称</param>
        private void OnSceneLoadComplete(string sceneName)
        {
            if (m_model != null)
            {
                if (sceneName == "MainMenu")
                {
                    // 主菜单场景加载完成后，显示主菜单
                    // 避免Show方法被调用两次导致FadeIn协程被中断
                    GameEvents.TriggerMenuShow(UIType.MainMenu, true);
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 注册事件监听
        /// </summary>
        private void RegisterEvents()
        {
            GameEvents.OnGameStart += OnGameStart;
            GameEvents.OnSceneLoadComplete += OnSceneLoadComplete;
        }

        /// <summary>
        /// 注销事件监听
        /// </summary>
        private void UnregisterEvents()
        {
            GameEvents.OnGameStart -= OnGameStart;
            GameEvents.OnSceneLoadComplete -= OnSceneLoadComplete;
        }

        #endregion
    }
}
