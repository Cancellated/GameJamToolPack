using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MyGame.Data;
using MyGame.Managers;
using MyGame.UI.SaveLoad.View;

namespace MyGame.UI.SaveLoad.View
{
    /// <summary>
    /// 存档菜单面板类，继承自SaveLoadMenuView
    /// 实现具体的存档菜单UI显示逻辑
    /// </summary>
    public class SaveLoadMenuPanel : SaveLoadMenuView
    {
        [Header("自定义UI组件")]
        [SerializeField]
        private TextMeshProUGUI _menuTitleText;
        
        [SerializeField]
        private TextMeshProUGUI _selectedSlotInfoText;
        
        /// <summary>
        /// 初始化面板
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            
            // 设置初始标题
            if (_menuTitleText != null)
            {
                _menuTitleText.text = "选择存档";
            }
            
            // 初始化选中信息文本
            if (_selectedSlotInfoText != null)
            {
                _selectedSlotInfoText.text = "请选择一个存档";
            }
        }
        
        /// <summary>
        /// 更新视图显示
        /// </summary>
        public override void UpdateView()
        {
            base.UpdateView();
            
            // 更新选中存档信息显示
            UpdateSelectedSlotInfo();
        }
        
        /// <summary>
        /// 更新选中存档信息显示
        /// 通过 Controller 查询方法获取数据，不直接访问Model
        /// </summary>
        protected override void UpdateSelectedSlotInfo()
        {
            base.UpdateSelectedSlotInfo();

            if (_selectedSlotInfoText == null || m_controller == null)
                return;

            string selectedSlotName = m_controller.GetSelectedSaveSlotName();
            if (string.IsNullOrEmpty(selectedSlotName))
            {
                _selectedSlotInfoText.text = "请选择一个存档";
                return;
            }

            // 查找选中的存档信息
            SaveSlotInfo selectedSlotInfo = null;
            var saveSlots = m_controller.GetSaveSlots();
            if (saveSlots != null)
            {
                foreach (var slotInfo in saveSlots)
                {
                    if (slotInfo.SlotName == selectedSlotName)
                    {
                        selectedSlotInfo = slotInfo;
                        break;
                    }
                }
            }

            // 显示选中的存档信息
            if (selectedSlotInfo != null && selectedSlotInfo.SaveData != null)
            {
                string info = $"选中存档：{selectedSlotInfo.DisplayName}\n";
                info += $"存档时间：{selectedSlotInfo.LastModified}\n";
                info += $"游戏版本：{selectedSlotInfo.Version}\n";

                // 添加玩法进度摘要（由玩法存档提供者生成的纯文本，核心只透传）
                if (!string.IsNullOrEmpty(selectedSlotInfo.SaveData.progressSummary))
                {
                    info += $"进度：{selectedSlotInfo.SaveData.progressSummary}\n";
                }

                _selectedSlotInfoText.text = info;
            }
            else
            {
                _selectedSlotInfoText.text = $"选中存档：{selectedSlotName}\n(空存档槽)";
            }
        }
        
        /// <summary>
        /// 显示面板
        /// </summary>
        public override void Show()
        {
            // UIManager 直接调用 panel.Show()，不会经过 Controller.Show()；
            // 这里在显示前刷新槽位与选中数据，保证打开菜单时列表是最新的
            if (m_controller != null)
            {
                m_controller.RefreshSaveSlots();
            }

            // 保证 UI 输入图（Esc/Cancel）启用：
            // 正常路径由 UIManager 根据 UIConfig 切换，这里兜底处理直接 Show 或配置缺失的情况
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SwitchToUIMode();
            }

            // 每次显示前幂等重绑按钮并订阅 Cancel，防止 Hide→Show 后交互失效
            EnsureInteractionBindings();

            // 主菜单保持可见作为背景，只关闭其交互，避免界面切换出现空白
            SetMainMenuInteractable(false);

            base.Show();

            // 隐藏存档选项菜单与确认弹窗
            HideSaveOptionsMenu();
            HideConfirmationDialog();
        }
        
        /// <summary>
        /// 隐藏面板
        /// </summary>
        public override void Hide()
        {
            // 关闭存档菜单时先恢复主菜单交互，主菜单全程未隐藏，因此没有空白真空期
            SetMainMenuInteractable(true);

            base.Hide();
            
            // 隐藏存档选项菜单与确认弹窗
            HideSaveOptionsMenu();
            HideConfirmationDialog();
        }
    }
}