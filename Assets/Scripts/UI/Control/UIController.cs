using MyGame.Events;
using MyGame.DevTools;
using MyGame.Managers;
using MyGame.UI;
using Logger;
using UnityEngine;

namespace MyGame.UI.Control
{
    /// <summary>
    /// UI控制器，负责处理UI相关的输入事件和控制UI面板的显示隐藏
    /// 它监听唤出UI的按键输入，并通知事件系统-对应面板监听到对应事件后完成相应的方法
    /// </summary>
    public class UIController : Singleton<UIController>
    {
        #region 常量
        private const string LOG_MODULE = LogModules.UI;
        #endregion

        #region 属性

        /// <summary>
        /// 调试控制台是否可用：编辑器/Development Build 默认可用；
        /// 发布版需通过启动参数、dev_config.json 或 PlayerPrefs 开启开发者模式。
        /// </summary>
        private static bool IsDebugConsoleAvailable
        {
            get { return DeveloperMode.IsDebugConsoleEnabled; }
        }

        #endregion

        #region 字段
        private GameControl _inputActions;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            
            // 延迟一帧获取InputManager，避免初始化顺序问题
            StartCoroutine(InitializeInputActionsCoroutine());
        }
        
        private System.Collections.IEnumerator InitializeInputActionsCoroutine()
        {
            // 等待一帧，确保所有Awake方法都已执行
            yield return null;
            
            // 获取InputManager中的InputActions实例
            try
            {
                if (InputManager.Instance != null && InputManager.Instance.InputActions != null)
                {
                    _inputActions = InputManager.Instance.InputActions;
                    Log.Info(LOG_MODULE, "UI控制器初始化完成");
                    // 重新注册输入回调，确保使用正确的_inputActions
                    UnregisterInputCallbacks();
                    RegisterInputCallbacks();
                }
                else
                {
                    Log.Error(LOG_MODULE, "InputManager instance not found! Creating fallback input actions.");
                    _inputActions = new GameControl();
                }
            }
            catch (System.Exception ex)
            {
                Log.Error(LOG_MODULE, $"初始化输入动作时发生异常: {ex.Message}");
                // 确保即使发生异常，也有一个可用的输入动作实例
                _inputActions ??= new GameControl();
            }
        }

        private void OnEnable()
        {
            // 注册按键回调（先退订保证幂等：OnEnable 与 OnDisable 成对出现）
            UnregisterInputCallbacks();
            RegisterInputCallbacks();

            // 订阅UI显隐事件：暂停菜单显示期间需要单独保活 GamePlay.Pause 按键（见 OnMenuShow）。
            // 订阅放在 OnEnable（而非 Awake 协程）中，与 OnDisable 的退订严格成对，
            // 保证对象经历 disable→enable 循环后订阅仍然有效
            GameEvents.OnMenuShow -= OnMenuShow;
            GameEvents.OnMenuShow += OnMenuShow;
        }

        private void OnDisable()
        {
            // 注销按键回调
            UnregisterInputCallbacks();

            // 注销UI显隐事件监听（与 OnEnable 中的订阅成对出现）
            GameEvents.OnMenuShow -= OnMenuShow;
        }
        #endregion

        #region 输入处理
        /// <summary>
        /// 注册输入回调
        /// </summary>
        private void RegisterInputCallbacks()
        {
            if (_inputActions != null)
            {
                // 注册游戏玩法中的UI相关按键
                _inputActions.GamePlay.Pause.performed += OnPausePerformed;
                _inputActions.GamePlay.Inventory.performed += OnInventoryPerformed;

                // 控制台按键仅在开发者模式可用时注册；发布版默认不注册，mod 可通过 DeveloperMode 开启
                if (IsDebugConsoleAvailable)
                {
                    _inputActions.GamePlay.Console.performed += OnConsolePerformed;
                }
            }
        }

        /// <summary>
        /// 注销输入回调
        /// </summary>
        private void UnregisterInputCallbacks()
        {
            if (_inputActions != null)
            {
                _inputActions.GamePlay.Pause.performed -= OnPausePerformed;
                _inputActions.GamePlay.Inventory.performed -= OnInventoryPerformed;
                _inputActions.GamePlay.Console.performed -= OnConsolePerformed;
            }
        }

        /// <summary>
        /// 暂停菜单按键处理
        /// </summary>
        private void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Log.Info(LOG_MODULE, "暂停菜单按键被按下");
                
                // 检查暂停菜单当前状态并切换
                if (UIManager.Instance != null && UIManager.Instance.PanelMap.ContainsKey(UIType.PauseMenu))
                {
                    bool isCurrentlyVisible = UIManager.Instance.PanelMap[UIType.PauseMenu].IsVisible;
                    // 触发暂停菜单显示/隐藏事件，切换当前状态
                    GameEvents.TriggerMenuShow(UIType.PauseMenu, !isCurrentlyVisible);
                }
                else
                {
                    // 如果无法获取面板状态，默认显示
                    GameEvents.TriggerMenuShow(UIType.PauseMenu, true);
                }
            }
        }

        /// <summary>
        /// 物品栏按键处理
        /// </summary>
        private void OnInventoryPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Log.Info(LOG_MODULE, "物品栏按键被按下");
                
                // 检查物品栏当前状态并切换
                if (UIManager.Instance != null && UIManager.Instance.PanelMap.ContainsKey(UIType.Inventory))
                {
                    bool isCurrentlyVisible = UIManager.Instance.PanelMap[UIType.Inventory].IsVisible;
                    // 触发物品栏显示/隐藏事件，切换当前状态
                    GameEvents.TriggerMenuShow(UIType.Inventory, !isCurrentlyVisible);
                }
                else
                {
                    // 如果无法获取面板状态，默认显示
                    GameEvents.TriggerMenuShow(UIType.Inventory, true);
                }
            }
        }

        /// <summary>
        /// 控制台按键处理
        /// </summary>
        private void OnConsolePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!IsDebugConsoleAvailable || !context.performed)
            {
                return;
            }

            Log.Info(LOG_MODULE, "控制台按键被按下");

            // 检查控制台当前状态并切换。
            // 注意：与 Pause/Inventory 一致，必须经 UIManager.PanelMap 判空——
            // DebugConsole.Instance 在面板尚未加载（Addressable 异步加载中、场景切换后）时为 null，
            // 直接访问会空引用崩溃。
            if (UIManager.Instance != null && UIManager.Instance.PanelMap.ContainsKey(UIType.Console))
            {
                bool isCurrentlyVisible = UIManager.Instance.PanelMap[UIType.Console].IsVisible;
                // 触发控制台显示/隐藏事件，切换当前状态
                GameEvents.TriggerMenuShow(UIType.Console, !isCurrentlyVisible);
            }
            else
            {
                // 控制台尚未加载/注册：默认请求显示，
                // UIManager.SetUIState 检测到面板缺失后会自动走 Addressable 加载路径
                GameEvents.TriggerMenuShow(UIType.Console, true);
            }
        }

        /// <summary>
        /// UI显隐事件响应：按暂停菜单的实际可见状态校准 GamePlay.Pause 按键。
        /// 暂停菜单的 inputMode 为 UI 独占（显示时 SwitchToUIMode 会禁用整个 GamePlay 图），
        /// 而关闭/恢复暂停依赖该按键（本类的回调 + GameManager 的 performed 事件处理），
        /// 因此需要像 InputManager.SwitchToUIMode 对 GamePlay.Console 的特判一样单独保持启用。
        /// 校准覆盖两条隐藏路径：
        ///   1. 正常关闭：OnMenuShow(PauseMenu, false)；
        ///   2. 静默隐藏：其他面板（如结算面板）经 hideOnShow 互斥直接 Hide 暂停菜单而不发事件——
        ///      因此在显示任意面板时按 PauseMenu.IsVisible 重新校准。
        /// </summary>
        private void OnMenuShow(UIType state, bool show)
        {
            if (_inputActions == null)
                return;

            if (show && state == UIType.PauseMenu)
            {
                _inputActions.GamePlay.Pause.Enable();
                return;
            }

            if (!show)
            {
                // 正常关闭路径：仅当 GamePlay 图未整体启用（仍处于 UI 模式）时才禁用；
                // 若已由 UIManager 恢复为玩法模式，保持启用交由模式切换管理
                if (state == UIType.PauseMenu && !_inputActions.GamePlay.enabled)
                {
                    _inputActions.GamePlay.Pause.Disable();
                }
                return;
            }

            // 显示其他面板（可能静默隐藏了暂停菜单）：按暂停菜单实际可见状态校准
            var uiManager = UIManager.Instance;
            bool pauseVisible = uiManager != null
                && uiManager.PanelMap.TryGetValue(UIType.PauseMenu, out var pausePanel)
                && pausePanel.IsVisible;
            if (!pauseVisible && !_inputActions.GamePlay.enabled)
            {
                _inputActions.GamePlay.Pause.Disable();
            }
        }
        #endregion
    }
}