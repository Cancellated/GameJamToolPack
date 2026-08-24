using System;
using Logger;

namespace MyGame.Dialogue
{
    /// <summary>
    /// 对话模块的事件总线（模块内事件，不占用核心 GameEvents）。
    /// 由 DialogueManager 触发，UI 面板与游戏玩法系统订阅。
    /// </summary>
    public static class DialogueEvents
    {
        private const string LOG_MODULE = LogModules.DIALOGUE;

        /// <summary>
        /// 对话开始事件（参数为本次对话的数据资产）。
        /// </summary>
        public static event Action<DialogueData> OnDialogueStarted;

        public static void TriggerDialogueStarted(DialogueData data)
        {
            if (data == null)
            {
                Log.Warning(LOG_MODULE, "触发对话开始事件失败：对话数据为空");
                return;
            }

            Log.Info(LOG_MODULE, $"触发对话开始事件: {data.name}");
            OnDialogueStarted?.Invoke(data);
        }

        /// <summary>
        /// 节点变更事件（进入新节点或通过存档恢复节点时触发）。
        /// </summary>
        public static event Action<DialogueNode> OnNodeChanged;

        public static void TriggerNodeChanged(DialogueNode node)
        {
            if (node == null)
            {
                Log.Warning(LOG_MODULE, "触发节点变更事件失败：节点为空");
                return;
            }

            Log.Info(LOG_MODULE, $"触发对话节点变更事件: {node.nodeId}");
            OnNodeChanged?.Invoke(node);
        }

        /// <summary>
        /// 对话结束事件。
        /// </summary>
        public static event Action OnDialogueEnded;

        public static void TriggerDialogueEnded()
        {
            Log.Info(LOG_MODULE, "触发对话结束事件");
            OnDialogueEnded?.Invoke();
        }
    }
}
