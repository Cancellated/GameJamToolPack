using MyGame.Managers;
using Logger;
using UnityEngine;
using MyGame.UI.Loading.Model;
using MyGame.Events;
using MyGame.UI.Loading.View;

namespace MyGame.UI.Loading.Controller
{
    /// <summary>
    /// 加载界面控制器
    /// 负责处理加载界面的逻辑、事件响应和与视图的交互
    /// </summary>
    public class LoadingScreenController : BaseController<LoadingScreen, LoadingScreenModel>
    {
        private const string module = LogModules.LOADING;

        #region 初始化和清理
        /// <summary>
        /// 初始化控制器
        /// 调用基类Initialize并执行初始化逻辑
        /// </summary>
        public override void Initialize()
        {
            // 创建并初始化模型
            if (m_model == null)
            {
                m_model = new LoadingScreenModel();
                m_model.Initialize();
                SetModel(m_model);
            }
            
            base.Initialize();
        }

        /// <summary>
        /// 初始化控制器逻辑
        /// 订阅场景加载相关的事件
        /// </summary>
        protected override void OnInitialize()
        {
            Log.Info(module, "初始化加载界面控制器");
            
            // 订阅场景加载相关事件
            GameEvents.OnSceneLoadStart += HandleSceneLoadStart;
            GameEvents.OnSceneLoadComplete += HandleSceneLoadComplete;
        }

        /// <summary>
        /// 清理控制器资源
        /// 取消订阅所有事件
        /// </summary>
        protected override void OnCleanup()
        {
            Log.Info(module, "清理加载界面控制器");
            
            // 取消订阅所有事件
            GameEvents.OnSceneLoadStart -= HandleSceneLoadStart;
            GameEvents.OnSceneLoadComplete -= HandleSceneLoadComplete;
            
            // 清理模型资源
            if (m_model != null)
            {
                m_model.Cleanup();
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理场景加载开始事件
        /// 通知视图更新加载信息
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        private void HandleSceneLoadStart(string sceneName)
        {
            // 更新模型数据
            if (m_model != null)
            {
                m_model.StartLoading(sceneName);
            }
            
            // 通知视图更新
            if (m_view != null)
            {
                m_view.OnSceneLoadStarted(sceneName);
            }
        }

        /// <summary>
        /// 处理场景加载完成事件
        /// 通知视图更新加载状态
        /// </summary>
        /// <param name="sceneName">已加载完成的场景名称</param>
        private void HandleSceneLoadComplete(string sceneName)
        {
            // 更新模型数据
            if (m_model != null)
            {
                m_model.CompleteLoading();
            }
            
            // 通知视图更新
            if (m_view != null)
            {
                m_view.OnSceneLoadCompleted(sceneName);
            }
        }

        #endregion

        #region 视图和模型设置回调

        /// <summary>
        /// 视图设置后的回调
        /// 在这里可以进行视图相关的初始化操作
        /// </summary>
        protected override void OnViewSet()
        {
            Log.Info(module, "加载界面视图已设置");
            // 可以在这里进行视图相关的初始化操作
        }
        
        /// <summary>
        /// 模型设置后的回调
        /// 在这里可以进行模型相关的初始化操作
        /// </summary>
        protected override void OnModelSet()
        {
            Log.Info(module, "加载界面模型已设置");
            // 可以在这里进行模型相关的初始化操作
        }

        #endregion
    }
}