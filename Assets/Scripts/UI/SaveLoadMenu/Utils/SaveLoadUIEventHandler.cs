using UnityEngine.UI;
using System;
using MyGame.Data;
using Logger;
using UnityEngine;
using MyGame.UI.SaveLoad.View;
using TMPro;

namespace MyGame.UI.SaveLoad.Utils
{
    /// <summary>
    /// 存档加载UI事件处理器
    /// 负责处理UI事件的绑定和响应
    /// </summary>
    public class SaveLoadUIEventHandler
    {
        private const string LOG_MODULE = "SaveLoadUIEventHandler";
        private Button _backButton;
        private Button _saveButton;
        private Button _loadButton;
        private Button _deleteButton;
        private Button _confirmButton;
        private Button _cancelButton;

        private Action _onBackButtonClicked;
        private Action<SaveSlotInfo> _onSaveButtonClicked;
        private Action<SaveSlotInfo> _onLoadButtonClicked;
        private Action<SaveSlotInfo> _onDeleteButtonClicked;
        private Action _onConfirmButtonClicked;
        private Action _onCancelButtonClicked;

        /// <summary>
        /// 初始化事件处理器
        /// </summary>
        /// <param name="backButton">返回按钮</param>
        /// <param name="saveButton">保存按钮</param>
        /// <param name="loadButton">加载按钮</param>
        /// <param name="deleteButton">删除按钮</param>
        /// <param name="confirmButton">确认按钮</param>
        /// <param name="cancelButton">取消按钮</param>
        public void Initialize(
            Button backButton, 
            Button saveButton, 
            Button loadButton, 
            Button deleteButton, 
            Button confirmButton, 
            Button cancelButton)
        {
            _backButton = backButton;
            _saveButton = saveButton;
            _loadButton = loadButton;
            _deleteButton = deleteButton;
            _confirmButton = confirmButton;
            _cancelButton = cancelButton;
        }

        /// <summary>
        /// 绑定事件处理函数
        /// </summary>
        public void BindEvents()
        {
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(() => _onBackButtonClicked?.Invoke());
            }

            if (_saveButton != null)
            {
                _saveButton.onClick.AddListener(() => HandleActionButtonClick(_onSaveButtonClicked));
            }

            if (_loadButton != null)
            {
                _loadButton.onClick.AddListener(() => HandleActionButtonClick(_onLoadButtonClicked));
            }

            if (_deleteButton != null)
            {
                _deleteButton.onClick.AddListener(() => HandleActionButtonClick(_onDeleteButtonClicked));
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(() => _onConfirmButtonClicked?.Invoke());
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(() => _onCancelButtonClicked?.Invoke());
            }
        }

        /// <summary>
        /// 解绑所有事件处理函数
        /// </summary>
        public void UnbindEvents()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
            }

            if (_saveButton != null)
            {
                _saveButton.onClick.RemoveAllListeners();
            }

            if (_loadButton != null)
            {
                _loadButton.onClick.RemoveAllListeners();
            }

            if (_deleteButton != null)
            {
                _deleteButton.onClick.RemoveAllListeners();
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
            }
        }

        /// <summary>
        /// 处理操作按钮点击
        /// </summary>
        /// <param name="action">要执行的操作</param>
        private void HandleActionButtonClick(Action<SaveSlotInfo> action)
        {
            try
            {
                action?.Invoke(_currentSelectedSlotInfo);
            }
            catch (Exception e)
            {
                Log.Error(LOG_MODULE, "处理操作按钮点击时出错: " + e.Message);
            }
        }

        // 当前选中的存档槽信息
        private SaveSlotInfo _currentSelectedSlotInfo;

        /// <summary>
        /// 设置当前选中的存档槽信息
        /// </summary>
        /// <param name="slotInfo">存档槽信息</param>
        public void SetCurrentSelectedSlotInfo(SaveSlotInfo slotInfo)
        {
            _currentSelectedSlotInfo = slotInfo;
        }

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        /// <param name="isSlotSelected">是否有选中的存档槽</param>
        /// <param name="hasSaveData">选中的存档槽是否有存档数据</param>
        public void UpdateButtonStates(bool isSlotSelected, bool hasSaveData)
        {
            if (_saveButton != null)
            {
                _saveButton.interactable = isSlotSelected;
            }

            if (_loadButton != null)
            {
                _loadButton.interactable = isSlotSelected && hasSaveData;
            }

            if (_deleteButton != null)
            {
                _deleteButton.interactable = isSlotSelected && hasSaveData;
            }
        }

        /// <summary>
        /// 注册返回按钮点击事件
        /// </summary>
        /// <param name="action">事件处理函数</param>
        public void RegisterBackButtonCallback(Action action)
        {
            _onBackButtonClicked = action;
        }

        /// <summary>
        /// 注册保存按钮点击事件
        /// </summary>
        /// <param name="action">事件处理函数</param>
        public void RegisterSaveButtonCallback(Action<SaveSlotInfo> action)
        {
            _onSaveButtonClicked = action;
        }

        /// <summary>
        /// 注册加载按钮点击事件
        /// </summary>
        /// <param name="action">事件处理函数</param>
        public void RegisterLoadButtonCallback(Action<SaveSlotInfo> action)
        {
            _onLoadButtonClicked = action;
        }

        /// <summary>
        /// 注册删除按钮点击事件
        /// </summary>
        /// <param name="action">事件处理函数</param>
        public void RegisterDeleteButtonCallback(Action<SaveSlotInfo> action)
        {
            _onDeleteButtonClicked = action;
        }

        /// <summary>
        /// 注册确认按钮点击事件
        /// </summary>
        /// <param name="action">事件处理函数</param>
        public void RegisterConfirmButtonCallback(Action action)
        {
            _onConfirmButtonClicked = action;
        }

        /// <summary>
        /// 注册取消按钮点击事件
        /// </summary>
        /// <param name="action">事件处理函数</param>
        public void RegisterCancelButtonCallback(Action action)
        {
            _onCancelButtonClicked = action;
        }
    }
}