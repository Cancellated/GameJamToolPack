using UnityEngine;
using System.Collections;
using System;

namespace MyGame.UI
{
    /// <summary>
    /// UI面板基类，实现IUIPanel接口并提供基础功能
    /// 适配MVC架构，作为View层的基类
    /// </summary>
    public abstract class BaseUIView<TController> : MonoBehaviour, IUIPanel where TController : class
    {
        #region 字段和属性
        
        [Header("基础UI设置")]
        [Tooltip("面板的CanvasGroup组件")]
        [SerializeField] protected CanvasGroup m_canvasGroup;
        
        [Tooltip("面板类型")]
        [SerializeField] protected UIType m_panelType = UIType.None;
        
        [Tooltip("淡入淡出动画时长(秒)")]
        [SerializeField] protected float m_fadeDuration = 0.3f;
        
        [Tooltip("面板ID")]
        [SerializeField] protected string m_panelId = "";
        
        /// <summary>
        /// 是否显示面板
        /// </summary>
        public bool IsVisible { get; protected set; }
        
        /// <summary>
        /// 面板ID，用于标识不同的面板
        /// </summary>
        public string PanelId { get { return string.IsNullOrEmpty(m_panelId) ? gameObject.name : m_panelId; } }
        
        /// <summary>
        /// 面板类型，用于UIManager进行状态管理
        /// </summary>
        public UIType PanelType { get { return m_panelType; } }
        
        /// <summary>
        /// 控制器引用
        /// </summary>
        protected TController m_controller;
        
        #endregion
        
        #region 生命周期
        
        /// <summary>
        /// 初始化面板
        /// </summary>
        protected virtual void Awake()
        {
            // 自动获取CanvasGroup组件
            if (m_canvasGroup == null)
            {
                m_canvasGroup = GetComponent<CanvasGroup>();
                if (m_canvasGroup == null)
                {
                    m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            
            // 初始状态为隐藏
            SetCanvasVisible(false);
            IsVisible = false;
            
            // 尝试自动绑定控制器
            TryBindController();
        }
        
        /// <summary>
        /// 当对象被销毁时
        /// </summary>
        protected virtual void OnDestroy()
        {
            UnbindController();
        }
        
        /// <summary>
        /// 当面板被启用时
        /// </summary>
        protected virtual void OnEnable()
        {
            Initialize();
        }
        
        #endregion
        
        #region IUIPanel接口实现
        
        /// <summary>
        /// 显示面板
        /// </summary>
        public virtual void Show()
        {
            if (!IsVisible)
            {
                gameObject.SetActive(true);
                StartCoroutine(FadeIn());
            }
        }
        
        /// <summary>
        /// 隐藏面板
        /// </summary>
        public virtual void Hide()
        {
            if (IsVisible)
            {
                StartCoroutine(FadeOut(() => 
                {
                    gameObject.SetActive(false);
                }));
            }
        }
        
        /// <summary>
        /// 初始化面板
        /// </summary>
        public virtual void Initialize()
        {
            // 子类可以重写此方法进行初始化
        }
        
        /// <summary>
        /// 清理面板资源
        /// </summary>
        public virtual void Cleanup()
        {
            // 子类可以重写此方法进行资源清理
            UnbindController();
        }
        
        /// <summary>
        /// 绑定控制器
        /// </summary>
        /// <param name="controller">要绑定的控制器实例</param>
        public virtual void BindController(TController controller)
        {
            if (m_controller != null && m_controller != controller)
            {
                UnbindController();
            }
            
            m_controller = controller;
            OnControllerBound();
        }
        
        /// <summary>
        /// 解绑控制器
        /// </summary>
        public virtual void UnbindController()
        {
            if (m_controller != null)
            {
                OnControllerUnbound();
                m_controller = null;
            }
        }
        
        /// <summary>
        /// 控制器绑定后的回调
        /// 子类可以重写此方法来处理控制器绑定后的逻辑
        /// </summary>
        protected virtual void OnControllerBound() { }
        
        /// <summary>
        /// 控制器解绑后的回调
        /// 子类可以重写此方法来处理控制器解绑后的逻辑
        /// </summary>
        protected virtual void OnControllerUnbound() { }
        
        /// <summary>
        /// 尝试自动绑定控制器
        /// 可以在子类中重写以提供自定义的绑定逻辑
        /// </summary>
        protected virtual void TryBindController() { }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 设置CanvasGroup的可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        protected void SetCanvasVisible(bool visible)
        {
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = visible ? 1f : 0f;
                m_canvasGroup.interactable = visible;
                m_canvasGroup.blocksRaycasts = visible;
            }
        }
        
        /// <summary>
        /// 淡入动画
        /// </summary>
        /// <returns>协程</returns>
        protected IEnumerator FadeIn()
        {
            float elapsedTime = 0f;
            float startAlpha = m_canvasGroup.alpha;
            
            while (elapsedTime < m_fadeDuration)
            {
                float t = elapsedTime / m_fadeDuration;
                m_canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            m_canvasGroup.alpha = 1f;
            m_canvasGroup.interactable = true;
            m_canvasGroup.blocksRaycasts = true;
            IsVisible = true;
        }
        
        /// <summary>
        /// 淡出动画
        /// </summary>
        /// <param name="onComplete">完成回调</param>
        /// <returns>协程</returns>
        protected IEnumerator FadeOut(System.Action onComplete)
        {
            float elapsedTime = 0f;
            float startAlpha = m_canvasGroup.alpha;
            
            while (elapsedTime < m_fadeDuration)
            {
                float t = elapsedTime / m_fadeDuration;
                m_canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            m_canvasGroup.alpha = 0f;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
            IsVisible = false;

            onComplete?.Invoke();
        }
        
        #endregion
    }
    
    /// <summary>
    /// 非泛型版本的BaseUI，保持向后兼容性
    /// </summary>
    public abstract class BaseUI : BaseUIView<object>
    { }    
}
