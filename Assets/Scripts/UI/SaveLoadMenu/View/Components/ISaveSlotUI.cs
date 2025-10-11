using MyGame.Data;
using UnityEngine;
using MyGame.UI.SaveLoad.View.Components;

namespace MyGame.UI.SaveLoad.View
{
    /// <summary>
    /// 存档槽UI接口
    /// 定义存档槽UI应具备的基本功能和菜单控制能力
    /// </summary>
    public interface ISaveSlotUI
    {
        /// <summary>
        /// 初始化存档槽UI
        /// </summary>
        /// <param name="slotInfo">存档槽信息</param>
        /// <param name="view">视图引用</param>
        void Initialize(SaveSlotInfo slotInfo, SaveLoadMenuView view);
        
        /// <summary>
        /// 更新存档槽显示内容
        /// </summary>
        void UpdateDisplay();
        
        /// <summary>
        /// 设置存档槽选中状态
        /// </summary>
        /// <param name="selected">是否选中</param>
        void SetSelected(bool selected);
        
        /// <summary>
        /// 获取存档槽名称
        /// </summary>
        string SlotName { get; }
        
        /// <summary>
        /// 显示存档槽关联的二级菜单
        /// </summary>
        void ShowOptionsMenu();
        
        /// <summary>
        /// 隐藏存档槽关联的二级菜单
        /// </summary>
        void HideOptionsMenu();
        
        /// <summary>
        /// 显示确认面板
        /// </summary>
        /// <param name="confirmType">确认类型</param>
        /// <param name="callback">确认回调</param>
        void ShowConfirmPanel(ConfirmType confirmType, System.Action<bool> callback = null);
        
        /// <summary>
        /// 隐藏确认面板
        /// </summary>
        void HideConfirmPanel();
        
        /// <summary>
        /// 检查存档槽是否可以进行交互操作
        /// </summary>
        /// <returns>是否可以交互</returns>
        bool CanInteract();
    }
}