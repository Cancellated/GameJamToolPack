using MyGame.UI.About.Controller;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MyGame.UI.About.View
{
    /// <summary>
    /// 关于面板视图类，负责显示关于界面的UI元素和处理用户输入
    /// </summary>
    public class AboutPanelView : BaseView<AboutPanelController>
    {
        #region 字段

        [Header("UI References")]
        [Tooltip("关于面板根对象")]
        [SerializeField] private GameObject m_aboutPanel;

        [Tooltip("关闭按钮")]
        [SerializeField] private Button m_closeButton;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化面板（仅设置面板类型与基础绑定）
        /// </summary>
        protected override void Awake()
        {
            // 设置面板类型
            m_panelType = UIType.AboutPanel;
            base.Awake();
        }

        /// <summary>
        /// 初始化面板：绑定按钮事件（遵循基类规范：在 Initialize 中绑定，与 Cleanup 中的解绑成对）
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            BindButtonEvents();
        }

        /// <summary>
        /// 清理面板资源：解绑按钮事件
        /// </summary>
        public override void Cleanup()
        {
            UnbindButtonEvents();
            base.Cleanup();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示面板
        /// </summary>
        public override void Show()
        {
            base.Show();
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public override void Hide()
        {
            base.Hide();
        }


        #endregion

        #region 私有方法

        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        private void BindButtonEvents()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        /// <summary>
        /// 解绑按钮事件（与 BindButtonEvents 成对出现）
        /// </summary>
        private void UnbindButtonEvents()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }

        /// <summary>
        /// 关闭按钮点击事件处理
        /// </summary>
        private void OnCloseButtonClicked()
        {
            if (m_controller != null)
            {
                m_controller.OnCloseButtonClick();
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// 尝试自动绑定控制器（标准模式：同物体查找，初始化并注入视图）
        /// </summary>
        protected override void TryBindController()
        {
            if (TryGetComponent<AboutPanelController>(out var controller))
            {
                // 找到控制器：初始化、注入视图并绑定
                controller.Initialize();
                controller.SetView(this);
                BindController(controller);
                return;
            }

            // 如果没有找到控制器，则创建一个新的
            controller = gameObject.AddComponent<AboutPanelController>();
            controller.Initialize();
            controller.SetView(this);

            // 绑定控制器到视图
            BindController(controller);
        }

        #endregion
    }
}