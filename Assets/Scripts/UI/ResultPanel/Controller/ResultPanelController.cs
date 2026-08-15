using Logger;
using MyGame.Events;
using MyGame.Managers;
using MyGame.UI.ResultPanel.Model;
using MyGame.UI.ResultPanel.View;

namespace MyGame.UI.ResultPanel.Controller
{
    /// <summary>
    /// 结算面板控制器
    /// 处理结算面板的按钮操作：重新开始 / 返回主菜单
    /// 面板的显示与隐藏由 UIManager 统一调度，此处只负责按钮的业务逻辑
    /// </summary>
    public class ResultPanelController : BaseController<ResultPanelView, ResultPanelModel>
    {
        private const string LOG_MODULE = LogModules.RESULT;

        /// <summary>
        /// 初始化控制器
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                Log.Info(LOG_MODULE, "初始化结算面板控制器");

                // 创建并初始化模型
                CreateAndInitializeModel();

                // 调用基类初始化
                base.Initialize();
            }
        }

        /// <summary>
        /// 重新开始：触发重开事件。
        /// GameManager 复位流程状态（切回 Playing/分数/时间流速），
        /// 各游戏系统订阅后复位背景滚动、对象池、玩家与生成器状态。
        /// </summary>
        public void OnRestartClicked()
        {
            Log.Info(LOG_MODULE, "点击重新开始");
            GameEvents.TriggerGameRestart();
        }

        /// <summary>
        /// 返回主菜单：加载主菜单场景
        /// </summary>
        public void OnMainMenuClicked()
        {
            Log.Info(LOG_MODULE, "点击返回主菜单");
            SceneSwitcher.RequestLoadScene("MainMenu");
        }

        /// <summary>
        /// 清理控制器资源
        /// </summary>
        public override void Cleanup()
        {
            if (IsInitialized)
            {
                Log.Info(LOG_MODULE, "清理结算面板控制器");

                // 清理模型资源
                if (m_model != null)
                {
                    m_model.Cleanup();
                    m_model = null;
                }

                // 调用基类清理
                base.Cleanup();
            }
        }
    }
}
