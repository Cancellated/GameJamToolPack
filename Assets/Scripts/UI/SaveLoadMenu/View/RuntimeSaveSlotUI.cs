using MyGame.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame.UI.SaveLoad.View
{
    /// <summary>
    /// 运行时兜底存档槽UI。
    /// 当 SaveSlotPrefab 未配置或 Addressable 加载失败时，用于动态生成可交互的存档槽。
    /// 也可直接挂在 SaveSlotPrefab 上，作为标准槽位实现使用。
    /// </summary>
    public sealed class RuntimeSaveSlotUI : MonoBehaviour, ISaveSlotUI
    {
        private SaveSlotInfo _slotInfo;
        private SaveLoadMenuView _view;
        private Button _button;
        private Image _background;
        private Text _infoText;
        private LayoutElement _layoutElement;
        private bool _selected;

        public string SlotName
        {
            get { return _slotInfo != null ? _slotInfo.SlotName : string.Empty; }
        }

        /// <summary>
        /// 初始化槽位：补齐自身视觉组件并刷新显示。
        /// </summary>
        public void Initialize(SaveSlotInfo slotInfo, SaveLoadMenuView view)
        {
            if (slotInfo == null || view == null)
            {
                return;
            }

            _slotInfo = slotInfo;
            _view = view;

            EnsureVisuals();
            UpdateDisplay();
            UpdateHighlight();
        }

        public void UpdateDisplay()
        {
            if (_slotInfo == null || _infoText == null)
            {
                return;
            }

            if (!_slotInfo.HasSave)
            {
                _infoText.text = _slotInfo.DisplayName + "\n（空存档槽）";
                return;
            }

            string timeText = string.IsNullOrEmpty(_slotInfo.LastModified)
                ? "存档时间未知"
                : _slotInfo.LastModified;

            string progressText = string.IsNullOrEmpty(_slotInfo.ProgressText)
                ? string.Empty
                : "\n" + _slotInfo.ProgressText;

            _infoText.text = _slotInfo.DisplayName + "\n" + timeText + progressText;
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            UpdateHighlight();
        }

        private void EnsureVisuals()
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 64f);

            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
            {
                _layoutElement = gameObject.AddComponent<LayoutElement>();
            }
            _layoutElement.minHeight = 56f;
            _layoutElement.preferredHeight = 64f;

            if (GetComponent<CanvasRenderer>() == null)
            {
                gameObject.AddComponent<CanvasRenderer>();
            }

            _background = GetComponent<Image>();
            if (_background == null)
            {
                _background = gameObject.AddComponent<Image>();
            }
            _background.raycastTarget = true;

            _button = GetComponent<Button>();
            if (_button == null)
            {
                _button = gameObject.AddComponent<Button>();
            }
            _button.targetGraphic = _background;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(HandleClick);

            if (_infoText == null)
            {
                GameObject textGO = new GameObject("Info",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textGO.transform.SetParent(transform, false);

                RectTransform textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10f, 4f);
                textRect.offsetMax = new Vector2(-10f, -4f);

                _infoText = textGO.GetComponent<Text>();
                _infoText.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                _infoText.raycastTarget = false;
                _infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _infoText.fontSize = 18;
                _infoText.alignment = TextAnchor.MiddleLeft;
                _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
                _infoText.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        private void UpdateHighlight()
        {
            if (_background == null)
            {
                return;
            }

            if (_selected)
            {
                _background.color = new Color(0.35f, 0.72f, 0.45f, 1f);
            }
            else if (_slotInfo != null && !_slotInfo.HasSave)
            {
                _background.color = new Color(0.42f, 0.42f, 0.42f, 0.9f);
            }
            else
            {
                _background.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            }
        }

        private void HandleClick()
        {
            if (_view != null && _slotInfo != null)
            {
                _view.OnSaveSlotClick(_slotInfo.SlotName, _slotInfo.SaveData);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }
    }
}
