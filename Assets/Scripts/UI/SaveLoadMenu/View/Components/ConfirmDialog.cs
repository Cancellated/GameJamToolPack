using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MyGame.UI.SaveLoad;

namespace MyGame.UI.SaveLoadMenu.View
{
    public class ConfirmDialog : MonoBehaviour
    {
        [Tooltip("确认面板游戏对象")]
        [SerializeField] private GameObject confirmPanel;

        [Tooltip("确认按钮")]
        [SerializeField] private Button confirmButton;

        [Tooltip("取消按钮")]
        [SerializeField] private Button cancelButton;

        [Tooltip("确认信息文本")]
        [SerializeField] private TextMeshProUGUI confirmDirectionText;
        
        [Tooltip("存档菜单配置文件")]
        [SerializeField] private SaveLoadMenuConfig config;

        // 当前确认操作类型
        private ConfirmActionType _currentConfirmActionType;
        
        // 当前确认操作的存档槽名称
        private string _currentConfirmSlotName;

        // 确认操作回调委托
        public delegate void ConfirmActionHandler(ConfirmActionType actionType, string slotName);
        public event ConfirmActionHandler OnConfirmAction;
        
        // 取消操作回调委托
        public delegate void CancelActionHandler();
        public event CancelActionHandler OnCancelAction;

        private void Awake()
        {
            // 注册按钮事件
            RegisterEvents();
            HideConfirmPanel();
        }

        private void OnDestroy()
        {
            // 注销按钮事件
            UnregisterEvents();
        }

        /// <summary>
        /// 注册事件监听
        /// </summary>
        private void RegisterEvents()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirmButtonClick);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HandleCancelButtonClick);
            }
        }

        /// <summary>
        /// 注销事件监听
        /// </summary>
        private void UnregisterEvents()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmButtonClick);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelButtonClick);
            }
        }

        /// <summary>
        /// 显示确认面板
        /// </summary>
        /// <param name="actionType">确认操作类型</param>
        /// <param name="slotName">存档槽名称</param>
        /// <param name="confirmText">确认信息文本</param>
        public void ShowConfirmPanel(ConfirmActionType actionType, string slotName)
        {
            // 设置当前确认操作类型和存档槽名称
            _currentConfirmActionType = actionType;
            _currentConfirmSlotName = slotName;
            string confirmText = string.Empty;
            
            // 根据操作类型从配置中获取确认文本
            if (config != null)
            {
                switch (actionType)
                {
                    case ConfirmActionType.Save:
                        confirmText = config.SaveConfirmText;
                        break;
                    case ConfirmActionType.Load:
                        confirmText = config.LoadSaveConfirmText;
                        break;
                    case ConfirmActionType.Delete:
                        confirmText = config.DeleteSaveConfirmText;
                        break;
                    case ConfirmActionType.Create:
                        confirmText = config.NewGameConfirmText;
                        break;
                }
            }
            
            // 设置确认信息文本
            if (confirmDirectionText != null && !string.IsNullOrEmpty(confirmText))
            {
                confirmDirectionText.text = confirmText;
            }
            
            // 显示确认面板
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// 隐藏确认面板
        /// </summary>
        public void HideConfirmPanel()
        {
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 处理确认按钮点击事件
        /// </summary>
        private void HandleConfirmButtonClick()
        {
            // 触发确认事件
            OnConfirmAction?.Invoke(_currentConfirmActionType, _currentConfirmSlotName);
            
            // 隐藏确认面板
            HideConfirmPanel();
        }
        
        /// <summary>
        /// 处理取消按钮点击事件
        /// </summary>
        private void HandleCancelButtonClick()
        {
            // 触发取消事件
            OnCancelAction?.Invoke();
            
            // 隐藏确认面板
            HideConfirmPanel();
        }
    }

    public enum ConfirmActionType
    {
        Save,
        Load,
        Delete,
        Create
    }
}