using UnityEngine;
using System.Collections;
using System;

namespace MyGame.UI
{
    /// <summary>
    /// UI面板基类，实现IUIPanel接口并提供基础功能
    /// 适配MVC架构，作为View层的基类
    /// 
    /// 【使用规范】
    /// - 每个View子类必须重写 TryBindController()，在其中查找或创建Controller并调用 BindController()
    /// - View应通过 m_controller 直接调用Controller的公共方法处理用户交互（如按钮点击）
    /// - View不应直接持有Model引用，所有数据访问必须通过Controller中转
    /// - 子类在Awake中设置 m_panelType 后再调用 base.Awake()
    /// - 子类重写OnDestroy时必须调用 base.OnDestroy() 以确保控制器解绑
    /// - 子类重写OnEnable时必须调用 base.OnEnable() 以确保 Initialize() 被触发
    /// - Initialize() 用于组件初始化和按钮事件绑定
    /// - Cleanup() 用于解绑事件和清理资源
    /// - 如面板不需要淡入淡出动画，可重写Show/Hide直接操作CanvasGroup，但必须维护IsVisible状态
    /// - 按钮点击事件中应先判空 m_controller 再调用其方法
    /// - View 与 Controller 的挂载关系由各 View 子类的 TryBindController 决定
    ///   （常见做法：同 GameObject 查找或创建 Controller，见 HUD/PauseMenu/MainMenu 等实现）
    /// </summary>
    public abstract class BaseView<TController> : MonoBehaviour, IUIPanel where TController : class
    {
        #region 字段和属性
        
        [Header("基础UI设置")]
        [Tooltip("面板的CanvasGroup组件")]
        [SerializeField] protected CanvasGroup m_canvasGroup;
        
        [Tooltip("面板类型")]
        [SerializeField] protected UIType m_panelType = UIType.None;
        
        [Tooltip("淡入淡出动画时长(秒)")]
        [SerializeField] protected float m_fadeDuration = 0.3f;
        
        /// <summary>
        /// 是否显示面板
        /// </summary>
        public bool IsVisible { get; protected set; }

        /// <summary>
        /// 面板是否已完成首次初始化（防重入标志）。
        /// OnEnable 每次激活都会触发，若每次都执行 Initialize() 会导致按钮重复绑定、
        /// 控制器重复创建等连锁问题；此标志保证 Initialize() 只执行一次，
        /// 直到面板被 Cleanup 清理后才允许重新初始化。
        /// </summary>
        private bool _initialized;
        
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
        /// 初始化面板：
        /// 1. 自动获取CanvasGroup组件
        /// 2. 初始状态为隐藏
        /// 3. 尝试自动绑定控制器
        /// 
        /// 【使用规范】
        /// - 子类重写时必须在调用 base.Awake() 之前设置 m_panelType（如：m_panelType = UIType.MainMenu）
        /// - 子类重写时必须调用 base.Awake()
        /// - 按钮事件绑定请在 Initialize() 中完成，不要在 Awake 中绑定
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
        /// 当对象被销毁时，自动解绑控制器
        /// 
        /// 【使用规范】
        /// - 子类重写时必须调用 base.OnDestroy() 以确保控制器正确解绑
        /// - 子类重写时应在 Cleanup() 中解绑按钮事件，在此处仅做额外的销毁清理
        /// </summary>
        protected virtual void OnDestroy()
        {
            UnbindController();
        }
        
        /// <summary>
        /// 当面板被启用时，自动调用 Initialize() 进行初始化
        /// 
        /// 【使用规范】
        /// - 子类重写时必须调用 base.OnEnable() 以确保初始化被触发
        /// - 不要在 OnEnable 中绑定按钮事件（应在 Initialize 中完成）
        /// - 初始化只执行一次（_initialized 防重入）：面板重复 SetActive 不会重复初始化，
        ///   避免按钮重复绑定 / 控制器重复创建；Cleanup() 后重置标志，允许重新初始化
        /// </summary>
        protected virtual void OnEnable()
        {
            if (!_initialized)
            {
                _initialized = true;
                Initialize();
            }
        }

        /// <summary>
        /// 当面板被禁用时
        /// 
        /// 【使用规范】
        /// - 子类可以重写此方法以在面板禁用时执行清理（如停止协程、暂停动画）
        /// - 不要在此处解绑按钮事件（应在 Cleanup 中完成）
        /// </summary>
        protected virtual void OnDisable()
        {

        }
        
        #endregion
        
        #region IUIPanel接口实现
        
        /// <summary>
        /// 显示面板（带淡入动画）
        /// 
        /// 【使用规范】
        /// - 默认使用 FadeIn() 协程播放淡入动画
        /// - 子类可重写以跳过动画（直接设置 CanvasGroup.alpha/interactable/blocksRaycasts 和 IsVisible = true）
        /// - 重写时必须确保 IsVisible 状态被正确设置为 true
        /// - 不要在此方法中绑定按钮事件（应在 Initialize 中完成）
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
        /// 隐藏面板（带淡出动画）
        /// 
        /// 【使用规范】
        /// - 默认使用 FadeOut() 协程播放淡出动画，动画完成后关闭GameObject
        /// - 子类可重写以跳过动画（直接设置 CanvasGroup.alpha/interactable/blocksRaycasts 和 IsVisible = false）
        /// - 重写时必须确保 IsVisible 状态被正确设置为 false
        /// - 不要在此方法中解绑按钮事件（应在 Cleanup 中完成）
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
        /// 只切换 CanvasGroup 的可交互性，不改变 alpha、激活状态或 IsVisible。
        /// 用于子界面打开时保留下层面板作为背景，同时暂时禁止其接收输入。
        /// </summary>
        public virtual void SetInteractable(bool interactable)
        {
            if (m_canvasGroup != null)
            {
                m_canvasGroup.interactable = interactable;
                m_canvasGroup.blocksRaycasts = interactable;
            }
        }
        
        /// <summary>
        /// 初始化面板
        /// 由基类的 OnEnable() 自动调用，不需要手动调用
        /// 
        /// 【使用规范】
        /// - 子类应在此方法中进行：
        ///   1. 按钮事件绑定（如需绑定，在此处完成）
        ///   2. UI组件初始化（如设置默认值、填充列表等）
        ///   3. 订阅 Controller 或全局事件
        /// - 不要在此处访问 Model 数据（Model 可能尚未就绪），数据填充应通过 Controller 回调或 UpdateView 方法完成
        /// </summary>
        public virtual void Initialize()
        {
            // 子类可以重写此方法进行初始化
        }
        
        /// <summary>
        /// 清理面板资源
        /// 
        /// 【使用规范】
        /// - 子类应在此方法中进行：
        ///   1. 解绑按钮事件（与 Initialize 中的绑定成对出现）
        ///   2. 取消订阅 Controller/Model/全局事件
        ///   3. 释放托管资源（如 Addressable 资源句柄）
        /// - 子类重写时应调用 base.Cleanup()
        /// </summary>
        public virtual void Cleanup()
        {
            // 重置初始化标志：面板注销后再次注册时可重新初始化
            _initialized = false;
        }
        
        /// <summary>
        /// 绑定控制器
        /// 
        /// 【使用规范】
        /// - 此方法由 TryBindController() 调用，子类一般不需要直接调用
        /// - 如果已有旧控制器，会先解绑再绑定新的
        /// - 绑定后会自动调用 OnControllerBound() 回调
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
        /// 
        /// 【使用规范】
        /// - 此方法由基类的 OnDestroy() 自动调用，子类一般不需要直接调用
        /// - 解绑前会先调用 OnControllerUnbound() 回调，子类可在该回调中取消订阅
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
        /// 由 BindController() 在绑定成功后自动调用
        /// 
        /// 【使用规范】
        /// - 子类可重写此方法以执行 Controller 绑定后的初始化：
        ///   1. 通过 Controller 获取初始化数据并更新 UI
        ///   2. 订阅 Controller 暴露的事件
        /// - 注意：不要在此处直接访问 Model，应通过 Controller 中转
        /// </summary>
        protected virtual void OnControllerBound() { }
        
        /// <summary>
        /// 控制器解绑前的回调
        /// 由 UnbindController() 在解绑前调用
        /// 
        /// 【使用规范】
        /// - 子类可重写此方法以取消订阅 Controller 相关事件
        /// - 应与此类的 OnControllerBound() 中的订阅成对出现
        /// </summary>
        protected virtual void OnControllerUnbound() { }
        
        /// <summary>
        /// 尝试自动绑定控制器
        /// 由基类的 Awake() 自动调用
        /// 
        /// 【使用规范】
        /// - 每个View子类 MUST 重写此方法！
        /// - 在方法中查找或创建 Controller 实例，然后调用 BindController(controller)
        /// - 标准实现模式：
        ///   1. 先尝试在父物体/根物体中查找 Controller 组件
        ///   2. 若未找到，则通过 AddComponent 创建
        ///   3. 调用 BindController(controller)
        /// </summary>
        protected virtual void TryBindController() { }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 设置CanvasGroup的可见性（瞬间切换，无动画）
        /// 
        /// 【使用规范】
        /// - 内部方法，由 Awake 初始化时调用，子类一般不需要直接调用
        /// - 如果子类需要瞬间切换可见性，使用此方法而非手动设置 CanvasGroup 属性
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
        /// 淡入动画协程
        /// 
        /// 【使用规范】
        /// - 由 Show() 自动调用，子类一般不需要直接调用
        /// - 动画时长由 m_fadeDuration 控制（默认0.3秒），子类可在 Inspector 或代码中调整
        /// - 使用 unscaledDeltaTime 确保在 Time.timeScale=0 时动画仍能正常播放
        /// </summary>
        /// <returns>协程</returns>
        protected IEnumerator FadeIn()
        {
            if (m_canvasGroup == null)
            {
                IsVisible = true;
                yield break;
            }
            
            float elapsedTime = 0f;
            float startAlpha = m_canvasGroup.alpha;
            
            while (elapsedTime < m_fadeDuration)
            {
                float t = elapsedTime / m_fadeDuration;
                m_canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                // 使用不受时间缩放影响的deltaTime，确保在timeScale为0时动画仍能正常进行
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            
            m_canvasGroup.alpha = 1f;
            m_canvasGroup.interactable = true;
            m_canvasGroup.blocksRaycasts = true;
            IsVisible = true;
        }
        
        /// <summary>
        /// 淡出动画协程
        /// 
        /// 【使用规范】
        /// - 由 Hide() 自动调用，子类一般不需要直接调用
        /// - 动画时长由 m_fadeDuration 控制（默认0.3秒）
        /// - 使用 unscaledDeltaTime 确保在 Time.timeScale=0 时动画仍能正常播放
        /// - onComplete 回调在动画完成后执行（默认会关闭 GameObject）
        /// </summary>
        /// <param name="onComplete">完成回调</param>
        /// <returns>协程</returns>
        protected IEnumerator FadeOut(System.Action onComplete)
        {
            if (m_canvasGroup == null)
            {
                IsVisible = false;
                onComplete?.Invoke();
                yield break;
            }

            // 淡出开始就立即关闭交互与射线阻挡：
            // 若等到动画结束才关闭，淡出期间本面板仍会挡住下层按钮（如返回主菜单后的加载按钮）
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;

            float elapsedTime = 0f;
            float startAlpha = m_canvasGroup.alpha;
            
            while (elapsedTime < m_fadeDuration)
            {
                float t = elapsedTime / m_fadeDuration;
                m_canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                // 使用不受时间缩放影响的deltaTime，确保在timeScale为0时动画仍能正常进行
                elapsedTime += Time.unscaledDeltaTime;
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
}
