using UnityEngine;
using Unity.VisualScripting;

namespace MyGame.UI
{
    /// <summary>
    /// MVC架构中的控制器基类（非泛型版本）
    /// 负责处理用户输入、更新模型和通知视图更新
    /// 
    /// 【使用规范】
    /// - 非泛型版本仅提供基础的 Initialize/Cleanup 生命周期
    /// - 若 Controller 需要持有 View 和 Model 引用，请使用泛型版本 BaseController&lt;TView, TModel&gt;
    /// - Initialize() 应在子类 Awake/OnEnable 的合适时机调用
    /// - Cleanup() 应在子类 OnDestroy/OnDisable 的合适时机调用
    /// </summary>
    public abstract class BaseController : MonoBehaviour
    {
        #region 字段和属性
        
        /// <summary>
        /// 是否初始化
        /// </summary>
        public bool IsInitialized { get; private set; }
        
        #endregion
        
        #region 构造函数
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public BaseController()
        {
            IsInitialized = false;
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 初始化控制器（双重检查，防止重复初始化）
        /// 
        /// 【使用规范】
        /// - 子类应在 Awake 或 OnEnable 中调用此方法
        /// - 调用后会触发 OnInitialize() 回调
        /// - IsInitialized 标志确保只初始化一次
        /// </summary>
        public virtual void Initialize()
        {
            if (!IsInitialized)
            {
                OnInitialize();
                IsInitialized = true;
            }
        }
        
        /// <summary>
        /// 清理控制器资源（双重检查，仅清理已初始化的实例）
        /// 
        /// 【使用规范】
        /// - 子类应在 OnDestroy 或 OnDisable 中调用此方法
        /// - 调用前会触发 OnCleanup() 回调
        /// - 调用后 IsInitialized 重置为 false
        /// </summary>
        public virtual void Cleanup()
        {
            if (IsInitialized)
            {
                OnCleanup();
                IsInitialized = false;
            }
        }
        
        #endregion
        
        #region 保护方法
        
        /// <summary>
        /// 初始化逻辑回调
        /// 由 Initialize() 在首次初始化时调用
        /// 
        /// 【使用规范】
        /// - 子类重写此方法来添加初始化逻辑（如创建Model、设置View引用、订阅事件等）
        /// - 不要在此处直接调用 Initialize()，会导致递归
        /// </summary>
        protected virtual void OnInitialize() { }
        
        /// <summary>
        /// 清理逻辑回调
        /// 由 Cleanup() 调用
        /// 
        /// 【使用规范】
        /// - 子类重写此方法来添加清理逻辑（如取消订阅事件、释放资源等）
        /// - 不要在此处直接调用 Cleanup()，会导致递归
        /// </summary>
        protected virtual void OnCleanup() { }
        
        #endregion
    }
    
    /// <summary>
    /// 泛型版本的控制器基类
    /// 支持与特定的视图和模型类型关联
    /// 
    /// 【使用规范】
    /// - 所有需要管理 View 和 Model 引用的 Controller 应继承此类
    /// - 子类在 Awake 中应依次调用：
    ///   1. CreateAndInitializeModel() —— 创建并初始化Model实例
    ///   2. SetView(view) —— 设置View引用
    /// - View通过 m_view 调用，Model通过 m_model 读写
    /// - 若Model继承自 ObservableModel，应在子类的初始化中订阅 m_model.OnPropertyChanged 事件
    /// - 若Model继承自 ObservableModel，应在子类的清理中取消订阅 m_model.OnPropertyChanged 事件
    /// - View不应直接访问 m_model，所有Model数据读写由Controller中转
    /// </summary>
    /// <typeparam name="TView">视图类型</typeparam>
    /// <typeparam name="TModel">模型类型（必须有无参构造函数）</typeparam>
    public abstract class BaseController<TView, TModel> : BaseController 
        where TView : class
        where TModel : class, new()
    {
        #region 字段和属性
        
        /// <summary>
        /// 视图引用
        /// </summary>
        protected TView m_view;
        
        /// <summary>
        /// 模型引用
        /// </summary>
        protected TModel m_model;
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 创建并初始化模型实例
        /// 由于模型通常不是MonoBehaviour，不能直接挂载到游戏对象上
        /// 通过代码创建实例来解决这个问题
        /// 
        /// 【使用规范】
        /// - 子类应在 Awake 中调用此方法（在 SetView 之前或之后均可，但建议在 SetView 之前）
        /// - 内部使用 new() 创建实例，通过反射调用 Initialize()（如果存在）
        /// - 创建后会通过 SetModel() 设置 m_model 引用
        /// - 返回的实例已设置到 m_model，子类可直接使用
        /// </summary>
        /// <returns>创建并初始化后的模型实例</returns>
        protected TModel CreateAndInitializeModel()
        {
            // 创建模型实例
            TModel model = new();
            
            // 使用反射检查并调用Initialize方法
            var initializeMethod = typeof(TModel).GetMethod("Initialize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
            initializeMethod?.Invoke(model, null);
            
            // 设置模型引用
            SetModel(model);
            
            return model;
        }
        
        /// <summary>
        /// 设置视图引用
        /// 
        /// 【使用规范】
        /// - 子类应在 Awake 中调用此方法，将 View 的引用传递给 Controller
        /// - 通常通过 [SerializeField] 在 Inspector 中拖拽赋值，然后在 Awake 中调用 SetView
        /// - 设置后 Controller 即可通过 m_view 调用 View 的公共方法
        /// </summary>
        /// <param name="view">视图实例</param>
        public virtual void SetView(TView view)
        {
            m_view = view;
        }
        
        /// <summary>
        /// 设置模型引用
        /// 
        /// 【使用规范】
        /// - 通常由 CreateAndInitializeModel() 内部调用，子类一般不需要直接调用
        /// - 如果需要自行创建 Model 实例，可调用此方法设置引用
        /// - 设置后 Controller 即可通过 m_model 读写 Model 属性
        /// </summary>
        /// <param name="model">模型实例</param>
        public virtual void SetModel(TModel model)
        {
            m_model = model;
        }
        
        #endregion
    }
}
