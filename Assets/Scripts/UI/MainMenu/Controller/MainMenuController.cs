using MyGame.Events;
using MyGame.Managers;
using MyGame.UI.MainMenu.Model;
using MyGame.UI.MainMenu.View;
using UnityEngine;
using Logger;

namespace MyGame.UI.MainMenu.Controller
{
    /// <summary>
    /// 主菜单的控制器，连接模型和视图，处理业务逻辑，同时负责MVC组件的初始化和协调
    /// </summary>
    public class MainMenuController : BaseController<MainMenuView, MainMenuModel>
    {
        #region 字段

        [Header("菜单配置")]
        [Tooltip("默认启动的游戏场景名称")]
        [SerializeField] private string m_defaultGameScene = "GameLevel1";

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化控制器和MVC组件
        /// </summary>
        private void Awake()
        {
            // 初始化MVC组件（创建Model、设置View引用）
            InitializeMVCComponents();

            // 初始化控制器生命周期
            Initialize();
        }

        /// <summary>
        /// 当对象启用时，注册事件监听
        /// </summary>
        private void OnEnable()
        {
            RegisterEvents();
            BindModelEvents();
        }

        /// <summary>
        /// 当对象禁用时，注销事件监听
        /// </summary>
        private void OnDisable()
        {
            UnregisterEvents();
            UnbindModelEvents();
        }

        /// <summary>
        /// 当对象被销毁时，清理资源
        /// </summary>
        private void OnDestroy()
        {
            // 注销事件监听
            UnregisterEvents();
            UnbindModelEvents();

            // 清理模型资源
            m_model?.Cleanup();

            // 解绑视图
            m_view?.UnbindController();
        }

        #endregion

        #region MVC组件初始化

        /// <summary>
        /// 初始化MVC组件
        /// 如果组件不存在，则自动创建
        /// </summary>
        private void InitializeMVCComponents()
        {
            // 创建并初始化Model
            CreateAndInitializeModel();

            // 设置默认游戏场景
            if (m_model != null && !string.IsNullOrEmpty(m_defaultGameScene))
            {
                m_model.DefaultGameScene = m_defaultGameScene;
            }

            // 查找或创建View
            if (m_view == null)
            {
                m_view = GetComponentInChildren<MainMenuView>(true);
                if (m_view == null)
                {
                    Log.Warning(LogModules.MAINMENU, "未找到MainMenuView组件，尝试创建");

                    // 创建视图对象
                    GameObject viewObject = new("MainMenuView");
                    viewObject.transform.SetParent(transform, false);
                    m_view = viewObject.AddComponent<MainMenuView>();
                }
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
        /// 开始游戏
        /// </summary>
        public void OnStartGame()
        {
            // 触发游戏开始事件
            GameEvents.TriggerGameStart();
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
