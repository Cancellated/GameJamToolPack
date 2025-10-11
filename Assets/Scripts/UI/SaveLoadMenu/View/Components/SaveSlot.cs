using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MyGame.Data;
using MyGame.UI.SaveLoad.View;
using MyGame.UI.SaveLoad.View.Components;

namespace MyGame.UI.SaveLoad.View.Components
{
    /// <summary>
    /// 具体的存档槽UI实现类
    /// 负责显示单个存档槽的信息并处理用户交互
    /// 支持直接控制二级菜单和确认面板的显示/隐藏
    /// 挂载到存档槽预制体上使用
    /// </summary>
    public class SaveSlot : MonoBehaviour, ISaveSlotUI
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
        [Tooltip("显示选中状态的高亮图片组件")]
        private Image _highlightImage;
        
        [SerializeField]
        [Tooltip("点击存档槽时触发的按钮组件")]
        private Button _slotButton;

        private SaveSlotInfo _slotInfo;
        private SaveLoadMenuView _view;
        private bool _isSelected = false;
        private ISaveSlotMenuHandler _menuHandler;

        /// <summary>
        /// 初始化存档槽UI
        /// </summary>
        /// <param name="slotInfo">存档槽信息</param>
        /// <param name="view">视图引用</param>
        public void Initialize(SaveSlotInfo slotInfo, SaveLoadMenuView view)
        {
            _slotInfo = slotInfo;
            _view = view;
            // 初始化菜单处理器，使用传入的视图作为菜单处理器
            _menuHandler = view as ISaveSlotMenuHandler;
            
            // 设置存档槽名称
            if (_slotNameText != null)
            {
                _slotNameText.text = slotInfo.DisplayName;
            }
            
            // 注册点击事件
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
            UpdateHighlight();
        }

        /// <summary>
        /// 获取存档槽名称
        /// </summary>
        public string SlotName
        {
            get { return _slotInfo != null ? _slotInfo.SlotName : string.Empty; }
        }

        /// <summary>
        /// 更新存档槽高亮状态
        /// </summary>
        private void UpdateHighlight()
        {
            if (_highlightImage != null)
            {
                _highlightImage.enabled = _isSelected;
            }
        }

        /// <summary>
        /// 处理存档槽按钮点击事件
        /// </summary>
        private void HandleSlotButtonClick()
        {
            if (_view != null)
            {
                // 首先调用原始的视图点击处理，保持兼容性
                _view.OnSaveSlotClick(_slotInfo.SlotName, _slotInfo.SaveData);
            }
            
            // 如果启用了存档槽直接控制菜单的功能，并且菜单处理器有效
            if (_menuHandler != null && _slotInfo.HasSave)
            {
                // 显示二级菜单
                _menuHandler.ShowSaveOptionsMenu(_slotInfo);
            }
        }
        
        /// <summary>
        /// 在存档槽内直接显示二级菜单
        /// 可由外部调用，实现更灵活的菜单显示逻辑
        /// </summary>
        public void ShowOptionsMenu()
        {
            if (_menuHandler != null && _slotInfo.HasSave)
            {
                _menuHandler.ShowSaveOptionsMenu(_slotInfo);
            }
        }
        
        /// <summary>
        /// 在存档槽内直接隐藏二级菜单
        /// </summary>
        public void HideOptionsMenu()
        {
            _menuHandler?.HideSaveOptionsMenu();
        }
        
        /// <summary>
        /// 在存档槽内直接显示确认面板
        /// </summary>
        /// <param name="confirmType">确认类型</param>
        /// <param name="callback">确认回调（当前版本不使用）</param>
        public void ShowConfirmPanel(ConfirmType confirmType, System.Action<bool> callback = null)
        {
            // 传递存档槽名称给菜单处理器
            _menuHandler?.ShowConfirmPanel(confirmType, _slotInfo.SlotName); 
        }
        
        /// <summary>
        /// 在存档槽内直接隐藏确认面板
        /// </summary>
        public void HideConfirmPanel()
        {
            _menuHandler?.HideConfirmPanel();
        }
        
        /// <summary>
        /// 检查存档槽是否可以进行交互操作
        /// </summary>
        /// <returns>是否可以交互</returns>
        public bool CanInteract()
        {
            // 检查存档槽本身和菜单处理器是否可用
            return gameObject.activeInHierarchy && _menuHandler != null;
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