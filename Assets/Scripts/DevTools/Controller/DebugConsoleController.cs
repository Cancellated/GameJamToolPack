using Logger;
using MyGame.UI;

namespace MyGame.DevTool
{
    /// <summary>
    /// 调试控制台控制器：连接视图与命令注册表，处理输入、执行命令并按四态结果反馈。
    /// 继承泛型 BaseController 以遵循统一生命周期契约（Initialize/Cleanup）。
    /// </summary>
    public class DebugConsoleController : BaseController<DebugConsole, DebugCommandRegistry>
    {
        private const string LOG_MODULE = LogModules.DEBUGCONSOLE;

        private static DebugConsoleController s_instance;

        /// <summary>
        /// 全局实例（业务系统可经 Instance.Registry 动态注册自定义调试命令）
        /// </summary>
        public static DebugConsoleController Instance
        {
            get { return s_instance; }
        }

        /// <summary>
        /// 命令注册表（模型）；外部系统通过它注册/注销命令
        /// </summary>
        public DebugCommandRegistry Registry
        {
            get { return m_model; }
        }

        #region 生命周期

        /// <summary>
        /// 初始化控制器（由 DebugConsole.TryBindController 调用）：创建并初始化命令注册表
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                // 创建注册表并初始化（内部完成静态命令扫描）
                CreateAndInitializeModel();

                base.Initialize();
            }
        }

        /// <summary>
        /// 初始化回调：登记静态实例，供业务系统访问注册表
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            s_instance = this;
        }

        /// <summary>
        /// 清理回调：清除静态实例，防止悬空引用
        /// </summary>
        protected override void OnCleanup()
        {
            base.OnCleanup();
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        /// <summary>
        /// 对象销毁时清理资源（与 MainMenuController 等标准控制器对齐）
        /// </summary>
        private void OnDestroy()
        {
            Cleanup();

            // 解绑视图
            if (m_view != null)
            {
                m_view.UnbindController();
            }
        }

        /// <summary>
        /// 清理控制器资源
        /// </summary>
        public override void Cleanup()
        {
            if (IsInitialized)
            {
                // 清理注册表（清空命令）
                if (m_model != null)
                {
                    m_model.Cleanup();
                    m_model = null;
                }

                base.Cleanup();
            }
        }

        #endregion

        #region 命令处理

        /// <summary>
        /// 处理用户输入的命令，按四态结果输出不同反馈：
        /// Ok → 命令已自行输出（附带消息时补打印）；
        /// NotFound → 未知命令提示；InvalidArgs → 参数错误 + 用法；Error → 执行失败。
        /// </summary>
        /// <param name="commandText">整行命令文本</param>
        public void HandleCommand(string commandText)
        {
            if (m_model == null)
            {
                Log.Error(LOG_MODULE, "命令注册表未初始化");
                return;
            }

            var context = new DebugCommandContext(this, m_model);
            CommandResult result = m_model.Execute(commandText, context);

            switch (result.Status)
            {
                case CommandStatus.Ok:
                    // 命令一般已通过上下文输出内容；仅当 Ok 附带消息时额外打印
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        m_view?.Print(result.Message);
                    }
                    break;

                case CommandStatus.NotFound:
                    m_view?.Print($"未知命令: {result.Message}，输入 help 查看可用命令。");
                    Log.Warning(LOG_MODULE, "未知命令: " + result.Message);
                    break;

                case CommandStatus.InvalidArgs:
                    // Usage 为空（显式注册命令未填用法）时省略"用法:"部分，避免空洞文案
                    if (string.IsNullOrEmpty(result.Message))
                    {
                        m_view?.Print(string.IsNullOrEmpty(result.Usage)
                            ? "参数错误。"
                            : "参数错误，用法: " + result.Usage);
                    }
                    else
                    {
                        m_view?.Print(string.IsNullOrEmpty(result.Usage)
                            ? "参数错误: " + result.Message
                            : $"参数错误: {result.Message}，用法: {result.Usage}");
                    }
                    break;

                case CommandStatus.Error:
                    m_view?.Print("执行命令失败: " + result.Message);
                    break;
            }
        }

        /// <summary>
        /// 输出信息到控制台
        /// </summary>
        /// <param name="message">消息内容</param>
        public void PrintToConsole(string message)
        {
            if (m_view != null)
            {
                m_view.Print(message);
            }
        }

        /// <summary>
        /// 清空控制台输出（clear 命令调用）
        /// </summary>
        public void ClearConsoleOutput()
        {
            if (m_view != null)
            {
                m_view.ClearOutput();
            }
        }

        #endregion
    }
}
