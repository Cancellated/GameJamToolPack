namespace MyGame.UI
{
    /// <summary>
    /// UI 面板排序层级常量（单一事实来源）。
    /// 同时被 PanelLoader.SetPanelSortingOrder 与各面板的 canvasSortingOrder 默认值引用，
    /// 避免层级数值多处硬编码不一致。
    /// </summary>
    public static class UISortingOrder
    {
        /// <summary>加载界面（最高层级）</summary>
        public const int Loading = 1000;

        /// <summary>调试控制台（次高，低于加载界面、高于普通面板）</summary>
        public const int Console = 900;

        /// <summary>普通UI面板（默认层级）</summary>
        public const int Default = 10;
    }
}
