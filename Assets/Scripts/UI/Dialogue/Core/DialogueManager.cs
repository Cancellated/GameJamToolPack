using System;
using System.Collections.Generic;
using UnityEngine;
using Logger;

namespace MyGame.Dialogue
{
    /// <summary>
    /// 对话运行时管理器（纯 C# 服务，不依赖场景对象）。
    ///
    /// 职责边界：
    ///   - 推进节点、过滤选项、触发 DialogueEvents；
    ///   - 不负责显示（由 DialoguePanelView 订阅事件显示）；
    ///   - 不负责玩法 flag（条件判断通过 IDialogueConditionProvider 注入）；
    ///   - 不直接写存档，仅提供 ExportState/RestoreState 钩子，
    ///     由游戏在 IGameplaySaveProvider 中归口调用。
    /// </summary>
    public sealed class DialogueManager
    {
        private const string LOG_MODULE = LogModules.DIALOGUE;

        private static DialogueManager s_instance;

        /// <summary>全局唯一运行时实例（懒创建）。</summary>
        public static DialogueManager Instance
        {
            get { return s_instance ??= new DialogueManager(); }
        }

        /// <summary>当前是否有进行中的对话。</summary>
        public bool IsActive { get; private set; }

        /// <summary>当前对话数据资产。</summary>
        public DialogueData CurrentDialogue { get; private set; }

        /// <summary>当前停留的对话节点。</summary>
        public DialogueNode CurrentNode { get; private set; }

        /// <summary>
        /// 当前条件判定提供者。每次 StartDialogue 时由游戏注入；
        /// 为 null 时所有带 conditionKey 的选项都会被过滤掉。
        /// </summary>
        public IDialogueConditionProvider ConditionProvider { get; set; }

        #region 对话流程

        /// <summary>
        /// 开始一段对话。若已有进行中的对话会先结束旧的。
        /// </summary>
        /// <returns>是否成功开始</returns>
        public bool StartDialogue(DialogueData data, IDialogueConditionProvider conditionProvider = null)
        {
            if (data == null)
            {
                Log.Error(LOG_MODULE, "开始对话失败：对话数据为空");
                return false;
            }

            List<string> errors = data.Validate();
            if (errors.Count > 0)
            {
                Log.Error(LOG_MODULE, $"开始对话失败（{data.name}）：{string.Join("；", errors)}");
                return false;
            }

            if (IsActive)
            {
                Log.Warning(LOG_MODULE, "已有进行中的对话，将先结束再开始新对话");
                EndDialogue();
            }

            DialogueNode startNode = data.GetNode(data.StartNodeId);
            if (startNode == null)
            {
                Log.Error(LOG_MODULE, $"开始对话失败：起始节点不存在（{data.StartNodeId}）");
                return false;
            }

            ConditionProvider = conditionProvider;
            CurrentDialogue = data;
            CurrentNode = startNode;
            IsActive = true;

            DialogueEvents.TriggerDialogueStarted(data);
            DialogueEvents.TriggerNodeChanged(startNode);
            return true;
        }

        /// <summary>
        /// 无选项节点上的"继续"：推进到 nextNodeId；
        /// 目标为空则结束对话。
        /// </summary>
        /// <returns>是否成功推进或正常结束</returns>
        public bool Advance()
        {
            if (!IsActive || CurrentNode == null)
            {
                Log.Warning(LOG_MODULE, "推进对话失败：当前没有进行中的对话");
                return false;
            }

            IReadOnlyList<DialogueChoice> availableChoices = GetAvailableChoices();
            if (availableChoices.Count > 0)
            {
                Log.Warning(LOG_MODULE, "当前节点存在可选选项，必须先选择选项再推进");
                return false;
            }

            // 全部选项被条件过滤且没有默认跳转：拒绝推进并提示配置问题，
            // 避免静默结束对话掩盖剧情配置错误。
            if (CurrentNode.choices != null && CurrentNode.choices.Count > 0
                && string.IsNullOrEmpty(CurrentNode.nextNodeId))
            {
                Log.Error(LOG_MODULE,
                    $"节点 {CurrentNode.nodeId} 的所有选项均被条件过滤，且未配置 nextNodeId 兜底跳转，对话无法推进");
                return false;
            }

            return MoveToNode(CurrentNode.nextNodeId);
        }

        /// <summary>
        /// 按"当前节点可用选项"的下标选择选项并跳转。
        /// </summary>
        public bool SelectChoice(int availableChoiceIndex)
        {
            if (!IsActive || CurrentNode == null)
            {
                Log.Warning(LOG_MODULE, "选择对话选项失败：当前没有进行中的对话");
                return false;
            }

            IReadOnlyList<DialogueChoice> availableChoices = GetAvailableChoices();
            if (availableChoiceIndex < 0 || availableChoiceIndex >= availableChoices.Count)
            {
                Log.Error(LOG_MODULE,
                    $"选择对话选项失败：下标越界 {availableChoiceIndex}/{availableChoices.Count}");
                return false;
            }

            return MoveToNode(availableChoices[availableChoiceIndex].nextNodeId);
        }

        /// <summary>
        /// 结束当前对话并触发 OnDialogueEnded。
        /// </summary>
        public void EndDialogue()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            CurrentDialogue = null;
            CurrentNode = null;
            ConditionProvider = null;

            DialogueEvents.TriggerDialogueEnded();
        }

        /// <summary>
        /// 获取当前节点经条件过滤后的可用选项。
        /// </summary>
        public IReadOnlyList<DialogueChoice> GetAvailableChoices()
        {
            var available = new List<DialogueChoice>();

            if (!IsActive || CurrentNode == null || CurrentNode.choices == null)
            {
                return available;
            }

            for (int i = 0; i < CurrentNode.choices.Count; i++)
            {
                DialogueChoice choice = CurrentNode.choices[i];
                if (choice == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(choice.conditionKey)
                    || (ConditionProvider != null && ConditionProvider.IsConditionMet(choice.conditionKey)))
                {
                    available.Add(choice);
                }
            }

            return available;
        }

        #endregion

        #region 存档钩子

        /// <summary>
        /// 导出当前对话运行时状态。无进行中对话时返回空的 DialogueState。
        /// 供游戏的 IGameplaySaveProvider 捕获后随玩法 JSON 一起存档。
        /// </summary>
        public DialogueState ExportState()
        {
            return new DialogueState
            {
                dialogueDataId = CurrentDialogue != null ? CurrentDialogue.name : string.Empty,
                currentNodeId = CurrentNode != null ? CurrentNode.nodeId : string.Empty
            };
        }

        /// <summary>
        /// 导出当前状态的 JSON 字符串（便于直接嵌入玩法存档负载）。
        /// </summary>
        public string ExportStateJson()
        {
            return JsonUtility.ToJson(ExportState());
        }

        /// <summary>
        /// 从存档状态恢复到指定对话数据中的某个节点。
        /// 恢复成功后按"新对话"语义触发 OnDialogueStarted + OnNodeChanged，
        /// 因此也会唤醒对话面板（若其已订阅）。
        /// </summary>
        /// <param name="state">存档导出的状态；nodeId 为空表示不恢复对话</param>
        /// <param name="data">由游戏按 state.dialogueDataId 解析出的对话资产</param>
        /// <param name="conditionProvider">恢复后的条件判定提供者</param>
        public bool RestoreState(DialogueState state, DialogueData data,
            IDialogueConditionProvider conditionProvider = null)
        {
            if (data == null)
            {
                Log.Error(LOG_MODULE, "恢复对话状态失败：对话数据为空");
                return false;
            }

            if (state == null || string.IsNullOrEmpty(state.currentNodeId))
            {
                Log.Info(LOG_MODULE, "存档中没有进行中的对话节点，跳过恢复");
                return false;
            }

            DialogueNode node = data.GetNode(state.currentNodeId);
            if (node == null)
            {
                Log.Error(LOG_MODULE,
                    $"恢复对话状态失败：节点不存在（{state.dialogueDataId}/{state.currentNodeId}）");
                return false;
            }

            if (IsActive)
            {
                EndDialogue();
            }

            ConditionProvider = conditionProvider;
            CurrentDialogue = data;
            CurrentNode = node;
            IsActive = true;

            DialogueEvents.TriggerDialogueStarted(data);
            DialogueEvents.TriggerNodeChanged(node);
            return true;
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 跳转到目标节点；目标为空则正常结束对话。
        /// </summary>
        private bool MoveToNode(string targetNodeId)
        {
            if (string.IsNullOrEmpty(targetNodeId))
            {
                Log.Info(LOG_MODULE, "对话到达结束节点，正常结束");
                EndDialogue();
                return true;
            }

            DialogueNode nextNode = CurrentDialogue.GetNode(targetNodeId);
            if (nextNode == null)
            {
                Log.Error(LOG_MODULE,
                    $"对话跳转失败：目标节点不存在（{CurrentDialogue.name}/{targetNodeId}），已终止对话");
                EndDialogue();
                return false;
            }

            CurrentNode = nextNode;
            DialogueEvents.TriggerNodeChanged(nextNode);
            return true;
        }

        #endregion
    }
}
