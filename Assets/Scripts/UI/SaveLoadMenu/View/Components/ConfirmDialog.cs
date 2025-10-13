using System.Collections;
using System.Collections.Generic;
using MyGame.UI.SaveLoad;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Logger;
using TMPro;

namespace MyGame.UI.SaveLoadMenu.View.Components
{
    public class ConfirmDialog : MonoBehaviour, IConfirm
    {
        #region UI组件
        [Tooltip("确认弹窗面板")]
        [SerializeField] private GameObject _confirmPanel;
        [Tooltip("确认弹窗文本")]
        [SerializeField] private TextMeshProUGUI _confirmText;
        [Tooltip("确认按钮")]
        [SerializeField] private Button _confirmButton;
        [Tooltip("取消按钮")]
        [SerializeField] private Button _cancelButton;
        #endregion
        #region 私有字段
        private SaveLoadMenuConfig _saveLoadMenuConfig;
        private readonly string LOG_MODULE = LogModules.SAVE + "Confirm";
        #endregion

        #region 生命周期
        private void Awake()
        {
            // 使用Addressable Assets初始化配置文件引用
            LoadConfigAsync();
        }

        private async void LoadConfigAsync()
        {
            // 加载Addressable配置文件
            AsyncOperationHandle<SaveLoadMenuConfig> handle = Addressables.LoadAssetAsync<SaveLoadMenuConfig>("SaveLoadMenuConfig");
            _saveLoadMenuConfig = await handle.Task;
            
            if (_saveLoadMenuConfig == null)
            {
                Log.Warning(LOG_MODULE,"配置文件未找到");
            }         
            // 释放句柄
            Addressables.Release(handle);
        }
        #endregion

        #region 接口实现
        public void ShowConfirmDialog(ConfirmDialogState state, System.Action onConfirm, System.Action onCancel = null)
        {
            GetConfirmMessage(state);
            _confirmPanel.SetActive(true);
            _confirmButton.onClick.AddListener(() =>
            {
                onConfirm?.Invoke();
                HideConfirmDialog();
            });
            _cancelButton.onClick.AddListener(() =>
            {
                onCancel?.Invoke();
                HideConfirmDialog();
            });
        }

        public void HideConfirmDialog()
        {
            _confirmPanel.SetActive(false);
            _confirmButton.onClick.RemoveAllListeners();
            _cancelButton.onClick.RemoveAllListeners();
        }

        public void GetConfirmMessage(ConfirmDialogState state)
        {
            _confirmText.text = state switch
            {
                ConfirmDialogState.Save => _saveLoadMenuConfig.SaveConfirmText,  
                ConfirmDialogState.Load => _saveLoadMenuConfig.LoadConfirmText,
                ConfirmDialogState.Delete => _saveLoadMenuConfig.DeleteConfirmText,
                ConfirmDialogState.Create => _saveLoadMenuConfig.CreateNewGameConfirmText,
                _ => "",
            };
        }
        #endregion
    }

    public enum ConfirmDialogState
    {
        None,
        Save,
        Load,
        Delete,
        Create,
    }
}