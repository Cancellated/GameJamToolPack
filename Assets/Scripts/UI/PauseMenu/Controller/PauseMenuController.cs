using UnityEngine;
using Logger;
using MyGame.UI.PauseMenu.Model;
using MyGame.UI.PauseMenu.View;
using MyGame.Events;

namespace MyGame.UI.PauseMenu.Controller
{
    /// <summary>
    /// 暂停菜单控制器，负责处理暂停菜单的逻辑和事件响应
    /// </summary>
    public class PauseMenuController : BaseController<PauseMenuView, PauseMenuModel>
    {
        private const string LOG_MODULE = LogModules.PAUSEMENU;

        /// <summary>
        /// 初始化控制器
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                Log.Info(LOG_MODULE, "初始化暂停菜单控制器");
                
                // 创建并初始化模型
                CreateAndInitializeModel();
                
                // 调用基类初始化
                base.Initialize();
            }
        }

        /// <summary>
        /// 创建并初始化模型
        /// </summary>
        private void CreateAndInitializeModel()
        {
            PauseMenuModel model = new PauseMenuModel();
            model.Initialize();
            SetModel(model);
        }

        /// <summary>
        /// 初始化逻辑
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // 注册事件监听
            RegisterEvents();
        }

        /// <summary>
        /// 注册事件监听
        /// </summary>
        private void RegisterEvents()
        {
            // 这里可以注册需要的游戏事件
            // GameEvents.OnSomeEvent += HandleSomeEvent;
        }

        /// <summary>
        /// 取消注册事件监听
        /// </summary>
        private void UnregisterEvents()
        {
            // 这里可以取消注册游戏事件
            // GameEvents.OnSomeEvent -= HandleSomeEvent;
        }

        /// <summary>
        /// 清理控制器资源
        /// </summary>
        public override void Cleanup()
        {
            if (IsInitialized)
            {
                Log.Info(LOG_MODULE, "清理暂停菜单控制器资源");
                
                // 取消注册事件
                UnregisterEvents();
                
                // 清理模型资源
                if (m_model != null)
                {
                    m_model.Cleanup();
                    m_model = null;
                }
                
                // 调用基类清理
                base.Cleanup();
            }
        }

        /// <summary>
        /// 视图设置后的回调
        /// </summary>
        protected override void OnViewSet()
        {
            base.OnViewSet();
            Log.Info(LOG_MODULE, "暂停菜单视图已设置");
        }

        /// <summary>
        /// 模型设置后的回调
        /// </summary>
        protected override void OnModelSet()
        {
            base.OnModelSet();
            Log.Info(LOG_MODULE, "暂停菜单模型已设置");
        }

        /// <summary>
        /// 清理逻辑
        /// </summary>
        protected override void OnCleanup()
        {
            base.OnCleanup();
        }
    }
}