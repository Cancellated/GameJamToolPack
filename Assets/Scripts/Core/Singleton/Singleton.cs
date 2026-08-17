using UnityEngine;
using Logger;

namespace MyGame
{
    /// <summary>
    /// 通用MonoBehaviour单例基类。
    /// 继承此类可快速实现全局唯一的管理器或工具类。
    ///
    /// 懒创建策略：
    /// - 优先使用场景中已激活的实例；
    /// - 找不到时不再静默创建一个孤立的 GameObject，而是优先挂载到场景中的 Managers 根对象；
    /// - 没有 Managers 根对象时才创建新的 Managers 对象；
    /// - 创建后调用 OnLazyInstanceCreated() 并输出警告日志，便于发现"场景漏挂管理器"问题。
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

        /// <summary>
        /// 场景中承载全局管理器的根对象名称。
        /// </summary>
        private const string MANAGERS_ROOT_NAME = "Managers";

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
                                _instance = CreateLazyInstance();
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
        /// 注意：只销毁重复的组件本身，不销毁其所在 GameObject——
        /// 所有管理器通常共用一个 Managers 根对象，销毁 GameObject 会连带移除其它管理器。
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
                Log.Warning(typeof(T).Name,
                    $"检测到重复的 {typeof(T).Name} 实例，位于 {gameObject.name}，仅销毁重复组件");
                Destroy(this);
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

        #region 懒创建

        /// <summary>
        /// 子类可重写此回调，在 Instance 懒创建完成后收到通知（例如记录错误或做额外初始化）。
        /// </summary>
        protected virtual void OnLazyInstanceCreated() { }

        private static T CreateLazyInstance()
        {
            GameObject managersRoot = GameObject.Find(MANAGERS_ROOT_NAME);
            GameObject singletonObject;
            bool usedExistingManagersRoot = managersRoot != null;

            if (usedExistingManagersRoot)
            {
                singletonObject = managersRoot;
            }
            else
            {
                singletonObject = new GameObject(MANAGERS_ROOT_NAME);
            }

            T instance = singletonObject.AddComponent<T>();
            DontDestroyOnLoad(singletonObject);

            Log.Warning(typeof(T).Name,
                usedExistingManagersRoot
                    ? $"场景中未找到 {typeof(T).Name} 实例，已自动补挂到现有 {MANAGERS_ROOT_NAME} 根对象，请检查场景配置"
                    : $"场景中未找到 {typeof(T).Name} 实例，也未找到 {MANAGERS_ROOT_NAME} 根对象，已自动创建 {MANAGERS_ROOT_NAME} 并补挂，请检查场景配置");

            (instance as Singleton<T>)?.OnLazyInstanceCreated();
            return instance;
        }

        #endregion
    }
}
