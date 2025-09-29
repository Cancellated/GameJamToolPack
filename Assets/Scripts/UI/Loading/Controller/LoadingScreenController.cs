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
    public class LoadingScreenController : BaseController<LoadingScreenView, LoadingScreenModel>
    {
        private const string LOG_MODULE = LogModules.LOADING;

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
        /// </summary>
        protected override void OnInitialize()
        {
        }

        /// <summary>
        /// 清理控制器资源
        /// </summary>
        protected override void OnCleanup()
        {
            // 清理模型资源
            m_model?.Cleanup();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 当加载界面隐藏动画播放完成时调用
        /// 负责清理加载界面资源
        /// </summary>
        public void OnHideAnimationComplete()
        {
            Log.Info(LOG_MODULE, "加载界面隐藏动画播放完成，清理加载界面资源");
            
            if (m_view != null && m_view.gameObject != null)
            {
                Log.Info(LOG_MODULE, "准备销毁加载界面对象（已添加到DontDestroyOnLoad Canvas）");
                
                // 确保UIManager注销此面板
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UnregisterUIPanel(UIType.Loading);
                }
                
                // 延迟一帧后销毁，确保所有事件都已处理完成
                // 视图对象销毁后，Canvas会在没有子对象时自动清理
                UnityEngine.Object.Destroy(m_view.gameObject, 0.1f);
            }
        }

        #endregion
    }
}