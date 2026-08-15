using System;
using Logger;
using UnityEngine;

using MyGame.UI.Loading.Controller;
using UnityEngine.UI;

namespace MyGame.UI.Loading.View
{
    /// <summary>
    /// 加载界面组件
    /// MVC架构中的View层，负责显示加载界面的UI元素和动画效果
    /// 显隐动画由代码控制（CanvasGroup 淡入淡出协程，unscaledTime 驱动），
    /// 不依赖 Animator 状态机，避免状态机重启/默认状态重播导致的播放不稳定
    /// </summary>
    public class LoadingScreenView : BaseView<LoadingScreenController>
    {
        private const string LOG_MODULE = LogModules.LOADING;

        [Header("层级设置")]
        [Tooltip("加载界面Canvas的Sorting Order。值越高，显示层级越高，不易被其他UI遮挡。")]
        public int canvasSortingOrder = 1000;

        /// <summary>
        /// 初始化加载界面
        /// </summary>
        protected override void Awake()
        {
            // 设置面板类型为Loading
            m_panelType = UIType.Loading;
            
            // 调用基类的Awake方法，完成基础初始化（获取/创建 CanvasGroup）
            base.Awake();
            
            // 确保使用全局Canvas
            EnsureGlobalCanvasParent();
        }
        
        /// <summary>
        /// 确保加载界面使用全局Canvas作为父级并设置正确的排序层级。
        /// 若全局UI容器下已存在其他加载界面实例，则销毁当前（新）实例，
        /// 保证全局唯一——场景中放置的 Loading 会被移入 GlobalUI（DontDestroyOnLoad），
        /// 不随场景卸载，若不在此去重，每次场景切换都会堆积一份且永不销毁
        /// </summary>
        private void EnsureGlobalCanvasParent()
        {
            // 获取全局Canvas
            GameObject globalCanvasObj = GameObject.Find("GlobalUI");
            Canvas globalCanvas = globalCanvasObj != null ? globalCanvasObj.GetComponent<Canvas>() : null;
            if (globalCanvas != null)
            {
                // 全局唯一性：GlobalUI 下已存在其他加载界面实例时销毁当前实例
                foreach (var other in FindObjectsByType<LoadingScreenView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (other != this && other.transform.IsChildOf(globalCanvas.transform))
                    {
                        Log.Info(LOG_MODULE, "全局UI下已存在加载界面实例，销毁重复实例", this);
                        // 先禁用再销毁：避免 Destroy 延迟执行期间被场景扫描误注册为面板
                        gameObject.SetActive(false);
                        Destroy(gameObject);
                        return;
                    }
                }

                // 如果当前对象不在全局Canvas下，则将其移动到全局Canvas下
                if (transform.parent != globalCanvas.transform)
                {
                    transform.SetParent(globalCanvas.transform, false);
                }
                
                // 获取或添加面板自身的Canvas组件
                if (!TryGetComponent<Canvas>(out var panelCanvas))
                {
                    panelCanvas = gameObject.AddComponent<Canvas>();
                    panelCanvas.overrideSorting = true;
                }
                
                // 设置Canvas的排序层级为1000，与PanelLoader中设置的Loading类型排序层级保持一致
                panelCanvas.sortingOrder = canvasSortingOrder;
                Log.Info(LOG_MODULE, $"已设置加载界面Canvas排序层级为: {canvasSortingOrder}", this);
            }
            else
            {
                Log.Warning(LOG_MODULE, "未找到全局Canvas组件。", this);
            }
        }
        
        /// <summary>
        /// 尝试自动绑定控制器（标准模式：同物体查找，初始化并注入视图）。
        /// 注意：Loading prefab 上已预挂 LoadingScreenController，必须先 TryGetComponent，
        /// 否则会叠加第二个 Controller 组件造成重复挂载。
        /// </summary>
        protected override void TryBindController()
        {
            if (TryGetComponent<LoadingScreenController>(out var controller))
            {
                // 找到预挂的控制器：初始化、注入视图并绑定
                controller.Initialize();
                controller.SetView(this);
                BindController(controller);
                return;
            }

            // 未预挂时创建（仅作为防御路径，prefab 上应始终预挂）
            controller = gameObject.AddComponent<LoadingScreenController>();
            controller.Initialize();
            controller.SetView(this);

            // 绑定控制器到视图
            BindController(controller);
        }
        
        /// <summary>
        /// 初始化面板
        /// 重写IUIPanel接口的Initialize方法
        /// </summary>
        public override void Initialize()
        {
            // 可以在这里进行额外的初始化逻辑
        }
        
        /// <summary>
        /// 清理面板资源
        /// 重写IUIPanel接口的Cleanup方法
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
        }
        
        /// <summary>
        /// 显示加载界面
        /// 重写IUIPanel接口的Show方法：激活物体后播放 CanvasGroup 淡入动画。
        /// 面板常驻 GlobalUI 且隐藏时物体被 SetActive(false) 关闭，
        /// 因此显示前必须先重新激活物体，否则动画不会播放
        /// </summary>
        public override void Show()
        {
            if (!IsVisible)
            {
                // 常驻面板：显示前确保物体激活（隐藏流程会 SetActive(false)，见 Hide）
                gameObject.SetActive(true);

                // 中断可能进行中的淡出协程，避免两个协程并发写入 alpha
                StopAllCoroutines();
                StartCoroutine(FadeIn());

                IsVisible = true;
                Log.Info(LOG_MODULE, "加载界面显示（淡入）");
            }
        }
        
        /// <summary>
        /// 隐藏加载界面
        /// 重写IUIPanel接口的Hide方法：播放 CanvasGroup 淡出动画，
        /// 动画播完后关闭物体并通知控制器——常驻的隐藏面板不占渲染开销（见重构文档 3.3）
        /// </summary>
        public override void Hide()
        {
            if (IsVisible)
            {
                Log.Info(LOG_MODULE, "隐藏加载界面（淡出）");

                // 立即重置可见状态，保证淡出期间 Show() 可正常重新显示面板
                IsVisible = false;

                // 中断可能进行中的淡入协程，避免两个协程并发写入 alpha
                StopAllCoroutines();
                StartCoroutine(FadeOut(() =>
                {
                    // 防御：淡出期间面板被重新显示（Show）时不再关闭物体
                    if (IsVisible)
                    {
                        return;
                    }
                    OnHideAnimationComplete();
                }));
            }
        }
        
        /// <summary>
        /// 隐藏动画完成后的回调处理
        /// 由 Hide 的淡出协程在动画完成后调用（不再依赖动画状态机事件）
        /// </summary>
        public void OnHideAnimationComplete()
        {
            IsVisible = false;

            // 通知控制器加载界面已隐藏
            if (m_controller != null && m_controller is LoadingScreenController loadingController)
            {
                loadingController.OnHideAnimationComplete();
            }
        }
    
    }
}
