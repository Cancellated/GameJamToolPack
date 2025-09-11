using MyGame.Events;
using UnityEngine;
using MyGame.Managers;

namespace MyGame.UI.MainMenu
{
    /// <summary>
    /// 主菜单管理器，负责游戏开始界面的交互逻辑。
    /// 处理开始游戏、设置、退出等功能。
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        #region 字段与属性

        [Header("菜单配置")]
        [Tooltip("默认启动的游戏场景名称")]
        public string defaultGameScene = "GameLevel1";

        // 注意：这些引用现在仅用于编辑器预览，实际显示/隐藏通过事件系统控制
        [Tooltip("设置面板")]
        public CanvasGroup settingsPanel;

        [Tooltip("关于面板")]
        public CanvasGroup aboutPanel;

        #endregion

        #region 生命周期
        private void Awake()
        {
            // 初始隐藏设置和关于面板
            HideSettingsPanel();
            HideAboutPanel();

            // 注册事件监听
            GameEvents.OnGameStart += OnGameStart;
            GameEvents.OnSceneLoadComplete += OnSceneLoadComplete;
        }

        private void OnDestroy()
        {
            // 注销事件监听
            GameEvents.OnGameStart -= OnGameStart;
            GameEvents.OnSceneLoadComplete -= OnSceneLoadComplete;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始游戏按钮点击事件
        /// </summary>
        public void OnStartGameButtonClick()
        {
            GameEvents.TriggerMenuShow(UIType.MainMenu, false);
            // 触发游戏开始事件
            GameEvents.TriggerGameStart();
        }

        /// <summary>
        /// 显示设置面板
        /// </summary>
        public void ShowSettingsPanel()
        {
            GameEvents.TriggerMenuShow(UIType.SettingsPanel, true);
        }

        /// <summary>
        /// 隐藏设置面板
        /// </summary>
        public void HideSettingsPanel()
        {
            GameEvents.TriggerMenuShow(UIType.SettingsPanel, false);
        }

        /// <summary>
        /// 显示关于面板
        /// </summary>
        public void ShowAboutPanel()
        {
            GameEvents.TriggerMenuShow(UIType.AboutPanel, true);
        }

        /// <summary>
        /// 隐藏关于面板
        /// </summary>
        public void HideAboutPanel()
        {
            GameEvents.TriggerMenuShow(UIType.AboutPanel, false);
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region 事件响应

        /// <summary>
        /// 游戏开始事件响应
        /// </summary>
        private void OnGameStart()
        {
            SceneSwitcher.RequestLoadScene(defaultGameScene);
        }

        /// <summary>
        /// 场景加载完成事件响应
        /// </summary>
        /// <param name="sceneName">加载完成的场景名称</param>
        private void OnSceneLoadComplete(string sceneName)
        {
            if (sceneName == defaultGameScene)
            {
                // 游戏场景加载完成后，隐藏主菜单
                GameEvents.TriggerMenuShow(UIType.MainMenu, false);
            }
            else if (sceneName == "MainMenu")
            {
                // 主菜单场景加载完成后，显示主菜单
                GameEvents.TriggerMenuShow(UIType.MainMenu, true);
            }
        }

        #endregion
    }
}