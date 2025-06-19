using System.Collections;
using MyGame.System;
using UnityEngine;

namespace UI.Managers
{
    /// <summary>
    /// 全局UI管理器，负责调度和管理所有UI界面。
    /// 通过事件系统与其他模块通信，实现解耦。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region UI引用

        [Header("UI面板引用")]
        public CanvasGroup mainMenu;
        public CanvasGroup pauseMenu;
        public CanvasGroup resultPanel;
        public CanvasGroup hudPanel;

        [Header("动画设置")]
        [Tooltip("UI淡入淡出动画时长（秒）")]
        public float fadeDuration = 0.3f;

        #endregion

        #region 枚举和状态

        public enum UIState
        {
            None,
            MainMenu,
            PauseMenu,
            ResultPanel,
            HUD
        }

        private UIState currentState = UIState.None;

        #endregion

        #region 生命周期

        private void Awake()
        {
            // 初始隐藏所有UI
            HideAllUI();

            // 注册UI相关事件监听
            GameEvents.OnMainMenuShow += ShowMainMenu;
            GameEvents.OnPauseMenuShow += ShowPauseMenu;
            GameEvents.OnResultPanelShow += ShowResultPanel;
            GameEvents.OnHUDShow += ShowHUD;
            GameEvents.OnMenuShow += OnMenuShow;
        }

        private void OnDestroy()
        {
            // 注销事件监听
            GameEvents.OnMainMenuShow -= ShowMainMenu;
            GameEvents.OnPauseMenuShow -= ShowPauseMenu;
            GameEvents.OnResultPanelShow -= ShowResultPanel;
            GameEvents.OnHUDShow -= ShowHUD;
            GameEvents.OnMenuShow -= OnMenuShow;
        }

        #endregion

        #region UI控制核心方法

        /// <summary>
        /// 隐藏所有UI界面
        /// </summary>
        private void HideAllUI()
        {
            ShowCanvasGroup(mainMenu, false);
            ShowCanvasGroup(pauseMenu, false);
            ShowCanvasGroup(resultPanel, false);
            ShowCanvasGroup(hudPanel, false);
            currentState = UIState.None;
        }

        /// <summary>
        /// 设置UI状态并处理互斥关系
        /// </summary>
        private void SetUIState(UIState state, bool show)
        {
            // 处理互斥关系
            if (show)
            {
                switch (state)
                {
                    case UIState.MainMenu:
                        SetUIState(UIState.PauseMenu, false);
                        SetUIState(UIState.HUD, false);
                        break;
                    case UIState.PauseMenu:
                        SetUIState(UIState.MainMenu, false);
                        SetUIState(UIState.ResultPanel, false);
                        break;
                    case UIState.ResultPanel:
                        SetUIState(UIState.PauseMenu, false);
                        SetUIState(UIState.HUD, false);
                        break;
                }
            }

            // 更新当前状态
            if (show) currentState = state;
            else if (currentState == state) currentState = UIState.None;

            // 根据状态显示/隐藏对应UI
            switch (state)
            {
                case UIState.MainMenu:
                    ShowCanvasGroup(mainMenu, show);
                    break;
                case UIState.PauseMenu:
                    ShowCanvasGroup(pauseMenu, show);
                    break;
                case UIState.ResultPanel:
                    ShowCanvasGroup(resultPanel, show);
                    break;
                case UIState.HUD:
                    ShowCanvasGroup(hudPanel, show);
                    break;
            }
        }

        /// <summary>
        /// CanvasGroup显隐通用方法（带动画）
        /// </summary>
        private void ShowCanvasGroup(CanvasGroup group, bool show)
        {
            if (group == null) return;

            // 停止可能正在进行的动画
            StopAllCoroutines();

            // 启动新动画
            StartCoroutine(FadeCanvasGroup(group, show));
        }

        /// <summary>
        /// CanvasGroup淡入淡出动画协程
        /// </summary>
        private IEnumerator FadeCanvasGroup(CanvasGroup group, bool show)
        {
            float startAlpha = group.alpha;
            float targetAlpha = show ? 1f : 0f;
            float elapsed = 0f;

            // 动画期间禁用交互
            group.interactable = false;

            while (elapsed < fadeDuration)
            {
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                elapsed += Time.unscaledDeltaTime; // 使用unscaledTime确保暂停时也能工作
                yield return null;
            }

            group.alpha = targetAlpha;

            // 动画结束后恢复交互状态
            if (show)
            {
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            else
            {
                group.blocksRaycasts = false;
            }
        }

        #endregion

        #region UI事件响应

        private void ShowMainMenu(bool show)
        {
            SetUIState(UIState.MainMenu, show);
        }

        private void ShowPauseMenu(bool show)
        {
            SetUIState(UIState.PauseMenu, show);
        }

        private void ShowResultPanel(bool isWin)
        {
            SetUIState(UIState.ResultPanel, true);
            // 可在此处根据isWin显示不同内容
        }

        private void ShowHUD(bool show)
        {
            SetUIState(UIState.HUD, show);
        }

        private void OnMenuShow(UIState state, bool show)
        {
            SetUIState(state, show);
        }

        #endregion

        #region 调试方法

#if UNITY_EDITOR
        [ContextMenu("隐藏所有UI")]
        public void DebugHideAllUI()
        {
            HideAllUI();
        }

        [ContextMenu("显示主菜单")]
        public void DebugShowMainMenu()
        {
            SetUIState(UIState.MainMenu, true);
        }

        [ContextMenu("显示暂停菜单")]
        public void DebugShowPauseMenu()
        {
            SetUIState(UIState.PauseMenu, true);
        }

        [ContextMenu("显示结算面板")]
        public void DebugShowResultPanel()
        {
            SetUIState(UIState.ResultPanel, true);
        }

        [ContextMenu("显示HUD")]
        public void DebugShowHUD()
        {
            SetUIState(UIState.HUD, true);
        }
#endif

        private void Update()
        {
#if UNITY_EDITOR
            // 快捷键调试（仅编辑器下生效）
            //按下数字键切换UI状态（注意：调试时绕过了事件系统）
            if (Input.GetKeyDown(KeyCode.Alpha1))       
                SetUIState(UIState.MainMenu, true);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SetUIState(UIState.PauseMenu, true);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                SetUIState(UIState.ResultPanel, true);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                SetUIState(UIState.HUD, true);
            if (Input.GetKeyDown(KeyCode.Alpha0))
                HideAllUI();
#endif
        }

        #endregion
    }
}