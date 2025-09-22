using System;
using System.Collections.Generic;

namespace MyGame.UI
{
    /// <summary>
    /// MVC架构中的模型基类
    /// 负责管理数据和业务逻辑
    /// </summary>
    public abstract class BaseModel
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
        /// 初始化模型
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
        /// 清理模型资源
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
    /// 带数据变化通知的模型基类
    /// 支持属性变更通知机制
    /// </summary>
    public abstract class ObservableModel : BaseModel
    {
        #region 事件
        
        /// <summary>
        /// 数据变更事件
        /// </summary>
        public event Action<string> OnPropertyChanged;

        #endregion

        #region 保护方法

        /// <summary>
        /// 通知属性变更
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged?.Invoke(propertyName);
        }
        /// 设置属性值并通知变更
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="field">字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>是否成功设置新值</returns>
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