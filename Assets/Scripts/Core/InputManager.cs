using UnityEngine;

namespace MyGame.Managers
{
    /// <summary>
    /// 输入管理器，集中管理InputSystem实例
    /// 避免重复创建InputSystem实例，确保输入系统状态一致性
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        #region 字段
        private GameControl _inputActions;
        #endregion

        #region 属性
        /// <summary>
        /// 全局唯一的InputActions实例。
        /// 属性在首次访问时自举初始化，避免依赖 Awake 执行顺序：
        /// 即使其它对象在其 Awake 之前访问，也能立即得到可用的输入实例。
        /// </summary>
        public GameControl InputActions
        {
            get
            {
                EnsureInitialized();
                return _inputActions;
            }
        }
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            EnsureInitialized();
        }

        /// <summary>
        /// 创建并启用输入实例（幂等）。
        /// </summary>
        private void EnsureInitialized()
        {
            if (_inputActions != null)
            {
                return;
            }

            // 创建InputActions实例
            _inputActions = new GameControl();

            // 默认启用游戏玩法输入
            _inputActions.GamePlay.Enable();
        }

        protected override void OnDestroy()
        {
            // 清理资源
            if (_inputActions != null)
            {
                _inputActions.Disable();
                _inputActions.Dispose();
            }

            // 通知基类置位销毁标志，禁止后续 Instance 懒创建
            base.OnDestroy();
        }
        #endregion

        #region 输入模式切换
        /// <summary>
        /// 切换到游戏玩法输入模式
        /// </summary>
        public void SwitchToGamePlayMode()
        {
            _inputActions.UI.Disable();
            _inputActions.GamePlay.Enable();
        }

        /// <summary>
        /// 切换到UI输入模式
        /// 特殊处理：保留控制台按键的功能，即使在UI模式下也能响应
        /// </summary>
        public void SwitchToUIMode()
        {
            _inputActions.GamePlay.Disable();
            // 单独启用控制台按键，确保在任何模式下都能唤出控制台
            _inputActions.GamePlay.Console.Enable();
            _inputActions.UI.Enable();
        }

        /// <summary>
        /// 同时启用游戏玩法和UI输入模式
        /// </summary>
        public void EnableBothModes()
        {
            _inputActions.GamePlay.Enable();
            _inputActions.UI.Enable();
        }

        /// <summary>
        /// 禁用所有输入
        /// </summary>
        public void DisableAllInputs()
        {
            _inputActions.GamePlay.Disable();
            _inputActions.UI.Disable();
        }
        #endregion
    }
}