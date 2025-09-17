using UnityEngine;
using Logger;

using MyGame.UI.Loading.Controller;

namespace MyGame.UI.Loading.View
{
    /// <summary>
    /// 加载界面组件
    /// MVC架构中的View层，负责显示加载界面的UI元素和动画效果
    /// </summary>
    public class LoadingScreen : BaseView<LoadingScreenController>
    {
        private const string LOG_MODULE = LogModules.LOADING;
        
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
            // 正确的方法：在GameObject上添加控制器组件
            Controller.LoadingScreenController controller = gameObject.AddComponent<Controller.LoadingScreenController>();
            
            // 初始化控制器
            controller.Initialize();
            
            // 设置控制器的视图引用
            controller.SetView(this);
            
            // 绑定控制器到视图
            BindController(controller);
        }
        
        /// <summary>
        /// 控制器绑定后的回调
        /// 可以在这里进行与控制器相关的初始化
        /// </summary>
        protected override void OnControllerBound()
        {
            // 控制器绑定后的初始化逻辑
        }
        
        /// <summary>
        /// 控制器解绑后的回调
        /// 可以在这里清理与控制器相关的资源
        /// </summary>
        protected override void OnControllerUnbound()
        {
            // 控制器解绑后的清理逻辑
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
        /// 重写IUIPanel接口的Hide方法
        /// </summary>
        public override void Hide()
        {
            Log.Info(LOG_MODULE, "隐藏加载界面");
            base.Hide();
        }
    }
}