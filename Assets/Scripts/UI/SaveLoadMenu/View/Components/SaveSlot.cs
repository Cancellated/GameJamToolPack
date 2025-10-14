using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace MyGame.UI.SaveLoad.View
{
    /// <summary>
    /// 具体的存档槽UI实现类
    /// 负责显示单个存档槽的信息并处理用户交互
    /// 挂载到存档槽预制体上使用
    /// </summary>
    public class SaveSlot : MonoBehaviour
    {
        [Header("存档槽UI组件")]
        [SerializeField]
        [Tooltip("显示存档槽名称的文本组件")]
        private TextMeshProUGUI _slotNameText;
        
        [SerializeField]
        [Tooltip("显示存档时间的文本组件")]
        private TextMeshProUGUI _saveTimeText;
        
        [SerializeField]
        [Tooltip("显示游戏进度的文本组件")]
        private TextMeshProUGUI _gameProgressText;
        
        [SerializeField]
        [Tooltip("点击存档槽时触发的按钮组件")]
        private Button _slotButton;

        private SaveSlotInfo _slotInfo;
        private SaveLoadMenuView _view;
        private bool _isSelected = false;

        /// <summary>
        /// 初始化存档槽UI
        /// </summary>
        /// <param name="slotInfo">存档槽信息</param>
        /// <param name="view">视图引用</param>
        public void Initialize(SaveSlotInfo slotInfo, SaveLoadMenuView view)
        {
            _slotInfo = slotInfo;
            _view = view;
            
            // 设置存档槽名称
            if (_slotNameText != null)
            {
                _slotNameText.text = slotInfo.SlotName;
            }
            
            // 注册点击事件
            if (_slotButton != null)
            {
                _slotButton.onClick.RemoveAllListeners();
                _slotButton.onClick.AddListener(HandleSlotButtonClick);
            }
            
            UpdateDisplay();
        }

        /// <summary>
        /// 更新显示内容
        /// 优先使用SaveData中的存档信息，确保UI能反映存档的实际名称和类型
        /// </summary>
        public void UpdateDisplay()
        {
            // 更新存档槽名称显示
            if (_slotNameText != null)
            {
                // 优先使用SaveData中的存档名称，如果存在
                if (_slotInfo.SaveData != null && !string.IsNullOrEmpty(_slotInfo.SaveData.saveSlotName))
                {
                    string displayName = _slotInfo.SaveData.saveSlotName;
                    _slotNameText.text = displayName;
                }
                // 否则使用槽位信息中的存档名称
                else
                {
                    _slotNameText.text = _slotInfo.SlotName;
                }
            }
            
            // 设置存档时间
            if (_saveTimeText != null)
            {
                if (_slotInfo.SaveData != null)
                {
                    _saveTimeText.text = _slotInfo.SaveData.saveTime;
                }
                else
                {
                    _saveTimeText.text = "空存档槽";
                }
            }
            
            // 设置游戏进度
            if (_gameProgressText != null)
            {
                if (_slotInfo.SaveData != null && _slotInfo.SaveData.gameProgress != null)
                {
                    _gameProgressText.text = $"关卡：{_slotInfo.SaveData.gameProgress.currentLevel}";
                }
                else
                {
                    _gameProgressText.text = "";
                }
            }
        }

        /// <summary>
        /// 设置存档槽选中状态
        /// </summary>
        /// <param name="selected">是否选中</param>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
        }

        /// <summary>
        /// 获取存档槽名称
        /// </summary>
        public string SlotName
        {
            get { return _slotInfo != null ? _slotInfo.SlotName : string.Empty; }
        }

        /// <summary>
        /// 处理存档槽按钮点击事件
        /// </summary>
        private void HandleSlotButtonClick()
        {
            if (_view != null)
            {
                _view.OnSaveSlotClick(_slotInfo.SlotName, _slotInfo.SaveData);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_slotButton != null)
            {
                _slotButton.onClick.RemoveAllListeners();
            }
        }
    }
}