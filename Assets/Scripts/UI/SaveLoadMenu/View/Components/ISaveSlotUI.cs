using MyGame.Data;
using UnityEngine;
using MyGame.UI.SaveLoad.Model;

namespace MyGame.UI.SaveLoad.View
{
    /// <summary>
    /// 存档槽UI接口
    /// 定义存档槽UI应具备的基本功能
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

    }
}