using MyGame.System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyGame.Managers
{
    /// <summary>
    /// 游戏流程状态枚举。
    /// </summary>
    public enum GameState
    {
        Init,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// 游戏管理器，负责控制游戏主流程和状态切换。
    /// 继承自通用单例基类，保证全局唯一。
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
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
        }

        /// <summary>
        /// 销毁时注销事件监听，防止内存泄漏。
        /// </summary>
        private void OnDestroy()
        {
            // 注销事件监听，防止内存泄漏
            GameEvents.OnGameStart -= StartGame;
            GameEvents.OnGamePause -= PauseGame;
            GameEvents.OnGameResume -= ResumeGame;
            GameEvents.OnGameOver -= GameOver;
        }

        /// <summary>
        /// 启动时自动进入游戏。
        /// </summary>
        private void Start()
        {
            StartGame();
        }

        /// <summary>
        /// 监听输入，实现暂停与恢复及快速测试。
        /// </summary>
        private void Update()
        {
            // 按下ESC切换暂停/继续
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing)
                    PauseGame();
                else if (State == GameState.Paused)
                    ResumeGame();
            }

            // 快速测试：F1重开，F2胜利，F3失败
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log("[测试] 快速重开游戏");
                GameEvents.TriggerGameStart();
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Debug.Log("[测试] 快速胜利结束");
                GameEvents.TriggerGameOver(true);
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                Debug.Log("[测试] 快速失败结束");
                GameEvents.TriggerGameOver(false);
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
                Debug.LogWarning($"非法状态切换：{State}->{newState}");
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
                [GameState.Init] = new[] { GameState.Playing },
                [GameState.Playing] = new[] { GameState.Paused, GameState.GameOver },
                [GameState.Paused] = new[] { GameState.Playing, GameState.GameOver },
                [GameState.GameOver] = new[] { GameState.Init }
            };

            // 检查当前状态是否有合法的转换到下一个状态
            return validTransitions.ContainsKey(current) &&
                   validTransitions[current].Contains(next);
        }

        #endregion

        #region 游戏流程控制-注意：其他模块应通过GameEvents触发这些方法，而不是直接调用

        /// <summary>
        /// 开始游戏，进入Playing状态。
        /// </summary>
        public void StartGame()
        {
            if (!TryChangeState(GameState.Playing))
                return;
            // TODO: 初始化关卡、玩家等
            Time.timeScale = 1f;
            Debug.Log("游戏开始");
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
            Debug.Log("游戏暂停");
        }

        /// <summary>
        /// 恢复游戏，设置Time.timeScale为1。
        /// </summary>
        public void ResumeGame()
        {
            if (!TryChangeState(GameState.Playing))
                return;
            Time.timeScale = 1f;
            Debug.Log("游戏继续");
        }

        /// <summary>
        /// 游戏结束，进入GameOver状态。
        /// </summary>
        /// <param name="isWin">true为胜利，false为失败</param>
        public void GameOver(bool isWin)
        {
            if (!TryChangeState(GameState.GameOver))    
                return;
            Time.timeScale = 0f;
            Debug.Log($"游戏结束，胜利：{isWin}");
            // TODO: 显示结算界面等
        }

        #endregion

        #region 快速测试方法

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下通过Inspector按钮快速切换状态（可选）。
        /// </summary>
        [ContextMenu("测试/重开游戏")]
        public void TestRestartGame() => GameEvents.TriggerGameStart();

        [ContextMenu("测试/胜利结束")]
        public void TestWinGame() => GameEvents.TriggerGameOver(true);

        [ContextMenu("测试/失败结束")]
        public void TestLoseGame() => GameEvents.TriggerGameOver(false);
#endif

        #endregion
    }
}