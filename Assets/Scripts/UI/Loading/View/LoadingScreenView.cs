using Logger;
using UnityEngine;

using MyGame.UI.Loading.Controller;
using UnityEngine.UI;

namespace MyGame.UI.Loading.View
{
    /// <summary>
    /// 加载界面组件
    /// MVC架构中的View层，负责显示加载界面的UI元素和动画效果
    /// </summary>
    public class LoadingScreenView : BaseView<LoadingScreenController>
    {
        private const string LOG_MODULE = LogModules.LOADING;
        private static GameObject s_loadingCanvas;

        /// <summary>
        /// 初始化加载界面
        /// </summary>
        protected override void Awake()
        {
            // 设置面板类型为Loading
            m_panelType = UIType.Loading;
            
            // 调用基类的Awake方法，完成基础初始化
            base.Awake();
        }
        
        /// <summary>
        /// 清理加载Canvas
        /// </summary>
        private static void CleanupLoadingCanvas()
        {
            if (s_loadingCanvas != null)
            {
                // 检查Canvas下是否还有其他子对象
                if (s_loadingCanvas.transform.childCount == 0)
                {
                    Log.Info(LOG_MODULE, "加载Canvas下没有子对象，准备销毁");
                    UnityEngine.Object.Destroy(s_loadingCanvas);
                    s_loadingCanvas = null;
                }
            }
        }
        
        /// <summary>
        /// 尝试自动绑定控制器
        /// 创建并绑定LoadingScreenController实例
        /// </summary>
        protected override void TryBindController()
        {
            // 正确的方法：在GameObject上添加控制器组件
            LoadingScreenController controller = gameObject.AddComponent<LoadingScreenController>();
            
            // 初始化控制器
            controller.Initialize();
            
            // 设置控制器的视图引用
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
        /// 重写IUIPanel接口的Show方法
        /// </summary>
        public override void Show()
        {
            base.Show();
        }
        
        /// <summary>
        /// 隐藏加载界面
        /// 重写IUIPanel接口的Hide方法，添加完成回调支持
        /// </summary>
        public override void Hide()
        {
            Log.Info(LOG_MODULE, "隐藏加载界面");
            // 自定义淡出动画完成后的行为：销毁加载界面
            StartCoroutine(CustomHideRoutine());
        }
        
        /// <summary>
        /// 自定义隐藏协程
        /// 执行淡出动画后触发隐藏完成事件并销毁加载界面
        /// </summary>
        private System.Collections.IEnumerator CustomHideRoutine()
        {
            // 播放淡出动画
            if (IsVisible)
            {
                yield return StartCoroutine(base.FadeOut(() => 
                {
                    gameObject.SetActive(false);
                }));
            }
            
            // 动画播放完成后，通知控制器可以进行场景切换
            if (m_controller != null)
            {
                var loadingController = m_controller as LoadingScreenController;
                if (loadingController != null)
                {
                    loadingController.OnHideAnimationComplete();
                }
            }
            
            // 延迟一帧后清理Canvas，确保对象销毁顺序正确
            yield return null;
            
            // 清理加载Canvas（如果没有其他子对象）
            CleanupLoadingCanvas();
            
            Log.Info(LOG_MODULE, "加载界面淡出动画播放完成，已请求清理Canvas");
        }
    }
}