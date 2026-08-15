using UnityEngine;
using Logger;
using MyGame.UI.Core;
using MyGame.UI.PauseMenu.Controller;

namespace MyGame.UI.PauseMenu.View
{
    /// <summary>
    /// 暂停菜单视图，负责显示暂停界面和处理基本的UI显隐
    /// </summary>
    public class PauseMenuView : BaseView<PauseMenuController>
    {
        private const string LOG_MODULE = LogModules.PAUSEMENU;

        /// <summary>
        /// 初始化暂停菜单
        /// </summary>
        protected override void Awake()
        {
            // 设置面板类型为暂停菜单
            m_panelType = UIType.PauseMenu;

            // 调用基类的Awake方法，完成基础初始化
            base.Awake();
        }

        /// <summary>
        /// 尝试自动绑定控制器
        /// </summary>
        protected override void TryBindController()
        {
            // 查找当前GameObject上的控制器
            // 如果没有找到控制器，则创建一个新的
            if (!gameObject.TryGetComponent<PauseMenuController>(out var controller))
            {
                controller = gameObject.AddComponent<PauseMenuController>();
            }

            // 初始化控制器（幂等，基类 IsInitialized 防重入）
            controller.Initialize();
            
            // 设置控制器的视图引用
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
            Log.Info(LOG_MODULE, "暂停菜单控制器已绑定");
        }

        /// <summary>
        /// 控制器解绑后的回调
        /// </summary>
        protected override void OnControllerUnbound()
        {
            base.OnControllerUnbound();
            Log.Info(LOG_MODULE, "暂停菜单控制器已解绑");
        }

        /// <summary>
        /// 初始化面板
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Log.Info(LOG_MODULE, "暂停菜单已初始化");
        }

        /// <summary>
        /// 清理面板资源
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
            Log.Info(LOG_MODULE, "暂停菜单资源已清理");
        }

        /// <summary>
        /// 显示暂停菜单（淡入）。
        /// 立即置 IsVisible 并中断淡出协程：防止"淡出后释放"回调在重新显示期间误释放面板。
        /// </summary>
        public override void Show()
        {
            if (!IsVisible)
            {
                Log.Info(LOG_MODULE, "显示暂停菜单（淡入）");

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
        /// 隐藏暂停菜单（淡出后释放面板实例与 Addressable 资源）。
        /// 懒加载面板：释放后下次 SetUIState(PauseMenu, true) 会自动重新加载并显示。
        /// </summary>
        public override void Hide()
        {
            if (IsVisible)
            {
                Log.Info(LOG_MODULE, "隐藏暂停菜单（淡出）");

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
                        PanelLoader.Instance.UnloadPanel(UIType.PauseMenu);
                    }
                }));
            }
        }
    }
}