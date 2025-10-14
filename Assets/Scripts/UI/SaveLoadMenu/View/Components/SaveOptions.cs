using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MyGame.Data;
using MyGame.UI.SaveLoad.Controller;
using MyGame.UI.SaveLoad;
using UnityEditor.EditorTools;

namespace MyGame.UI.SaveLoadMenu.View
{
    /// <summary>
    /// 保存选项菜单组件
    /// 负责处理存档操作的二级菜单UI和用户交互
    /// </summary>
    public class SaveOptions : MonoBehaviour
    {
        #region 组件引用
        [Tooltip("操作菜单面板")]
        [SerializeField] protected GameObject saveOptionsPanel;
        
        [Tooltip("保存按钮")]
        [SerializeField] protected Button saveButton;
        
        [Tooltip("加载按钮")]
        [SerializeField] protected Button loadButton;
        
        [Tooltip("删除按钮")]
        [SerializeField] protected Button deleteButton;
        [Tooltip("面板的canvas group")]
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Tooltip("确认对话框组件")]
        [SerializeField] private ConfirmDialog confirmDialog;
        #endregion
        
        /// <summary>
        /// 存档菜单控制器引用
        /// </summary>
        protected SaveLoadMenuController m_controller;
        
        /// <summary>
        /// 当前选中的存档槽信息
        /// </summary>
        protected SaveSlotInfo currentSelectedSlotInfo;
        
        #region 生命周期
        /// <summary>
        /// 初始化组件
        /// </summary>
        protected virtual void Awake()
        {
            RegisterEvents();
        }
        
        /// <summary>
        /// 销毁组件
        /// </summary>
        protected virtual void OnDestroy()
        {
            UnregisterEvents();
        }
        
        /// <summary>
        /// 设置控制器引用
        /// </summary>
        /// <param name="controller">存档菜单控制器</param>
        public virtual void SetController(SaveLoadMenuController controller)
        {
            m_controller = controller;
        }
        
        /// <summary>
        /// 注册按钮事件
        /// </summary>
        protected virtual void RegisterEvents()
        {
            // 绑定二级菜单按钮事件
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(HandleSaveButtonClick);
            }
            
            if (loadButton != null)
            {
                loadButton.onClick.AddListener(HandleLoadButtonClick);
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(HandleDeleteButtonClick);
            }
        }
        
        /// <summary>
        /// 注销按钮事件
        /// </summary>
        protected virtual void UnregisterEvents()
        {
            // 解绑二级菜单按钮事件
            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(HandleSaveButtonClick);
            }
            
            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(HandleLoadButtonClick);
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(HandleDeleteButtonClick);
            }
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 处理保存按钮点击事件
        /// </summary>
        public virtual void HandleSaveButtonClick()
        {
            if (m_controller != null && currentSelectedSlotInfo != null)
            {
                // 显示保存确认面板
                if (confirmDialog != null)
                {
                    confirmDialog.ShowConfirmPanel(ConfirmActionType.Save, currentSelectedSlotInfo.SlotName);
                }
            }
        }
        
        /// <summary>
        /// 处理加载按钮点击事件
        /// </summary>
        public virtual void HandleLoadButtonClick()
        {
            if (m_controller != null && currentSelectedSlotInfo != null)
            {
                // 显示加载确认面板
                if (confirmDialog != null)
                {
                    confirmDialog.ShowConfirmPanel(ConfirmActionType.Load, currentSelectedSlotInfo.SlotName);
                }
            }
        }
        
        /// <summary>
        /// 处理删除按钮点击事件
        /// </summary>
        public virtual void HandleDeleteButtonClick()
        {
            if (m_controller != null && currentSelectedSlotInfo != null)
            {
                // 调用确认删除方法显示确认面板
                ConfirmDeleteSave(currentSelectedSlotInfo.SlotName);
            }
        }
        #endregion

        
        /// <summary>
        /// 确认删除存档
        /// 显示删除确认面板
        /// </summary>
        /// <param name="slotName">要删除的存档槽名称</param>
        protected virtual void ConfirmDeleteSave(string slotName)
        {
            // 显示删除确认面板
            if (confirmDialog != null && m_controller != null && m_controller.Config != null)
            {
                confirmDialog.ShowConfirmPanel(ConfirmActionType.Delete, slotName);
            }
        }
        
        /// <summary>
        /// 更新保存选项菜单的按钮状态
        /// 根据存档类型决定按钮的可交互性
        /// </summary>
        protected virtual void UpdateSaveOptionsButtonStates()
        {
            bool hasSaveData = currentSelectedSlotInfo != null && currentSelectedSlotInfo.SaveData != null;
            bool isAutoSave = currentSelectedSlotInfo != null && currentSelectedSlotInfo.IsAutoSave;
            
            if (saveButton != null)
            {
                // 空存档或非空手动存档支持保存
                saveButton.interactable = !hasSaveData || (hasSaveData && !isAutoSave);
            }
            
            if (loadButton != null)
            {
                // 非空存档支持加载
                loadButton.interactable = hasSaveData;
            }
            
            if (deleteButton != null)
            {
                // 非空手动存档支持删除
                deleteButton.interactable = hasSaveData && !isAutoSave;
            }
        }
        
        /// <summary>
        /// 显示保存选项菜单（更新存档信息和按钮状态）
        /// </summary>
        /// <param name="slotInfo">存档槽信息</param>
        public virtual void ShowSaveOptionsMenu(SaveSlotInfo slotInfo)
        {
            currentSelectedSlotInfo = slotInfo;
            
            // 更新按钮状态
            UpdateSaveOptionsButtonStates();

        }
    }
}
