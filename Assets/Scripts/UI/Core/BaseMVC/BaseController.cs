using UnityEngine;
using Unity.VisualScripting;

namespace MyGame.UI
{
    /// <summary>
    /// MVC架构中的控制器基类
    /// 负责处理用户输入、更新模型和通知视图更新
    /// </summary>
    public abstract class BaseController : MonoBehaviour
    {
        #region 字段和属性
        
        /// <summary>
        /// 是否初始化
        /// </summary>
        public bool IsInitialized { get; protected set; }
        
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
        /// 初始化控制器
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
        /// 清理控制器资源
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
        /// 初始化逻辑
        /// 子类可以重写此方法来实现特定的初始化逻辑
        /// </summary>
        protected virtual void OnInitialize() { }
        
        /// <summary>
        /// 清理逻辑
        /// 子类可以重写此方法来实现特定的清理逻辑
        /// </summary>
        protected virtual void OnCleanup() { }
        
        #endregion
    }
    
    /// <summary>
    /// 泛型版本的控制器基类
    /// 支持与特定的视图和模型类型关联
    /// </summary>
    /// <typeparam name="TView">视图类型</typeparam>
    /// <typeparam name="TModel">模型类型</typeparam>
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
        /// 初始化控制器
        /// 设置视图和模型引用
        /// </summary>
        public override void Initialize()
        {
            SetView(m_view);
            SetModel(CreateAndInitializeModel());
        }
        
        /// <summary>
        /// 创建并初始化模型实例
        /// 由于模型通常不是MonoBehaviour，不能直接挂载到游戏对象上
        /// 通过代码创建实例来解决这个问题
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
        /// 设置视图引用并建立双向绑定
        /// </summary>
        /// <param name="view">视图实例</param>
        public virtual void SetView(TView view)
        {
            // 如果已经有视图引用，先解绑旧的视图
            if (m_view != null && m_view != view)
            {
                // 尝试调用旧视图的UnbindController方法（如果存在）
                var unbindMethod = typeof(TView).GetMethod("UnbindController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
                unbindMethod?.Invoke(m_view, null);
            }
            
            // 设置新的视图引用
            m_view = view;
            
            // 如果传入的视图为null，尝试自动查找视图
            m_view ??= TryFindView();
            
            // 如果找到了视图，尝试建立双向绑定
            if (m_view != null)
            {
                // 尝试调用视图的BindController方法（如果存在）
                var bindMethod = typeof(TView).GetMethod("BindController", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                    null, 
                    new[] { this.GetType() },
                    null);
                
                if (bindMethod != null)
                {
                    bindMethod.Invoke(m_view, new object[] { this });
                }
            }
        }
        
        /// <summary>
        /// 尝试自动查找视图实例
        /// 按照特定的查找策略尝试找到匹配的视图
        /// </summary>
        /// <returns>找到的视图实例，如果没有找到则返回null</returns>
        protected virtual TView TryFindView()
        {
            // 尝试从同一GameObject获取视图组件
            
            // 如果同一GameObject没有找到，则尝试从子对象中查找
            if (!TryGetComponent<TView>(out var view))
            {
                view = GetComponentInChildren<TView>();
            }
            
            // 如果还没找到，则尝试从父对象中查找
            view ??= GetComponentInParent<TView>();
            
            return view;
        }
        
        /// <summary>
        /// 设置模型引用
        /// </summary>
        /// <param name="model">模型实例</param>
        public virtual void SetModel(TModel model)
        {
            m_model = model;
        }
        
        #endregion
    }
}