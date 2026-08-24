using MyGame.UI;

namespace MyGame.UI.Dialogue.Model
{
    /// <summary>
    /// 对话面板模型：只保存面板要显示的当前节点内容。
    /// 对话流程状态由 DialogueManager 持有，模型仅作为 View 的展示数据中转。
    /// </summary>
    public class DialoguePanelModel : ObservableModel
    {
        private string m_speakerName = string.Empty;
        private string m_dialogueText = string.Empty;
        private bool m_hasChoices;

        /// <summary>当前说话人显示名（空串表示隐藏说话人栏）</summary>
        public string SpeakerName
        {
            get { return m_speakerName; }
            set { SetProperty(ref m_speakerName, value ?? string.Empty, nameof(SpeakerName)); }
        }

        /// <summary>当前台词全文（打字机效果由 View 负责播放）</summary>
        public string DialogueText
        {
            get { return m_dialogueText; }
            set { SetProperty(ref m_dialogueText, value ?? string.Empty, nameof(DialogueText)); }
        }

        /// <summary>当前节点是否有可用选项（决定显示选项列表还是继续按钮）</summary>
        public bool HasChoices
        {
            get { return m_hasChoices; }
            set { SetProperty(ref m_hasChoices, value, nameof(HasChoices)); }
        }
    }
}
