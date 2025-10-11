using MyGame.Data;

namespace MyGame.UI.SaveLoad.View.Components
{
    /// <summary>
    /// 存档槽菜单处理器接口
    /// 定义存档槽与二级菜单和确认面板交互的方法
    /// </summary>
    public interface ISaveSlotMenuHandler
    {
        /// <summary>
        /// 显示存档槽对应的二级菜单
        /// </summary>
        /// <param name="slotInfo">存档槽信息</param>
        void ShowSaveOptionsMenu(SaveSlotInfo slotInfo);
        
        /// <summary>
        /// 隐藏存档槽对应的二级菜单
        /// </summary>
        void HideSaveOptionsMenu();
        
        /// <summary>
        /// 显示确认面板
        /// </summary>
        /// <param name="actionType">确认操作类型</param>
        /// <param name="slotName">存档槽名称</param>
        void ShowConfirmPanel(ConfirmType actionType, string slotName);
        
        /// <summary>
        /// 隐藏确认面板
        /// </summary>
        void HideConfirmPanel();
        
        /// <summary>
        /// 处理保存游戏操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        void HandleSaveGame(string slotName);
        
        /// <summary>
        /// 处理加载游戏操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        void HandleLoadGame(string slotName);
        
        /// <summary>
        /// 处理删除存档操作
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        void HandleDeleteSave(string slotName);
    }
}