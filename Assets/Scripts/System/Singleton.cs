using UnityEngine;

namespace MyGame.System
{
    /// <summary>
    /// ͨ��MonoBehaviour�������ࡣ
    /// �̳д���ɿ���ʵ��ȫ��Ψһ�Ĺ������򹤾��ࡣ
    /// </summary>
    /// <typeparam name="T">�������ͣ���̳���MonoBehaviour</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region �ֶ�

        /// <summary>
        /// ����ʵ����
        /// </summary>
        private static T _instance;

        /// <summary>
        /// �߳�����ȷ�����̻߳����µİ�ȫ����ȻUnity���߳�Ϊ���������������ף���
        /// </summary>
        private static readonly object _lock = new();

        #endregion

        #region ����

        /// <summary>
        /// ��ȡ����ʵ���������������Զ����һ򴴽���
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // ���Ȳ��ҳ������Ѵ��ڵ�ʵ��
                            _instance = Object.FindFirstObjectByType<T>();
                            if (_instance == null)
                            {
                                // �����������Զ�����
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

        #region ��������

        /// <summary>
        /// ��֤����Ψһ�ԣ��ظ�ʵ���Զ����١�
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

        #endregion
    }
}
