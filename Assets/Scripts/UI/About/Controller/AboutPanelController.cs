using MyGame.Events;
using MyGame.UI.About.Model;
using MyGame.UI.About.View;
using UnityEngine;
using MyGame.UI;

namespace MyGame.UI.About.Controller
{
    /// <summary>
    /// 关于面板控制器类
    /// 负责连接视图和模型，处理用户输入和业务逻辑
    /// </summary>
    public class AboutPanelController : BaseController<AboutPanelView, AboutModel>
    {
        #region 生命周期

        /// <summary>
        /// 初始化控制器
        /// </summary>
        public override void Initialize()
        {
            // 创建并初始化模型
            if (m_model == null)
            {
                m_model = new AboutModel();
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
            // 注册事件监听
            GameEvents.OnMenuShow += OnMenuShow;
        }

        /// <summary>
        /// 清理控制器资源
        /// </summary>
        public override void Cleanup()
        {
            UnregisterEvents();
            
            // 清理模型资源
            if (m_model != null)
            {
                m_model.Cleanup();
                m_model = null;
            }
            
            // 清理视图引用
            if (m_view != null)
            {
                m_view.UnbindController();
                m_view = null;
            }
            
            base.Cleanup();
        }
        
        /// <summary>
        /// 当对象被销毁时
        /// </summary>
        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion
        
        #region 私有辅助方法
        
        /// <summary>
        /// 注销所有事件监听
        /// </summary>
        private void UnregisterEvents()
        {
            // 注销事件监听
            GameEvents.OnMenuShow -= OnMenuShow;
        }

        #endregion

        #region 事件监听

        /// <summary>
        /// UI显示事件处理
        /// </summary>
        /// <param name="type">UI类型</param>
        /// <param name="show">是否显示</param>
        private void OnMenuShow(UIType type, bool show)
        {
            if(type == UIType.AboutPanel)
            {
                if(show)
                {
                    m_view.Show();
                }
                else
                {
                    m_view.Hide();
                }
            }
        }

        #endregion

        #region 控制器方法

        /// <summary>
        /// 设置视图引用
        /// </summary>
        /// <param name="view">关于面板视图</param>
        public override void SetView(AboutPanelView view)
        {
            base.SetView(view);
        }

        #endregion

        #region 用户交互处理

        /// <summary>
        /// 关闭按钮点击事件处理
        /// </summary>
        public void OnCloseButtonClick()
        {
            // 触发UI隐藏事件
            GameEvents.TriggerMenuShow(UIType.AboutPanel, false);
        }

        #endregion
    }
}