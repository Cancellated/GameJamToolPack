using Logger;
using MyGame.Core;
using MyGame.Events;
using MyGame.Managers;
using MyGame.UI.MainMenu.Model;
using MyGame.UI.MainMenu.View;
using UnityEngine;

namespace MyGame.UI.MainMenu.Controller
{
    /// <summary>
    /// 主菜单的控制器，连接模型和视图，处理业务逻辑，同时负责MVC组件的初始化和协调
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        #region 字段与属性

        [Header("菜单配置")]
        [Tooltip("默认启动的游戏场景名称")]
        [SerializeField] private string m_defaultGameScene = "GameLevel1";
        
        [Header("MVC组件")]
        [Tooltip("主菜单模型")]
        [SerializeField] private MainMenuModel m_model;
        
        [Tooltip("主菜单视图")]
        [SerializeField] private MainMenuView m_view;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化控制器和MVC组件
        /// </summary>
        private void Awake()
        {
            // 初始化MVC组件
            InitializeMVCComponents();
            
            // 注册事件监听
            RegisterEvents();
        }
        
        /// <summary>
        /// 当对象启用时
        /// </summary>
        private void OnEnable()
        {
            // 确保MVC组件正确初始化
            if (m_model == null || m_view == null)
            {
                InitializeMVCComponents();
            }
        }

        /// <summary>
        /// 当对象被销毁时
        /// </summary>
        private void OnDestroy()
        {
            // 注销事件监听
            UnregisterEvents();
            
            // 清理模型资源
            m_model?.Cleanup();
            
            // 解绑视图
            if (m_view != null)
            {
                m_view.UnbindController();
            }
        }

        #endregion

        #region MVC组件初始化

        /// <summary>
        /// 初始化MVC组件
        /// 如果组件不存在，则自动创建
        /// </summary>
        protected virtual void InitializeMVCComponents()
        {
            // 初始化模型
            m_model ??= new MainMenuModel();
            
            if (!m_model.IsInitialized)
            {
                m_model.Initialize();
            }
            
            // 设置默认游戏场景
            if (!string.IsNullOrEmpty(m_defaultGameScene))
            {
                m_model.DefaultGameScene = m_defaultGameScene;
            }
            
            // 初始化视图
            if (m_view == null)
            {
                m_view = gameObject.GetComponentInChildren<MainMenuView>(true);
                if (m_view == null)
                {

                    
                    // 创建视图对象
                    GameObject viewObject = new("MainMenuView");
                    viewObject.transform.SetParent(transform, false);
                    m_view = viewObject.AddComponent<MainMenuView>();
                }
            }
            
            // 建立MVC之间的连接
            SetModel(m_model);
            SetView(m_view);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置视图引用
        /// </summary>
        /// <param name="view">主菜单视图</param>
        public void SetView(MainMenuView view)
        {
            if (m_view != null && m_view != view)
            {
                m_view.UnbindController();
            }
            
            m_view = view;
            
            if (m_view != null)
            {
                m_view.BindController(this);
            }
        }

        /// <summary>
        /// 设置模型引用
        /// </summary>
        /// <param name="model">主菜单模型</param>
        public void SetModel(MainMenuModel model)
        {
            if (m_model != null && m_model != model)
            {
                m_model.Cleanup();
            }
            
            m_model = model;
            
            if (m_model != null)
            {
                if (!m_model.IsInitialized)
                {
                    m_model.Initialize();
                }
                
                if (!string.IsNullOrEmpty(m_defaultGameScene))
                {
                    m_model.DefaultGameScene = m_defaultGameScene;
                }
            }
        }

        /// <summary>
        /// 获取主菜单模型
        /// </summary>
        /// <returns>主菜单模型实例</returns>
        public MainMenuModel GetModel()
        {
            return m_model;
        }

        /// <summary>
        /// 获取主菜单视图
        /// </summary>
        /// <returns>主菜单视图实例</returns>
        public MainMenuView GetView()
        {
            return m_view;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 开始游戏
        /// 只触发创建新游戏事件，游戏开始和场景加载会在新游戏创建过程中自动触发
        /// </summary>
        public void OnStartGame()
        {
            // 使用GameFlowCoordinator统一管理游戏流程，传递明确的触发源
            if (GameFlowCoordinator.Instance != null)
            {
                GameFlowCoordinator.Instance.StartNewGame("AutoSave", "MainMenuButton");
            }
            else
            {
                // 降级处理：如果协调器不存在，仍然直接触发事件
                Log.Warning("MAIN_MENU", "GameFlowCoordinator实例不存在，直接触发CreateNewGame事件");
                GameEvents.TriggerCreateNewGame("AutoSave");
            }
        }

        /// <summary>
        /// 显示加载游戏菜单
        /// </summary>
        public void OnShowLoadGameMenu()
        {
            if (m_model == null)
            {
                Log.Error("MAIN_MENU", "OnShowLoadGameMenu: 模型未初始化");
                return;
            }
            
            m_model.IsLoadGameVisible = true;
            Log.Info("MAIN_MENU", "已触发显示加载游戏菜单事件");
            GameEvents.TriggerMenuShow(UIType.SaveLoadMenu, true);
        }

        /// <summary>
        /// 显示设置面板
        /// </summary>
        public void OnShowSettings()
        {
            if (m_model == null)
            {
                Log.Error("MAIN_MENU", "OnShowSettings: 模型未初始化");
                return;
            }
            
            m_model.IsSettingsVisible = true;
            Log.Info("MAIN_MENU", "已触发显示设置面板事件");
            GameEvents.TriggerMenuShow(UIType.SettingsPanel, true);
        }

        /// <summary>
        /// 显示关于面板
        /// </summary>
        public void OnShowAbout()
        {
            if (m_model == null)
            {
                Log.Error("MAIN_MENU", "OnShowAbout: 模型未初始化");
                return;
            }
            
            m_model.IsAboutVisible = true;
            Log.Info("MAIN_MENU", "已触发显示关于面板事件");
            GameEvents.TriggerMenuShow(UIType.AboutPanel, true);
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void OnExitGame()
        {
            Log.Info("MAIN_MENU", "触发退出游戏操作");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

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