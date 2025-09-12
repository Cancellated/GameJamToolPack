using System.Collections;
using UnityEngine;
using Logger;
using MyGame.UI;

namespace MyGame.UI.Loading
{
    /// <summary>
    /// 加载界面组件
    /// MVC架构中的View层，负责显示加载界面的UI元素和动画效果
    /// </summary>
    public class LoadingScreen : BaseUIView<LoadingScreenController>
    {
        private const string module = "Loading";
        
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
        /// 尝试自动绑定控制器
        /// 创建并绑定LoadingScreenController实例
        /// </summary>
        protected override void TryBindController()
        {
            // 创建控制器实例
            LoadingScreenController controller = new();
            
            // 初始化控制器
            controller.Initialize();
            
            // 设置控制器的视图引用
            controller.SetView(this);
            
            // 绑定控制器到视图
            BindController(controller);
            
            Log.Info(module, "加载界面已绑定控制器");
        }
        
        /// <summary>
        /// 控制器绑定后的回调
        /// 可以在这里进行与控制器相关的初始化
        /// </summary>
        protected override void OnControllerBound()
        {
            Log.Info(module, "加载界面控制器已绑定");
        }
        
        /// <summary>
        /// 控制器解绑后的回调
        /// 可以在这里清理与控制器相关的资源
        /// </summary>
        protected override void OnControllerUnbound()
        {
            Log.Info(module, "加载界面控制器已解绑");
        }
        
        /// <summary>
        /// 初始化面板
        /// 重写IUIPanel接口的Initialize方法
        /// </summary>
        public override void Initialize()
        {
            Log.Info(module, "初始化加载界面");
            // 可以在这里进行额外的初始化逻辑
        }
        
        /// <summary>
        /// 清理面板资源
        /// 重写IUIPanel接口的Cleanup方法
        /// </summary>
        public override void Cleanup()
        {
            Log.Info(module, "清理加载界面资源");
            base.Cleanup();
        }
        
        /// <summary>
        /// 显示加载界面
        /// 重写IUIPanel接口的Show方法
        /// </summary>
        public override void Show()
        {
            Log.Info(module, "显示加载界面");
            base.Show();
        }
        
        /// <summary>
        /// 隐藏加载界面
        /// 重写IUIPanel接口的Hide方法
        /// </summary>
        public override void Hide()
        {
            Log.Info(module, "隐藏加载界面");
            base.Hide();
        }
        
        /// <summary>
        /// 当场景开始加载时的回调
        /// 由控制器调用，用于更新UI显示
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        public void OnSceneLoadStarted(string sceneName)
        {
            Log.Info(module, $"场景加载开始: {sceneName}");
            // 可以在这里添加加载动画的启动逻辑
            // 例如：显示加载进度条、播放加载动画等
        }
        
        /// <summary>
        /// 当场景加载完成时的回调
        /// 由控制器调用，用于更新UI显示
        /// </summary>
        /// <param name="sceneName">已加载完成的场景名称</param>
        public void OnSceneLoadCompleted(string sceneName)
        {
            Log.Info(module, $"场景加载完成: {sceneName}");
            // 可以在这里添加加载动画的结束逻辑
            // 例如：隐藏进度条、停止动画等
        }
    }
}
