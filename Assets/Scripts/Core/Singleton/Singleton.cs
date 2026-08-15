using UnityEngine;

namespace MyGame
{
    /// <summary>
    /// 通用MonoBehaviour单例基类。
    /// 继承此类可快速实现全局唯一的管理器或工具类。
    /// </summary>
    /// <typeparam name="T">单例类型，需继承自MonoBehaviour</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region 字段

        /// <summary>
        /// 单例实例。
        /// </summary>
        private static T _instance;

        /// <summary>
        /// 线程锁，确保多线程环境下的安全（虽然Unity主线程为主，但加锁更稳妥）。
        /// </summary>
        private static readonly object _lock = new();

        /// <summary>
        /// 主实例是否已销毁。
        /// 销毁后禁止 Instance 再懒创建，避免在 OnDestroy（场景关闭/退出）期间
        /// 生成新 GameObject 引发 "Some objects were not cleaned up" 警告。
        /// </summary>
        private static bool _destroyed;

        #endregion

        #region 属性

        /// <summary>
        /// 获取单例实例，若不存在则自动查找或创建。
        /// 主实例已销毁或应用正在退出时返回 null（禁止在销毁阶段重新创建）。
        /// </summary>
        public static T Instance
        {
            get
            {
                // 主实例已销毁或非运行状态下，禁止懒创建（防止在 OnDestroy 中产生新对象）
                if (_destroyed || !Application.isPlaying)
                {
                    return null;
                }

                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // 优先查找场景中已存在的实例
                            _instance = Object.FindFirstObjectByType<T>();
                            if (_instance == null)
                            {
                                // 若不存在则自动创建
                                var singletonObject = new GameObject(typeof(T).Name);
                                _instance = singletonObject.AddComponent<T>();
                                DontDestroyOnLoad(singletonObject);
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 保证单例唯一性，重复实例自动销毁。
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }   
        }

        /// <summary>
        /// 主实例销毁时置位销毁标志并清空静态引用，禁止后续 Instance 访问再懒创建。
        /// 【注意】子类若自定义 OnDestroy，必须在其中调用 base.OnDestroy()，
        /// 否则 Unity 消息只调用最派生的 OnDestroy，本方法不会执行。
        /// 重复副本实例被销毁时不会置位（_instance 仍指向主实例），不影响后续正常使用。
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _destroyed = true;
                _instance = null;
            }
        }

        #endregion
    }
}
