using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Logger;

namespace MyGame.UI
{
    /// <summary>
    /// MVC架构中的模型基类
    /// 负责管理数据和业务逻辑
    /// 
    /// 【使用规范】
    /// - Model不继承MonoBehaviour，由Controller通过 new() 创建
    /// - 若需要属性变更通知功能，请使用 ObservableModel 子类
    /// - Initialize() 由 Controller 的 CreateAndInitializeModel() 直接调用
    /// - Cleanup() 由 Controller 在清理时调用
    /// - Model不应持有View或Controller的引用
    /// </summary>
    public abstract class BaseModel :IInitializable
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
        public BaseModel()
        {
            IsInitialized = false;
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 初始化模型（双重检查，防止重复初始化）
        /// 
        /// 【使用规范】
        /// - 由 Controller 的 CreateAndInitializeModel() 直接调用
        /// - 子类重写时应调用 base.Initialize() 以确保 IsInitialized 标志被正确设置
        /// - 子类重写时在此方法中加载数据（如从 PlayerPrefs、配置文件等）
        /// - 注意：base.Initialize() 使用 try-catch 包装，异常不会中断流程
        /// </summary>
        public virtual void Initialize()
        {
            if (!IsInitialized)
            {
                try
                {
                    IsInitialized = true;
                }
                catch (Exception ex)
                {
                    // 记录初始化失败的异常信息
                    Log.Error(LogModules.UI, $"初始化模型 {GetType().Name}: 失败，异常信息： {ex.Message}");
                    // 保持IsInitialized为false，允许后续重试初始化
                }
            }
        }
        
        /// <summary>
        /// 清理模型资源
        /// 
        /// 【使用规范】
        /// - 由 Controller 在清理时调用
        /// - 子类重写时应调用 base.Cleanup() 以确保 IsInitialized 标志被正确重置
        /// - 子类重写时在此方法中释放引用、清空集合等
        /// </summary>
        public virtual void Cleanup()
        {
            if (IsInitialized)
            {
                IsInitialized = false;
            }
        }
        
        #endregion
        
    }
    
    /// <summary>
    /// 带数据变化通知的模型基类
    /// 支持属性变更通知机制，使Controller可以响应式更新View
    /// 
    /// 【使用规范】
    /// - 子类的属性setter应使用 SetProperty(ref field, value, nameof(Property)) 模式
    /// - Controller应在初始化时订阅 OnPropertyChanged 事件，在清理时取消订阅
    /// - View不应直接订阅 OnPropertyChanged 事件（应由Controller中转）
    /// - 若需要自定义事件粒度（如区分"保存列表更新"和"选中项变更"），可在子类中额外声明事件
    /// </summary>
    public abstract class ObservableModel : BaseModel
    {
        #region 事件
        
        /// <summary>
        /// 数据变更事件
        /// 
        /// 【使用规范】
        /// - Controller 订阅此事件以在 Model 属性变更时更新 View
        /// - 回调参数为变更的属性名称（string）
        /// - View 不应直接订阅此事件！
        /// </summary>
        public event Action<string> OnPropertyChanged;

        #endregion

        #region 保护方法

        /// <summary>
        /// 通知属性变更
        /// 
        /// 【使用规范】
        /// - 由 SetProperty() 自动调用，子类一般不需要直接调用
        /// - 如需手动触发通知（如批量更新的场景），可调用此方法
        /// - 传入的 propertyName 应与属性名一致（建议使用 nameof()）
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged?.Invoke(propertyName);
        }

        /// <summary>
        /// 设置属性值并自动通知变更
        /// 属性 setter 的标准实现辅助方法
        /// 
        /// 【使用规范】
        /// - 子类的每个需要通知变更的属性应使用此方法实现 setter：
        ///   set { SetProperty(ref m_field, value, nameof(PropertyName)); }
        /// - 方法内部会自动比较新旧值，仅在值真正变更时才触发通知
        /// - 返回值表示值是否发生了实际变更
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="field">字段引用（ref传递）</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称（使用 nameof(Property)）</param>
        /// <returns>是否成功设置新值（值发生实际变更时为true）</returns>
        protected bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }
            
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }
        
        #endregion
    }
}
