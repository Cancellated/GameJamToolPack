using System;

namespace MyGame.UI.SaveLoad.View.Components
{
    /// <summary>
    /// 确认面板类型枚举
    /// 定义各种需要用户确认的操作类型
    /// </summary>
    public enum ConfirmType
    {
        /// <summary>
        /// 删除存档确认
        /// </summary>
        Delete,
        
        /// <summary>
        /// 覆盖存档确认
        /// </summary>
        Overwrite,

        /// <summary>
        /// 加载存档确认
        /// </summary>
        Load,
        
        /// <summary>
        /// 退出未保存游戏确认
        /// </summary>
        ExitWithoutSaving,
    }
}