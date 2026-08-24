using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Dialogue
{
    /// <summary>
    /// 对话选项。nextNodeId 指向 DialogueData 中的节点；
    /// conditionKey 由游戏实现的 IDialogueConditionProvider 判定，
    /// 留空表示该选项始终可选。
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("选项显示文本")]
        public string choiceText;

        [Tooltip("选择后跳转到的节点 ID；留空表示选择后结束对话")]
        public string nextNodeId;

        [Tooltip("选项可见/可选条件键，由 IDialogueConditionProvider.IsConditionMet 判定；留空表示无条件")]
        public string conditionKey;
    }

    /// <summary>
    /// 对话节点：一段台词 + 说话人 + 可选分支。
    /// 有选项时由玩家选择推进；无选项时按 nextNodeId 顺序推进。
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        [Tooltip("节点 ID（同一份对话数据内唯一）")]
        public string nodeId;

        [Tooltip("说话人显示名；留空则面板隐藏说话人栏")]
        public string speakerName;

        [Tooltip("台词文本")]
        [TextArea(2, 8)]
        public string text;

        [Tooltip("本节点的选项列表（可空）")]
        public List<DialogueChoice> choices = new();

        [Tooltip("无选项时点击继续跳转到的节点 ID；留空表示对话结束")]
        public string nextNodeId;
    }

    /// <summary>
    /// 对话数据资产：一份完整对话的节点集合。
    /// 只定义"节点/台词/选项/跳转"的中立结构，具体剧本内容由游戏资产提供。
    /// 条件判断通过 IDialogueConditionProvider 注入，本资产不持有任何玩法状态。
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [Header("流程")]
        [Tooltip("起始节点 ID")]
        [SerializeField] private string m_startNodeId;

        [Header("节点列表")]
        [SerializeField] private List<DialogueNode> m_nodes = new();

        /// <summary>起始节点 ID</summary>
        public string StartNodeId
        {
            get { return m_startNodeId; }
        }

        /// <summary>全部节点（只读）</summary>
        public IReadOnlyList<DialogueNode> Nodes
        {
            get { return m_nodes; }
        }

        /// <summary>
        /// 按 ID 获取节点；未找到返回 null。
        /// </summary>
        public DialogueNode GetNode(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < m_nodes.Count; i++)
            {
                if (m_nodes[i] != null && m_nodes[i].nodeId == id)
                {
                    return m_nodes[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 校验数据完整性：起始节点存在、节点 ID 唯一。
        /// 返回错误描述列表（为空表示合法），供编辑器/运行时调试使用。
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            if (string.IsNullOrEmpty(m_startNodeId))
            {
                errors.Add("未配置起始节点 ID");
            }

            for (int i = 0; i < m_nodes.Count; i++)
            {
                DialogueNode node = m_nodes[i];
                if (node == null)
                {
                    errors.Add($"节点 #{i} 为空引用");
                    continue;
                }

                if (string.IsNullOrEmpty(node.nodeId))
                {
                    errors.Add($"节点 #{i} 缺少 nodeId");
                }
                else if (!ids.Add(node.nodeId))
                {
                    errors.Add($"nodeId 重复：{node.nodeId}");
                }
            }

            if (!string.IsNullOrEmpty(m_startNodeId) && GetNode(m_startNodeId) == null)
            {
                errors.Add($"起始节点不存在：{m_startNodeId}");
            }

            return errors;
        }
    }

    /// <summary>
    /// 对话运行时状态（可序列化），供 IGameplaySaveProvider 归口存档。
    /// 框架只保存"哪份对话 + 停在哪个节点"，对话变量/flag 由游戏自行持有。
    /// </summary>
    [Serializable]
    public class DialogueState
    {
        [Tooltip("对话数据资产标识（通常用 asset.name）；由游戏在恢复时解析为 DialogueData 资产")]
        public string dialogueDataId = string.Empty;

        [Tooltip("当前停留的节点 ID")]
        public string currentNodeId = string.Empty;
    }
}
