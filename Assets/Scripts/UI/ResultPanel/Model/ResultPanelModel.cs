using Logger;

namespace MyGame.UI.ResultPanel.Model
{
    /// <summary>
    /// 结算面板模型
    /// 当前无独立数据，保持标准生命周期（Initialize/Cleanup）供后续扩展（如显示本局得分）
    /// </summary>
    public class ResultPanelModel : BaseModel
    {
        private const string LOG_MODULE = LogModules.RESULT;

        /// <summary>
        /// 初始化模型
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                Log.Info(LOG_MODULE, "初始化结算面板模型");
                base.Initialize();
            }
        }

        /// <summary>
        /// 清理模型资源
        /// </summary>
        public override void Cleanup()
        {
            if (IsInitialized)
            {
                Log.Info(LOG_MODULE, "清理结算面板模型");
                base.Cleanup();
            }
        }
    }
}
