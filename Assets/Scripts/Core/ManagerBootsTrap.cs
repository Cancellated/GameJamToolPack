using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger;
using MyGame.Managers;

namespace MyGame.Core
{
    /// <summary>
    /// 管理器引导程序，负责初始化所有核心管理器
    /// 确保从任意场景启动时，所有必要的管理器都能正确初始化
    /// </summary>
    public class ManagerBootstrap : Singleton<ManagerBootstrap>
    {
        // 存储需要初始化的管理器类型
        private List<System.Type> m_managerTypes = new List<System.Type>();
        private const string LOG_MODULE = LogModules.MANAGERBOOTSTRAP;

        // 标记是否已经初始化
        private bool m_isInitialized = false;

        /// <summary>
        /// 初始化所有管理器
        /// </summary>
        public void Initialize()
        {
            if (m_isInitialized)
            {
                return;
            }

            Log.Info(LOG_MODULE, "开始初始化管理器...");

            // 注册需要初始化的管理器类型
            RegisterManagers();

            // 初始化所有注册的管理器
            InitializeManagers();

            m_isInitialized = true;
            Log.Info(LOG_MODULE, "管理器初始化完成");
        }

        /// <summary>
        /// 注册需要初始化的管理器
        /// 在这里定义管理器的初始化顺序
        /// </summary>
        private void RegisterManagers()
        {
            // 按依赖顺序添加管理器
            // 核心管理器先初始化
            m_managerTypes.Add(typeof(GameManager));
            m_managerTypes.Add(typeof(UIManager));
            // m_managerTypes.Add(typeof(AudioManager));    //这个还没实装
            m_managerTypes.Add(typeof(FontManager));
            m_managerTypes.Add(typeof(SceneSwitcher));
        }

        /// <summary>
        /// 初始化所有注册的管理器
        /// </summary>
        private void InitializeManagers()
        {
            foreach (var type in m_managerTypes)
            {
                InitializeManager(type);
            }
        }

        /// <summary>
        /// 初始化指定类型的管理器
        /// </summary>
        /// <param name="type">管理器类型</param>
        private void InitializeManager(System.Type type)
        {
            // 检查类型是否是MonoBehaviour
            if (!typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                Log.Error(LOG_MODULE, $"类型 {type.Name} 不是MonoBehaviour,无法初始化");
                return;
            }

            // 尝试查找已存在的实例
            var instance = FindObjectOfType(type) as MonoBehaviour;

            // 如果不存在，则创建新实例
            if (instance == null)
            {
                GameObject managerObj = new GameObject(type.Name);
                instance = managerObj.AddComponent(type) as MonoBehaviour;
                DontDestroyOnLoad(managerObj);
                Log.Info(LOG_MODULE, $"创建管理器: {type.Name}");
            }
            else
            {
                Log.Info(LOG_MODULE, $"找到已存在的管理器: {type.Name}");
            }

            // 如果管理器实现了IInitializable接口，则调用Initialize方法
            if (instance is IInitializable initializable)
            {
                initializable.Initialize();
            }
        }

        /// <summary>
        /// 确保指定类型的管理器已初始化
        /// </summary>
        /// <typeparam name="T">管理器类型</typeparam>
        /// <returns>管理器实例</returns>
        public T EnsureManager<T>() where T : MonoBehaviour
        {
            T instance = FindObjectOfType<T>();

            if (instance == null)
            {
                GameObject managerObj = new GameObject(typeof(T).Name);
                instance = managerObj.AddComponent<T>();
                DontDestroyOnLoad(managerObj);
                Log.Warning(LOG_MODULE, $"自动创建缺失的管理器: {typeof(T).Name}");

                // 如果管理器实现了IInitializable接口，则调用Initialize方法
                if (instance is IInitializable initializable)
                {
                    initializable.Initialize();
                }
            }

            return instance;
        }

        /// <summary>
        /// 单例初始化
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 游戏启动时初始化
        /// </summary>
        private void Start()
        {
            Initialize();
        }
    }

    /// <summary>
    /// 初始化接口，供管理器实现
    /// </summary>
    public interface IInitializable
    {
        void Initialize();
    }
}
