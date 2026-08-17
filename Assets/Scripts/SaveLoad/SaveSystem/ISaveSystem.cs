using System;
using System.Collections.Generic;

namespace MyGame.Data
{
    /// <summary>
    /// 存档系统接口，定义游戏数据的保存和加载操作。
    /// 遵循单一职责原则，为不同存储实现提供统一接口。
    /// 仅提供同步接口：内部实现涉及 JsonUtility / Application 等 Unity API，
    /// 只能在主线程调用，后台线程异步包装是非法用法，故不在此提供异步版本。
    /// </summary>
    public interface ISaveSystem
    {
        /// <summary>
        /// 保存游戏数据到指定路径。
        /// </summary>
        /// <param name="saveData">要保存的游戏数据对象。</param>
        /// <param name="slotName">存档槽名称，用于区分不同存档。</param>
        /// <returns>保存操作是否成功。</returns>
        bool SaveGame(SaveData saveData, string slotName);
        
        /// <summary>
        /// 从指定路径加载游戏数据。
        /// </summary>
        /// <param name="slotName">存档槽名称，用于指定要加载的存档。</param>
        /// <returns>加载的游戏数据对象，如果加载失败则返回null。</returns>
        SaveData LoadGame(string slotName);
        
        /// <summary>
        /// 删除指定存档槽的游戏数据。
        /// </summary>
        /// <param name="slotName">要删除的存档槽名称。</param>
        /// <returns>删除操作是否成功。</returns>
        bool DeleteGame(string slotName);
        
        /// <summary>
        /// 检查指定存档槽是否存在游戏数据。
        /// </summary>
        /// <param name="slotName">要检查的存档槽名称。</param>
        /// <returns>存档是否存在。</returns>
        bool DoesSaveExist(string slotName);

        /// <summary>
        /// 获取指定存档槽文件的最后写入时间（UTC）；存档不存在时返回 DateTime.MinValue。
        /// 仅做文件状态查询（廉价），供 UI 层判断存档数据缓存是否失效。
        /// </summary>
        /// <param name="slotName">存档槽名称。</param>
        DateTime GetSaveLastWriteTime(string slotName);
        
        /// <summary>
        /// 获取所有可用的存档槽信息。
        /// </summary>
        /// <returns>存档槽名称列表。</returns>
        List<string> GetAvailableSaves();
    }
}