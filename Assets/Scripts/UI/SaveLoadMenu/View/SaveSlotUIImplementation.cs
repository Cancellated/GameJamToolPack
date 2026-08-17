using MyGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame.UI.SaveLoad.View
{
    /// <summary>
    /// 正式存档槽UI实现：直接实现 ISaveSlotUI，供 SaveSlotPrefab 使用。
    /// 仅使用 TMP 文本组件，避免与旧基类的 Legacy Text/双按钮结构混用。
    /// 独立成文件，保证 Unity 能正确生成 MonoScript GUID 并序列化到 Prefab。
    /// </summary>
    public sealed class SaveSlotUIImplementation : MonoBehaviour, ISaveSlotUI
    {
        [SerializeField]
        private TextMeshProUGUI _slotNameText;

        [SerializeField]
        private TextMeshProUGUI _saveTimeText;

        [SerializeField]
        private TextMeshProUGUI _gameProgressText;

        [SerializeField]
        private Image _highlightImage;

        [SerializeField]
        private Button _slotButton;

        private SaveSlotInfo _slotInfo;
        private SaveLoadMenuView _view;
        private bool _isSelected;

        public string SlotName
        {
            get { return _slotInfo != null ? _slotInfo.SlotName : string.Empty; }
        }

        /// <summary>
        /// 初始化存档槽UI
        /// </summary>
        public void Initialize(SaveSlotInfo slotInfo, SaveLoadMenuView view)
        {
            _slotInfo = slotInfo;
            _view = view;

            // 注册点击事件（幂等）
            if (_slotButton != null)
            {
                _slotButton.onClick.RemoveAllListeners();
                _slotButton.onClick.AddListener(HandleSlotButtonClick);
            }

            UpdateDisplay();
            UpdateHighlight();
        }

        /// <summary>
        /// 更新显示内容
        /// </summary>
        public void UpdateDisplay()
        {
            if (_slotInfo == null)
            {
                return;
            }

            if (_slotNameText != null)
            {
                _slotNameText.text = _slotInfo.DisplayName;
            }

            if (_saveTimeText != null)
            {
                _saveTimeText.text = _slotInfo.HasSave
                    ? (string.IsNullOrEmpty(_slotInfo.LastModified) ? "时间未知" : _slotInfo.LastModified)
                    : "空存档槽";
            }

            if (_gameProgressText != null)
            {
                _gameProgressText.text = _slotInfo.HasSave
                    ? (string.IsNullOrEmpty(_slotInfo.ProgressText) ? string.Empty : _slotInfo.ProgressText)
                    : string.Empty;
            }
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            if (_highlightImage != null)
            {
                _highlightImage.enabled = _isSelected;
            }
        }

        private void HandleSlotButtonClick()
        {
            if (_view != null && _slotInfo != null)
            {
                _view.OnSaveSlotClick(_slotInfo.SlotName, _slotInfo.SaveData);
            }
        }

        private void OnDestroy()
        {
            if (_slotButton != null)
            {
                _slotButton.onClick.RemoveListener(HandleSlotButtonClick);
            }
        }
    }
}
