namespace MyGame.Dialogue
{
    /// <summary>
    /// 对话条件判定契约：框架只负责"查询"，游戏负责"flag 从哪来"。
    /// 典型实现：把"拥有钥匙""已见过某人"等玩法变量映射为条件键的布尔值。
    /// 未提供实现时，所有带条件的选项一律视为不可选，不带条件的选项正常显示。
    /// </summary>
    public interface IDialogueConditionProvider
    {
        /// <summary>
        /// 判定条件键当前是否满足。
        /// </summary>
        /// <param name="conditionKey">DialogueChoice.conditionKey 的原始值</param>
        bool IsConditionMet(string conditionKey);
    }
}
