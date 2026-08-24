namespace MyGame.Data
{
    /// <summary>
    /// 玩法存档提供者契约（框架与具体游戏之间的唯一扩展点）。
    ///
    /// 设计原则：框架核心不定义任何玩法进度字段（不预设关卡/任务/数值等结构），
    /// 只负责"保存设置与元数据 + 透传一段玩法自有的 JSON 字符串"。
    /// 具体游戏实现本接口，自行决定数据的结构、序列化与版本迁移。
    ///
    /// 典型接入方式：
    ///   1. 玩法场景的引导对象在 Start 时调用
    ///      SaveManager.Instance.RegisterGameplaySaveProvider(this)；
    ///   2. 同一对象在 OnDestroy 时调用 UnregisterGameplaySaveProvider(this)，
    ///      避免场景卸载后 SaveManager 持有失效引用；
    ///   3. 读档时订阅 GameEvents.OnGameplayDataLoaded(string)，
    ///      用自己定义的 Schema 反序列化并恢复游戏世界。
    /// </summary>
    public interface IGameplaySaveProvider
    {
        /// <summary>
        /// 把当前玩法状态序列化为 JSON 字符串。
        /// 返回 null 或空串表示本轮没有玩法数据（核心仍会保存设置与元数据）。
        /// 建议返回 JsonUtility 序列化结果；复杂结构请自行升级序列化方案。
        /// </summary>
        string CaptureGameplayDataJson();

        /// <summary>
        /// 返回显示在存档槽位上的纯文本进度摘要（例如"第3章 码头"）。
        /// 摘要语义完全由玩法定义，核心只负责透传与显示，不解析内容。
        /// </summary>
        string GetProgressSummary();
    }
}
