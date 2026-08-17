using UnityEngine;

namespace MyGame.UI.SaveLoad
{
    /// <summary>
    /// 存档菜单配置类
    /// 用于定义存档菜单的各种配置项。
    /// 注意：自动存档开关与间隔已移至 SaveManager（存档属于数据层职责），
    /// 本配置仅保留存档菜单 UI 实际使用的字段。
    /// </summary>
    [CreateAssetMenu(fileName = "SaveLoadMenuConfig", menuName = "UI/SaveLoadMenuConfig")]
    public class SaveLoadMenuConfig : ScriptableObject
    {
        [Header("存档配置")]
        [Tooltip("最大手动存档数量")]
        public int MaxManualSaveCount = 10;

        [Header("UI配置")]
        [Tooltip("存档槽预制件的Addressable Address")]
        public string SaveSlotPrefabAddress = "SaveSlotPrefab";
    }
}
