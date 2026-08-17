using MyGame.Events;
using MyGame.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger;

namespace MyGame.Managers
{
    /// <summary>
    /// 游戏管理器，负责控制游戏主流程和状态切换。
    /// 继承自通用单例基类，保证全局唯一。
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        private const string LOG_MODULE = LogModules.GAMEMANAGER;

        #region 字段与属性

        /// <summary>
        /// 当前游戏状态。
        /// </summary>
        public GameState State { get; private set; } = GameState.Init;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化游戏管理器，设置初始状态。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            State = GameState.Init;

            // 注册事件监听
            GameEvents.OnGameStart += StartGame;
            GameEvents.OnGamePause += PauseGame;
            GameEvents.OnGameResume += ResumeGame;
            GameEvents.OnGameOver += GameOver;
            GameEvents.OnGameRestart += RestartGame;
        }

        /// <summary>
        /// 销毁时注销事件监听，防止内存泄漏。
        /// </summary>
        protected override void OnDestroy()
        {
            // 注销事件监听，防止内存泄漏
            GameEvents.OnGameStart -= StartGame;
            GameEvents.OnGamePause -= PauseGame;
            GameEvents.OnGameResume -= ResumeGame;
            GameEvents.OnGameOver -= GameOver;
            GameEvents.OnGameRestart -= RestartGame;

            // 注销暂停按键事件（与 Start 中的订阅成对出现）
            if (InputManager.Instance != null && InputManager.Instance.InputActions != null)
            {
                InputManager.Instance.InputActions.GamePlay.Pause.performed -= OnPausePerformed;
            }

            // 通知基类置位销毁标志，禁止后续 Instance 懒创建
            base.OnDestroy();
        }

        /// <summary>
        /// 启动时自动进入游戏。
        /// </summary>
        private void Start()
        {
            StartMenu();

            // 订阅暂停按键事件（事件驱动，替代 Update 中每帧轮询 Pause.triggered）。
            // Start 阶段所有 Awake 均已执行完毕，InputManager 的 InputActions 已就绪
            if (InputManager.Instance != null && InputManager.Instance.InputActions != null)
            {
                InputManager.Instance.InputActions.GamePlay.Pause.performed += OnPausePerformed;
            }
            else
            {
                Log.Warning(LOG_MODULE, "InputManager 未就绪，暂停按键事件未订阅");
            }
        }

        /// <summary>
        /// 暂停按键事件处理：在 Playing 与 Paused 状态间切换。
        /// 注意：仅处理游戏状态，暂停菜单的显隐由 UIController 通过同一按键的
        /// performed 回调经 GameEvents.TriggerMenuShow 处理，两者职责分离。
        /// </summary>
        private void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            if (State == GameState.Playing)
            {
                GameEvents.TriggerGamePause();
            }
            else if (State == GameState.Paused)
            {
                GameEvents.TriggerGameResume();
            }
        }
        #endregion

        #region 状态切换校验

        /// <summary>
        /// 校验并切换状态，返回是否切换成功，并全局广播。
        /// 合法切换状态返回true，并触发状态变更事件；
        /// 非法切换状态返回false，并输出警告日志。
        /// </summary>
        private bool TryChangeState(GameState newState)
        {
            if (!IsValidTransition(State, newState))
            {
                Log.Warning(LOG_MODULE, $"非法状态切换：{State}->{newState}", this);
                return false;
            }

            var oldState = State;
            State = newState;
            GameEvents.TriggerGameStateChanged(oldState, newState);
            return true;
        }

        /// <summary>
        /// 状态切换验证，用来确保状态转移的合法性。
        /// </summary>
        private bool IsValidTransition(GameState current, GameState next)
        {
            // 定义合法状态转换关系：左边的状态可以转换到右边的状态
            var validTransitions = new Dictionary<GameState, GameState[]>
            {
                [GameState.Init] = new[] { GameState.Menu },
                [GameState.Menu] = new[] { GameState.Playing, GameState.Menu},
                [GameState.Playing] = new[] { GameState.Paused, GameState.GameOver,GameState.Playing },
                [GameState.Paused] = new[] { GameState.Playing, GameState.GameOver, GameState.Menu, GameState.Paused },
                [GameState.GameOver] = new[] { GameState.Menu, GameState.Playing, GameState.GameOver }
            };

            // 检查当前状态是否有合法的转换到下一个状态
            return validTransitions.ContainsKey(current) &&
                   validTransitions[current].Contains(next);
        }

        #endregion

        #region 游戏流程控制-注意：其他模块应通过GameEvents触发这些方法，而不是直接调用

        public void StartMenu()
        {
            if (!TryChangeState(GameState.Menu))
                return;
            // TODO: 初始化菜单
            Time.timeScale = 1f;
            Log.Info(LOG_MODULE, "进入菜单", this);
        }

        /// <summary>
        /// 开始游戏，进入Playing状态。
        /// </summary>
        public void StartGame()
        {
            if (!TryChangeState(GameState.Playing))
                return;
            // TODO: 初始化关卡、玩家等
            Time.timeScale = 1f;
            Log.Info(LOG_MODULE, "游戏开始", this);
        }

        /// <summary>
        /// 暂停游戏，设置Time.timeScale为0。
        /// </summary>
        public void PauseGame()
        {
            if (!TryChangeState(GameState.Paused))      //尝试切换状态并验证合法性
                                                        //若不合法输出日志返回false，合法则切换状态并回true
                return;
            Time.timeScale = 0f;
            Log.Info(LOG_MODULE, "游戏暂停", this);
        }

        /// <summary>
        /// 恢复游戏，设置Time.timeScale为1。
        /// </summary>
        public void ResumeGame()
        {
            if (!TryChangeState(GameState.Playing))
                return;
            Time.timeScale = 1f;
            Log.Info(LOG_MODULE, "游戏继续", this);
        }

        /// <summary>
        /// 游戏结束，进入GameOver状态。
        /// 同时显示结算面板；SaveManager 通过 OnGameOver 事件执行自动存档。
        /// </summary>
        /// <param name="isWin">true为胜利，false为失败</param>
        public void GameOver(bool isWin)
        {
            if (!TryChangeState(GameState.GameOver))    
                return;
            Time.timeScale = 0f;
            Log.Info(LOG_MODULE, $"游戏结束，胜利：{isWin}", this);

            // 进入结算流程：UIManager 根据 UIConfig 互斥配置隐藏 HUD/PauseMenu
            GameEvents.TriggerMenuShow(UIType.ResultPanel, true);
        }

        /// <summary>
        /// 重新开始：从结算状态回到 Playing，并关闭结算面板。
        /// 各玩法系统应订阅 OnGameRestart 复位自身状态；
        /// SaveManager 也会通过该事件重置内存中的本局进度。
        /// </summary>
        public void RestartGame()
        {
            if (!TryChangeState(GameState.Playing))
                return;
            Time.timeScale = 1f;
            Log.Info(LOG_MODULE, "游戏重新开始", this);

            // 关闭结算面板并恢复 HUD（由 UIManager.SetUIState(false) 统一处理）
            GameEvents.TriggerMenuShow(UIType.ResultPanel, false);
        }

        #endregion
    }

    /// <summary>
    /// 游戏流程状态枚举。
    /// </summary>
    public enum GameState
    {
        Init,
        Menu,
        Playing,
        Paused,
        GameOver
    }
}