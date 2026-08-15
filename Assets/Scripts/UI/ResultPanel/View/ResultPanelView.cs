using Logger;
using MyGame.UI.Core;
using MyGame.UI.ResultPanel.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame.UI.ResultPanel.View
{
    /// <summary>
    /// 结算面板视图
    /// 提供 重新开始 / 返回主菜单 两个按钮；
    /// 显示时隐藏 PauseMenu 与 HUD（由 UIConfig 互斥配置驱动），
    /// 游戏场景由面板自带的全屏蒙版（BackDrop）遮挡
    /// </summary>
    public class ResultPanelView : BaseView<ResultPanelController>
    {
        private const string LOG_MODULE = LogModules.RESULT;

        [Header("结算面板按钮")]
        [Tooltip("重新开始按钮（未在 Inspector 指定时自动查找子物体 Btn_Restart）")]
        [SerializeField] private Button _restartButton;

        [Tooltip("返回主菜单按钮（未指定时自动查找子物体 Btn_Menu）")]
        [SerializeField] private Button _mainMenuButton;

        /// <summary>
        /// 初始化视图：设置面板类型后调用基类完成基础绑定
        /// </summary>
        protected override void Awake()
        {
            // 设置面板类型为结算面板
            m_panelType = UIType.ResultPanel;

            // 调用基类的Awake方法，完成基础初始化
            base.Awake();
        }

        /// <summary>
        /// 尝试自动绑定控制器
        /// </summary>
        protected override void TryBindController()
        {
            if (TryGetComponent<ResultPanelController>(out var controller))
            {
                // 找到控制器：初始化、注入视图并绑定
                controller.Initialize();
                controller.SetView(this);
                BindController(controller);
                return;
            }

            // 如果没有找到控制器，则创建一个新的
            controller = gameObject.AddComponent<ResultPanelController>();
            controller.Initialize();
            controller.SetView(this);

            // 绑定控制器到视图
            BindController(controller);
        }

        /// <summary>
        /// 控制器绑定后的回调
        /// </summary>
        protected override void OnControllerBound()
        {
            base.OnControllerBound();
            Log.Info(LOG_MODULE, "结算面板控制器已绑定");
        }

        /// <summary>
        /// 控制器解绑后的回调
        /// </summary>
        protected override void OnControllerUnbound()
        {
            base.OnControllerUnbound();
            Log.Info(LOG_MODULE, "结算面板控制器已解绑");
        }

        /// <summary>
        /// 初始化面板：确保按钮引用并绑定点击事件
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Log.Info(LOG_MODULE, "结算面板已初始化");

            EnsureButtons();
            BindButtonEvents();
        }

        /// <summary>
        /// 清理面板资源：解绑按钮事件
        /// </summary>
        public override void Cleanup()
        {
            UnbindButtonEvents();
            base.Cleanup();
            Log.Info(LOG_MODULE, "结算面板资源已清理");
        }

        /// <summary>
        /// 显示结算面板（淡入）。
        /// 立即置 IsVisible 并中断淡出协程：防止"淡出后释放"回调在重新显示期间误释放面板。
        /// </summary>
        public override void Show()
        {
            if (!IsVisible)
            {
                Log.Info(LOG_MODULE, "显示结算面板（淡入）");

                // 显示前确保物体激活（隐藏流程会 SetActive(false)，见 Hide）
                gameObject.SetActive(true);

                // 立即置可见：淡出回调检测到 IsVisible 为 true 会放弃释放
                IsVisible = true;

                // 中断可能进行中的淡出协程，避免两个协程并发写入 alpha
                StopAllCoroutines();
                StartCoroutine(FadeIn());
            }
        }

        /// <summary>
        /// 隐藏结算面板（淡出后释放面板实例与 Addressable 资源）。
        /// 懒加载面板：释放后下次 SetUIState(ResultPanel, true) 会自动重新加载并显示。
        /// </summary>
        public override void Hide()
        {
            if (IsVisible)
            {
                Log.Info(LOG_MODULE, "隐藏结算面板（淡出）");

                // 立即重置可见状态，保证淡出期间 Show() 可正常重新显示面板
                IsVisible = false;

                // 中断可能进行中的淡入协程，避免两个协程并发写入 alpha
                StopAllCoroutines();
                StartCoroutine(FadeOut(() =>
                {
                    // 防御：淡出期间面板被重新显示（Show）时不再关闭与释放
                    if (IsVisible)
                    {
                        return;
                    }

                    gameObject.SetActive(false);

                    // 懒加载释放闭环：销毁实例并释放 Addressable 资源
                    if (PanelLoader.Instance != null)
                    {
                        PanelLoader.Instance.UnloadPanel(UIType.ResultPanel);
                    }
                }));
            }
        }

        /// <summary>
        /// 确保两个按钮引用可用：Inspector 未指定时按约定名称查找子物体。
        /// 按钮可能被布局容器（LayOut）包裹而不在面板的直接子物体下，
        /// 因此使用 GetComponentsInChildren 递归匹配名称
        /// </summary>
        private void EnsureButtons()
        {
            _restartButton = EnsureButton(_restartButton, "Btn_Restart");
            _mainMenuButton = EnsureButton(_mainMenuButton, "Btn_Menu");
        }

        /// <summary>
        /// 确保单个按钮引用：Inspector 未指定时查找指定名称的子物体按钮
        /// </summary>
        private Button EnsureButton(Button button, string childName)
        {
            if (button != null)
            {
                return button;
            }

            // 递归查找子物体中的按钮（兼容按钮被 LayOut 容器包裹的结构）
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var candidate in buttons)
            {
                if (candidate.name == childName)
                {
                    return candidate;
                }
            }

            Log.Warning(LOG_MODULE, $"未找到按钮 {childName}（请在 Inspector 拖入或创建同名子物体）");
            return null;
        }

        /// <summary>
        /// 绑定按钮点击事件（幂等：先解绑再绑定）。
        /// Initialize 可能被多次调用（OnEnable + AddPanelToMap），
        /// 若只 AddListener 会导致点击一次触发多次回调
        /// </summary>
        private void BindButtonEvents()
        {
            // 先解绑旧监听，防止重复绑定
            UnbindButtonEvents();
            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        /// <summary>
        /// 解绑按钮点击事件（与 BindButtonEvents 成对出现）
        /// </summary>
        private void UnbindButtonEvents()
        {
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            }
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        /// <summary>
        /// 重新开始按钮点击：经控制器触发重新开始本局
        /// </summary>
        private void OnRestartClicked()
        {
            m_controller?.OnRestartClicked();
        }

        /// <summary>
        /// 返回主菜单按钮点击：经控制器加载主菜单场景
        /// </summary>
        private void OnMainMenuClicked()
        {
            m_controller?.OnMainMenuClicked();
        }
    }
}
