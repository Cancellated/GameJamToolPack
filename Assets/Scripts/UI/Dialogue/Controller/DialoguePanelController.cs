using System.Collections.Generic;
using Logger;
using MyGame.Dialogue;
using MyGame.Events;
using MyGame.UI.Dialogue.Model;
using MyGame.UI.Dialogue.View;
using UnityEngine;

namespace MyGame.UI.Dialogue.Controller
{
    /// <summary>
    /// 对话面板控制器：桥接 DialogueManager（流程）与 DialoguePanelView（显示）。
    /// 遵守既有 MVC 规范：Model 数据由本类写入，View 只能通过本类查询/操作。
    /// </summary>
    public class DialoguePanelController : BaseController<DialoguePanelView, DialoguePanelModel>
    {
        private const string LOG_MODULE = LogModules.DIALOGUE;

        /// <summary>
        /// 初始化控制器（由 DialoguePanelView.TryBindController 调用）。
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                Log.Info(LOG_MODULE, "初始化对话面板控制器");

                CreateAndInitializeModel();
                RegisterEvents();

                base.Initialize();

                // 面板晚于对话开始才被加载（首次显示经 Addressable 异步加载）时，
                // 立即补一次当前节点内容，避免第一句台词丢失
                if (DialogueManager.Instance.IsActive && DialogueManager.Instance.CurrentNode != null)
                {
                    ApplyCurrentNodeToModel(DialogueManager.Instance.CurrentNode);
                }
            }
        }

        /// <summary>
        /// 清理控制器：注销事件与模型订阅（面板随场景卸载/销毁时由 OnDestroy 调用）。
        /// </summary>
        public override void Cleanup()
        {
            if (IsInitialized)
            {
                Log.Info(LOG_MODULE, "清理对话面板控制器");

                UnregisterEvents();

                if (m_model != null)
                {
                    m_model.Cleanup();
                    m_model = null;
                }

                base.Cleanup();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
            m_view?.UnbindController();
        }

        #region 事件订阅

        private void RegisterEvents()
        {
            DialogueEvents.OnDialogueStarted += HandleDialogueStarted;
            DialogueEvents.OnNodeChanged += HandleNodeChanged;
            DialogueEvents.OnDialogueEnded += HandleDialogueEnded;
        }

        private void UnregisterEvents()
        {
            DialogueEvents.OnDialogueStarted -= HandleDialogueStarted;
            DialogueEvents.OnNodeChanged -= HandleNodeChanged;
            DialogueEvents.OnDialogueEnded -= HandleDialogueEnded;
        }

        private void HandleDialogueStarted(DialogueData data)
        {
            if (m_model == null)
            {
                return;
            }

            // 统一走 UIManager 调度显示：输入模式/互斥关系由 UIConfig 驱动
            GameEvents.TriggerMenuShow(UIType.DialoguePanel, true);

            if (DialogueManager.Instance.CurrentNode != null)
            {
                ApplyCurrentNodeToModel(DialogueManager.Instance.CurrentNode);
            }
        }

        private void HandleNodeChanged(DialogueNode node)
        {
            if (m_model == null)
            {
                return;
            }

            ApplyCurrentNodeToModel(node);
        }

        private void HandleDialogueEnded()
        {
            if (m_model == null)
            {
                return;
            }

            m_model.SpeakerName = string.Empty;
            m_model.DialogueText = string.Empty;
            m_model.HasChoices = false;

            m_view?.UpdateDialogueContent();
            GameEvents.TriggerMenuShow(UIType.DialoguePanel, false);
        }

        #endregion

        #region View 查询接口（View 不直接访问 Model）

        /// <summary>当前说话人显示名（空串表示隐藏）</summary>
        public string GetSpeakerName()
        {
            return m_model != null ? m_model.SpeakerName : string.Empty;
        }

        /// <summary>当前台词全文</summary>
        public string GetDialogueText()
        {
            return m_model != null ? m_model.DialogueText : string.Empty;
        }

        /// <summary>当前节点是否有可用选项</summary>
        public bool HasChoices()
        {
            return m_model != null && m_model.HasChoices;
        }

        #endregion

        #region View 可调用的操作方法

        /// <summary>
        /// 继续按钮：推进无选项节点。
        /// </summary>
        public void OnContinueClicked()
        {
            if (DialogueManager.Instance.IsActive)
            {
                DialogueManager.Instance.Advance();
            }
        }

        /// <summary>
        /// 选项按钮：按"可用选项下标"选择。
        /// </summary>
        public void OnChoiceClicked(int availableChoiceIndex)
        {
            if (DialogueManager.Instance.IsActive)
            {
                DialogueManager.Instance.SelectChoice(availableChoiceIndex);
            }
        }

        /// <summary>
        /// 跳过按钮：结束整段对话。
        /// </summary>
        public void OnSkipClicked()
        {
            if (DialogueManager.Instance.IsActive)
            {
                DialogueManager.Instance.EndDialogue();
            }
        }

        /// <summary>
        /// 供 View 查询当前节点可用选项（经条件过滤）。
        /// </summary>
        public IReadOnlyList<DialogueChoice> GetAvailableChoices()
        {
            return DialogueManager.Instance.GetAvailableChoices();
        }

        #endregion

        #region 私有方法

        private void ApplyCurrentNodeToModel(DialogueNode node)
        {
            if (node == null || m_model == null)
            {
                return;
            }

            m_model.SpeakerName = node.speakerName ?? string.Empty;
            m_model.DialogueText = node.text ?? string.Empty;
            m_model.HasChoices = DialogueManager.Instance.GetAvailableChoices().Count > 0;

            m_view?.UpdateDialogueContent();
        }

        #endregion
    }
}
